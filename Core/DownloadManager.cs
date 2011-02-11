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
        public Boolean Online;
        public int MaxConnections;

        private Dictionary<PutioFileHandle, Downloader> Handles;
        private List<Downloader> Downloaders;
        private Queue<PutioFileHandle> Queue;

        public DownloadManager(int max_connection_count)
        {
            this.Handles = new Dictionary<PutioFileHandle, Downloader>();
            this.Downloaders = new List<Downloader>();
            this.Queue = new Queue<PutioFileHandle>();
            this.Online = false;
            this.MaxConnections = max_connection_count;
        }


        public Boolean Register(PutioFileHandle handle)
        {
            lock (this.Handles)
            {
                // If this handle is already registered, return with failure.
                if (this.Handles.ContainsKey(handle))
                    return false;
                lock (this.Downloaders)
                {
                    // Check if we have an existing downloader close to the handle's position.
                    // If we find one, attach the handle to that.
                    foreach (Downloader d in this.Downloaders)
                    {
                        if (d.IsCloseToPosition(handle.Position))
                        {
                            d.AddHandle(handle);
                            this.Handles.Add(handle, d);
                            return true;
                        }

                    }

                    if (this.Downloaders.Count > this.MaxConnections)
                        this.Queue.Enqueue(handle);
                    else
                    {

                    }
                    return true;
                }
            }
        }

        public Boolean Unregister(PutioFileHandle handle)
        {
            throw new Exception("Not implemented.");
        }

        public Boolean UpdatePosition(PutioFileHandle handle)
        {
            throw new Exception("Not implemented.");
        }

        public void UpdateDownloaders()
        {
            throw new Exception("Not implemented.");
        }
      
    }

    class Downloader
    {
        private long InitialPosition;
        private long CurrentPosition;
        private List<PutioFileHandle> Handles;
        private int ConnectionCount;

        public Downloader(PutioFileHandle handle)
        {
            throw new Exception("Not implemented.");
        }

        public Boolean AddHandle(PutioFileHandle handle)
        {
            throw new Exception("Not implemented.");
        }

        public void SetConnectionCount(int n)
        {
            throw new Exception("Not implemented.");
        }

        public Boolean IsCloseToPosition(long pos)
        {
            throw new Exception("Not implemented.");
        }

        public void Start()
        {
            throw new Exception("Not implemented.");
        }

        public void Stop()
        {
            throw new Exception("Not implemented.");
        }
    }

    //class DownloadTask
    //{
    //    public long Position;

    //    private Thread DownloadThread;
    //    private Boolean ContinueDownloading;
    //    private PutioFileHandle Handle;

    //    public Boolean IsAlive { get { return this.DownloadThread.IsAlive; } }
    //    public DownloadTask(PutioFileHandle handle, long offset)
    //    {
    //        this.Position = offset;
    //        this.Handle = handle;
    //        this.ContinueDownloading = true;
    //        this.DownloadThread = new Thread(DownloadJob);
    //        this.DownloadThread.Start();
    //    }

    //    public void Stop()
    //    {
    //        this.ContinueDownloading = false;
    //        this.DownloadThread.Join();
    //    }

    //    public Boolean IsCloseTo(long offset)
    //    {
    //        return offset >= this.Position && this.Position + Constants.CHUNK_TOLERANCE >= offset;
    //    }

    //    public void DownloadJob()
    //    {
    //        byte[] buffer = new byte[Constants.CHUNK_READ_SIZE];
    //        PutioStream remote_stream = null;
    //        FileStream write_stream = this.Handle.PutioFile.DataProvider.GetNewLocalWriteStream();
    //        try
    //        {
    //            while (this.ContinueDownloading)
    //            {
    //                LongRange range = this.Handle.PutioFile.Cache.GetNextBufferRange(this.Position);
    //                if (range != null)
    //                {
    //                    remote_stream = this.Handle.PutioFile.DataProvider.GetNewRemoteStream(range.Start, range.End);
    //                    this.Position = range.Start;
    //                    int count = remote_stream.Read(buffer, 0, buffer.Length);
    //                    write_stream.Seek(this.Position, SeekOrigin.Begin);
    //                    while (this.ContinueDownloading && count > 0 && this.Position < range.End)
    //                    {
    //                        write_stream.Write(buffer, 0, count);
    //                        write_stream.Flush(true);
    //                        this.Handle.PutioFile.Cache.MarkAsBuffered(this.Position, this.Position + count);
    //                        this.Position += count;
    //                        count = remote_stream.Read(buffer, 0, buffer.Length);
    //                        if (this.Position == this.Handle.PutioFile.Size)
    //                            Debug.WriteLine("!!");
    //                    }
    //                    if (this.Position != range.End && this.ContinueDownloading)
    //                        Debug.WriteLine("Why not?");
    //                }
    //                else
    //                {
    //                    return;
    //                }
    //            }
    //        }
    //        finally
    //        {
    //            write_stream.Close();
    //            if (remote_stream != null)
    //                remote_stream.Close();
    //        }
    //    }
    //}

}
