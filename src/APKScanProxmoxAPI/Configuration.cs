using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APKScanSharedClasses;
namespace APKScanProxmoxAPI
{
    public class Configuration : SharedConfiguration
    {
        public Dictionary<string, string> authorized { get; set; }
        public string message_channel { get; set; }
        public int vm_restart_time_in_seconds { get; set; }
    }
}
