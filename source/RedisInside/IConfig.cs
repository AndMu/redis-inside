using System;

namespace RedisInside
{
    public interface IConfig
    {
        IConfig Port(int portNumber);

        IConfig KillAll();

        IConfig DisableCheck();

        IConfig LogTo(Action<string> logFunction);

        IConfig WithPersistence(string fileName = null);

        IConfig WithLocation(string directory = null, bool randomName = false);
    }
}