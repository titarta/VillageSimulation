using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    public enum Actions { Plant=0, Harvest=1, Recreative=2, Eat=3};
    public enum Effects { PlantGrow=0, PlantSeed=1, ChangePositionToFarm=2, ChangePositionToWorkshop=3, PlantHarvest=4, FoodConsumption=5, RecreativeFinish=6}
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
        private CropState[] farm;
        private List<EnvironmentStep> stepsQueue;
        private Dictionary<int, Agent> agents;
        private Dictionary<int, double> agentLastUpdateTimeStamp;
        private Dictionary<int, bool> agentsDeciding;
        private bool condition;
        private double simulationTime;
        private double timeToGrow = 20;
        private double harvestTime = 0;
        private double plantTime = 0;
        private int recreativeTime = 10;

        public Environment(int farmSize, int numAgents)
        {
            simulationTime = 0;
            farm = new CropState[farmSize];
            for(int i = 0; i < farmSize; i++)
            {
                farm[i] = CropState.None;
            }

            //create agents
            agents = new Dictionary<int, Agent>();
            agentsDeciding = new Dictionary<int, bool>();
            agentLastUpdateTimeStamp = new Dictionary<int, double>();
            for (int i = 0; i < numAgents; i++)
            {
                agents.Add(i, new RandomAgent());
                agentLastUpdateTimeStamp.Add(i, simulationTime);
                agentsDeciding.Add(i, true);
            }
            
            condition = true;
            stepsQueue = new List<EnvironmentStep>();

            updateEnvironment();
        }

        private void updateEnvironment()
        {
            while(condition)
            {
                bool agentsDecided = true; ;
                foreach(bool d in agentsDeciding.Values)
                {
                    if(d)
                    {
                        agentsDecided = false;
                    }
                }

                if(agentsDecided)
                {
                    updateStep();
                } else
                {
                    for(int agentID = 0; agentID < agents.Count; agentID++)
                    {
                        if(agentsDeciding[agentID])
                        {
                            Actions actionPerformed = agents[agentID].decide(); //get action made by agent (will get a state here)

                            publishAction(actionPerformed, agentID);

                            agentsDeciding[agentID] = false;
                            
                        }
                    }
                }

            }
        }
    
    
        private void publishAction(Actions action, int agentID)
        {
            AgentPosition agentPos = agents[agentID].GetAgentPosition();

            AgentPosition actionPos = getActionPosition(action, agentPos);

            //List<Effect> effects = getEffectsFromActions(action, agentID);

            double movementTime = getTimeFromPosToPos(agentPos, actionPos);

            //create envSteps from effects
            int plantPosition;
            switch (action)
            {
                case Actions.Eat:
                    stepsQueue.Add(new EnvironmentStep(simulationTime, agentID, Effects.FoodConsumption));
                    break;
                case Actions.Harvest:
                    plantPosition = getPlantToHarvest(agentID);
                    if (plantPosition == -1)
                    {
                        stepsQueue.Sort();
                        return;
                    }
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime, agentID, Effects.ChangePositionToFarm));
                    stepsQueue.Add(new EnvironmentStep(simulationTime + movementTime + harvestTime, agentID, Effects.PlantHarvest, plantPosition));
                    break;
                case Actions.Plant:
                    plantPosition = getFreeSpaceInFarm();
                    if (plantPosition == -1)
                    {
                        stepsQueue.Sort();
                        return;
                    }
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

            stepsQueue.Sort();



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
            if(pos1 == AgentPosition.Center)
            {
                switch (pos2)
                {
                    case AgentPosition.Farm:
                        return 5;
                    case AgentPosition.WorkShop:
                        return 5;
                }
            }
            if(pos1 == AgentPosition.Farm)
            {
                switch(pos2)
                {
                    case AgentPosition.Center:
                        return 5;
                    case AgentPosition.WorkShop:
                        return 10;
                }
            }
            if(pos1 == AgentPosition.WorkShop)
            {
                switch(pos2)
                {
                    case AgentPosition.Center:
                        return 5;
                    case AgentPosition.Farm:
                        return 10;
                }
            }
            throw new Exception();
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
            List<int> possiblePlants = agents[agentID].getOwnedHarvest();

            foreach(int plantID in possiblePlants)
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
                if(farm[i] == CropState.None)
                {
                    return i;
                }
            }
            return -1;
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


            //update do estado do jogo
            try
            {
                agents[envStep.getAgentID()].loseSaturationByTime(simulationTime - agentLastUpdateTimeStamp[envStep.getAgentID()]);
            } catch(AgentDiesException e)
            {
                Console.WriteLine("Agent should be dead");
            }
            
            agentLastUpdateTimeStamp[envStep.getAgentID()] = simulationTime;

            switch (envStep.getEffect())
            {
                case Effects.FoodConsumption:
                    try
                    {
                        agents[envStep.getAgentID()].consumeFood();
                    } catch (NoFoodToEatException e)
                    {
                        Console.WriteLine("Agent cannot eat");
                    }
                    Console.WriteLine("agent eated");
                    break;
                case Effects.ChangePositionToFarm:
                    agents[envStep.getAgentID()].updatePosition(AgentPosition.Farm);
                    Console.WriteLine("agent went to farm");
                    break;
                case Effects.ChangePositionToWorkshop:
                    agents[envStep.getAgentID()].updatePosition(AgentPosition.WorkShop);
                    Console.WriteLine("agent went to workshop");
                    break;
                case Effects.PlantGrow:
                    farm[envStep.getPlantID()] = CropState.Plant;
                    Console.WriteLine("plant grows");
                    break;
                case Effects.PlantHarvest:
                    farm[envStep.getPlantID()] = CropState.None;
                    agents[envStep.getAgentID()].removePlant(envStep.getPlantID());
                    agents[envStep.getAgentID()].getFood();
                    Console.WriteLine("plant is harvested");
                    break;
                case Effects.RecreativeFinish:
                    break;
                case Effects.PlantSeed:
                    farm[envStep.getPlantID()] = CropState.Seed;
                    agents[envStep.getAgentID()].addPlantToHarvest(envStep.getPlantID());
                    Console.WriteLine("seed is planted");
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
            EnvironmentState state = new EnvironmentState();
            state.freeCrops = getFreeSpaceInFarm();


            return state;
        }

    }

    
}
