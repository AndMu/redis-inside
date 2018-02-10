using System;
using System.Diagnostics;
using System.IO;

namespace RedisInside
{
    public class TemporaryFile : IDisposable
    {
        private bool disposed;

        public TemporaryFile(string extension = "tmp")
        {
            Info = new FileInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "." + extension));
        }

        public TemporaryFile(Stream stream, string extension = "tmp")
            : this(extension)
        {
            using (stream)
            using (var destination = Info.OpenWrite())
            {
                stream.CopyTo(destination);
            }
        }

        public FileInfo Info { get; }

        public void CopyTo(Stream result)
        {
            using (var stream = Info.OpenRead())
            {
                stream.CopyTo(result);
            }

            if (result.CanSeek)
            {
                result.Seek(0, SeekOrigin.Begin);
            }
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

            try
            {
                if (disposing)
                {
                    Info.Delete();
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }

            disposed = true;
        }
    }
}
