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
using SharpNeat.Core;
using SharpNeat.Phenomes;

namespace SharpNeat.Domains.Spelunky
{
    /// <summary>
    /// Evaluator for the prey capture task.
    /// </summary>
    public class SpelunkyEvaluator : IPhenomeEvaluator<IBlackBox>
    {
        readonly int _trialsPerEvaluation;

        // World parameters.
        readonly int _gridWidth;
        readonly int _gridHeight;
        readonly double _initPercentage;    //how much of the inital world should be filled
        readonly int _mooreSize;
        readonly int _steps;            // how many steps should the generator perform bafore showing the result

        ulong _evalCount;
        bool _stopConditionSatisfied;

        #region Constructor

        /// <summary>
        /// Construct with the provided task parameter arguments.
        /// </summary>
        public SpelunkyEvaluator(int trialsPerEvaluation, int gridWidth, int gridHeight, double initPercentage, int mooreSize, int steps)
        {
            _trialsPerEvaluation = trialsPerEvaluation;
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            _initPercentage = initPercentage/100.0;
            _mooreSize = mooreSize;
            _steps = steps;
        }

        #endregion

        #region IPhenomeEvaluator<IBlackBox> Members

        /// <summary>
        /// Gets the total number of evaluations that have been performed.
        /// </summary>
        public ulong EvaluationCount
        {
            get { return _evalCount; }
        }

        /// <summary>
        /// Gets a value indicating whether some goal fitness has been achieved and that
        /// the evolutionary algorithm/search should stop. This property's value can remain false
        /// to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied
        {
            get { return _stopConditionSatisfied; }
        }

        /// <summary>
        /// Evaluate the provided IBlackBox against the XOR problem domain and return its fitness score.
        /// </summary>
        public FitnessInfo Evaluate(IBlackBox box)
        {
            // Create a generator.
            SpelunkyGenerator generator = new SpelunkyGenerator(_gridWidth, _gridHeight, _initPercentage, _mooreSize, _steps);

            // Perform multiple independent trials.
            double fitness = 0;
            double weight = 1.0 / _trialsPerEvaluation;
            for (int i = 0; i < _trialsPerEvaluation; i++)
            {
                generator.GenerateWorld();
                // Run trials.
                for (int t = 0; t < _steps; t++)
                {
                    generator.ShapeTheWorld(box);
                }
                /*for (int t = 0; t < 2; t++)
                {
                    generator.CalculateRooms();
                }
                generator.RemoveRooms();*/
                if (!NotFullOrEmpty(generator))
                {
                    continue;
                }
                fitness += 1 * weight * TotalAverage(generator);
                
                fitness += 1 * weight * FullOrEmptyRowsOrColumns(generator);

                //fitness += 1 * weight * Consistency(generator);
                //fitness += 1 * RowsAndColumns(generator);
                fitness += 1 * weight * HorizontalORVertical(generator);
                //fitness += 5 * Rooms(generator);

            }
            // Track number of evaluations and test stop condition.
            _evalCount++;
            if(false && fitness >= _trialsPerEvaluation) {
                _stopConditionSatisfied = true;
            }
            
            // return fitness score.
            return new FitnessInfo(fitness, fitness);
        }

        private double RowsAndColumns(SpelunkyGenerator generator)
        {
            double fitness = 0;
            //each row (avoid half empty rows)
            {
                double target = 0.5;
                double weight = 10;
                double rowPercent = (generator.IntegralWorld[_gridWidth - 1, 0]) / (double)(_gridWidth);
                fitness += weight * SqrDeviationFromTarget(target, rowPercent) / (double)(_gridHeight);
                for (int y = 1; y < _gridHeight; y++)
                {
                    rowPercent = (generator.IntegralWorld[_gridWidth - 1, y]
                                - generator.IntegralWorld[_gridWidth - 1, y - 1]) / (double)(_gridWidth);
                    fitness += weight * SqrDeviationFromTarget(target, rowPercent) / (double)(_gridHeight);
                }
            }
            //for every five rows there should be about 10% walls
            {
                double target = 0.2;
                double weight = 20;
                int height = 5;
                double rowPercent = (generator.IntegralWorld[_gridWidth - 1, height - 1]) / (double)(_gridWidth * height);
                fitness += weight * SqrProximityToTarget(target, rowPercent) / (double)(_gridHeight - height + 1);
                for (int y = 1; y < _gridHeight - height; y++)
                {
                    rowPercent = (generator.IntegralWorld[_gridWidth - 1, y + height - 1]
                                - generator.IntegralWorld[_gridWidth - 1, y - 1]) / (double)(_gridWidth * height);
                    fitness += weight * SqrProximityToTarget(target, rowPercent) / (double)(_gridHeight - height + 1);
                }
            }

            //each column (avoid empty columns and full columns)
            {
                double target = 0.5;
                double weight = 15;
                double columnPercent = (generator.IntegralWorld[0, _gridHeight - 1]) / (_gridHeight * 1.0);
                fitness += weight * SqrProximityToTarget(target, columnPercent) / _gridWidth;
                for (int x = 1; x < _gridWidth; x++)
                {
                    columnPercent = (generator.IntegralWorld[x, _gridHeight - 1]
                                - generator.IntegralWorld[x - 1, _gridHeight - 1]) / (_gridHeight * 1.0);
                    fitness += weight * SqrProximityToTarget(target, columnPercent) / _gridWidth;
                }
            }
            //The outermost columns (lets say 5) should have about the same density
            {
                double weight = 15;
                int columns = 5;

                double density1 = generator.IntegralWorld[columns - 1, _gridHeight - 1] / (double)(_gridHeight * columns);
                double density2 = (generator.IntegralWorld[_gridWidth - 1, _gridHeight - 1]
                                 - generator.IntegralWorld[_gridWidth - columns - 1, _gridHeight - 1]) / (double)(_gridHeight * columns);
                fitness += weight * SqrProximityToTarget(density1, density2);
            }
            //The top- and bottommost rows (lets say 5) should have about the same density
            {
                double weight = 15;
                int rows = 5;

                double density1 = generator.IntegralWorld[_gridWidth - 1, rows - 1] / (double)(_gridWidth * rows);
                double density2 = (generator.IntegralWorld[_gridWidth - 1, _gridHeight - 1]
                                 - generator.IntegralWorld[_gridWidth - 1, _gridHeight - rows - 1]) / (double)(_gridWidth * rows);
                fitness += weight * SqrProximityToTarget(density1, density2);
            }

            return fitness;
        }

        private bool NotFullOrEmpty(SpelunkyGenerator generator)
        {
            //not full or empty
            {
                int value = generator.IntegralWorld[_gridWidth - 1, _gridHeight - 1];
                if (value > 0 && value < _gridWidth * _gridHeight)
                    return true;
            }
            return false;
        }
        private double FullOrEmptyRowsOrColumns(SpelunkyGenerator generator)
        {
            double fitness = 0;
            double weight = 0.5;
            //rows
            {
                int value = generator.IntegralWorld[_gridWidth - 1, 0];
                if (value > 0 && value < _gridWidth)
                    fitness += 1 * weight / _gridHeight;
                for (int y = 1; y < _gridHeight; y++)
                {
                    value = generator.IntegralWorld[_gridWidth - 1, y] - generator.IntegralWorld[_gridWidth - 1, y - 1];
                    if (value > 0 && value < _gridWidth)
                        fitness += 1 * weight / _gridHeight;
                }
            }
            //columns
            {
                int value = generator.IntegralWorld[0, _gridHeight - 1];
                if (value > 0 && value < _gridHeight)
                    fitness += 1 * weight / _gridWidth;
                for (int x = 1; x < _gridWidth; x++)
                {
                    value = generator.IntegralWorld[x, _gridHeight - 1] - generator.IntegralWorld[x - 1, _gridHeight - 1];
                    if (value > 0 && value < _gridHeight)
                        fitness += 1 * weight / _gridWidth;
                }
            }
            return fitness;
        }

        private double Features(SpelunkyGenerator generator)
        {
            double fitness = 0;
            double weight = 0.1;
            fitness += weight * BlockProximity(5, generator.Loners);
            fitness += weight * BlockProximity(5, generator.Holes);
            fitness += weight * BlockProximity(130, generator.Solids);
            fitness += weight * BlockProximity(288, generator.Empties);
            fitness += weight * BlockProximity(50, generator.Tunnels);
            fitness += weight * BlockProximity(5, generator.Pits);
            fitness += weight * BlockProximity(60, generator.Platforms);
            fitness += weight * BlockProximity(8, generator.Spires);
            fitness += weight * BlockProximity(25, generator.Nooks);
            fitness += weight * BlockProximity(51, generator.Ends);
            return fitness;
        }
        private double Consistency(SpelunkyGenerator generator)
        {
            double fitness = 0;
            double weight = 0.5;
            //fitness += weight * 1 * BlockProximity(120, generator.Solids);
            //fitness += weight * 1 * BlockProximity(240, generator.Empties);

            double clamp = Math.Max(generator.Solids, generator.Empties);
            if (clamp > 0)
            {
                fitness += weight * 1 * (generator.Solids / clamp);
                fitness += weight * 1 * (generator.Empties / clamp);
            }
            return fitness;
        }
        private double HorizontalORVertical(SpelunkyGenerator generator)
        {
            double fitness = 0;
            double weight = 0.5;

            double clamp = Math.Max(generator.Horizontals, generator.Verticals);
            if (clamp > 0)
            {
                fitness += weight * 1 * (generator.Horizontals / clamp);
                fitness += weight * 1 * (generator.Verticals / clamp);
            }
            return fitness;
        }

        private double BlockProximity(double target, double value)
        {
            return Math.Min(Math.Max(SqrProximityToTarget(target/1280.0, value/1280.0), 0),1);
        }

        private double Rooms(SpelunkyGenerator generator)
        {
            double fitness = 0;
            //roomCount
            {
                double target = 1;
                double weight = 1;
                double roomCount = generator.Rooms.Count/24;
                fitness += weight * Math.Min(SqrProximityToTarget(target, roomCount),0);
            }
            return fitness;
        }

        private double TotalAverage(SpelunkyGenerator generator)
        {
            double fitness = 0;
            {
                double target = 0.50;
                double weight = 1;
                fitness += weight * SqrProximityToTarget(target, generator.Percentage);
            }
            return fitness;
        }

        /// <summary>
        /// Reset the internal state of the evaluation scheme if any exists.
        /// </summary>
        public void Reset()
        {   
        }
        /// <summary>
        /// Returns the absolute inverted distance from target value to deviant value
        /// </summary>
        private double ProximityToTarget(double target, double deviant)
        {
            return 1 - DeviationFromTarget(target,deviant);
        }
        /// <summary>
        /// Returns the square of the absolute inverted distance from target value to deviant value
        /// </summary>
        private double SqrProximityToTarget(double target, double deviant)
        {
            return Math.Pow(ProximityToTarget(target, deviant), 2);
        }

        /// <summary>
        /// Returns the absolute distance from target value to deviant value
        /// </summary>
        private double DeviationFromTarget(double target, double deviant)
        {
            return Math.Abs(target - deviant);
        }
        /// <summary>
        /// Returns the square of the absolute distance from target value to deviant value
        /// </summary>
        private double SqrDeviationFromTarget(double target, double deviant)
        {
            return Math.Pow(DeviationFromTarget(target, deviant), 2);
        }

        #endregion
    }
}
