using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APKScanSharedClasses;
using System.IO;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace APKScanProxmoxAPI
{
    public class Program
    {
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
            
            //init subscriber

            Console.ReadLine();
        }
        //-------------------------------------------------------------------------------------------------------------------------------
    }
}
