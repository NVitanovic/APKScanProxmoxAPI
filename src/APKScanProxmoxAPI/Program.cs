using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APKScanSharedClasses;
using System.IO;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Diagnostics;

namespace APKScanProxmoxAPI
{
    public class Program
    {
        enum ePVEAction { get, create, delete};
        //-------------------------------------------------------------------------------------------------------------------------------
        public static   Configuration           config  { get; set; }
        private static  DataLayer               dl      = null;
        //-------------------------------------------------------------------------------------------------------------------------------
        private static bool LoadConfiguration()
        {
            if (!File.Exists("configuration.json"))
                return false;
            string data = File.ReadAllText("configuration.json");
            config = JsonConvert.DeserializeObject<Configuration>(data);
            if (config == null)
                return false;
            return true;
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        private static string ExecuteCommand(ePVEAction action, string url, Dictionary<string,string> data)
        {
            //apropriate action
            string str_action = "";
            switch(action)
            {
                case ePVEAction.get:
                    str_action = "get ";
                    break;
                case ePVEAction.create:
                    str_action = "create ";
                    break;
                case ePVEAction.delete:
                    str_action = "delete ";
                    break;
                default:
                    return null;
            }
            //build parameters
            string str_parameters = "";
            foreach(var command in data)
                str_parameters += $"-{command.Key} {command.Value} ";

            //setup the process
            Process api = new Process();
            api.StartInfo.FileName = "pvesh";
            api.StartInfo.Arguments = str_action + str_parameters;
            api.StartInfo.UseShellExecute = false;
            api.StartInfo.RedirectStandardOutput = true;
            api.StartInfo.CreateNoWindow = true;
            //start the process
            api.Start();

            //check if the first line is valid or not
            string first_line = api.StandardOutput.ReadLine();
            if (first_line.IndexOf("200 OK") != -1)
                return null;

            //read until the end of data
            string json_data = "";
            while(!api.StandardOutput.EndOfStream)
                json_data = api.StandardOutput.ReadLine();

            //everything is read sucesfully
            return json_data;
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        private static void RedisReader(RedisChannel channel, RedisValue value)
        {
            while(true)
            {
                string data = dl.redis.ListRightPop(config.message_channel);
                if (data == null)
                    continue;
                //TODO: Do action depending on the task

            }
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        public static void Main(string[] args)
        {
            //Load the configuration
            if(!LoadConfiguration())
            {
                Console.WriteLine("Error while reading the configuration file!");
                return;
            }
            //init DB Connection
            dl = DataLayer.getInstance();

            //init subscriber and subscribe to the channel
            ISubscriber sub = dl.redisCluster.GetSubscriber();
            sub.Subscribe(config.message_channel, RedisReader);

            //Execute the test command
            Console.ReadLine();
        }
        //-------------------------------------------------------------------------------------------------------------------------------
    }
}
