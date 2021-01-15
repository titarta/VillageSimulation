using ConvnetSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    [Serializable]
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

        public void setParameter(int position, double value)
        {
            switch(position)
            {
                case 0:
                    this.freeCrops = (int)value;
                    break;
                case 1:
                    this.cropsToCultivate = (int)value;
                    break;
                case 2:
                    this.cropsGrowing = (int)value;
                    break;
                case 3:
                    this.position = (int)value;
                    break;
                case 4:
                    this.saturation = value;
                    break;
                case 5:
                    this.foodReserved = value;
                    break;
                default:
                    break;

            }
        }

        public int getNumberInputs()
        {
            return 6;
        }

        public double[] toDoubleArray()
        {
            return new double[] { freeCrops, cropsToCultivate, cropsGrowing, position, saturation, foodReserved };

        }

        public Volume toVolume()
        {
            Volume ret = new Volume(1, 1, getNumberInputs());
            ret.w = toDoubleArray();
            return ret;
        }
    }

    [Serializable]
    class TabularStateConfig
    {
        private int[] freecropsLimits; //none, some, many
        private int[] cropsToCultivateLimits; //none, some, many
        private int[] cropsGrowingLimits; //none, some, many
        private double[] saturationLimits; //very low, low, ok, high
        private int[] foodReservedLimits;//none, some, many

        public TabularStateConfig()
        {
            freecropsLimits = new int[]{ 0, 2, int.MaxValue};
            cropsToCultivateLimits = new int[] { 0, 1, int.MaxValue };
            cropsGrowingLimits = new int[] { 0, 1, int.MaxValue };
            saturationLimits = new double[] { 2, 4, 10, double.MaxValue };
            foodReservedLimits = new int[] { 0, 1, int.MaxValue };
        }

        public TabularState GetTabularStateFromEnvState(EnvironmentState envState)
        {
            TabularState ret = new TabularState();
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
    
        public int getStateSize()
        {
            return 3 * 3 * 3 * 2 * 4 * 4;
        }
    }

    [Serializable]
    class TabularState
    {
        public int freeCrops; //none, some, many
        public int cropsToCultivate; //none, some, many
        public int cropsGrowing; //none, some, many
        public int position; //[0-1]
        public int saturation; //very low, low, ok, high
        public int foodReserved;//very low, low, ok, high

        public TabularState(int freeCrops, int cropsToCultivate, int cropsGrowing, int position, int saturation, int foodReserved)
        {
            this.freeCrops = freeCrops;
            this.cropsToCultivate = cropsToCultivate;
            this.cropsGrowing = cropsGrowing;
            this.position = position;
            this.saturation = saturation;
            this.foodReserved = foodReserved;
        }

        public TabularState()
        {
            freeCrops = 0;
            cropsToCultivate = 0;
            cropsGrowing = 0;
            position = 0;
            saturation = 0;
            foodReserved = 0;
        }

        public static int getNumberOfStates()
        {
            return 3 * 3 * 3 * 2 * 4 * 4;
        }

        public int getIntState()
        {
            return freeCrops + (3 * cropsToCultivate) + (9 * cropsGrowing) + (27 * position) + (54 * saturation) + (216 * foodReserved);
        }
    }
    
    [Serializable]
    abstract class ExplorationPolicy
    {
        public ExplorationPolicy() { }

        public abstract Actions decideAction(double[] stateQvalues);

    }

    [Serializable]
    class GreedyExploration : ExplorationPolicy
    {
        public GreedyExploration() { }

        public override Actions decideAction(double[] stateQvalues)
        {
            double maxQValue = Double.MinValue;
            Actions maxQvalueAction = 0;
            for(int i = 0; i < stateQvalues.Length; i++)
            {
                if(stateQvalues[i] > maxQValue)
                {
                    maxQValue = stateQvalues[i];
                    maxQvalueAction = (Actions)i;
                }
            }
            return maxQvalueAction;
        }
    }

    [Serializable]
    class EGreedyExploration : ExplorationPolicy
    {
        private double epsilon;
        private double epsilonDecay;
        private Random rd;

        public EGreedyExploration(double epsilonDecay)
        {
            this.epsilonDecay = epsilonDecay;
            epsilon = 1;
            rd = new Random((int)DateTime.Now.Ticks);
        }

        public override Actions decideAction(double[] stateQvalues)
        {
            if(rd.NextDouble() > epsilon)
            {
                double maxQValue = Double.MinValue;
                Actions maxQvalueAction = 0;
                for (int i = 0; i < stateQvalues.Length; i++)
                {
                    if (stateQvalues[i] > maxQValue)
                    {
                        maxQValue = stateQvalues[i];
                        maxQvalueAction = (Actions)i;
                    }
                }
                epsilon = Math.Max(0, epsilon - epsilonDecay);
                return maxQvalueAction;
            } else
            {
                epsilon = Math.Max(0, epsilon - epsilonDecay);
                return (Actions)rd.Next(stateQvalues.Length);
            }

            
        }
    }


}
