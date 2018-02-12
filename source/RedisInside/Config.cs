using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace RedisInside
{
    internal class Config : IConfig
    {
        private static readonly Random random = new Random();

        private static readonly ConcurrentDictionary<int, byte> usedPorts = new ConcurrentDictionary<int, byte>();

        public Config()
        {
            do
            {
                SelectedPort = random.Next(49152, 65535 + 1);
            }
            while (usedPorts.ContainsKey(SelectedPort));

            usedPorts.AddOrUpdate(SelectedPort, i => byte.MinValue, (i, b) => byte.MinValue);
            Logger = message => Debug.WriteLine(message);
        }

        public Action<string> Logger { get; private set; }

        public int SelectedPort { get; private set; }

        public bool IsWithPersistence { get; private set; }

        public string PersistenceFile { get; private set; }

        public string Persistence => !IsWithPersistence ? "persistence-available no" : $"dbfilename {PersistenceFile}";

        public IConfig WithPersistence(string fileName = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = Guid.NewGuid().ToString("N") + ".db";
            }

            PersistenceFile = fileName;
            IsWithPersistence = true;
            return this;
        }

        public IConfig LogTo(Action<string> logFunction)
        {
            Logger = logFunction;
            return this;
        }

        public IConfig Port(int portNumber)
        {
            SelectedPort = portNumber;
            return this;
        }
    }
}
