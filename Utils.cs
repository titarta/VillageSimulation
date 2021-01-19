using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    class SetupConfig
    {
        //Environment
        public int numberAgents;
        public int numberCrops;
        public int distanceFarmWorkshop;
        //Rewards
        public int rewardWorkShop;
        public int[] rewardEating;
        public int rewardPlant;
        public int rewardHarvest;
        public int rewardDying;
        public int rewardImpossibleAction;
        //Agents
        public int initialSaturation;
        public double saturationLostPerTime;
        public double saturationRecoveredByFood;
        public int foodReceivedByHarvest;
        public int initialFood;
        //Learning
        public string learningAlgorithm;
        public string explorationPolicy;
        public double eDecay;
        public double eMin;

        public SetupConfig(string filePath)
        {
            readSetupVariables(filePath);
        }

        private void readSetupVariables(string definitionsPath)
        {
            string[] lines = System.IO.File.ReadAllLines(definitionsPath);
            foreach (string line in lines)
            {
                if (line.First() == '#')
                {
                    continue;
                }
                string[] lineSplitted = line.Split(':');
                switch (lineSplitted[0])
                {
                    //environment
                    case "Number Agents":
                        numberAgents = Int32.Parse(lineSplitted[1]);
                        break;
                    case "Number Crops":
                        numberCrops = Int32.Parse(lineSplitted[1]);
                        break;
                    case "Distance Farm-Workshop":
                        distanceFarmWorkshop = Int32.Parse(lineSplitted[1]);
                        break;

                    //rewards
                    case "Reward WorkShop":
                        rewardWorkShop = Int32.Parse(lineSplitted[1]);
                        break;
                    case "Reward Eating":
                        string[] reEat = lineSplitted[1].Split(';');
                        List<int> lAux = new List<int>();
                        for (int i = 0; i < reEat.Length; i++)
                        {
                            lAux.Add(Int32.Parse(reEat[i]));
                        }
                        rewardEating = lAux.ToArray();
                        break;
                    case "Reward Plant":
                        rewardPlant = Int32.Parse(lineSplitted[1]);
                        break;
                    case "Reward Harvest":
                        rewardHarvest = Int32.Parse(lineSplitted[1]);
                        break;
                    case "Reward Dying":
                        rewardDying = Int32.Parse(lineSplitted[1]);
                        break;
                    case "Reward Impossible Action":
                        rewardImpossibleAction = Int32.Parse(lineSplitted[1]);
                        break;

                    //agent
                    case "Initial Saturation":
                        initialSaturation = Int32.Parse(lineSplitted[1]);
                        break;
                    case "Saturation Lost Per Time":
                        saturationLostPerTime = Double.Parse(lineSplitted[1]);
                        break;
                    case "Saturation Recovered By Food":
                        saturationRecoveredByFood = Double.Parse(lineSplitted[1]);
                        break;
                    case "Food Received by Harvest":
                        foodReceivedByHarvest = Int32.Parse(lineSplitted[1]);
                        break;
                    case "Initial Reserved Food":
                        initialFood = Int32.Parse(lineSplitted[1]);
                        break;

                    //learning
                    case "Learning Algorithm":
                        learningAlgorithm = lineSplitted[1];
                        break;
                    case "Exploration Policy":
                        explorationPolicy = lineSplitted[1];
                        break;
                    case "Edecay":
                        eDecay = Double.Parse(lineSplitted[1]);
                        break;
                    case "Emin":
                        eMin = Double.Parse(lineSplitted[1]);
                        break;
                }
            }
        }
    }
}
