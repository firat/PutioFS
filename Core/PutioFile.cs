using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Diagnostics;
using Putio;

namespace PutioFS.Core
{
    public class ApiException : Exception
    {
        public Item ApiItem;
        public ApiException(Item api_item, String msg)
            : base(msg)
        {
            this.ApiItem = api_item;
        }
    }

    public class ChunkCollection
    {
        public SortedList<int, Chunk> ChunkStorage;
        public PutioFile File;

        public ChunkCollection(PutioFile file)
        {
            this.ChunkStorage = new SortedList<int, Chunk>();
            this.File = file;
            foreach (String f in Directory.GetFiles(
                Constants.LocalStoragePath,
                String.Format("{0}_*.ptc", this.File.Id))
                )
            {
                int id_str_start_pos = f.IndexOf("_") + 1;
                int id_str_length = f.LastIndexOf(".") - id_str_start_pos;
                long chunk_offset = Convert.ToInt64(f.Substring(id_str_start_pos, id_str_length));
                this.CreateChunkAt(chunk_offset);
            }

        }

        private Chunk CreateChunkAt(long offset)
        {
            Chunk prev_chunk = null;
            Chunk next_chunk = null;
            Chunk the_chunk = null;
            lock (this.ChunkStorage)
            {
                foreach (KeyValuePair<int, Chunk> c in this.ChunkStorage)
                {
                    if (c.Key < offset)
                        prev_chunk = c.Value;

                    if (c.Key > offset)
                    {
                        next_chunk = c.Value;
                        break;
                    }

                }

                if (prev_chunk != null)
                    prev_chunk.End = offset;

                if (next_chunk == null)
                    the_chunk = new Chunk(offset, this.File.Size, this);
                else
                    the_chunk = new Chunk(offset, next_chunk.Start, this);

                this.ChunkStorage.Add((int)the_chunk.Start, the_chunk);
            }
            return the_chunk;
        }

        public void StopDowloadingAll()
        {
            IEnumerator<KeyValuePair<int, Chunk>> ie = this.ChunkStorage.GetEnumerator();
            while (ie.MoveNext())
            {
                ie.Current.Value.StopDownload();
            }
        }

        private Chunk FindChunk(long offset)
        {
            foreach (KeyValuePair<int, Chunk> c in this.ChunkStorage)
            {
                if (c.Value.Contains(offset))
                    return c.Value;
            }

            return null;
        }

        public Chunk GetOrCreateChunkAt(long offset)
        {
            
            Chunk the_chunk = null;
            

            the_chunk = this.FindChunk(offset);
            if (the_chunk == null)
            {
                lock (this.ChunkStorage)
                {
                    the_chunk = this.FindChunk(offset);
                    if (the_chunk == null)
                    {
                        the_chunk = this.CreateChunkAt(offset);
                    }
                }
            }

            return the_chunk;
        }
    }

    public class ChunkFileHandle : IDisposable
    {
        public static int State;
        private bool _IsDisposed = false;

        public Chunk Chunk;
        private FileStream ReadStream;

        public ChunkFileHandle(Chunk chunk)
        {
            this.Chunk = chunk;
        }

        
        
	    protected virtual void Dispose(bool disposing)
	    {
            lock (this)
            {
                if (!this._IsDisposed)
                {
                    if (disposing)
                    {
                       this.Chunk.CloseHandle(this);
                    }
                    if (this.ReadStream != null)
                        this.ReadStream.Close();
                }
                this._IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Open()
        {
            if (this.ReadStream == null)
            {
                this.ReadStream = new FileStream(this.Chunk.LocalPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
                
                this.Chunk.ForceDownload();
            }
            
            if (this._IsDisposed)
            {
                throw new System.IO.IOException("Can't open: File stream is already dispoed.");
            }
        }

        private bool NeedToBuffer(long read_until)
        {
            if (this.Chunk.BufferedOffset >= this.Chunk.End)
                return false;

            if (this.Chunk.BufferedOffset < read_until)
                return true;

            return false;
        }

        public long Read(byte[] data, long offset, int max_read_length)
        {
            ChunkFileHandle.State = 1;
            if (!this.ReadStream.CanRead || this.ReadStream == null || this._IsDisposed)
                throw new System.IO.IOException("Can't read from a closed file stream.");

            long read_until = Math.Min(offset + max_read_length, this.Chunk.End);
            ChunkFileHandle.State = 2;
            while (this.NeedToBuffer(read_until))
            {
                ChunkFileHandle.State =3;
                this.Chunk.ForceDownload();
                Thread.Sleep(50);
                ChunkFileHandle.State = 4;
            }
            ChunkFileHandle.State = 5;

            int internal_offset = (int)(offset - this.Chunk.Start);
            ChunkFileHandle.State = 6;
            int read_bytes = 0;
            ChunkFileHandle.State = 7;
           
            if (this.ReadStream.Position != internal_offset)
            {
                lock (this.Chunk.padlock)
                {
                    Debug.WriteLine(String.Format("Seeking to {0} from {1}", internal_offset, this.ReadStream.Position));
                    if (internal_offset - this.ReadStream.Position > this.ReadStream.Length)
                        Debug.WriteLine("Seeking ahead!");
                    this.ReadStream.Seek(internal_offset - this.ReadStream.Position, SeekOrigin.Current);
                    Debug.WriteLine(String.Format("Seek complete: {0} -> {1}", internal_offset, this.ReadStream.Position));
                    ChunkFileHandle.State = 8;
                }
            }

            ChunkFileHandle.State = 9;
            read_bytes = this.ReadStream.Read(data, 0, max_read_length);
            ChunkFileHandle.State = 10;
            if (read_bytes < 0)
            {
                Debug.WriteLine("There was a problem reading.");
            }
            ChunkFileHandle.State = 11;
            return read_bytes;
        }


        public void Close()
        {
            this.Dispose(true);
        }

        ~ChunkFileHandle()
        {
            Dispose(false);
        }

    }

    [DebuggerDisplay("[Chunk {Start} - {End}, {BufferedOffset}]")]
    public class Chunk
    {
        internal object padlock;
        private Thread  Downloader;
        private bool ContinueDownloading;
        public bool FinishedDownloading;
        private ChunkCollection Chunks;
        private int HandleCounter;
        private long _Start;
        private long _End;
        private FileStream WriteStream;

        public long BufferedOffset { get { return this.FinishedDownloading ? this._End : this._Start + this.WriteStream.Length; }}
        public PutioFile File { get { return this.Chunks.File; } }
        public String LocalFilename;
        public String LocalPath { get { return Path.Combine(Constants.LocalStoragePath, this.LocalFilename); } }
        public long Start { get { return this._Start; } set { return; } }
        public long End
        {
            get { return this._End; } 
            set
            {
                lock (this)
                {
                    if (value <= this.Start)
                    {
                        //throw new PutioException("End position can't be smaller than starting offset");
                    }
                    this._End = value;
                    if (this.BufferedOffset > this.End)
                    {
                        this.FinishedDownloading = true;
                        this.StopDownload();
                        //this.WriteStream.SetLength(this.End - this.Start);
                    }
                }
            }
        }

        public Chunk(long start, long end, ChunkCollection chunks)
        {
            if (start >= end)
            {
                throw new ApiException(this.File.ApiItem, "Can not create a chunk with a start greater than or equal to its end.");
            }

            if (end <= 0)
            {
                throw new ApiException(this.File.ApiItem, "Can not create a chunk with an end less than or equal to zero.");
            }
            this.padlock = new object();
            this.Chunks = chunks;
            this._Start = start;
            this._End = end;
            this.LocalFilename = this.Chunks.File.Id + "_" + this.Start + ".ptc";
            this.ContinueDownloading = false;
            this.WriteStream = new FileStream(this.LocalPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            this.Downloader = new Thread(this._DownloadJob);

        }
        
        public ChunkFileHandle GetNewHandle()
        {
            lock (this)
            {
                this.HandleCounter++;
            }

            return new ChunkFileHandle(this);
        }

        public void CloseHandle(ChunkFileHandle handle)
        {
            lock(this)
            {
                this.HandleCounter--;
                if (this.HandleCounter == 0)
                {
                    this.StopDownload();
                }
            }
            
        }


        public bool Contains(long offset)
        {
            return (offset >= this.Start && this.End > offset && this.BufferedOffset >= offset);
        }


        private void _DownloadJob()
        {
            if (this.FinishedDownloading)
                return;

            Stream responseStream = null;
            WebResponse myWebResponse = null;

            try
            {
                
                String url = this.Chunks.File.URL;
                HttpWebRequest myWebRequest = (HttpWebRequest)WebRequest.Create(url);

                myWebRequest.AddRange((int)this.BufferedOffset, (int)this.End);
                myWebRequest.KeepAlive = false;
                myWebResponse = myWebRequest.GetResponse();
                responseStream = myWebResponse.GetResponseStream();

                byte[] buffer = new byte[Constants.CHUNK_READ_SIZE];
                int count = responseStream.Read(buffer, 0, Constants.CHUNK_READ_SIZE);

                this.WriteStream.Seek(0, SeekOrigin.End);

                while (this.ContinueDownloading && (count > 0) && this.BufferedOffset < this.End)
                {
                    lock (this.padlock)
                    {
                        if (this.BufferedOffset + count >= this.End)
                        {
                            this.WriteStream.Write(buffer, 0, (int)(this.End - this.BufferedOffset));
                        }
                        else
                        {
                            this.WriteStream.Write(buffer, 0, (int)count);
                        }
                        this.WriteStream.Flush(true);
                        //Debug.WriteLine("Reading...");
                    }
                    count = responseStream.Read(buffer, 0, Constants.CHUNK_READ_SIZE);
                    //Debug.WriteLine("Read!");
                }
                Debug.WriteLine("Finished downloading");
                if (this.BufferedOffset >= this.End)
                    this.FinishedDownloading = true;
            }
            finally
            {
                if (responseStream != null)
                    responseStream.Close();

                if (myWebResponse != null)
                    myWebResponse.Close();
            }

        }

        public void ForceDownload()
        {
            if (this.BufferedOffset >= this.End)
                return;

            lock (this.Downloader)
            {
                this.ContinueDownloading = true;
                if (this.BufferedOffset < this.End && !this.Downloader.IsAlive)
                {
                    this.Downloader = new Thread(this._DownloadJob);
                    this.Downloader.Name = String.Format("Downloader for {0}", this.Start);
                    this.Downloader.Start();
                }
            }
        }

        public void StopDownload()
        {
            this.ContinueDownloading = false;
        }



        public bool isCloseTo(long offset)
        {
            return (this.Start <= offset) && (offset - this.BufferedOffset <= Constants.CHUNK_TOLERANCE);
        }
    }
    
    public class PutioFile : PutioFSItem
    {
        public ChunkCollection Chunks;

        
        public long Size { get { return this.ApiItem.Size; } }
        public string URL { get { return this.ApiItem.StreamUrl + "/atk/" + this.Api.GetAccessToken(); } }

        public PutioFile(Api putio_api, Item putio_item, PutioFolder parent) :
            base(putio_api, putio_item, parent)
        {
            this.Chunks = new ChunkCollection(this);
        }

        public PutioFileHandle Open()
        {
            return new PutioFileHandle(this);
        }


    }

    public class PutioFileHandle : IDisposable
    {
        public static int State;
        private bool _IsDisposed = false;

        private PutioFile File;
        private ChunkFileHandle ChunkFileHandle;
        public PutioFileHandle(PutioFile file)
        {
            this.File = file;
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (this)
            {
                if (!this._IsDisposed)
                {
                    if (disposing)
                    {
                        if (this.ChunkFileHandle != null)
                            this.ChunkFileHandle.Close();
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

        private void UpdateChunkFileHandle(long offset)
        {
            lock (this)
            {
                if (this.ChunkFileHandle != null && this.ChunkFileHandle.Chunk.Contains(offset) && this.ChunkFileHandle.Chunk.isCloseTo(offset))
                {
                    return;
                }

                Debug.WriteLine("Getting a new chunk!");
                if (this.ChunkFileHandle != null)
                    this.ChunkFileHandle.Close();

                this.ChunkFileHandle = this.File.Chunks.GetOrCreateChunkAt(offset).GetNewHandle();
                this.ChunkFileHandle.Open();
            }
        }

        public int Read(byte[] data, long offset, int max_read_length)
        {
            //Debug.WriteLine(String.Format("Reading {0} from {1}.", max_read_length, offset));
            int num_bytes_to_read = 0;
            int num_actual_bytes_read = 0;
            int last_read_size = 0;

            if (this.File.Size < offset + max_read_length)
            {
                num_bytes_to_read = (int)(this.File.Size - offset);
            }
            else
            {
                num_bytes_to_read = max_read_length;
            }

            while (num_actual_bytes_read < num_bytes_to_read)
            {
                PutioFileHandle.State = 1;
                this.UpdateChunkFileHandle(offset);
                PutioFileHandle.State = 2;
                last_read_size = (int)this.ChunkFileHandle.Read(data, offset, num_bytes_to_read - num_actual_bytes_read);
                PutioFileHandle.State = 3;
                num_actual_bytes_read += last_read_size;
                PutioFileHandle.State = 4;
                offset += last_read_size;
            }
            //Debug.WriteLine(String.Format("Finished reading {0} from {1}.", max_read_length, offset));
            return num_actual_bytes_read;
        }

        public void Close()
        {
            this.Dispose(true);
        }

        ~PutioFileHandle()
        {
            Dispose(false);
        }
    }

    
}
