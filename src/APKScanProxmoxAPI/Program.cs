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
        private class test
        {
            public string keyboard { get; set; }
            public string release { get; set; }
            public string repoid { get; set; }
            public string version { get; set; }

        }
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
        private static string ExecuteCommand(ePVEAction action, string url, Dictionary<string,string> data = null)
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
            if(data != null)
                foreach(var command in data)
                    str_parameters += $" -{command.Key} {command.Value} ";

            //setup the process
            Process api = new Process();
            api.StartInfo.FileName = "pvesh";
            api.StartInfo.Arguments = str_action + url + str_parameters;
            api.StartInfo.UseShellExecute = false;
            api.StartInfo.RedirectStandardOutput = true;
            api.StartInfo.CreateNoWindow = true;
            //start the process
            api.Start();

            //check the read data if it's empty there was a error with the request
            string json_data = api.StandardOutput.ReadToEnd();
            if (json_data.Length == 0)
                return null;
            //works, does not show 200 OK
            //just reports the content
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
            string data = ExecuteCommand(ePVEAction.get, "/version");
            
            Console.ReadLine();
            Console.WriteLine("DATA: " + data);
            Console.ReadLine();

            test x = JsonConvert.DeserializeObject<test>(data);
            Console.WriteLine(x.keyboard);
            Console.WriteLine(x.release);
            Console.WriteLine(x.repoid);
            Console.WriteLine(x.version);

            data = ExecuteCommand(ePVEAction.get, "/xcvxcgsdf");
            if (data == null)
                Console.WriteLine("NO DATA");
        }
        //-------------------------------------------------------------------------------------------------------------------------------
    }
}
