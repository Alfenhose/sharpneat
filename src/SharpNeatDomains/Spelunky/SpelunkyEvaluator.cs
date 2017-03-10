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
        readonly int _steps;            // how many steps should the generator perform bafore showing the result

        ulong _evalCount;
        bool _stopConditionSatisfied;

        #region Constructor

        /// <summary>
        /// Construct with the provided task parameter arguments.
        /// </summary>
        public SpelunkyEvaluator(int trialsPerEvaluation, int gridWidth, int gridHeight, double initPercentage, int steps)
        {
            _trialsPerEvaluation = trialsPerEvaluation;
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            _initPercentage = initPercentage;
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
            SpelunkyGenerator generator = new SpelunkyGenerator(_gridWidth, _gridHeight, _initPercentage, _steps);

            // Perform multiple independent trials.
            int fitness = 0;
            for(int i=0; i<_trialsPerEvaluation; i++)
            {
                // Run trials.
                generator.GenerateWorld(box);
                fitness += generator.PercentFilled();
            }

            // Track number of evaluations and test stop condition.
            _evalCount++;
            if(fitness == _trialsPerEvaluation) {
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

        #endregion
    }
}
