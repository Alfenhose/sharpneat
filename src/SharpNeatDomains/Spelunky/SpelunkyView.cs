/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2016 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software; you can redistribute it and/or modify
 * it under the terms of The MIT License (MIT).
 *
 * You should have received a copy of the MIT License
 * along with SharpNEAT; if not, see https://opensource.org/licenses/MIT.
 */
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using SharpNeat.Core;
using SharpNeat.Domains.BoxesVisualDiscrimination;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System.Threading;

namespace SharpNeat.Domains.Spelunky
{
    /// <summary>
    /// View for the prey capture task.
    /// </summary>
    partial class SpelunkyView : AbstractDomainView
    {
        // View painting consts & objects.
        const int GridTop = 2;
        const int GridLeft = 2;
        const PixelFormat ViewportPixelFormat = PixelFormat.Format16bppRgb565;
        static readonly Pen __penGrey = new Pen(Color.LightGray, 1F);
        readonly Brush _brushBackground = new SolidBrush(Color.Gray);
        readonly Brush _brushWall = new SolidBrush(Color.Chocolate);
        readonly Brush _brushAir = new SolidBrush(Color.Black);//good browns: SaddleBrown, Chocolate
        readonly Brush _brushStart = new SolidBrush(Color.Blue);
        readonly Brush _brushEnd = new SolidBrush(Color.Red);

        IGenomeDecoder<NeatGenome,IBlackBox> _genomeDecoder;
        SpelunkyGenerator _world;
        /// <summary>
        /// The agent used by the simulation thread.
        /// </summary>
        IBlackBox _agent;
        Image _image;
        bool _initializing = true;
        /// <summary>
        /// Thread for running simulation.
        /// </summary>
        Thread _simThread;
        /// <summary>
        /// Indicates is a simulation is running. Access is thread synchronised using Interlocked.
        /// </summary>
        int _simRunningFlag = 0;
        /// <summary>
        /// Event that signals simulation thread to start a simulation.
        /// </summary>
        AutoResetEvent _simStartEvent = new AutoResetEvent(false);

        #region Constructor

        /// <summary>
        /// Construct the view with an appropriately configured world and a genome decoder for decoding genomes as they are passed into RefreshView().
        /// </summary>
        public SpelunkyView(IGenomeDecoder<NeatGenome,IBlackBox> genomeDecoder, SpelunkyGenerator world)
        {
            try
            {
                InitializeComponent();

                _genomeDecoder = genomeDecoder;
                _world = world;
                
                // Create a bitmap for the picturebox.
                int width = Width;
                int height = Height;
                _image = new Bitmap(width, height, ViewportPixelFormat);           
                pbx.Image = _image;

                // Create background thread for running simulation alongside NEAT algorithm.
                _simThread = new Thread(new ThreadStart(SimulationThread));
                _simThread.IsBackground = true;
                _simThread.Start();
            }
            finally
            {
                _initializing = false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refresh/update the view with the provided genome.
        /// </summary>
        public override void RefreshView(object genome)
        {
            // Zero indicates that the simulation is not currently running.
            if(0 == Interlocked.Exchange(ref _simRunningFlag, 1))
            {
                // We got the lock. Decode the genome and store result in an instance field.
                NeatGenome neatGenome = genome as NeatGenome;
                _agent = _genomeDecoder.Decode(neatGenome);

                // Signal simulation thread to start running a simulation.
                _simStartEvent.Set();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Simulate prey capture until thread is terminated.
        /// </summary>
        private void SimulationThread()
        {
            try
            {
                // Wait for first agent to be passed in.
                _simStartEvent.WaitOne();
                for(;;)
                {
                    
                    try
                    {
                        RunTrial();
                    }
                    finally
                    {   // Simulation completed. Reset _simRunningFlag to allow another simulation to be started.
                        Interlocked.Exchange(ref _simRunningFlag, 0);
                    }
                }
            }
            catch(ThreadAbortException)
            {   // Thread abort exceptions are expected.
            }
        }

        /// <summary>
        /// Run a single prey capture trial.
        /// </summary>
        private void RunTrial()
        {
            // Get local copy of agent so that the same agent is used throughout each individual simulation trial/run 
            // (_agent is being continually updated by the evolution algorithm update events). This is probably an atomic
            // operation and thus thread safe.
            IBlackBox agent = _agent;

            // world.
            _world.GenerateWorld();

            // Repaint view on GUI thread.
            Invoke(new MethodInvoker(delegate ()
            {
                PaintView();
            }));

            Thread.Sleep(1000);

            // Clear any prior agent state.
            agent.ResetState();

            // Let the chase begin!
            bool exit = false;
            for(int t = 0; t < _world.Steps; t++)
            {
                _world.ShapeTheWorld(agent);

                // Repaint view on GUI thread.
                Invoke(new MethodInvoker(delegate() 
                    {
                        PaintView();
                    }));

                // Sleep. Even if the sim is about to exit - that way we see the end result for a moment.
                Thread.Sleep(100);
                if(exit) {
                    break;
                }
            }
            string path = Environment.CurrentDirectory + "\\generated.lvl";
            StreamWriter fileOut = new StreamWriter(path);
            Save(agent, fileOut);
            fileOut.Close();
            /*
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.DefaultExt = ".lvl";
            dialog.FileName = "generated.lvl";
            dialog.Title = "Save generated Level";
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                Save(agent, dialog.OpenFile());
            }
            */
            Thread.Sleep(2000);
        }

        private void PaintView()
        {
            if(_initializing) {
                return;
            }

            Graphics g = Graphics.FromImage(_image);
            g.FillRectangle(_brushBackground, 0, 0, _image.Width, _image.Height);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Get control width and height.
            int width = Width;
            int height = Height;

            // Determine smallest dimension. Use that as the edge length of the square grid.
            //width = height = Math.Min(width, height);
            int gridSize = Math.Min(height / _world.GridHeight, width / _world.GridWidth);

            // Pixel size is calculated using integer division to produce cleaner lines when drawing.
            // The inherent rounding down may produce a grid 1 pixel smaller then the determined edge length.
            // Also make room for a button above the grid (next test case button).
            int visualFieldPixelSize = Math.Min((height-GridTop)/_world.GridHeight,gridSize);
            width = visualFieldPixelSize * _world.GridWidth;
            height = visualFieldPixelSize * _world.GridHeight;

            // Paint pixel outline grid.
            // Vertical lines.
            int xg = GridLeft;
            for(int i=0; i<=_world.GridWidth; i++, xg += visualFieldPixelSize) {
                g.DrawLine(__penGrey, xg, GridTop, xg, GridTop+height);
            }

            // Horizontal lines.
            int yg = GridTop;
            for(int i=0; i<=_world.GridHeight; i++, yg += visualFieldPixelSize) {
                g.DrawLine(__penGrey, GridLeft, yg, GridLeft+width, yg);
            }

            // Paint grid squares. Background color.
            Brush sensorBrush = _brushBackground;

            yg = GridTop;
            for(int y=0; y<_world.GridHeight; y++, yg += visualFieldPixelSize)
            {
                xg = GridLeft;
                for(int x=0; x<_world.GridWidth; x++, xg += visualFieldPixelSize)
                {
                    // Calc distance of square from agent.
                    switch (_world.GetWorldPoint(x, y))
                    {
                        case 0:
                            g.FillRectangle(_brushAir, xg + 1, yg + 1, visualFieldPixelSize - 2, visualFieldPixelSize - 2);
                            break;
                        case 1:
                            g.FillRectangle(_brushWall, xg + 1, yg + 1, visualFieldPixelSize - 2, visualFieldPixelSize - 2);
                            break;
                    }
                    
                }
            }
            Refresh();
        }

        #endregion

        #region Event Handlers

        private void pbx_SizeChanged(object sender, System.EventArgs e)
        {
            const float ImageSizeChangeDelta = 100f;

            if(_initializing) {
                return;
            }

            // Track viewport area.
            int width = Width;
            int height = Height;

            // If the viewport has grown beyond the size of the image then create a new image. 
            // Note. If the viewport shrinks we just paint on the existing (larger) image, this prevents unnecessary 
            // and expensive construction/destruction of Image objects.
            if(width > _image.Width || height > _image.Height) 
            {   // Reset the image's size. We round up the nearest __imageSizeChangeDelta. This prevents unnecessary 
                // and expensive construction/destruction of Image objects as the viewport is resized multiple times.
                int imageWidth = (int)(Math.Ceiling((float)width / ImageSizeChangeDelta) * ImageSizeChangeDelta);
                int imageHeight = (int)(Math.Ceiling((float)height / ImageSizeChangeDelta) * ImageSizeChangeDelta);
                _image = new Bitmap(imageWidth, imageHeight, ViewportPixelFormat);
                pbx.Image = _image;
            }
            
            // Repaint control.
            if(null != _world) {
                PaintView();
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            // Stop the simulation thread. Otherwise painting requests to the dead control will throw an exception.
            if(null != _simThread) {
                _simThread.Abort();
            }
            base.OnHandleDestroyed(e);
        }

        public void Save(IBlackBox agent, StreamWriter fileOut)//, string pathOut)
        {
            //_world = new SpelunkyGenerator(40, 32, 0.6, 2, 2);
            //FileStream temp = stream as FileStream; //File.Create(pathOut);
            //temp.Close();
            
            _world.SaveWorld(fileOut);
        }

        #endregion
    }
}
