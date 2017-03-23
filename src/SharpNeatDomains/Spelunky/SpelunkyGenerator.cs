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
        readonly int _mooreSize;
        readonly double _initPercentage;    //how much of the inital world should be filled
        readonly int _steps;            // how many steps should the generator perform before showing the result

        // World state.
        int[,] _world;
        IntPoint _startPos;
        IntPoint _endPos;

        //for the evaluator
        double percentage = -1;
        int[,] _integralWorld;
        int _loners;

        // Random number generator.
        Random _rng;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs with the provided world parameter arguments.
        /// </summary>
        public SpelunkyGenerator(int gridWidth, int gridHeight, double initPercentage, int mooreSize, int steps)
        {
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            _initPercentage = initPercentage;
            _mooreSize = mooreSize;
            _steps = steps;
            _rng = new Random();
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

        /// <summary>
        /// Gets the percentage of filled blocks.
        /// </summary>
        public double Percentage
        {
            get {
                if (percentage >= 0 ) {
                    return percentage;
                }
                percentage = ((float) (IntegralWorld[GridWidth-1,GridHeight-1])) / ((float) (GridWidth*GridHeight));
                return percentage;
            }
        }
        /// <summary>
        /// Gets the integral of the world.
        /// </summary>
        public int[,] IntegralWorld
        {
            get
            {
                if (_integralWorld != null)
                {
                    return _integralWorld;
                }
                _integralWorld = CalcIntegralWorld(World);
                return _integralWorld;
            }
            private set { _integralWorld = value; }
        }
        /// <summary>
        /// Counts Loners.
        /// </summary>
        public int Loners
        {
            get
            {
                if (_loners >= 0)
                {
                    return _loners;
                }
                _loners = CalcLoners(World);
                return _loners;
            }
        }
        /// <summary>
        /// Gets the integral of the world.
        /// </summary>
        public int[,] World
        {
            get
            {
                if (_world != null)
                {
                    return _world;
                }
                _world = new int[GridWidth, GridHeight];
                return _world;
            }
            private set { _world = value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates the world
        /// </summary>
        public void GenerateWorld(IBlackBox agent)
        {
            // Init world state.
            Reset();
            // make a random world by filling it with noise
            RandomizeWorld(World);
        }
        /// <summary>
        /// Get value at coordinates
        /// </summary>
        public int GetWorldPoint(int x, int y)
        {
            if (x < 0 || y < 0 || x >= GridWidth || y >= GridHeight)
            {
                return wall;
            }
            return World[x, y];
        }

        /// <summary>
        /// Runs one trial of the provided agent in the world. Returns true if the agent captures the prey within
        /// the maximum number of timesteps allowed.
        /// returns true if every result is between 0 and 1
        /// returns false if any result is less than 0 or greater than 1
        /// </summary>
        public bool ShapeTheWorld(IBlackBox agent)
        {
            bool consistent = true;
            var tempWorld = new int[_gridWidth, _gridHeight];
            // Let the chase begin!
            for(int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    // Reset all inputs.
                    agent.InputSignalArray.Reset();
                    //set inputs for the spot
                    SetAgentInputs(agent, new IntPoint(x, y));
                    //make the agent do the calculations
                    agent.Activate();

                    double value = agent.OutputSignalArray[0];
                    tempWorld[x, y] = (int)Math.Round(value);
                    if (value < 0 || value > 1) {consistent = false;}
                }
            }
            World = tempWorld;
            return consistent;
        }

        /// <summary>
        /// counts loners of world
        /// </summary>
        public int CalcLoners(int[,] world)
        {
            int loners = 0;
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    int temp = 0;
                    if (GetWorldPoint(x,y) == 1)
                    {
                        temp += GetWorldPoint(x - 1, y);
                        temp += GetWorldPoint(x + 1, y);
                        temp += GetWorldPoint(x, y - 1);
                        temp += GetWorldPoint(x, y + 1);
                        if (temp == 0) loners++;
                    }
                    if (GetWorldPoint(x, y) == 0)
                    {
                        temp += GetWorldPoint(x - 1, y);
                        temp += GetWorldPoint(x + 1, y);
                        temp += GetWorldPoint(x, y - 1);
                        temp += GetWorldPoint(x, y + 1);
                        if (temp == 4) loners++;
                    }
                }
            }
            return loners;
        }

        /// <summary>
        /// Determine the agent's position in the world relative to the prey and walls, and set its sensor inputs accordingly.
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="pos"></param>
        public void SetAgentInputs(IBlackBox agent, IntPoint pos)
        {
            // Determine agent sensor input values.
            int x, y;
            int index = 0;
            for (int dx = -_mooreSize; dx <= _mooreSize; dx++)
            {
                for (int dy = -_mooreSize; dy <= _mooreSize; dy++)
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
                        agent.InputSignalArray[index] = GetWorldPoint(x,y);
                    }
                    index++;
                }
            }
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Fills the given 2d array with random integers 1 or 0.
        /// </summary>
        private void RandomizeWorld(int[,] cells)
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
        /// <summary>
        ///I calculate the integral of the world for calculating the amount of walls at many regions much faster.
        /// </summary>
        private int[,] CalcIntegralWorld( int[,] world)
        {
            int x = 0, y = 0;
            int[,] integral = new int[GridWidth, GridHeight];
            //the corner is calculated first
            integral[x, y] = world[x, y];

            //then i calculate the entire first row
            y = 0;
            for (x = 1; x < GridWidth; x++)
            {
                integral[x, y] = world[x, y] + integral[x-1, y];
            }
            //and the entire first column
            x = 0;
            for (y = 1; y < GridHeight; y++)
            {
                integral[x, y] = world[x, y] + integral[x, y-1];
            }
            //now the rest can be calculated more easily.
            for (x = 1; x < GridWidth; x++)
            {
                for (y = 1; y < GridHeight; y++)
                {
                    integral[x, y] = world[x, y] + integral[x - 1, y] + integral[x, y - 1] - integral[x-1,y-1];
                }
            }
            return integral;
        }

        /// <summary>
        /// Reset the generator
        /// </summary>
        private void Reset()
        {
            World = null;
            percentage = -1;
            IntegralWorld = null;
            _loners = -1;
        }
        #endregion
    }
}
