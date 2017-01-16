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
        private static bool RollbackSnapshot(string vmid, string name = "normal")
        {
            if (ExecuteCommand(ePVEAction.create, $"/nodes/fr1pve/qemu/{vmid}/snapshot/{name}/rollback") != null)
                return true;
            return false;
        }
        private static bool CreateNewSnapshot(string vmid, string name = "normal")
        {
            //first rollback the last snapshot
            if (ExecuteCommand(ePVEAction.create, $"/nodes/fr1pve/qemu/{vmid}/snapshot/{name}/rollback") == null)
                return false;
            //second    reset the VM
            if (ExecuteCommand(ePVEAction.create, $"/nodes/fr1pve/qemu/{vmid}/status/reset") == null)
                return false;
            //wait for the reboot

            //third     remove the normal snapshot
            //fourth    create a new normal snapshot 
            return true;
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        private static void RedisReader(RedisChannel channel, RedisValue value)
        {
            string data = dl.redis.ListRightPop(config.message_channel);
            if (data != null)
            {
                Console.WriteLine("RUN:" + value + " DATA: " + data);
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
            string data = ExecuteCommand(ePVEAction.get, args[0]);
            
            Console.ReadLine();
        }
        //-------------------------------------------------------------------------------------------------------------------------------
    }
}
