using System;
using System.IO;

namespace RedisInside
{
    public class TemporaryFile : IDisposable
    {
        private bool disposed;

        public TemporaryFile(string target, string extension = "tmp")
        {
            Info = new FileInfo(Path.Combine(target, "Wikiled.RedisInside" + "." + extension));
        }

        public TemporaryFile(Stream stream, string target, string extension = "tmp")
            : this(target, extension)
        {
            if (Info.Exists)
            {
                return;
            }

            using (stream)
            using (var destination = Info.OpenWrite())
            {
                stream.CopyTo(destination);
            }
        }

        public FileInfo Info { get; }

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

            disposed = true;
        }
    }
}
