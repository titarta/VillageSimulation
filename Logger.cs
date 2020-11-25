using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    class Logger
    {
        private string filePath;
        private StreamWriter logFile;

        public Logger(string filePath)
        {
            this.filePath = filePath;
            logFile = new StreamWriter(filePath);
        }

        public void logEnvStep(EnvironmentStep es)
        {
            string line = "Environment Step: ";
            line += es.getTimeStamp() + ";";
            line += es.getAgentID() + ";";
            line += es.getEffect().ToString() + ";";
            line += (es.getPlantID() != -1) ? (es.getPlantID() + ";") : "";
            logFile.WriteLine(line);
        }

        public void logMovement()
        {

        }

        public void terminateLogging()
        {
            logFile.Close();
        }
    }
}
