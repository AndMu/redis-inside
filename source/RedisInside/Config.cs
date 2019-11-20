using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Wikiled.Common.Extensions;

namespace RedisInside
{
    internal class Config : IConfig
    {
        private static readonly Random random = new Random();

        private static readonly ConcurrentDictionary<int, byte> usedPorts = new ConcurrentDictionary<int, byte>();

        private string location;

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

        public string Location
        {
            get => location ?? Path.GetTempPath();
            private set => location = value;
        }

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

        public IConfig WithLocation(string directory = null, bool randomName = true)
        {
            if (!string.IsNullOrEmpty(directory))
            {
                directory = Path.Combine(directory, "RedisInside", randomName ? DateTime.UtcNow.Ticks.ToString() : "Server");
                directory.EnsureDirectoryExistence();
            }

            Location = directory;
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
