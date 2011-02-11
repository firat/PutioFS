using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace PutioFS.Core
{
    /// <summary>
    /// Main interface to the contents of the remote file.
    /// The file is dowloaded and stored in a local cache file. PutioFileHandle
    /// can be used like a read-only file handle to access that cache. Seeking to
    /// an offset and reading from there, like a regular file stream, will be
    /// identical to reading from the remote file.
    /// </summary>
    public class PutioFileHandle : IDisposable
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private bool _IsDisposed = false;
        private bool IsInitialized = false;
        public long Position { get { try { return this.ReadStream.Position; } catch { return 0; } } }
        public readonly PutioFile PutioFile;
        private Stream ReadStream;
        private object ReadSeekLock = new object();

        public PutioFileHandle(PutioFile file)
        {
            this.PutioFile = file;
            this.ReadStream = file.DataProvider.GetNewLocalReadStream();
        }

        private void Initialize()
        {
            this.PutioFile.Fs.DownloadManager.Register(this);
        }

        public int Read(byte[] buffer, int buffer_start_index, int max_read_size)
        {
            if (this.Position >= this.PutioFile.Size)
                return 0;

            lock (this.ReadSeekLock)
            {
                if (!this.IsInitialized)
                    this.Initialize();

                max_read_size = (int)Math.Min(max_read_size, this.PutioFile.Size - this.Position);
                while (this.PutioFile.Cache.CacheRange(this.Position) == 0)
                    Thread.Sleep(50);
                return this.ReadStream.Read(buffer, 
                                            buffer_start_index, 
                                            (int)Math.Min(this.PutioFile.Cache.CacheRange(this.Position), max_read_size));
            }
        }

        public long Seek(long offset)
        {
            lock (this.ReadSeekLock)
            {
                if (offset == this.Position)
                    return offset;
                long pos = this.ReadStream.Seek(offset, SeekOrigin.Begin);
                
                // Initialize the handle after seek.
                if (!this.IsInitialized)
                    this.Initialize();
                this.PutioFile.Fs.DownloadManager.UpdatePosition(this);

                return pos;
            }
        }


        public void Close()
        {
            this.ReadStream.Close();
            this.ReadStream = null;
            if (this.IsInitialized)
                this.PutioFile.Fs.DownloadManager.Unregister(this);
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (this)
            {
                if (!this._IsDisposed)
                {
                    if (disposing)
                    {
                        if (this.ReadStream != null)
                            this.ReadStream.Close();
                    }

                }
                this._IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PutioFileHandle()
        {
            Dispose(false);
        }
    }
}
