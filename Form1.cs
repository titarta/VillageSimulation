using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Simulation
{
    public partial class Form1 : Form
    {
        string setupFilePath;
        SimulationWrapper sw;
        bool agentsLoaded = false;
        private CooperativeAgents agentsCoop;
        private Dictionary<int, Agent> agentsInd;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(agentsLoaded)
            {
                if(checkBox2.Checked)
                {
                    sw = new SimulationWrapper(setupFilePath, "single", textBox1.Text, textBox3.Text, textBox4.Text, checkBox1.Checked, agentsCoop);
                } else
                {
                    sw = new SimulationWrapper(setupFilePath, "single", textBox1.Text, textBox3.Text, textBox4.Text, checkBox1.Checked, agentsInd);
                }
            } else
            {
                sw = new SimulationWrapper(setupFilePath, "single", textBox1.Text, textBox3.Text, textBox4.Text, checkBox2.Checked, checkBox1.Checked);
            }
            
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void openFileDialog2_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            openFileDialog2.InitialDirectory = @"C:\Users\trvca\Desktop\VillageSimulation\Setup Files";
            openFileDialog2.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog2.FilterIndex = 2;
            openFileDialog2.RestoreDirectory = true;

            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                
                setupFilePath = openFileDialog2.FileName;
                textBox2.Text = setupFilePath;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (agentsLoaded)
            {
                if (checkBox2.Checked)
                {
                    sw = new SimulationWrapper(setupFilePath, "multiple", textBox1.Text, textBox3.Text, textBox4.Text, checkBox1.Checked, agentsCoop);
                }
                else
                {
                    sw = new SimulationWrapper(setupFilePath, "multiple", textBox1.Text, textBox3.Text, textBox4.Text, checkBox1.Checked, agentsInd);
                }
            }
            else
            {
                sw = new SimulationWrapper(setupFilePath, "multiple", textBox1.Text, textBox3.Text, textBox4.Text, checkBox2.Checked, checkBox1.Checked);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        //stop learning button
        private void button4_Click(object sender, EventArgs e)
        {
            sw.endSimulation();
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            string agentsPath = @"C:\Users\trvca\Desktop\VillageSimulation\Outputs\Agents\";
            BinaryFormatter b = new BinaryFormatter();
            if (checkBox2.Checked)
            {
                string agentFile = agentsPath + textBox4.Text + ".dat";
                FileStream f = File.OpenRead(agentFile);
                agentsCoop = (CooperativeAgents)b.Deserialize(f);
            } else
            {
                agentsInd = new Dictionary<int, Agent>();
                for(int i = 0; i < 4; i++)
                {
                    string agentFile = agentsPath + textBox4.Text + i + ".dat";
                    FileStream f = File.OpenRead(agentFile);
                    Agent a = (Agent)b.Deserialize(f);
                    agentsInd.Add(i, a);
                }
            }

            agentsLoaded = true;
        }
    }
}
