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
using Redzen.Numerics;
using SharpNeat.Phenomes;

namespace SharpNeat.Domains.Spelunky
{
    /// <summary>
    /// The prey capture task's grid world. Encapsulates agent's sensor and motor hardware and the prey's simple stochastic movement.
    /// </summary>
    public class SpelunkyGenerator
    {
        #region Constants
        
        const int wall = 1;
        const int air = 0;

        #endregion

        #region Instance Fields

        // World parameters.
		
        readonly int _gridWidth;
        readonly int _gridHeight;
        readonly double _initPercentage;    //how much of the inital world should be filled
        readonly int _steps;            // how many steps should the generator perform before showing the result

        // World state.
        int[,] _world;
        IntPoint _startPos;
        IntPoint _endPos;
        
        // Random number generator.
        Random _rng;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs with the provided world parameter arguments.
        /// </summary>
        public SpelunkyGenerator(int gridWidth, int gridHeight, double initPercentage, int steps)
        {
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            _initPercentage = initPercentage;
            _steps = steps;
            _rng = new Random();
            _world = new int[_gridWidth, _gridHeight];
            randomizeWorld(_world);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the width of the grid in terms of number of squares.
        /// </summary>
        public int GridWidth
        {
            get { return _gridWidth; }
        }

        /// <summary>
        /// Gets the height of the grid in terms of number of squares.
        /// </summary>
        public int GridHeight
        {
            get { return _gridHeight; }
        }

        /// <summary>
        /// Gets the percentage of cells wich contain air
        /// </summary>
        public double InitPercentage
        {
            get { return _initPercentage; }
        }

        /// <summary>
        /// Gets the amount of steps being performed.
        /// </summary>
        public int Steps
        {
            get { return _steps; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates the world
        /// </summary>
        public void GenerateWorld(IBlackBox agent)
        {
            // Init world state.
            InitGenerator();
        }
        /// <summary>
        /// Returns how much of the world is blocks
        /// </summary>
        public double NowPercentage(IBlackBox agent)
        {
            double filled = 0;
            double total = 0;

            foreach (var i in _world)
            {
                total++;
                if (i == wall)
                { filled++; }
            }
            return filled / total;
        }

        /// <summary>
        /// Runs one trial of the provided agent in the world. Returns true if the agent captures the prey within
        /// the maximum number of timesteps allowed.
        /// </summary>
        public bool ShapeTheWorld(IBlackBox agent,int _steps)
        {
            
            int t = 0;
            IntPoint pos;
            // Let the chase begin!
            for(; t<_steps; t++)
            {
                for(int x = 0; x < GridWidth; x++)
                {
                    for (int y = 0; y < GridHeight; y++)
                    {
                        pos = new IntPoint(x, y);
                        SetAgentInputs(agent, pos);
                    }
                }
            }

            // Agent failed to capture prey in the alloted time.
            return false;
        }

        /// <summary>
        /// Initialise generator by filling the world with noise.
        /// </summary>
        private void InitGenerator()
        {
            randomizeWorld(_world);
        }

        /// <summary>
        /// Determine the agent's position in the world relative to the prey and walls, and set its sensor inputs accordingly.
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="pos"></param>
        public void SetAgentInputs(IBlackBox agent, IntPoint pos)
        {
            // Determine agent sensor input values.
            // Reset all inputs.
            agent.InputSignalArray.Reset();
            int x, y;
            int index = 0;
            int mooreSize = 2;
            for (int dx = -mooreSize; dx <= mooreSize; dx++)
            {
                for (int dy = -mooreSize; dy <= mooreSize; dy++)
                {
                    x = pos._x + dx;
                    y = pos._y + dy;
                    //check if coordinates are outside the bounds
                    if (x < 0 || y < 0 || x >= GridWidth || y >= GridHeight)
                    {
                        //if the position is outside the world we set the input to look like there is a wall
                        agent.InputSignalArray[index] = wall;
                    }
                    else
                    {
                        //if inside the bounds we set the input to what ever is in that spot
                        agent.InputSignalArray[index] = _world[x, y];
                    }
                    index++;
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculates minimum angle between two vectors (specified by angle only).
        /// </summary>
        private double CalcAngleDelta(double a, double b)
        {
            return Math.Min(Math.Abs(a-b), Math.Abs(b-a));
        }
        /// <summary>
        /// Fills the given 2d array with random integers 1 or 0.
        /// </summary>
        private void randomizeWorld(int[,] cells)
        {
            double randomNumber;
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    randomNumber = _rng.NextDouble();
                    if (randomNumber > InitPercentage)
                    {
                        cells[x, y] = air;
                    } else
                    {
                        cells[x, y] = wall;
                    }
                    
                }
            }
        }

        #endregion
    }
}
