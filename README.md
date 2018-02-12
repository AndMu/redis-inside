![](https://raw.githubusercontent.com/poulfoged/redis-inside/master/icon.png) &nbsp; ![](http://img.shields.io/nuget/v/Wikiled.Redis.Inside.svg?style=flat)
# .NET STANDARD Redis-Inside Port from (https://github.com/poulfoged/redis-inside)

Run integration tests against Redis without having to start/install an instance.


Redis inside works by extracting the Redis executable to a temporary location and executing it. Internally it uses [Redis for windows](https://github.com/MSOpenTech/redis) ported by [MS Open Tech](https://msopentech.com/opentech-projects/redis).


## How to
Launch a Redis instance from just by creating a new instance of Redis. After that the node name and port can be accessed from the node-property:

```c#
using (var redis = new Redis())
{
    // connect to redis.Endpoint here
}

```

Use external IP - useful if you test replication (Setups firewall)
```c#
using (var redis = new Redis(item => item.UseExternalIp()))
{
    // connect to redis.Endpoint here
}

```

Each instance will run on a random port so you can even run multiple instances:

```c#
using (var redis1 = new Redis())
using (var redis2 = new Redis())
{
    // connect to two nodes here
}

```
## Install

Simply add the Nuget package:

`PM> Install-Package redis-inside`

## Requirements

You'll need .NET STANDARD 1.6 or later on 64 bit Windows to use the precompiled binaries.


