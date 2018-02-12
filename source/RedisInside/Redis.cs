using System;
using System.Diagnostics;
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

        public Redis(Action<IConfig> configuration = null)
        {
            configuration?.Invoke(config);

            executable = new TemporaryFile(GetType().GetTypeInfo().Assembly.GetManifestResourceStream("RedisInside.Executables.redis-server.exe"), "exe");
            
            var processStartInfo = new ProcessStartInfo(" \"" + executable.Info.FullName + " \"")
                                   {
                                       UseShellExecute = false,
                                       Arguments = $"--port {config.SelectedPort} --bind {config.Host} --persistence-available no",
                                       CreateNoWindow = true,
                                       LoadUserProfile = false,
                                       RedirectStandardError = true,
                                       RedirectStandardOutput = true,
                                       StandardOutputEncoding = Encoding.ASCII
                                   };

            process = Process.Start(processStartInfo);
            process.ErrorDataReceived += (sender, eventargs) => config.Logger.Invoke(eventargs.Data);
            process.OutputDataReceived += (sender, eventargs) => config.Logger.Invoke(eventargs.Data);
            process.BeginOutputReadLine();
        }

        public EndPoint Endpoint => new IPEndPoint(IPAddress.Loopback, config.SelectedPort);

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

            try
            {
                process.CancelOutputRead();
                process.Kill();
                process.WaitForExit(2000);

                if (disposing)
                {
                    process.Dispose();
                    executable.Dispose();
                }
            }
            catch (Exception ex)
            {
                config.Logger(ex.ToString());
            }

            disposed = true;
        }
    }
}
