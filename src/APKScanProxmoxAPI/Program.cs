using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APKScanSharedClasses;
using System.IO;
using Newtonsoft.Json;

namespace APKScanProxmoxAPI
{
    public class Program
    {
        //-------------------------------------------------------------------------------------------------------------------------------
        public static SharedConfiguration config { get; set; }
        //-------------------------------------------------------------------------------------------------------------------------------
        private static bool LoadConfiguration()
        {
            if (!File.Exists("configuration.json"))
                return false;
            string data = File.ReadAllText("configuration.json");
            config = JsonConvert.DeserializeObject<SharedConfiguration>(data);
            if (config == null)
                return false;
            return true;
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        public static void Main(string[] args)
        {
            if(!LoadConfiguration())
            {
                Console.WriteLine("Error while reading the configuration file!");
                return;
            }

            Console.ReadLine();
        }
        //-------------------------------------------------------------------------------------------------------------------------------
    }
}
