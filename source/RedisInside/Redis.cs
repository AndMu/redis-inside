using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using Polly;
using StackExchange.Redis;

namespace RedisInside
{
    /// <summary>
    ///     Run integration-tests against Redis
    /// </summary>
    public class Redis : IDisposable
    {
        private readonly Config config = new Config();

        private readonly TemporaryFile executable;

        private readonly Process process;

        private bool disposed;

        private ConnectionMultiplexer multiplexer;

        public Redis(Action<IConfig> configuration = null)
        {
            configuration?.Invoke(config);

            executable = new TemporaryFile(
                GetType().GetTypeInfo().Assembly.GetManifestResourceStream("RedisInside.Executables.redis-server.exe"),
                config.Location,
                "exe");

            var processStartInfo = new ProcessStartInfo(" \"" + executable.Info.FullName + " \"")
            {
                UseShellExecute = false,
                Arguments = $"--port {config.SelectedPort} --{config.Persistence}",
                CreateNoWindow = true,
                LoadUserProfile = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.ASCII
            };

            Kill();
            process = Process.Start(processStartInfo);
            process.ErrorDataReceived += (sender, args) => Log(args.Data);
            process.OutputDataReceived += (sender, args) => Log(args.Data);
            process.BeginOutputReadLine();
            CheckStatus();
        }

        public EndPoint Endpoint => new IPEndPoint(IPAddress.Loopback, config.SelectedPort);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Log(string message)
        {
            config.Logger?.Invoke(message);
        }

        private void CheckStatus()
        {
            var option = new ConfigurationOptions
            {
                AbortOnConnectFail = true,
                EndPoints = { Endpoint },
                AllowAdmin = true
            };

            multiplexer = ConnectionMultiplexer.Connect(option);
            var status = multiplexer.GetStatus();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (!disposing)
            {
                return;
            }

            Log("Dispose");
            StopServer();
            ShutdownProcess();
            Kill();

            Policy
                .Handle<Exception>()
                .WaitAndRetry(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3) },
                              (exception, timeSpan) => { Log("Failed to delete files..."); })
                .Execute(DeleteFiles);
            disposed = true;
        }

        private void Kill()
        {
            if (!config.Kill)
            {
                return;
            }

            Log("Kill");
            foreach (var redis in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(executable.Info.Name)))
            {
                try
                {
                    redis.Kill();
                }
                catch (Exception e)
                {
                    Log(e.Message);
                }
            }
        }

        private void DeleteFiles()
        {
            try
            {
                if (config.IsWithPersistence)
                {
                    var file = Path.Combine(executable.Info.DirectoryName, config.PersistenceFile);

                    if (File.Exists(file))
                    {
                        Log($"Deleting DB: {file}");
                        File.Delete(file);
                    }
                }

                if (File.Exists(executable.Info.FullName))
                {
                    Log($"Deleting: {executable.Info.FullName}");
                    File.Delete(executable.Info.FullName);
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        private void ShutdownProcess()
        {
            try
            {
                process.CancelOutputRead();
                Log("Redis was started for this test run. Shutting down");

                if (process != null)
                {
                    if (process.HasExited)
                    {
                        Log("Process already existed");
                    }
                    else
                    {
                        if (!process.CloseMainWindow())
                        {
                            Log("Close failed");
                            process.Kill();
                        }
                        else
                        {
                            process.WaitForExit(1000);
                        }
                    }
                }

                process.Dispose();
                executable.Dispose();
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        private void StopServer()
        {
            Log("StopServer");
            try
            {
                multiplexer.GetServer(Endpoint).Shutdown();
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }

            multiplexer.Dispose();
        }
    }
}