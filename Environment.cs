using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    [Serializable]
    public enum Actions { Plant=0, Harvest=1, Recreative=2, Eat=3};
    [Serializable]
    public enum Effects { PlantGrow=0, PlantSeed=1, ChangePositionToFarm=2, ChangePositionToWorkshop=3, PlantHarvest=4, FoodConsumption=5, RecreativeFinish=6}
    [Serializable]
    public enum CropState { None=0, Seed=1, Plant=2}

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

        private MetricsLogger metricsLogger;


        private bool condition;

        private SetupConfig setupVariables;
        private double simulationTime;
        private double timeToGrow = 20;
        private double harvestTime = 0;
        private double plantTime = 0;
        private double recreativeTime = 5;

        private bool cooperativeLearning;

        public Environment(int farmSize, Dictionary<int, Agent> agentsDict, SetupConfig setupVariables, MetricsLogger metricsLogger)
        {
            cooperativeLearning = false;
            this.setupVariables = setupVariables;
            this.metricsLogger = metricsLogger;
            this.metricsLogger.startSimulationMetrics();
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
            for (int i = 0; i < agentsDict.Count; i++)
            {
                agentLastUpdateTimeStamp.Add(i, simulationTime);
                agentsDeciding.Add(i, true);
                deadAgents.Add(i, false);
                ownedHarvest.Add(i, new List<int>());
                agentFoodReserved.Add(i, setupVariables.initialFood);
                agentPositions.Add(i, AgentPosition.Center);
                agentSaturation.Add(i, setupVariables.initialSaturation);
            }
            
            condition = true;
            stepsQueue = new List<EnvironmentStep>();

            updateEnvironment();
        }

        public Environment(int farmSize, CooperativeAgents coopAgent, SetupConfig setupVariables, MetricsLogger metricsLogger)
        {
            cooperativeLearning = true;
            this.setupVariables = setupVariables;
            this.metricsLogger = metricsLogger;
            this.metricsLogger.startSimulationMetrics();
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
            for (int i = 0; i < coopAgents.getNumberAgents(); i++)
            {
                agentLastUpdateTimeStamp.Add(i, simulationTime);
                agentsDeciding.Add(i, true);
                deadAgents.Add(i, false);
                ownedHarvest.Add(i, new List<int>());
                agentFoodReserved.Add(i, setupVariables.initialFood);
                agentPositions.Add(i, AgentPosition.Center);
                agentSaturation.Add(i, setupVariables.initialSaturation);
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
                    if(simulationTime >= 100000)
                    {
                        for(int i = 0; i < deadAgents.Count; i++)
                        {
                            if(!deadAgents[i])
                            {
                                metricsLogger.addAgentDeath(i, 100000);
                            }
                        }
                    }
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
                            
                            if(cooperativeLearning)
                            {
                                actionPerformed = coopAgents.decide(agentID, getEnvState(agentID));
                            } else
                            {
                                actionPerformed = agents[agentID].decide(getEnvState(agentID)); //get action made by agent (will get a state here)
                            }
                            

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
                        stepsQueue.Add(new EnvironmentStep(simulationTime, agentID, Effects.FoodConsumption));
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
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime, agentID, Effects.ChangePositionToFarm));
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
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime, agentID, Effects.ChangePositionToFarm));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime + plantTime, agentID, Effects.PlantSeed, plantPosition));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime + plantTime + timeToGrow, agentID, Effects.PlantGrow, plantPosition));
                    break;
                case Actions.Recreative:
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime, agentID, Effects.ChangePositionToWorkshop));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime + recreativeTime, agentID, Effects.RecreativeFinish));
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
                    return AgentPosition.WorkShop;
                case Actions.Eat:
                    return agentCurrentPos;

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
                for (int j = 0; j < ownedHarvest.Count; j++)
                {
                    if (ownedHarvest[j].Contains(i))
                    {
                        isSpotFree = false;
                    }
                }
                if (isSpotFree)
                {
                    return i;
                }
            }
            return -1;
            //for (int i = 0; i < farm.Length; i++)
            //{
            //    if(farm[i] == CropState.None)
            //    {
            //        return i;
            //    }
            //}
            //return -1;
        }

        private int getNumberOfFreeCrops()
        {
            int nCrops = 0;

            for (int i = 0; i < farm.Length; i++)
            {
                bool isSpotFree = true;
                for (int j = 0; j < ownedHarvest.Count; j++)
                {
                    if (ownedHarvest[j].Contains(i))
                    {
                        isSpotFree = false;
                    }
                }
                if (isSpotFree)
                {
                    nCrops++;
                }
            }
            return nCrops;
            //for (int i = 0; i < farm.Length; i++)
            //{
            //    if (farm[i] == CropState.None)
            //    {
            //        nCrops++;
            //    }
            //}
            //return nCrops;
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
                return;
            }
            
            agentLastUpdateTimeStamp[agentID] = simulationTime;

            switch (envStep.getEffect())
            {
                case Effects.FoodConsumption:
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
                case Effects.PlantGrow:
                    farm[envStep.getPlantID()] = CropState.Plant;
                    //Console.WriteLine("plant grows");
                    break;
                case Effects.PlantHarvest:
                    harvestAgentCrop(agentID, envStep.getPlantID());
                    addFoodToAgent(agentID);
                    //Console.WriteLine("plant is harvested");
                    break;
                case Effects.RecreativeFinish:
                    endRecreative(agentID);
                    break;
                case Effects.PlantSeed:
                    plantCrop(agentID, envStep.getPlantID());
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

        private void endRecreative(int agentID)
        {
            updateAgentReward(agentID, setupVariables.rewardWorkShop);
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
            if(cooperativeLearning)
            {
                coopAgents.updateReward(agentID, reward);
            } else
            {
                agents[agentID].updateReward(reward);
            }
            metricsLogger.addRewardToMetrics(agentID, reward);
        }
    }

    
}
