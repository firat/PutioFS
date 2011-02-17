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
    /// This is the master download manager. It manages smaller threads that do the actual download.
    /// </summary>
    public class DownloadManager
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Boolean Online;
        public int MaxConnections;

        private object Padlock = new object();

        private Downloader Downloader;
        private List<PutioFileHandle> Queue;

        public DownloadManager(int max_connection_count)
        {
            this.Downloader = new Downloader(this);
            this.Queue = new List<PutioFileHandle>();
            this.Online = false;
            this.MaxConnections = max_connection_count;
        }


        public Boolean Register(PutioFileHandle handle)
        {
            lock (this.Padlock)
            {
               if (this.Queue.Contains(handle))
                   return false;
               logger.Debug("Registering handle {0}", handle);
               this.Queue.Add(handle);
               this.ProcessQueue();
               return true;
            }
        }

        public Boolean Unregister(PutioFileHandle handle)
        {
            lock (this.Padlock)
            {
                if (!this.Queue.Contains(handle))
                    return false;
                logger.Debug("UNRegistering handle {0}", handle);
                this.Queue.Remove(handle);
                this.ProcessQueue();
                return true;
            }
        }

        public Boolean UpdatePosition(PutioFileHandle handle)
        {
            lock (this.Padlock)
            {
                this.ProcessQueue();
                return true;
            }
        }

        public void ProcessQueue()
        {
            lock (this.Padlock)
            {
                int counter = 0;
                while(this.Queue.Count > counter)
                {
                    PutioFileHandle h = this.Queue.ElementAt(counter);
                    if (h.PutioFile.Cache.GetNextBufferRange(h.Position) == null)
                    {
                        counter++;
                        continue;
                    }

                    this.Downloader.Download(h);
                    break;
                }

                if (this.Queue.Count == 0)
                {
                    logger.Debug("There are zero handles, stopping download.");
                    this.Downloader.Stop();
                }
            }   
        }

    }

    class Downloader
    {
        private object Padlock = new object();

        private long InitialPosition;
        private long CurrentPosition;
        private Boolean ContinueDownloading;
        private DownloadManager DM;

        private PutioFileHandle _Handle;
        public PutioFileHandle Handle
        {
            get { return this._Handle; }
            set
            {
                this._Handle = value;
            }
        }

        private Thread DLThread;

        public Boolean IsActive { get { return this.ContinueDownloading; } }

        public Downloader(DownloadManager dm)
        {
            this.DM = dm;
        }


        public void Download(PutioFileHandle h)
        {
            lock (this.Padlock)
            {
                if (this.DLThread != null && this.DLThread.IsAlive)
                {
                    if (this.Handle == h)
                        return;
                    this.Stop();
                    this.Handle = h;
                    this.ContinueDownloading = true;
                    this.DLThread = new Thread(DownloadJob);
                    this.DLThread.Start();
                }
                else
                {
                    this.Handle = h;
                    this.DLThread = new Thread(DownloadJob);
                    this.ContinueDownloading = true;
                    this.DLThread.Start();
                }
            }
        }

        public void Stop()
        {
            lock (this.Padlock)
            {
                if (this.DLThread == null || !this.DLThread.IsAlive)
                    return;
                this.ContinueDownloading = false;
                this.DLThread.Join();
            }
        }

        public Boolean IsClosetoHandle(LongRange range, long position)
        {
            if (this.Handle.BufferPosition < range.Start)
                return false;
            return this.Handle.BufferPosition <= (position + Constants.CHUNK_TOLERANCE);
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
                    LongRange range = this.Handle.PutioFile.Cache.GetNextBufferRange(this.Handle.Position);
                    if (range != null)
                    {
                        long position = range.Start;
                        remote_stream = this.Handle.PutioFile.DataProvider.GetNewRemoteStream(range.Start, range.End);
                        int count = remote_stream.Read(buffer, 0, buffer.Length);
                        write_stream.Seek(position, SeekOrigin.Begin);
                        while (this.ContinueDownloading && count > 0 && position < range.End && this.IsClosetoHandle(range, position))
                        {
                            write_stream.Write(buffer, 0, count);
                            write_stream.Flush(true);
                            this.Handle.PutioFile.Cache.MarkAsBuffered(position, position + count);
                            position += count;
                            count = remote_stream.Read(buffer, 0, buffer.Length);
                            if (position == this.Handle.PutioFile.Size)
                                Debug.WriteLine("!!");
                        }
                        remote_stream.Close();
                        remote_stream = null;
                        if (position != range.End && this.ContinueDownloading)
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
                this.Handle = null;
                if (this.ContinueDownloading)
                    new Thread(this.DM.ProcessQueue).Start();
            }
        }
    }
}
