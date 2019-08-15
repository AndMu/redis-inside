using NUnit.Framework;
using StackExchange.Redis;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RedisInside.Tests
{
    [TestFixture]
    public class RedisTests
    {
        [Test]
        public void CanConfigure()
        {
            using (var redis = new Redis(i => i.Port(1234).LogTo(message => Trace.WriteLine(message))))
            {
                Assert.That(redis.Endpoint.ToString().EndsWith("1234"));
            }
        }

        [Test]
        public void CanStart()
        {
            using (var redis = new Redis())
            using (var client = ConnectionMultiplexer.Connect(GetConfiguration(redis)))
            {
                client.GetDatabase().StringSet("key", "value");
                var value = client.GetDatabase().StringGet("key");
                Assert.That(value.ToString(), Is.EqualTo("value"));
            }
        }

        [TestCase(null)]
        [TestCase("dump1")]
        public void CanStartWithPersistence(string fileName)
        {
            using (var redis = new Redis(i => i.WithPersistence(fileName)))
            using (var client = ConnectionMultiplexer.Connect(GetConfiguration(redis)))
            {
                client.GetDatabase().StringSet("key", "value");
                var value = client.GetDatabase().StringGet("key");
                Assert.That(value.ToString(), Is.EqualTo("value"));
            }
        }

        [Test]
        public void CanStartMultiple()
        {
            using (var redis = new Redis())
            using (var redis2 = new Redis())
            using (var client = ConnectionMultiplexer.Connect(GetConfiguration(redis, redis2)))
            {
                client.GetDatabase().StringSet("key", "value");
                var value = client.GetDatabase().StringGet("key");
                Assert.That(value.ToString(), Is.EqualTo("value"));
            }
        }

        [Test]
        public async Task CanStartSlave()
        {
            using (var redis = new Redis())
            using (var redis2 = new Redis())
            {
                ////Arrange
                // configure slave
                using (var client = ConnectionMultiplexer.Connect(GetConfiguration(redis, redis2)))
                {
                    await client.GetServer(redis.Endpoint).SlaveOfAsync(redis2.Endpoint).ConfigureAwait(false);
                }

                // new single-node client
                string actualValue;
                using (var client = ConnectionMultiplexer.Connect(GetConfiguration(redis2)))
                {
                    await client.GetDatabase().StringSetAsync("key", "value").ConfigureAwait(false);
                    actualValue = await client.GetDatabase().StringGetAsync("key").ConfigureAwait(false);
                }

                Assert.That(actualValue, Is.EqualTo("value"));
            }
        }

        private ConfigurationOptions GetConfiguration(params Redis[] redis)
        {
            var config = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                AllowAdmin = true,
                KeepAlive = 60,
                ConnectTimeout = 5000,
                SyncTimeout = 5000,
            };

            foreach (var item in redis)
            {
                config.EndPoints.Add(item.Endpoint);
            }

            return config;
        }
    }
}