using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    [Serializable]
    public enum Actions { Plant=0, Harvest=1, Recreative=2, Eat=3, Sleep=4, Work=5};
    [Serializable]
    public enum Effects
    {
        PlantGrow = 0, PlantSeed = 1, ChangePositionToFarm = 2, ChangePositionToWorkshop = 3, PlantHarvest = 4, FoodConsumptionStart = 5, WorkFinish = 6,
        WorkStart = 7, SleepStart = 8, SleepFinish = 9, PlantStart = 10, PlantFinish = 11, HarvestStart = 12, HarvestFinish = 13, ChangePositionToHouse = 14,
        FoodConsumptionEnd = 15, FunStart = 16, FunFinish = 17, StartChangePositionToFarm = 18, StartChangePositionToWorkshop = 19, StartChangePositionToHouse = 21
    }
    [Serializable]
    public enum CropState { None=0, Seed=1, Plant=2}
    [Serializable]
    public enum SocialPractices { Sleep=0, EatTime = 1,  EatQuantity = 2 , Schedule = 3, LimitFarm = 4}

    class NoFarmLandAvailableException : Exception
    {
        public NoFarmLandAvailableException() { }
    }
    class NoPlantToHarvestException : Exception
    {
        public NoPlantToHarvestException() { }
    }

    class NoFoodToEatException : Exception
    {
        public NoFoodToEatException() { }
    }

    class AgentDiesException : Exception
    {
        public AgentDiesException() { }
    }

    public class EnvironmentStep : IComparable
    {
        private double timeStamp;
        private int agentID;
        private Effects effect;
        private int plantID = -1;
        public EnvironmentStep(double timeStamp, int agentID, Effects effect)
        {
            this.timeStamp = timeStamp;
            this.agentID = agentID;
            this.effect = effect;
        }

        public EnvironmentStep(double timeStamp, int agentID, Effects effect, int plantID)
        {
            this.timeStamp = timeStamp;
            this.agentID = agentID;
            this.effect = effect;
            this.plantID = plantID;
        }

        public double getTimeStamp()
        {
            return timeStamp;
        }
        public int getAgentID()
        {
            return agentID;
        }
        public Effects getEffect()
        {
            return effect;
        }
        public int getPlantID()
        {
            return plantID;
        }

        public int CompareTo(object obj)
        {
            EnvironmentStep otherStep = obj as EnvironmentStep;
            return (int)Math.Ceiling(this.timeStamp - otherStep.getTimeStamp());
        }

        public static bool operator <(EnvironmentStep left, EnvironmentStep right)
        {
            return left.getTimeStamp() < right.getTimeStamp();
        }

        public static bool operator >(EnvironmentStep left, EnvironmentStep right)
        {
            return left.getTimeStamp() > right.getTimeStamp();
        }
    }

    
    
    class Environment
    {
        //the current state of the farm, each position contains one of the three possible states
        private CropState[] farm;
        //the current queue of the events to happen in the environment
        private List<EnvironmentStep> stepsQueue;
        //agent's references
        private Dictionary<int, Agent> agents;
        private CooperativeAgents coopAgents;
        //last time each agent was updated (in order to reduce saturation and for metrics)
        private Dictionary<int, double> agentLastUpdateTimeStamp;
        //wether or not the agents is free and needs to decide an action
        private Dictionary<int, bool> agentsDeciding;
        //wether or not the agent is dead
        private Dictionary<int, bool> deadAgents;
        //the harvest that corresponds to each agent
        private Dictionary<int, List<int>> ownedHarvest;
        //the food each agent has reserved
        private Dictionary<int, int> agentFoodReserved;
        //the current position of each agent
        private Dictionary<int, AgentPosition> agentPositions;
        //the saturation of each agent
        private Dictionary<int, double> agentSaturation;
        //flag to know wether or not the agent is executinga  social practice or not
        private Dictionary<int, bool> socialPracticesFlag;

        private MetricsLogger metricsLogger;
        private OutputLogger outputLogger;


        private bool condition;

        private SetupConfig setupVariables;
        private double simulationTime; // each hour is 4 time units, making 1 time unit = 15min
        private double timeToGrow = 2 * 4;
        private double harvestTime = 4;
        private double plantTime = 4;
        private double recreativeTime = 4;
        private double workTime = 4;
        private double sleepTime = 4;
        private double eatTime = 2;

        private bool cooperativeLearning;

        public Environment(int farmSize, Dictionary<int, Agent> agentsDict, SetupConfig setupVariables, MetricsLogger metricsLogger, OutputLogger outputLogger)
        {
            cooperativeLearning = false;
            this.setupVariables = setupVariables;
            this.metricsLogger = metricsLogger;
            this.metricsLogger.startSimulationMetrics();
            this.outputLogger = outputLogger;
            simulationTime = 0;
            farm = new CropState[farmSize];
            for(int i = 0; i < farmSize; i++)
            {
                farm[i] = CropState.None;
            }

            //create agents
            agents = agentsDict;
            coopAgents = new EmptyCoopAgents();
            agentsDeciding = new Dictionary<int, bool>();
            agentLastUpdateTimeStamp = new Dictionary<int, double>();
            deadAgents = new Dictionary<int, bool>();
            ownedHarvest = new Dictionary<int, List<int>>();
            agentFoodReserved = new Dictionary<int, int>();
            agentPositions = new Dictionary<int, AgentPosition>();
            agentSaturation = new Dictionary<int, double>();
            socialPracticesFlag = new Dictionary<int, bool>();
            for (int i = 0; i < agentsDict.Count; i++)
            {
                agentLastUpdateTimeStamp.Add(i, simulationTime);
                agentsDeciding.Add(i, true);
                deadAgents.Add(i, false);
                ownedHarvest.Add(i, new List<int>());
                agentFoodReserved.Add(i, setupVariables.initialFood);
                agentPositions.Add(i, AgentPosition.Center);
                agentSaturation.Add(i, setupVariables.initialSaturation);
                socialPracticesFlag.Add(i, false);
            }
            
            condition = true;
            stepsQueue = new List<EnvironmentStep>();

            

            updateEnvironment();
        }

        public Environment(int farmSize, CooperativeAgents coopAgent, SetupConfig setupVariables, MetricsLogger metricsLogger, OutputLogger outputLogger)
        {
            cooperativeLearning = true;
            this.setupVariables = setupVariables;
            this.metricsLogger = metricsLogger;
            this.metricsLogger.startSimulationMetrics();
            this.outputLogger = outputLogger;
            simulationTime = 0;
            farm = new CropState[farmSize];
            for (int i = 0; i < farmSize; i++)
            {
                farm[i] = CropState.None;
            }

            //create agents
            coopAgents = coopAgent;
            agents = new Dictionary<int, Agent>();
            agentsDeciding = new Dictionary<int, bool>();
            agentLastUpdateTimeStamp = new Dictionary<int, double>();
            deadAgents = new Dictionary<int, bool>();
            ownedHarvest = new Dictionary<int, List<int>>();
            agentFoodReserved = new Dictionary<int, int>();
            agentPositions = new Dictionary<int, AgentPosition>();
            agentSaturation = new Dictionary<int, double>();
            socialPracticesFlag = new Dictionary<int, bool>();
            for (int i = 0; i < coopAgents.getNumberAgents(); i++)
            {
                agentLastUpdateTimeStamp.Add(i, simulationTime);
                agentsDeciding.Add(i, true);
                deadAgents.Add(i, false);
                ownedHarvest.Add(i, new List<int>());
                agentFoodReserved.Add(i, setupVariables.initialFood);
                agentPositions.Add(i, AgentPosition.Center);
                agentSaturation.Add(i, setupVariables.initialSaturation);
                socialPracticesFlag.Add(i, false);
            }

            condition = true;
            stepsQueue = new List<EnvironmentStep>();

            updateEnvironment();
        }
        private void updateEnvironment()
        {
            while(condition)
            {
                //check if agents are dead
                bool allAgentsDead = true;
                foreach (bool dead in deadAgents.Values)
                {
                    if (!dead)
                    {
                        allAgentsDead = false;
                    }
                }
                if (allAgentsDead || simulationTime >= 100000)
                {
                    condition = false;
                    continue;
                }

                //check if agents need to decide
                bool agentsDecided = true;
                for(int i = 0; i < agentsDeciding.Count; i++)
                {
                    if (agentsDeciding[i] && !deadAgents[i])
                    {
                        agentsDecided = false;
                    }
                }

                if(agentsDecided)
                {
                    updateStep();
                } else
                {
                    for(int agentID = 0; agentID < Math.Max(agents.Count, coopAgents.getNumberAgents()); agentID++)
                    {
                        if(deadAgents[agentID])
                        {
                            continue;
                        }
                        if(agentsDeciding[agentID])
                        {

                            Actions actionPerformed;

                            socialPracticesFlag[agentID] = false;
                            if (cooperativeLearning)
                            {
                                actionPerformed = coopAgents.decide(agentID, getEnvState(agentID));
                            } else
                            {
                                actionPerformed = agents[agentID].decide(getEnvState(agentID)); //get action made by agent (will get a state here)
                            }

                            //social practices constraints


                            actionPerformed = socialPractices(agentID, actionPerformed);


                            //end social practices constraints
                            

                            if(publishAction(actionPerformed, agentID))
                            {
                                agentsDeciding[agentID] = false;
                                continue;
                            } else
                            {
                                agentID--;
                                continue;
                            }

                            
                            
                        }
                    }
                }

            }
            metricsLogger.calculateSimulationMetrics();
        }
    
        //retorna true se a ação poder ser executado, false otherwise
        private bool publishAction(Actions action, int agentID)
        {

            AgentPosition actionPos = getActionPosition(action, agentPositions[agentID]);

            //List<Effect> effects = getEffectsFromActions(action, agentID);

            double movementTime = getTimeFromPosToPos(agentPositions[agentID], actionPos);

            //create envSteps from effects
            int plantPosition;
            switch (action)
            {
                case Actions.Eat:
                    if (agentFoodReserved[agentID] > 0)
                    {
                        stepsQueue.Add(new EnvironmentStep(simulationTime, agentID, Effects.FoodConsumptionStart));
                        stepsQueue.Add(new EnvironmentStep(simulationTime + eatTime, agentID, Effects.FoodConsumptionEnd));
                    } else
                    {
                        stepsQueue.Sort();

                        updateAgentReward(agentID, setupVariables.rewardImpossibleAction);
                        return false;
                    }
                        
                    break;
                case Actions.Harvest:
                    plantPosition = getPlantToHarvest(agentID);
                    if (plantPosition == -1)
                    {
                        stepsQueue.Sort();
                        updateAgentReward(agentID, setupVariables.rewardImpossibleAction);
                        return false;
                    }
                    stepsQueue.Add(new EnvironmentStep(simulationTime, agentID, Effects.StartChangePositionToFarm));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime, agentID, Effects.ChangePositionToFarm));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime, agentID, Effects.HarvestStart));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime + harvestTime, agentID, Effects.HarvestFinish));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime + harvestTime, agentID, Effects.PlantHarvest, plantPosition));
                    break;
                case Actions.Plant:
                    plantPosition = getFreeSpaceInFarm();
                    if (plantPosition == -1)
                    {
                        stepsQueue.Sort();
                        updateAgentReward(agentID, setupVariables.rewardImpossibleAction);
                        return false;
                    }
                    ownedHarvest[agentID].Add(plantPosition);
                    stepsQueue.Add(new EnvironmentStep(simulationTime, agentID, Effects.StartChangePositionToFarm));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime, agentID, Effects.ChangePositionToFarm));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime, agentID, Effects.PlantStart, plantPosition));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime + plantTime, agentID, Effects.PlantFinish, plantPosition));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime + plantTime, agentID, Effects.PlantSeed, plantPosition));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime + plantTime + timeToGrow, agentID, Effects.PlantGrow, plantPosition));
                    break;
                case Actions.Recreative:
                    stepsQueue.Add(new EnvironmentStep(simulationTime, agentID, Effects.StartChangePositionToHouse));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime, agentID, Effects.ChangePositionToHouse));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime, agentID, Effects.FunStart));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime + recreativeTime, agentID, Effects.FunFinish));
                    break;
                case Actions.Work:
                    stepsQueue.Add(new EnvironmentStep(simulationTime, agentID, Effects.StartChangePositionToWorkshop));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime, agentID, Effects.ChangePositionToWorkshop));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime, agentID, Effects.WorkStart));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime + workTime, agentID, Effects.WorkFinish));
                    break;
                case Actions.Sleep:
                    stepsQueue.Add(new EnvironmentStep(simulationTime, agentID, Effects.StartChangePositionToHouse));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime, agentID, Effects.ChangePositionToHouse));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime, agentID, Effects.SleepStart));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime + sleepTime, agentID, Effects.SleepFinish));
                    break;
                default:
                    break;

            }

            metricsLogger.addActionToCounter(agentID, action);
            stepsQueue.Sort();
            return true;


        }

        private AgentPosition getActionPosition(Actions action, AgentPosition agentCurrentPos)
        {
            switch(action)
            {
                case Actions.Harvest:
                    return AgentPosition.Farm;
                case Actions.Plant:
                    return AgentPosition.Farm;
                case Actions.Recreative:
                    return AgentPosition.House;
                case Actions.Eat:
                    return agentCurrentPos;
                case Actions.Work:
                    return AgentPosition.WorkShop;
                case Actions.Sleep:
                    return AgentPosition.House;

            }
            return agentCurrentPos;
        }

        private double getTimeFromPosToPos(AgentPosition pos1, AgentPosition pos2)
        {
            if(pos1 == pos2)
            {
                return 0;
            }
            return setupVariables.distanceFarmWorkshop;
            //if(pos1 == AgentPosition.Center)
            //{
            //    switch (pos2)
            //    {
            //        case AgentPosition.Farm:
            //            return setupVariables.distanceFarmWorkshop / 2;
            //        case AgentPosition.WorkShop:
            //            return setupVariables.distanceFarmWorkshop / 2;
            //    }
            //}
            //if(pos1 == AgentPosition.Farm)
            //{
            //    switch(pos2)
            //    {
            //        case AgentPosition.Center:
            //            return setupVariables.distanceFarmWorkshop / 2;
            //        case AgentPosition.WorkShop:
            //            return setupVariables.distanceFarmWorkshop;
            //    }
            //}
            //if(pos1 == AgentPosition.WorkShop)
            //{
            //    switch(pos2)
            //    {
            //        case AgentPosition.Center:
            //            return setupVariables.distanceFarmWorkshop / 2;
            //        case AgentPosition.Farm:
            //            return setupVariables.distanceFarmWorkshop;
            //    }
            //}
        }
    
    
        /*private List<Effect> getEffectsFromActions(Actions action, int agentID)
        {
            List<Effect> effects = new List<Effect>();
            int plantPosition;
            switch (action)
            {
                case Actions.Eat:
                    effects.Add(new Effect(Effects.FoodConsumption));
                    break;
                case Actions.Harvest:
                    plantPosition = getPlantToHarvest(agentID);
                    if(plantPosition == -1)
                    {
                        throw new NoPlantToHarvestException();
                    }
                    effects.Add(new Effect(Effects.ChangePositionToFarm));
                    effects.Add(new Effect(Effects.PlantHarvest, plantPosition));
                    break;
                case Actions.Plant:
                    plantPosition = getFreeSpaceInFarm();
                    if (plantPosition == -1)
                    {
                        throw new NoFarmLandAvailableException();
                    }
                    effects.Add(new Effect(Effects.ChangePositionToFarm));
                    effects.Add(new Effect(Effects.PlantSeed, plantPosition));
                    effects.Add(new Effect(Effects.PlantGrow, plantPosition));
                    break;
                case Actions.Recreative:
                    effects.Add(new Effect(Effects.changePositionToWorkshop));
                    effects.Add(new Effect(Effects.RecreativeFinish));
                    break;
                default:
                    break;

            }

            return effects;
        }*/

        private int getPlantToHarvest(int agentID)
        {
            foreach(int plantID in ownedHarvest[agentID])
            {
                if(farm[plantID] == CropState.Plant)
                {
                    return plantID;
                }
            }

            return -1;
        }

        private int getFreeSpaceInFarm()
        {
            for (int i = 0; i < farm.Length; i++)
            {
                bool isSpotFree = true;
                for(int j = 0; j < ownedHarvest.Count; j++)
                {
                    if (ownedHarvest[j].Contains(i))
                    {
                        isSpotFree = false;
                    }
                }
                if(isSpotFree)
                {
                    return i;
                }
            }
            return -1;
        }

        private int getNumberOfFreeCrops()
        {
            int nCrops = 0;
            for (int i = 0; i < farm.Length; i++)
            {
                if (farm[i] == CropState.None)
                {
                    nCrops++;
                }
            }
            return nCrops;
        }



        private void updateStep()
        {
            if(stepsQueue.Count == 0)
            {
                for(int i = 0; i < agentsDeciding.Count; i++)
                {
                    agentsDeciding[i] = true;
                }
                return;
            }
            EnvironmentStep envStep = stepsQueue.First();
            simulationTime = envStep.getTimeStamp();
            int agentID = envStep.getAgentID();
            outputLogger.writeOutput(envStep, agentFoodReserved[agentID], agentSaturation[agentID]);
            //update do estado do jogo
            if (!updateAgentSaturation(agentID, simulationTime - agentLastUpdateTimeStamp[agentID]))
            {
                deadAgents[agentID] = true;
                updateAgentReward(agentID, setupVariables.rewardDying);
                if (cooperativeLearning)
                {
                    coopAgents.decide(agentID, getEnvState(agentID));
                }
                else
                {
                    agents[agentID].decide(getEnvState(agentID));
                }
                metricsLogger.addAgentDeath(agentID, simulationTime);
                //delete all agentActions
                for (int i = 0; i < stepsQueue.Count; i++)
                {
                    if (stepsQueue[i].getAgentID() == agentID)
                    {
                        stepsQueue.RemoveAt(i);
                        i--;
                    }
                }
                foreach(int plantID in ownedHarvest[agentID])
                {
                    farm[plantID] = CropState.None;
                }
                ownedHarvest[agentID] = new List<int>();
                return;
            }
            
            agentLastUpdateTimeStamp[agentID] = simulationTime;

            switch (envStep.getEffect())
            {
                case Effects.FoodConsumptionEnd:
                    consumeFood(agentID);
                    //Console.WriteLine("agent ate");
                    break;
                case Effects.ChangePositionToFarm:
                    updateAgentPosition(agentID, AgentPosition.Farm);
                    //Console.WriteLine("agent went to farm");
                    break;
                case Effects.ChangePositionToWorkshop:
                    updateAgentPosition(agentID, AgentPosition.WorkShop);
                    //Console.WriteLine("agent went to workshop");
                    break;
                case Effects.ChangePositionToHouse:
                    updateAgentPosition(agentID, AgentPosition.House);
                    //Console.WriteLine("agent went to workshop");
                    break;
                case Effects.PlantGrow:
                    farm[envStep.getPlantID()] = CropState.Plant;
                    //Console.WriteLine("plant grows");
                    break;
                case Effects.PlantHarvest:
                    harvestAgentCrop(agentID, envStep.getPlantID());
                    addFoodToAgent(agentID);
                    //Console.WriteLine("plant is harvested");
                    break;
                case Effects.FunFinish:
                    endFun(agentID);
                    break;
                case Effects.WorkFinish:
                    endWork(agentID);
                    break;
                case Effects.SleepFinish:
                    endSleep(agentID);
                    break;
                case Effects.PlantSeed:
                    plantCrop(agentID, envStep.getPlantID());
                    //Console.WriteLine("seed is planted");
                    break;
                case Effects.PlantStart:
                    //plantCrop(agentID, envStep.getPlantID());
                    //Console.WriteLine("seed is planted");
                    break;
                default:
                    break;
            }

            stepsQueue.RemoveAt(0);

            //check if there are more steps for the agent
            bool agentHasMoreSteps = false;
            foreach(EnvironmentStep es in stepsQueue)
            {
                if(es.getAgentID() == envStep.getAgentID())
                {
                    if(es.getEffect() == Effects.PlantGrow)
                    {
                        continue;
                    }
                    agentHasMoreSteps = true;
                }
            }

            if(!agentHasMoreSteps)
            {
                agentsDeciding[envStep.getAgentID()] = true;
            }


        }

        private EnvironmentState getEnvState(int agentID)
        {
            int freeCrops = getNumberOfFreeCrops();
            int cropsToCultivate = 0;
            int cropsGrowing = 0;
            foreach (int cropID in ownedHarvest[agentID])
            {
                if(farm[cropID] == CropState.Plant)
                {
                    cropsToCultivate++;
                } else
                {
                    cropsGrowing++;
                }
            }
            int position = (int)agentPositions[agentID]; 
            double saturation = agentSaturation[agentID]; 
            double foodReserved = agentFoodReserved[agentID];

            return new EnvironmentState(freeCrops, cropsToCultivate, cropsGrowing, position, saturation, foodReserved);
        }

        //make agent consume food, returns true if the agent can eat food and false if otherwise
        private void consumeFood(int agentID)
        {
            if (agentSaturation[agentID] < 0.5)
            {
                updateAgentReward(agentID, setupVariables.rewardEating[0]);
            }
            else if (agentSaturation[agentID] < 1.5)
            {
                updateAgentReward(agentID, setupVariables.rewardEating[1]);
            }
            else if (agentSaturation[agentID] < 4)
            {
                updateAgentReward(agentID, setupVariables.rewardEating[2]);
            }
            else
            {
                updateAgentReward(agentID, setupVariables.rewardEating[3]);
            }



            agentFoodReserved[agentID]--;
            agentSaturation[agentID] += setupVariables.saturationRecoveredByFood;
            metricsLogger.addSaturationToMetrics(agentID, agentSaturation[agentID]);
            metricsLogger.addFoodInvToMetrics(agentID, agentFoodReserved[agentID]);

        }

        private void updateAgentPosition(int agentID, AgentPosition position)
        {
            agentPositions[agentID] = position;
        }

        private void harvestAgentCrop(int agentID, int plantID)
        {

            farm[plantID] = CropState.None;
            ownedHarvest[agentID].Remove(plantID);
        }

        private void addFoodToAgent(int agentID)
        {
            agentFoodReserved[agentID] += setupVariables.foodReceivedByHarvest;
            updateAgentReward(agentID, setupVariables.rewardHarvest);
            metricsLogger.addFoodInvToMetrics(agentID, agentFoodReserved[agentID]);

        }

        private void plantCrop(int agentID, int plantID)
        {
            farm[plantID] = CropState.Seed;
            //ownedHarvest[agentID].Add(plantID);
            updateAgentReward(agentID, setupVariables.rewardPlant);
        }

        private void endWork(int agentID)
        {
            updateAgentReward(agentID, setupVariables.rewardWorkShop);
        }

        private void endFun(int agentID)
        {
            updateAgentReward(agentID, setupVariables.rewardFun);
        }

        private void endSleep(int agentID)
        {
            updateAgentReward(agentID, setupVariables.rewardSleep);
        }


        //updates agent saturation, returns true if the agent is not dead, and false if he is
        private bool updateAgentSaturation(int agentID, double timePassed)
        {
            agentSaturation[agentID] -= timePassed * setupVariables.saturationLostPerTime;
            if (agentSaturation[agentID] <= 0)
            {
                return false;
            }
            return true;
        }

        private void updateAgentReward(int agentID, double reward)
        {
            if(socialPracticesFlag[agentID])
            {
                reward = 0;
            }
            if(cooperativeLearning)
            {
                coopAgents.updateReward(agentID, reward);
            } else
            {
                agents[agentID].updateReward(reward);
            }
            metricsLogger.addRewardToMetrics(agentID, reward);
        }
    
        private int getDayTime()
        {
            return (int)Math.Floor((simulationTime % (24 * 4)) / 4);
        }
    

        private Actions socialPractices(int agentID, Actions actionChosen)
        {
            if(agentSaturation[agentID] <= 4) //confirm agent doesn't die
            {
                if (agentFoodReserved[agentID] > 0)
                {
                    socialPracticesFlag[agentID] = true;
                    return Actions.Eat;
                }
            }
            if (agents[agentID].doesSocialPractice(SocialPractices.Sleep)) //sleep from 10pm to 8am
            {
                int dayTime = getDayTime();
                if(dayTime >= 22 || dayTime <= 8)
                {
                    socialPracticesFlag[agentID] = true;
                    return Actions.Sleep;
                }
            }
            if (agents[agentID].doesSocialPractice(SocialPractices.LimitFarm)) //sleep from 10pm to 8am
            {
                if (actionChosen == Actions.Plant && ownedHarvest[agentID].Count >= 5)
                {
                    socialPracticesFlag[agentID] = true;
                    if (getPlantToHarvest(agentID) != -1)
                    {
                        return Actions.Harvest;
                    }
                    return Actions.Work;
                }
            }
            if (agents[agentID].doesSocialPractice(SocialPractices.EatTime)) // Eats between 12am and 2pm and 8pm to 10pm until a certain saturation is achieved
            {
                int dayTime = getDayTime();
                if (((dayTime >= 12 && dayTime <= 14) || (dayTime >= 20 && dayTime <= 22)) && agentSaturation[agentID] <= 30 && agentFoodReserved[agentID] >= 1)
                {
                    socialPracticesFlag[agentID] = true;
                    return Actions.Eat;
                }
            }
            if (agents[agentID].doesSocialPractice(SocialPractices.EatQuantity))// Cannot eat if a certain saturation threshold is achieved
            {
                if (agentSaturation[agentID] > 30 && actionChosen == Actions.Eat)
                {
                    socialPracticesFlag[agentID] = true;
                    return Actions.Work;
                }
            }
            if (agents[agentID].doesSocialPractice(SocialPractices.Schedule)) // can only work from 8am to 4pm, can only have fun from 4pm to 8pm
            {
                int dayTime = getDayTime();
                if (dayTime >= 8 && dayTime <= 16) //work
                {
                    socialPracticesFlag[agentID] = true;
                    Console.WriteLine(farm[1].ToString());
                    if(getPlantToHarvest(agentID) != -1)
                    {
                        return Actions.Harvest;
                    }
                    if(agentFoodReserved[agentID] < 14 && getFreeSpaceInFarm() != -1 && ownedHarvest[agentID].Count <= 4)
                    {
                        return Actions.Plant;
                    }
                    return Actions.Work;

                }
                if (dayTime >= 16 || dayTime <= 20) //have fun
                {
                    socialPracticesFlag[agentID] = true;
                    return Actions.Recreative;
                }

            }
            

            return actionChosen;
        }
    }

    
}
