using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    abstract class CooperativeAgents
    {
        public Dictionary<int, double> stateReward;
        protected Dictionary<int, int> stateAction;
        protected Dictionary<int, int> currentState;

        public CooperativeAgents()
        {
            
        }

        public abstract Actions decide(int agentID, EnvironmentState newState);

        public abstract void updateReward(int AgentID, double reward);

        public abstract int getNumberAgents();
    }

    class EmptyCoopAgents : CooperativeAgents
    {
        public EmptyCoopAgents()
        {

        }

        public override Actions decide(int agentID, EnvironmentState newState)
        {
            return 0;
        }

        public override int getNumberAgents()
        {
            return 0;
        }

        public override void updateReward(int AgentID, double reward)
        {
            return;
        }
    }
    class QLearningCooperative : CooperativeAgents
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


        private TabularStateConfig qLearningStateConf;

        private MetricsLogger metricsLogger;

        public QLearningCooperative(int agentsNumber, int statesNumber, ExplorationPolicy explorationPolicy, TabularStateConfig configuration, MetricsLogger metricsLogger)
        {
            stateReward = new Dictionary<int, double>();
            stateAction = new Dictionary<int, int>();
            currentState = new Dictionary<int, int>();
            actions = 4;
            this.metricsLogger = metricsLogger;
            states = statesNumber;
            this.explorationPolicy = explorationPolicy;
            qvalues = new double[statesNumber][];
            qLearningStateConf = configuration;
            for (int i = 0; i < agentsNumber; i++)
            {
                stateReward.Add(i, 0);
                stateAction.Add(i, 0);
                currentState.Add(i, 0);
            }
            for (int i = 0; i < statesNumber; i++)
            {
                qvalues[i] = new double[actions];
            }
            
        }

        public void updateState(int agentID, int previousState, int nextState)
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
            qvalues[previousState][stateAction[agentID]] *= (1.0 - learningRate);
            qvalues[previousState][stateAction[agentID]] += learningRate * (stateReward[agentID] + discountFactor * maxNextExpectedReward);
            stateReward[agentID] = 0;
        }


        public override Actions decide(int agentID, EnvironmentState newState)
        {
            int newIntState = qLearningStateConf.GetTabularStateFromEnvState(newState).getIntState(); //codes the state into an unique integer
            Actions chosenAction = explorationPolicy.decideAction(qvalues[newIntState]);
            updateState(agentID, currentState[agentID], newIntState);
            stateAction[agentID] = (int)chosenAction;
            currentState[agentID] = newIntState;
            return chosenAction;
        }

        public override void updateReward(int agentID, double reward)
        {
            stateReward[agentID] += reward;
        }

        public override int getNumberAgents()
        {
            return currentState.Count;
        }
    }
}
