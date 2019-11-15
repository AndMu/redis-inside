using Polly;
using StackExchange.Redis;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

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

            executable = new TemporaryFile(GetType().GetTypeInfo().Assembly.GetManifestResourceStream("RedisInside.Executables.redis-server.exe"), config.Location, "exe");
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

            process = Process.Start(processStartInfo);
            process.ErrorDataReceived += (sender, args) => config.Logger.Invoke(args.Data);
            process.OutputDataReceived += (sender, args) => config.Logger.Invoke(args.Data);
            process.BeginOutputReadLine();
            CheckStatus();
        }

        public EndPoint Endpoint => new IPEndPoint(IPAddress.Loopback, config.SelectedPort);

        private void CheckStatus()
        {
            var option = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                EndPoints = { Endpoint },
                AllowAdmin = true
            };

            multiplexer = ConnectionMultiplexer.Connect(option);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                StopServer();
                ShutdownProcess();

                Policy
                   .Handle<Exception>()
                   .WaitAndRetry(new[]
                                 {
                                     TimeSpan.FromSeconds(1),
                                     TimeSpan.FromSeconds(2),
                                     TimeSpan.FromSeconds(3)
                                 },
                                 (exception, timeSpan) => { config.Logger($"Failed to delete files..."); })
                   .Execute(DeleteFiles);
            }

            disposed = true;
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
                        config.Logger($"Deleting DB: {file}");
                        File.Delete(file);
                    }
                }

                if (File.Exists(executable.Info.FullName))
                {
                    config.Logger($"Deleting: {executable.Info.FullName}");
                    File.Delete(executable.Info.FullName);
                }
            }
            catch (Exception ex)
            {
                config.Logger(ex.ToString());
            }
        }

        private void ShutdownProcess()
        {
            try
            {
                process.CancelOutputRead();
                config.Logger("Redis was started for this test run. Shutting down");
                if (process != null)
                {
                    if (process.HasExited)
                    {
                        config.Logger("Process already existed");
                    }
                    else
                    {
                        if (!process.CloseMainWindow())
                        {
                            config.Logger("Close failed");
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
                config.Logger(ex.ToString());
            }
        }

        private void StopServer()
        {
            try
            {
                multiplexer.GetServer(Endpoint).Shutdown();
                multiplexer.Dispose();
            }
            catch (Exception ex)
            {
                config.Logger(ex.ToString());
            }
        }
    }
}
