using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;

namespace PutioFS.Core
{
    public class LocalFileCache : IDisposable
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private bool _IsDisposed;

        public readonly LongRangeCollection RangeCollection;
        public readonly PutioFile PutioFile;

        private FileStream WriteStream;
        private object WriteLock = new object();
        private int WritesWithoutIndexUpdate;
        private bool Initialized;

        public LocalFileCache(PutioFile file)
        {
            this.PutioFile = file;
            this.RangeCollection = new LongRangeCollection(0, this.PutioFile.Size);
        }

        public void Init()
        {
            lock (this.WriteLock)
            {
                if (this.Initialized)
                    return;

                this.Initialized = true;

                if (!File.Exists(this.PutioFile.DataProvider.LocalIndexFile))
                {
                    if (!Directory.Exists(this.PutioFile.DataProvider.LocalStorageDirectory))
                        Directory.CreateDirectory(this.PutioFile.DataProvider.LocalStorageDirectory);
                    File.Create(this.PutioFile.DataProvider.LocalIndexFile).Close();
                }

                using (StreamReader sr = new StreamReader(this.PutioFile.DataProvider.LocalIndexFile))
                {
                    while (sr.Peek() >= 0)
                    {
                        String[] range_str = sr.ReadLine().Split(',');
                        this.RangeCollection.AddRange(Int64.Parse(range_str[0]), Int64.Parse(range_str[1]));
                    }
                }
            }
        }

        public Boolean Contains(long offset, long end)
        {
            if (end > this.PutioFile.Size)
                end = this.PutioFile.Size;
            return this.RangeCollection.Contains(offset, end);
        }

        public void MarkAsBuffered(long start, long end)
        {
            lock (this.WriteLock)
            {
                this.RangeCollection.AddRange(start, end);
                if (this.WritesWithoutIndexUpdate >= Constants.INDEX_UPDATE_WRITE_INTERVAL)
                {
                    this.UpdateIndexFile();
                    this.WritesWithoutIndexUpdate = 0;
                }
                else
                    this.WritesWithoutIndexUpdate++;
            }
        }

        public void UpdateIndexFile()
        {
            using (StreamWriter sr = new StreamWriter(this.PutioFile.DataProvider.LocalIndexFile))
            {
                sr.Flush();
                sr.BaseStream.Seek(0, SeekOrigin.Begin);
                foreach (LongRange lr in this.RangeCollection.RangeSet)
                {
                    sr.WriteLine(String.Format("{0},{1}", lr.Start, lr.End));
                }
            }
            
        }

        /// <summary>
        /// Return how much is buffered after offset.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public long CacheRange(long offset)
        {
            LongRange r = this.RangeCollection.BinarySearch(offset);
            if (r == null)
                return 0;
            else
                return r.End - offset;
        }

        public void WaitToBuffer(long range_start, long range_end)
        {
            logger.Debug("There are {0} open handles.", PutioFileSystem.GetInstance().NumOpenHandles);
            while(!this.Contains(range_start, range_end))
                Thread.Sleep(50);
        }

        public LongRange GetNextBufferRange(long offset)
        {
            if (offset >= this.PutioFile.Size)
                return null;

            lock (this.RangeCollection)
            {
                LongRange BufferThis = new LongRange(0, this.PutioFile.Size);
                int index = this.RangeCollection.BinaryIndexSearch(offset);
                if (index < 0)
                {
                    BufferThis.Start = offset;
                    index = this.RangeCollection.Bisect(offset);
                    if (index < this.RangeCollection.RangeSet.Count)
                        BufferThis.End = this.RangeCollection.RangeSet.ElementAt(index).Start;
                    else
                        BufferThis.End = this.PutioFile.Size;
                }
                else
                {
                    return this.GetNextBufferRange(this.RangeCollection.RangeSet.ElementAt(index).End);
                }

                return BufferThis;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (this)
            {
                if (!this._IsDisposed)
                {
                    if (disposing)
                    {
                        if (this.WriteStream != null)
                            this.WriteStream.Close();
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

        ~LocalFileCache()
        {
            Dispose(false);
        }
    }
}
