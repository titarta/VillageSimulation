using System;
using System.Collections.Generic;
using ConvnetSharp;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;

namespace Simulation
{
    class SimulationWrapper
    {
        private Dictionary<int, Agent> agents;
        private CooperativeAgents coopAgent;
        //Setup variables configuration
        private SetupConfig variablesSetup;
        private TabularStateConfig tabStateConfig;
        private MetricsLogger metricsLogger;
        private bool cooperativeLearning;
        private bool saveAgents;
        private string agentNames;

        private bool endingCondition = false;
        public SimulationWrapper(string definitionsPath, string singleOrMultiple, string metricsFileName, string outputFileName, string agentsName, bool coopLearning, bool saveAgents)
        {
            endingCondition = false;
            agentNames = agentsName;
            variablesSetup = new SetupConfig(definitionsPath);
            tabStateConfig = new TabularStateConfig();
            metricsLogger = new MetricsLogger(metricsFileName, variablesSetup.numberAgents);
            agents = new Dictionary<int, Agent>();
            cooperativeLearning = coopLearning;
            this.saveAgents = saveAgents;
            if(cooperativeLearning)
            {
                switch (variablesSetup.learningAlgorithm)
                {
                    case "Random":
                        break;
                    case "Q-Learning":
                        ExplorationPolicy expPolicy = new GreedyExploration();
                        switch (variablesSetup.explorationPolicy)
                        {
                            case "Greedy":
                                expPolicy = new GreedyExploration();
                                break;
                            case "Egreedy":
                                expPolicy = new EGreedyExploration(variablesSetup.eDecay);
                                break;
                        }


                        coopAgent = new QLearningCooperative(variablesSetup.numberAgents, tabStateConfig.getStateSize(), expPolicy, tabStateConfig, metricsLogger);
                        break;
                }
            } else
            {
                for (int i = 0; i < variablesSetup.numberAgents; i++)
                {

                    switch (variablesSetup.learningAlgorithm)
                    {
                        case "Random":
                            agents.Add(i, new RandomAgent(metricsLogger));
                            break;
                        case "Q-Learning":
                            ExplorationPolicy expPolicy = new GreedyExploration();
                            switch (variablesSetup.explorationPolicy)
                            {
                                case "Greedy":
                                    expPolicy = new GreedyExploration();
                                    break;
                                case "Egreedy":
                                    expPolicy = new EGreedyExploration(variablesSetup.eDecay);
                                    break;
                            }


                            agents.Add(i, new QLearningAgent(tabStateConfig.getStateSize(), expPolicy, false, tabStateConfig, metricsLogger));
                            break;
                        case "deepQ-Learning":
                            addDeepQlearningAgents(i);
                            break;
                    }

                }
            }
            
            if (singleOrMultiple == "single")
            {
                SimulationWrapperSingle();
                metricsLogger.closeFile();
            } else
            {
                Thread t1 = new Thread(SimulationWrapperMultiple);
                t1.Start();
                
            }
        }

        public SimulationWrapper(string definitionsPath, string singleOrMultiple, string metricsFileName, string outputFileName, string agentsName, bool saveAgents, CooperativeAgents agents)
        {
            endingCondition = false;
            agentNames = agentsName;
            variablesSetup = new SetupConfig(definitionsPath);
            tabStateConfig = new TabularStateConfig();
            metricsLogger = new MetricsLogger(metricsFileName, variablesSetup.numberAgents);
            this.agents = new Dictionary<int, Agent>();
            cooperativeLearning = true;
            this.saveAgents = saveAgents;
            coopAgent = agents;

            if (singleOrMultiple == "single")
            {
                SimulationWrapperSingle();
                metricsLogger.closeFile();
            }
            else
            {
                Thread t1 = new Thread(SimulationWrapperMultiple);
                t1.Start();

            }
        }

        public SimulationWrapper(string definitionsPath, string singleOrMultiple, string metricsFileName, string outputFileName, string agentsName, bool saveAgents, Dictionary<int, Agent> agents)
        {
            endingCondition = false;
            agentNames = agentsName;
            variablesSetup = new SetupConfig(definitionsPath);
            tabStateConfig = new TabularStateConfig();
            metricsLogger = new MetricsLogger(metricsFileName, variablesSetup.numberAgents);
            this.agents = agents;
            cooperativeLearning = false;
            this.saveAgents = saveAgents;
            coopAgent = new QLearningCooperative(0, 0, null, null, null);

            if (singleOrMultiple == "single")
            {
                SimulationWrapperSingle();
                metricsLogger.closeFile();
            }
            else
            {
                Thread t1 = new Thread(SimulationWrapperMultiple);
                t1.Start();

            }
        }



        private void SimulationWrapperSingle()
        {
            if(cooperativeLearning)
            {
                Environment env = new Environment(variablesSetup.numberCrops, coopAgent, variablesSetup, metricsLogger);
            } else
            {
                Environment env = new Environment(variablesSetup.numberCrops, agents, variablesSetup, metricsLogger);
            }
           
        }

        private void SimulationWrapperMultiple()
        {
            int i = 0;
            int numSims = (int)((0.05 / variablesSetup.eDecay));
            while(!endingCondition) {
                if(cooperativeLearning)
                {
                    Environment env = new Environment(variablesSetup.numberCrops, coopAgent, variablesSetup, metricsLogger);
                } else
                {
                    Environment env = new Environment(variablesSetup.numberCrops, agents, variablesSetup, metricsLogger);
                }
                i++;
                Console.WriteLine("Simulation ended");
                /*if(i > numSims / 2)
                {
                    Console.WriteLine("Reached halfpoint");
                }*/
                //if(i > numSims) { break; }
            }
            metricsLogger.closeFile();
            if (saveAgents)
            {
                storeAgents();
            }

        }


        public void endSimulation()
        {
            endingCondition = true;
        }

        private void addDeepQlearningAgents(int agentID)
        {
            int numInputs = 6;
            int numActions = 4;
            int temporalWindow = 0;
            int network_size = numInputs * temporalWindow + numActions * temporalWindow + numInputs;

            List<LayerDefinition> layer_defs = new List<LayerDefinition>();

            
            layer_defs.Add(new LayerDefinition { type = "input", out_sx = 1, out_sy = 1, out_depth = network_size });
            layer_defs.Add(new LayerDefinition { type = "fc", num_neurons = 3, activation = "relu" });
            layer_defs.Add(new LayerDefinition { type = "fc", num_neurons = 3, activation = "relu" });
            layer_defs.Add(new LayerDefinition { type = "regression", num_neurons = numActions });


            Options opt = new Options { method = "adadelta", l2_decay = 0.001, batch_size = 10 };

            TrainingOptions tdtrainer_options = new TrainingOptions();
            tdtrainer_options.temporal_window = temporalWindow;
            tdtrainer_options.experience_size = 30000;
            tdtrainer_options.start_learn_threshold = 100;
            tdtrainer_options.gamma = 0.7;
            tdtrainer_options.learning_steps_total = 10000000;
            tdtrainer_options.learning_steps_burnin = 10000;
            tdtrainer_options.epsilon_min = 0.05;
            tdtrainer_options.epsilon_test_time = 0.00;
            tdtrainer_options.layer_defs = layer_defs;
            tdtrainer_options.options = opt;

            agents.Add(agentID, new DeepQLearningAgent(numInputs, numActions, tdtrainer_options));
        }


        private void storeAgents()
        {
            string agentsPath = @"C:\Users\trvca\Desktop\VillageSimulation\Outputs\Agents\";
            BinaryFormatter b = new BinaryFormatter();
            if(cooperativeLearning)
            {
                FileStream f = File.Create(agentsPath + agentNames + ".dat");
                b.Serialize(f, coopAgent);
                f.Close();
            } else
            {
                for(int i = 0; i < agents.Count; i++)
                {
                    FileStream f = File.Create(agentsPath + agentNames + i + ".dat");

                    b.Serialize(f, agents[i]);

                    f.Close();
                }
            }
        }


    }
}
