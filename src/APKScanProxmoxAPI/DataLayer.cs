using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APKScanSharedClasses;
namespace APKScanProxmoxAPI
{
    public class DataLayer
    {
        public ConnectionMultiplexer redisCluster = null;
        public IDatabase redis = null;
        public static DataLayer singleton = null;
        private static object lockObj = new object();
        public static DataLayer getInstance()
        {
            if (singleton == null)
            {
                lock (lockObj)
                {
                    if (singleton == null)
                        return singleton = new DataLayer(Program.config);
                    else
                        return singleton;
                }
            }
            else
                return singleton;
        }
        private DataLayer(SharedConfiguration config)
        {
            if (redisCluster == null)
            {
                string redisConfig = "";

                for (int i = 0; i < config.redis.masters.Count; i++)
                    redisConfig += config.redis.masters[i] + ",";
                for (int i = 0; i < config.redis.slaves.Count; i++)
                    if (i == config.redis.slaves.Count - 1)
                        redisConfig += config.redis.slaves[i];
                    else
                        redisConfig += config.redis.slaves[i] + ",";

                redisCluster = ConnectionMultiplexer.Connect(redisConfig);

                if (redis == null)
                    redis = redisCluster.GetDatabase();


                //privremeno pokrecemo testSubscribe
                //testSubscribe();
            }
        }
        /*public void testSubscribe()
        {
            ISubscriber sub = redisCluster.GetSubscriber();
            sub.Subscribe("receive", (channel, message) =>
            {
                while (true)
                {
                    string work = redis.ListRightPop("receive");
                    if (work != null)
                    {
                        Console.WriteLine("Subscrb: " + (string)work);
                    }
                }
            });
       }*/
    }
}
