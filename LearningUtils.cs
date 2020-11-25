using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    class EnvironmentState
    {
        public int freeCrops; //[0-N]
        public int cropsToCultivate; //[0-N]
        public int cropsGrowing; //[0-N]
        public int position; //[0-2]
        public double saturation; //[1-inf]
        public double foodReserved;//[1-inf]

        public EnvironmentState()
        {
            freeCrops = 0;
            cropsToCultivate = 0;
            cropsGrowing = 0;
            position = 0;
            saturation = 0;
            foodReserved = 0;
        }

        public EnvironmentState(int freeCrops, int cropsToCultivate, int cropsGrowing, int position, double saturation, double foodReserved)
        {
            this.freeCrops = freeCrops;
            this.cropsToCultivate = cropsToCultivate;
            this.cropsGrowing = cropsGrowing;
            this.position = position;
            this.saturation = saturation;
            this.foodReserved = foodReserved;
        }
    }

    class QLearningStateConfig
    {
        private int[] freecropsLimits; //none, some, many
        private int[] cropsToCultivateLimits; //none, some, many
        private int[] cropsGrowingLimits; //none, some, many
        private double[] saturationLimits; //very low, low, ok, high
        private double[] foodReservedLimits;//very low, low, ok, high

        public QLearningStateConfig()
        {

        }

        public QLearningState GetQLearningStateFromEnvState(EnvironmentState envState)
        {
            QLearningState ret = new QLearningState();
            for(int i = 0; i < freecropsLimits.Length; i++)
            {
                if(envState.freeCrops <= freecropsLimits[i])
                {
                    ret.freeCrops = i;
                    break;
                }
            }
            for (int i = 0; i < cropsToCultivateLimits.Length; i++)
            {
                if (envState.cropsToCultivate <= cropsToCultivateLimits[i])
                {
                    ret.cropsToCultivate = i;
                    break;
                }
            }
            for (int i = 0; i < cropsGrowingLimits.Length; i++)
            {
                if (envState.cropsGrowing <= cropsGrowingLimits[i])
                {
                    ret.cropsGrowing = i;
                    break;
                }
            }
            ret.position = envState.position;
            for (int i = 0; i < saturationLimits.Length; i++)
            {
                if (envState.saturation <= saturationLimits[i])
                {
                    ret.saturation = i;
                    break;
                }
            }
            for (int i = 0; i < foodReservedLimits.Length; i++)
            {
                if (envState.foodReserved <= foodReservedLimits[i])
                {
                    ret.foodReserved = i;
                    break;
                }
            }

            return ret;
        }
    }

    class QLearningState
    {
        public int freeCrops; //none, some, many
        public int cropsToCultivate; //none, some, many
        public int cropsGrowing; //none, some, many
        public int position; //[0-1]
        public int saturation; //very low, low, ok, high
        public int foodReserved;//very low, low, ok, high

        public QLearningState(int freeCrops, int cropsToCultivate, int cropsGrowing, int position, int saturation, int foodReserved)
        {
            this.freeCrops = freeCrops;
            this.cropsToCultivate = cropsToCultivate;
            this.cropsGrowing = cropsGrowing;
            this.position = position;
            this.saturation = saturation;
            this.foodReserved = foodReserved;
        }

        public QLearningState()
        {
            freeCrops = 0;
            cropsToCultivate = 0;
            cropsGrowing = 0;
            position = 0;
            saturation = 0;
            foodReserved = 0;
        }
    }
    abstract class ExplorationPolicy
    {
        public ExplorationPolicy() { }

        public abstract Actions decideAction(QLearningState state);

    }


}
