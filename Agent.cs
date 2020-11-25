using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    public enum AgentPosition { Center=0, Farm=1, WorkShop=2}
    abstract class Agent
    {
        private AgentPosition agentPos;
        private double saturation;
        private int foodReserved;
        private List<int> ownedHarvest;
        private double saturationLostPerTime = 0.2;


        public Agent()
        {
            agentPos = AgentPosition.Center;
            ownedHarvest = new List<int>();
            saturation = 20;
        }

        public abstract Actions decide();

        public void updatePosition(AgentPosition agentPos)
        {
            this.agentPos = agentPos;
        }

        public AgentPosition GetAgentPosition()
        {
            return agentPos;
        }

        public void consumeFood()
        {
            if(foodReserved > 0)
            {
                saturation += 5;
                foodReserved--;
            } else
            {
                throw new NoFoodToEatException();
            }
        }

        public void loseSaturationByTime(double timeElapsed)
        {
            saturation -= saturationLostPerTime * timeElapsed;

            if(saturation < 0)
            {
                throw new AgentDiesException();
            }
        }

        public void getFood()
        {
            foodReserved++;
        }
    
        public List<int> getOwnedHarvest()
        {
            return ownedHarvest;
        }

        public void addPlantToHarvest(int plantID)
        {
            ownedHarvest.Add(plantID);
        }

        public void removePlant(int plantID)
        {
            ownedHarvest.Remove(plantID);
        }

        public double getFoodReserved()
        {
            return foodReserved;
        }

        public double getSaturation()
        {
            return saturation;
        }
    }

    class RandomAgent : Agent
    {
        private Random r;
        public RandomAgent()
        {
            r = new Random();
        }
        public override Actions decide()
        {
            return (Actions) r.Next(Actions.GetNames(typeof(Actions)).Length);
        }
    }

    class QLearningAgent : Agent
    {
        // amount of possible states
        private int states;

        // amount of possible actions
        private int actions;

        // q-values
        private double[][] qvalues;

        // exploration policy
        private ExplorationPolicy explorationPolicy;

        // discount factor
        private double discountFactor = 0.95;

        // learning rate
        private double learningRate = 0.25;

        private double stateReward;
        private int stateAction;

        public QLearningAgent(int statesNumber, ExplorationPolicy explorationPolicy, bool randomizeQvalues)
        {
            stateReward = 0;
            stateAction = 0;
            actions = 4;
            states = statesNumber;
            this.explorationPolicy = explorationPolicy;
            qvalues = new double[states][];
            for(int i = 0; i < states; i++)
            {
                qvalues[i] = new double[actions];
            }

            if (randomizeQvalues)
            {
                Random rand = new Random();

                for (int i = 0; i < states; i++)
                {
                    for (int j = 0; j < actions; j++)
                    {
                        qvalues[i][j] = rand.NextDouble() / 5;
                    }
                }
            }
        }

        /// <summary>
        /// Update Q-function's value for the previous state-action pair.
        /// </summary>
        /// 
        /// <param name="previousState">Previous state.</param>
        /// <param name="action">Action, which leads from previous to the next state.</param>
        /// <param name="nextState">Next state.</param>
        /// 
        public void updateState(int previousState, int action, int nextState)
        {
            // next state's action estimations
            double[] nextActionEstimations = qvalues[nextState];

            // find maximum expected summary reward from the next state
            double maxNextExpectedReward = nextActionEstimations[0];

            for (int i = 1; i < actions; i++)
            {
                if (nextActionEstimations[i] > maxNextExpectedReward)
                    maxNextExpectedReward = nextActionEstimations[i];
            }

            // update expexted summary reward of the previous state
            qvalues[previousState][action] *= (1.0 - learningRate);
            qvalues[previousState][action] += learningRate * (stateReward + discountFactor * maxNextExpectedReward);
        }
        
        public void addReward(double reward)
        {
            stateReward += reward;
        }
        
        public override Actions decide()
        {

            return Actions.Eat;
        }
    }



    

}
