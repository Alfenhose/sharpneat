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
            for (int i = 0; i < _trialsPerEvaluation; i++)
            {
                generator.GenerateWorld();
                // Run trials.
                for (int t = 0; t < _steps; t++)
                {
                    generator.ShapeTheWorld(box);
                }

                //total average
                {
                    double target = 0.30;
                    double weight = 4;
                    fitness += weight * ProximityToTarget(target, generator.Percentage);
                }

                //each row (avoid half empty rows)
                {
                    double target = 0.25;
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
                    double weight = 15;
                    int height = 5;
                    double rowPercent = (generator.IntegralWorld[_gridWidth - 1, height - 1]) / (double)(_gridWidth * height);
                    fitness += weight * SqrProximityToTarget(target, rowPercent) / (double)( _gridHeight - height + 1);
                    for (int y = 1; y < _gridHeight-height; y++)
                    {
                        rowPercent = (generator.IntegralWorld[_gridWidth - 1, y + height - 1]
                                    - generator.IntegralWorld[_gridWidth - 1, y - 1]) / (double)(_gridWidth * height);
                        fitness += weight * SqrProximityToTarget(target, rowPercent) / (double)(_gridHeight - height + 1);
                    }
                }

                //each column (avoid empty columns and full columns)
                {
                    double target = 0.3;
                    double weight = 8;
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

                    double density1 = generator.IntegralWorld[columns-1, _gridHeight - 1] / (double)(_gridHeight * columns);
                    double density2 = (generator.IntegralWorld[_gridWidth - 1, _gridHeight - 1]
                                     - generator.IntegralWorld[_gridWidth - columns - 1, _gridHeight - 1]) / (double)(_gridHeight * columns);
                    fitness += weight * SqrProximityToTarget(density1, density2);
                }
                //loners
                {
                    double target = 0;
                    double weight = 8;
                    double loners = Math.Min(generator.Loners / 100.0, 1);
                    fitness += weight * SqrProximityToTarget(target, loners);
                }
                //not full or empty
                {
                    int value = generator.IntegralWorld[_gridWidth - 1, _gridHeight - 1];
                    if (value > 0 && value < _gridWidth * _gridHeight) fitness += 100;
                }
                //no empty rows
                {
                    bool hasEmpty = false;
                    if ((generator.IntegralWorld[_gridWidth -1, 0]) == 0)
                    {
                        hasEmpty = true;
                    }
                    else for (int y = 1; y < _gridHeight; y++)
                    {
                        if ((generator.IntegralWorld[_gridWidth - 1, y] - generator.IntegralWorld[_gridWidth - 1, y - 1]) == 0)
                        {
                            hasEmpty = true;
                            break;
                        }
                    }
                    if (!hasEmpty)
                    {
                        fitness += 5;
                    }
                }
                //no empty columns
                {
                    bool hasEmpty = false;
                    if ((generator.IntegralWorld[0, _gridHeight - 1]) == 0)
                    {
                        hasEmpty = true;
                    }
                    else for (int x = 1; x < _gridWidth; x++)
                        {
                            if ((generator.IntegralWorld[x, _gridHeight - 1] - generator.IntegralWorld[x - 1, _gridHeight - 1]) == 0)
                            {
                                hasEmpty = true;
                                break;
                            }
                        }
                    if (!hasEmpty)
                    {
                        fitness += 10;
                    }
                }
                //no full rows
                {
                    bool hasFull = false;
                    if ((generator.IntegralWorld[_gridWidth - 1, 0]) == _gridWidth)
                    {
                        hasFull = true;
                    }
                    else for (int y = 1; y < _gridHeight; y++)
                        {
                            if ((generator.IntegralWorld[_gridWidth - 1, y] - generator.IntegralWorld[_gridWidth - 1, y - 1]) == _gridWidth)
                            {
                                hasFull = true;
                                break;
                            }
                        }
                    if (!hasFull)
                    {
                        fitness += 10;
                    }
                }
                //no full columns
                {
                    bool hasFull = false;
                    if ((generator.IntegralWorld[0, _gridHeight - 1]) == _gridHeight)
                    {
                        hasFull = true;
                    }
                    else for (int x = 1; x < _gridWidth; x++)
                        {
                            if ((generator.IntegralWorld[x, _gridHeight - 1] - generator.IntegralWorld[x - 1, _gridHeight - 1]) == _gridHeight)
                            {
                                hasFull = true;
                                break;
                            }
                        }
                    if (!hasFull)
                    {
                        fitness += 10;
                    }
                }

            }
            // Track number of evaluations and test stop condition.
            _evalCount++;
            if(false && fitness >= _trialsPerEvaluation) {
                _stopConditionSatisfied = true;
            }
            
            // return fitness score.
            return new FitnessInfo(fitness, fitness);
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
