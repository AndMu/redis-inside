using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace RedisInside
{
    internal class Config : IConfig
    {
        private static readonly Random random = new Random();

        private static readonly ConcurrentDictionary<int, byte> usedPorts = new ConcurrentDictionary<int, byte>();

        internal Action<string> logger;

        internal int port;

        public Config()
        {
            do
            {
                port = random.Next(49152, 65535 + 1);
            }
            while (usedPorts.ContainsKey(port));

            usedPorts.AddOrUpdate(port, i => byte.MinValue, (i, b) => byte.MinValue);
            logger = message => Debug.WriteLine(message);
        }

        public IConfig LogTo(Action<string> logFunction)
        {
            logger = logFunction;
            return this;
        }

        public IConfig Port(int portNumber)
        {
            port = portNumber;
            return this;
        }
    }
}
