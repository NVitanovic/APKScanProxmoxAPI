﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APKScanSharedClasses;
using System.IO;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Diagnostics;
using System.Threading;

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
        //-------------------------------------------------------------------------------------------------------------------------------
        private static bool RollbackSnapshot(string vmid, string name = "normal")
        {
            DateTime datum = DateTime.UtcNow;
            Console.WriteLine($"[{datum}] Starting the rollback of the {vmid} VM from {name} snapshot...");
            if (ExecuteCommand(ePVEAction.create, $"/nodes/fr1pve/qemu/{vmid}/snapshot/{name}/rollback") != null)
            {
                Console.WriteLine($"[{datum}] Rollback of the {vmid} VM from {name} snapshot finished!");
                return true;
            }
            Console.WriteLine($"[{datum}] Rollback of the {vmid} VM from {name} snapshot FAILED!");
            return false;
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        private static bool CreateNewSnapshot(string vmid, string name = "normal")
        {
            DateTime datum = DateTime.UtcNow;
            //first rollback the last snapshot
            Console.WriteLine($"[{datum}] Starting the rollback of the {vmid} VM from {name} snapshot...");
            if (ExecuteCommand(ePVEAction.create, $"/nodes/fr1pve/qemu/{vmid}/snapshot/{name}/rollback") == null)
                return false;
            Console.WriteLine($"[{datum}] Restarting the VM {vmid}...");
            //second    reset the VM
            if (ExecuteCommand(ePVEAction.create, $"/nodes/fr1pve/qemu/{vmid}/status/reset") == null)
                return false;
            //wait for the reboot
            Console.WriteLine($"[{datum}] Restarting of {vmid} complete, waiting {config.vm_restart_time_in_seconds} seconds for boot...");
            Thread.Sleep(config.vm_restart_time_in_seconds*1000);
            //third     remove the normal snapshot
            Console.WriteLine($"[{datum}] Removing from {vmid} last good snapshot {name}...");
            if (ExecuteCommand(ePVEAction.delete, $"/nodes/fr1pve/qemu/{vmid}/snapshot/{name}") == null)
                return false;
            //fourth    create a new normal snapshot 
            Console.WriteLine($"[{datum}] Creating a new snapshot from VM {vmid} with name {name}...");
            Dictionary<string, string> data = new Dictionary<string, string>();
            data["snapname"] = "normal";
            data["vmstate"] = "true";
            if (ExecuteCommand(ePVEAction.create, $"/nodes/fr1pve/qemu/{vmid}/snapshot/{name}", data) == null)
                return false;
            Console.WriteLine($"[{datum}] New snapshot for {vmid} created with name {name}!");
            return true;
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        private static bool CheckAuth(string master_id, string password)
        {
            if (config.authorized.ContainsKey(master_id))
                if (config.authorized[master_id] == password)
                    return true;
            return false;
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        private static void RedisReader(RedisChannel channel, RedisValue value)
        {
            string data = dl.redis.ListRightPop(config.message_channel);
            if (data != null)
            {
                Console.WriteLine("RUN:" + value + " DATA: " + data);
                RedisProxmox res = JsonConvert.DeserializeObject<RedisProxmox>(data);
                if(res != null)
                {
                    //check if the user is with valid credentials
                    if(CheckAuth(res.master_id,res.auth))
                    {
                        //create the response object
                        RedisProxmoxResult returnResult = new RedisProxmoxResult();
                        returnResult.master_id  = res.master_id;
                        returnResult.lastRun    = DateTime.UtcNow;
                        returnResult.lastTask   = res.task;
                        returnResult.status     = false;

                        //tasks start
                        if (res.task == eTask.rollbackSnapshot)
                        {
                            //if the rollback is succesfull
                            if (RollbackSnapshot(res.vm_id))
                                returnResult.status = true;
                        }
                        else if(res.task == eTask.crateSnapshot)
                        {
                            if (CreateNewSnapshot(res.vm_id))
                                returnResult.status = true;
                        }
                        //tasks end
                        
                        //serialize the result
                        string rdata = JsonConvert.SerializeObject(returnResult);

                        //add the result value to redis and set its ttl to 1 minute
                        dl.redis.StringSet("proxmox:" + res.master_id, rdata); 
                        dl.redis.KeyExpire("proxmox:" + res.master_id, TimeSpan.FromMinutes(1)); //one minute TODO
                    }
                }
                else
                {
                    Console.WriteLine("Error while DeSerialization of object!");
                }
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        public static void Main(string[] args)
        {
            //Show the app was started
            DateTime datum = DateTime.UtcNow;
            Console.WriteLine($"[{datum}] APKScanProxmoxAPI started!");

            //Load the configuration
            if (!LoadConfiguration())
            {
                Console.WriteLine("Error while reading the configuration file!");
                return;
            }
            //init DB Connection
            dl = DataLayer.getInstance();

            //init subscriber and subscribe to the channel
            ISubscriber sub = dl.redisCluster.GetSubscriber();
            sub.Subscribe(config.message_channel, RedisReader);

            

            while (true)
            {
                datum = DateTime.UtcNow;
                Console.WriteLine($"[{datum}] Still alive!");
                Thread.Sleep(30000);
            } 
        }
        //-------------------------------------------------------------------------------------------------------------------------------
    }
}
