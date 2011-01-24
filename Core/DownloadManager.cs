using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace PutioFS.Core
{
    /// <summary>
    /// This is the master download manager per file handle. It manages smaller threads that do the actual download.
    /// </summary>
    public class DownloadManager
    {
        private PutioFileHandle Handle;
        private DownloadTask Task;

        private object Padlock = new object();

        public DownloadManager(PutioFileHandle handle)
        {
            this.Handle = handle;
        }

        public void OnRead(long offset, long max_read_size)
        {
            if (offset >= this.Handle.PutioFile.Size)
                return;

            if (this.Task == null)
            {
                this.Task = new DownloadTask(this.Handle, offset);
            }

            while (!this.Handle.PutioFile.Cache.Contains(offset, offset + max_read_size))
            {
                this.OnSeek(this.Handle.Position);
                Thread.Sleep(50);
            }
        }

        public void OnSeek(long offset)
        {
            if (this.Task == null)
                this.Task = new DownloadTask(this.Handle, offset);
            else
            {
                if (offset >= this.Handle.PutioFile.Size)
                {
                    this.Task.Stop();
                    return;
                }

                if (offset < this.Task.Position)
                {
                    if (this.Handle.PutioFile.Cache.Contains(offset, this.Task.Position) && (this.Task.IsAlive || this.Task.Position == this.Handle.PutioFile.Size))
                            return;
                }
                else if (this.IsBuffering(offset))
                {
                    return;
                }

                this.Task.Stop();
                this.Task = new DownloadTask(this.Handle, offset);
            }
        }

        public void OnClose()
        {
            if (this.Task != null)
                this.Task.Stop();
        }

        public Boolean IsBuffering(long offset)
        {
            return this.Task.IsCloseTo(offset) && this.Task.IsAlive;
        }
    }

    class DownloadTask
    {
        public long Position;

        private Thread DownloadThread;
        private Boolean ContinueDownloading;
        private PutioFileHandle Handle;

        public Boolean IsAlive { get { return this.DownloadThread.IsAlive; } }
        public DownloadTask(PutioFileHandle handle, long offset)
        {
            this.Position = offset;
            this.Handle = handle;
            this.ContinueDownloading = true;
            this.DownloadThread = new Thread(DownloadJob);
            this.DownloadThread.Start();
        }

        public void Stop()
        {
            this.ContinueDownloading = false;
            this.DownloadThread.Join();
        }

        public Boolean IsCloseTo(long offset)
        {
            return offset >= this.Position && this.Position + Constants.CHUNK_TOLERANCE >= offset;
        }

        public void DownloadJob()
        {
            byte[] buffer = new byte[Constants.CHUNK_READ_SIZE];
            PutioStream remote_stream = null;
            FileStream write_stream = this.Handle.PutioFile.DataProvider.GetNewLocalWriteStream();
            try
            {
                while (this.ContinueDownloading)
                {
                    LongRange range = this.Handle.PutioFile.Cache.GetNextBufferRange(this.Position);
                    if (range != null)
                    {
                        remote_stream = this.Handle.PutioFile.DataProvider.GetNewRemoteStream(range.Start, range.End);
                        this.Position = range.Start;
                        int count = remote_stream.Read(buffer, 0, buffer.Length);
                        write_stream.Seek(this.Position, SeekOrigin.Begin);
                        while (this.ContinueDownloading && count > 0 && this.Position < range.End)
                        {
                            write_stream.Write(buffer, 0, count);
                            write_stream.Flush(true);
                            this.Handle.PutioFile.Cache.MarkAsBuffered(this.Position, this.Position + count);
                            this.Position += count;
                            count = remote_stream.Read(buffer, 0, buffer.Length);
                            if (this.Position == this.Handle.PutioFile.Size)
                                Debug.WriteLine("!!");
                        }
                        if (this.Position != range.End && this.ContinueDownloading)
                            Debug.WriteLine("Why not?");
                    }
                    else
                    {
                        return;
                    }
                }
            }
            finally
            {
                write_stream.Close();
                if (remote_stream != null)
                    remote_stream.Close();
            }
        }
    }

}
