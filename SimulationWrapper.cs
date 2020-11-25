using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    class SimulationWrapper
    {
        private Dictionary<int, Agent> agents;
        public SimulationWrapper(string definitionsPath, string singleOrMultiple)
        {
            agents = new Dictionary<int, Agent>()
            if (singleOrMultiple == "single")
            {
                SimulationWrapperSingle(definitionsPath);
            } else
            {
                SimulationWrapperMultiple(definitionsPath);
            }
        }

        

        private void SimulationWrapperSingle(string definitionsPath)
        {

        }

        private void SimulationWrapperMultiple(string definitionsPath)
        {
            int limitEnvironments = -1;
            int numberAgents = 4;
            for(int i = 0; i < numberAgents; i++)
            {
                agents.Add(i, new RandomAgent());
            }

        }
    }
}
