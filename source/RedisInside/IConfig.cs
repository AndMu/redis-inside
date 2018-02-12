using System;

namespace RedisInside
{
    public interface IConfig
    {
        bool IsExternalIp { get; }

        IConfig UseExternalIp();

        IConfig Port(int portNumber);

        IConfig LogTo(Action<string> logFunction);

        IConfig WithPersistence(string fileName = null);
    }
}