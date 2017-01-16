using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APKScanSharedClasses;
namespace APKScanProxmoxAPI
{
    public class Configuration : SharedConfiguration
    {
        public List<string> authorized { get; set; }
        public string message_channel { get; set; }
    }
}
