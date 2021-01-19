using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    [Serializable]
    class MetricsLogger
    {
        private StreamWriter logFile;
        private StreamWriter averageRewardLogFile;
        private StreamWriter totalRewardLogFile;
        private StreamWriter lifeTimeLogFile;
        private StreamWriter actionsStdLogFile;
        private int agentNumbers = 0;
        private int simulationNumber;
        //death stamps by agent
        private Dictionary<int, double> deathTimeStamps;
        //all saturations for each agent
        private Dictionary<int, List<double>> saturations;
        //all inventory for each agent
        private Dictionary<int, List<double>> foodReserved;
        //all actions performed
        private Dictionary<int, Dictionary<Actions, int>> actionsPerformedCounter;

        private Dictionary<int, List<double>> rewards;
        public MetricsLogger(string filename, int agentNumber)
        {
            string filePath = @"C:\Users\trvca\Desktop\VillageSimulation\Outputs\Metrics\Metrics-" + filename + "-" + DateTime.Now.ToString("h-mm-ss") + ".csv";
            string averageRewardFilePath = @"C:\Users\trvca\Desktop\VillageSimulation\Outputs\Results\AverageReward-" + filename + "-" + DateTime.Now.ToString("h-mm-ss") + ".csv";
            string totalRewardFilePath = @"C:\Users\trvca\Desktop\VillageSimulation\Outputs\Results\TotalReward-" + filename + "-" + DateTime.Now.ToString("h-mm-ss") + ".csv";
            string lifeTimeFilePath = @"C:\Users\trvca\Desktop\VillageSimulation\Outputs\Results\LifeTime-" + filename + "-" + DateTime.Now.ToString("h-mm-ss") + ".csv";
            string actionsStdFilePath = @"C:\Users\trvca\Desktop\VillageSimulation\Outputs\Results\Actions-" + filename + "-" + DateTime.Now.ToString("h-mm-ss") + ".csv";
            simulationNumber = 0;
            this.agentNumbers = agentNumber;
            resetMetrics();
            logFile = File.CreateText(filePath);
            averageRewardLogFile = File.CreateText(averageRewardFilePath);
            totalRewardLogFile = File.CreateText(totalRewardFilePath);
            lifeTimeLogFile = File.CreateText(lifeTimeFilePath);
            actionsStdLogFile = File.CreateText(actionsStdFilePath);
        }


        public void startSimulationMetrics()
        {
            logFile.WriteLine("------- Simulation " + simulationNumber + " -------");
            simulationNumber++;
            resetMetrics();
        }

        private void resetMetrics()
        {
            deathTimeStamps = new Dictionary<int, double>();
            saturations = new Dictionary<int, List<double>>();
            foodReserved = new Dictionary<int, List<double>>();
            actionsPerformedCounter = new Dictionary<int, Dictionary<Actions, int>>();
            rewards = new Dictionary<int, List<double>>();

            for (int i = 0; i < agentNumbers; i++)
            {
                deathTimeStamps.Add(i, 0);
                saturations.Add(i, new List<double>());
                foodReserved.Add(i, new List<double>());
                actionsPerformedCounter.Add(i, new Dictionary<Actions, int>());
                rewards.Add(i, new List<double>());
                for (int j = 0; j < Enum.GetNames(typeof(Actions)).Length; j++)
                {
                    actionsPerformedCounter[i].Add((Actions)j, 1);
                }

            }


        }
        public void addRewardToMetrics(int agentID, double reward)
        {
            rewards[agentID].Add(reward);
        }

        public void addSaturationToMetrics(int agentID, double saturation)
        {
            saturations[agentID].Add(saturation);
        }

        public void addFoodInvToMetrics(int agentID, double food)
        {
            foodReserved[agentID].Add(food);
        }

        public void addActionToCounter(int agentID, Actions act)
        {
            actionsPerformedCounter[agentID][act]++;
        }

        public void addAgentDeath(int agentID, double timeStamp)
        {
            deathTimeStamps[agentID] = timeStamp;
        }

        public void calculateSimulationMetrics()
        {
            //average death time
            logFile.WriteLine("Average death time: " + deathTimeStamps.Values.Sum() / deathTimeStamps.Count);
            //Average saturation
            logFile.WriteLine("Average saturation: " + saturations.Values.Sum(x => x.Sum() / x.Count) / saturations.Count);
            //Average food reserved
            logFile.WriteLine("Average food reserved: " + foodReserved.Values.Sum(x => x.Sum() / x.Count) / foodReserved.Count);
            //Rewards Gained
            logFile.WriteLine("Average average reward gained: " + rewards.Values.Sum(x => x.Sum() / x.Count) / rewards.Count);
            //Average Total Rewards
            logFile.WriteLine("Average total reward gained: " + rewards.Values.Sum(x => x.Sum()) / rewards.Count);
            //Percent Actions
            logFile.WriteLine("Eating Average percentage: " + (double)actionsPerformedCounter.Values.Sum(x => (double)x[Actions.Eat] / (double)x.Values.Sum()) / (double)actionsPerformedCounter.Count);
            logFile.WriteLine("Harvest Average percentage: " + (double)actionsPerformedCounter.Values.Sum(x => (double)x[Actions.Harvest] / (double)x.Values.Sum()) / (double)actionsPerformedCounter.Count);
            logFile.WriteLine("Plant Average percentage: " + (double)actionsPerformedCounter.Values.Sum(x => (double)x[Actions.Plant] / (double)x.Values.Sum()) / (double)actionsPerformedCounter.Count);
            logFile.WriteLine("Recreative Average percentage: " + (double)actionsPerformedCounter.Values.Sum(x => (double)x[Actions.Recreative] / (double)x.Values.Sum()) / (double)actionsPerformedCounter.Count);
            for (int i = 0; i < agentNumbers; i++)
            {
                logFile.WriteLine("------- Agent " + i + " metrics -------");
                logFile.WriteLine("Agent death time: " + deathTimeStamps[i]);
                logFile.WriteLine("Agent average saturation: " + saturations[i].Sum() / saturations[i].Count);
                logFile.WriteLine("Agent average food reserved: " + foodReserved[i].Sum() / foodReserved[i].Count);
                //Rewards Gained
                logFile.WriteLine("Average reward gained: " + rewards[i].Sum() / rewards[i].Count);
                //Average Total Rewards
                logFile.WriteLine("Total reward gained: " + rewards[i].Sum());
                //Percent Actions
                logFile.WriteLine("Agent eating Average percentage: " + (double)actionsPerformedCounter[i][Actions.Eat] / (double)actionsPerformedCounter[i].Values.Sum());
                logFile.WriteLine("Agent harvest Average percentage: " + (double)actionsPerformedCounter[i][Actions.Harvest] / (double)actionsPerformedCounter[i].Values.Sum());
                logFile.WriteLine("Agent plant Average percentage: " + (double)actionsPerformedCounter[i][Actions.Plant] / (double)actionsPerformedCounter[i].Values.Sum());
                logFile.WriteLine("Agent recreative Average percentage: " + (double)actionsPerformedCounter[i][Actions.Recreative] / (double)actionsPerformedCounter[i].Values.Sum());
            }

            averageRewardLogFile.WriteLine(rewards.Values.Sum(x => x.Sum() / x.Count) / rewards.Count);
            totalRewardLogFile.WriteLine(rewards.Values.Sum(x => x.Sum()) / rewards.Count);
            lifeTimeLogFile.WriteLine(deathTimeStamps.Values.Sum() / deathTimeStamps.Count);


            double plantStd = 0;
            double harvestStd = 0;
            double eatStd = 0;
            double workStd = 0;
            double plantSum = (double)actionsPerformedCounter.Values.Sum(x => (double)x[Actions.Plant] / (double)x.Values.Sum()) / (double)actionsPerformedCounter.Count;
            double harvestSum = (double)actionsPerformedCounter.Values.Sum(x => (double)x[Actions.Harvest] / (double)x.Values.Sum()) / (double)actionsPerformedCounter.Count;
            double eatSum = (double)actionsPerformedCounter.Values.Sum(x => (double)x[Actions.Eat] / (double)x.Values.Sum()) / (double)actionsPerformedCounter.Count;
            double workSum = (double)actionsPerformedCounter.Values.Sum(x => (double)x[Actions.Recreative] / (double)x.Values.Sum()) / (double)actionsPerformedCounter.Count;
            for (int i = 0; i < agentNumbers; i++)
            {
                plantStd += Math.Pow(plantSum - (double)actionsPerformedCounter[i][Actions.Plant] / (double)actionsPerformedCounter[i].Values.Sum(), 2);
                harvestStd += Math.Pow(harvestSum - (double)actionsPerformedCounter[i][Actions.Harvest] / (double)actionsPerformedCounter[i].Values.Sum(), 2);
                workStd += Math.Pow(workSum - (double)actionsPerformedCounter[i][Actions.Recreative] / (double)actionsPerformedCounter[i].Values.Sum(), 2);
                eatStd += Math.Pow(eatSum - (double)actionsPerformedCounter[i][Actions.Eat] / (double)actionsPerformedCounter[i].Values.Sum(), 2);
            }
            plantStd = Math.Sqrt(plantStd / agentNumbers);
            harvestStd = Math.Sqrt(harvestStd / agentNumbers);
            eatStd = Math.Sqrt(eatStd / agentNumbers);
            workStd = Math.Sqrt(workStd / agentNumbers);
            actionsStdLogFile.WriteLine(eatStd + "," + harvestStd + "," + plantStd + "," + workStd);

        }


        public void closeFile()
        {
            logFile.Close();
            averageRewardLogFile.Close();
            totalRewardLogFile.Close();
            lifeTimeLogFile.Close();
            actionsStdLogFile.Close();

        }
    }
}
