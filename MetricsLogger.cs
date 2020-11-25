using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    class MetricsLogger
    {
        private StreamWriter logFile;

        public MetricsLogger(string filePath)
        {
            logFile = new StreamWriter(filePath);
        }

        public void logAgentDeath(int agentID, double timeStamp)
        {
            logFile.WriteLine("Agent death: " + agentID + ";" + timeStamp);
        }

        public void closeFile()
        {
            logFile.Close();
        }
    }
}
