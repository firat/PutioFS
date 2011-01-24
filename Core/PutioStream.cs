using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

using System.Diagnostics;

namespace PutioFS.Core
{
    abstract public class PutioStream
    {
        abstract public int Read(byte[] buffer, int start_pos, int max_read_size);
        abstract protected void Dispose(bool disposing);

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Close()
        {
            this.Dispose(true);
        }

        ~PutioStream()
        {
            this.Dispose(false);
        }
    }

    class PutioHttpStream : PutioStream
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private bool _IsDisposed;

        private String URL;
        private long RangeStart;
        private long RangeEnd;
        private long Position;
        private HttpWebRequest PutioRequest;
        private Stream ResponseStream;

        public PutioHttpStream(String url, long range_start, long range_end)
        {
            this.URL = url;
            this.RangeStart = range_start;
            this.RangeEnd = range_end;
            this.Position = range_start;
        }

        public override int Read(byte[] buffer, int start_pos, int max_read_size)
        {
            try
            {

                if (this.PutioRequest == null)
                {
                    this.PutioRequest = (HttpWebRequest)WebRequest.Create(this.URL);
                    // End position is inclusive. Therefore we should subtract 1.
                    this.PutioRequest.AddRange(this.Position, this.RangeEnd - 1);

                    if (this.ResponseStream != null)
                        this.ResponseStream.Close();
                    this.ResponseStream = this.PutioRequest.GetResponse().GetResponseStream();
                }

                int count = this.ResponseStream.Read(buffer, start_pos, max_read_size);
                this.Position += count;
                return count;
            }
            catch
            {
                if (this.ResponseStream != null)
                    this.ResponseStream.Close();
                this.PutioRequest = null;
            }

            return -1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this)
                {
                    if (!this._IsDisposed)
                    {
                        if (this.ResponseStream != null)
                            this.ResponseStream.Close();
                        if (this.PutioRequest != null)
                            this.PutioRequest.Abort();
                    }
                    this._IsDisposed = true;
                }
            }
        }

        

    }

    class PutioFileStream : PutioStream
    {
        private long RangeStart;
        private long RangeEnd;
        private Stream LocalStream;

        public PutioFileStream(String path, long range_start, long range_end)
        {
            this.RangeStart = range_start;
            this.RangeEnd = range_end;
            this.LocalStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            this.LocalStream.Seek(range_start, SeekOrigin.Begin);
        }

        public override int Read(byte[] buffer, int start_pos, int max_read_size)
        {
            int size_to_read = (int)Math.Min(max_read_size, this.RangeEnd - this.LocalStream.Position);
            if (size_to_read <= 0)
                return size_to_read;
            return this.LocalStream.Read(buffer, start_pos, size_to_read);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                this.LocalStream.Close();
            }
            catch { }
        }

    }
}
