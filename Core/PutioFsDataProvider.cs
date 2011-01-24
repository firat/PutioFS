using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Putio;

namespace PutioFS.Core
{
    abstract public class PutioFsDataProvider
    {
        abstract public long Id { get; }
        abstract public String Name { get; }
        abstract public bool IsDirectory { get; }
        abstract public long Size { get; }
        abstract public String URL { get; }
        abstract public String ContentHash { get; }
        abstract public String LocalStorageDirectory { get; }
        abstract public String LocalStorageFile { get; }
        abstract public String LocalIndexFile { get; }
        abstract public IEnumerable<Item> GetFsItems(PutioFolder folder);
        abstract public PutioStream GetNewRemoteStream(long range_start, long range_end);
        abstract public FileStream GetNewLocalWriteStream();
        abstract public FileStream GetNewLocalReadStream();
    }

    public class PutioFsApiDataProvider : PutioFsDataProvider
    {
        private Item PutioItem;

        public PutioFsApiDataProvider(Item putio_item)
        {
            this.PutioItem = putio_item;
        }


        public override long Id { get { return Int64.Parse(this.PutioItem.Id); } }
        public override String Name
        { 
            get 
            { 
                String r = this.PutioItem.Name;
                foreach (Char c in Constants.ILLEGAL_CHARACTERS)
                {
                    r = r.Replace(c, '_');
                }
                return r;
            } 
        }
        public override bool IsDirectory { get { return this.PutioItem.IsDirectory; } }
        public override long Size
        {
            get
            {
                if (this.IsDirectory)
                {
                    return 0;
                }
                else
                {
                    return this.PutioItem.Size;
                }
            }
        }
        public override String URL { get { return this.PutioItem.StreamUrl + "/atk/" + PutioFileSystem.GetInstance().PutioApi.GetAccessToken(); } }
        public override String ContentHash { get { return this.Id.ToString(); } }
        public override String LocalStorageDirectory { get { return Path.Combine(Constants.LocalStoragePath, this.ContentHash); } }
        public override String LocalStorageFile { get { return Path.Combine(this.LocalStorageDirectory, String.Format("{0}.pcd", this.ContentHash)); } }
        public override String LocalIndexFile { get { return Path.Combine(this.LocalStorageDirectory, String.Format("{0}.pci", this.ContentHash)); } }

        public override IEnumerable<Item> GetFsItems(PutioFolder folder)
        {
            return PutioFileSystem.GetInstance().PutioApi.GetItems(this.PutioItem.Id);
        }

        public override PutioStream GetNewRemoteStream(long range_start, long range_end)
        {
            return new PutioHttpStream(this.URL, range_start, range_end);
        }

        private void CreateLocalCache()
        {
            if (!File.Exists(this.LocalStorageFile))
            {
                if (!Directory.Exists(this.LocalStorageDirectory))
                    Directory.CreateDirectory(this.LocalStorageDirectory);
                using (FileStream fs = File.Create(this.LocalStorageFile))
                {
                    fs.SetLength(this.Size);
                }
            }
        }


        public override FileStream GetNewLocalWriteStream()
        {
            this.CreateLocalCache();
            return File.Open(this.LocalStorageFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
        }

        public override FileStream GetNewLocalReadStream()
        {
            this.CreateLocalCache();
            return File.Open(this.LocalStorageFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
        }
    }

    public class PutioFsTestDataProvider : PutioFsDataProvider
    {

        private String _Name;
        private String _URL;
        private long _Id;
        private long _Size;
        private bool _IsDirectory;
        private String _LocalStorageDirectory;

        public PutioFsTestDataProvider(String name, String url, long id, long size, bool is_directory, String local_storage_directory)
        {
            this._Name = name;
            this._URL = url;
            this._Id = id;
            this._Size = size;
            this._IsDirectory = is_directory;
            this._LocalStorageDirectory = local_storage_directory;
        }

        public override long Id { get { return this._Id; } }
        public override String Name { get { return this._Name; } }
        public override bool IsDirectory { get { return this._IsDirectory; } }
        public override long Size
        {
            get
            {
                if (this.IsDirectory)
                {
                    return 0;
                }
                else
                {
                    return this._Size;
                }
            }
        }
        public override String URL { get { return this._URL; } }
        public override String ContentHash { get { return this._Id.ToString(); } }
        public override String LocalStorageDirectory { get { return this._LocalStorageDirectory; } }
        public override String LocalStorageFile { get { return Path.Combine(this.LocalStorageDirectory, String.Format("{0}.pca", this.ContentHash)); } }
        public override String LocalIndexFile { get { return Path.Combine(this.LocalStorageDirectory, String.Format("{0}.pci", this.ContentHash)); } }

        public override IEnumerable<Item> GetFsItems(PutioFolder folder)
        {
            yield break;
        }

        public override PutioStream GetNewRemoteStream(long range_start, long range_end)
        {
            return new PutioFileStream(this.URL, range_start, range_end);
        }

        private void CreateLocalCache()
        {
            if (!File.Exists(this.LocalStorageFile))
            {
                if (!Directory.Exists(this.LocalStorageDirectory))
                    Directory.CreateDirectory(this.LocalStorageDirectory);
                using (FileStream fs = File.Create(this.LocalStorageFile))
                {
                    fs.SetLength(this.Size);
                }
            }
        }

        public override FileStream GetNewLocalReadStream()
        {
            this.CreateLocalCache();
            return File.Open(this.LocalStorageFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
        }

        public override FileStream GetNewLocalWriteStream()
        {
            this.CreateLocalCache();
            return File.Open(this.LocalStorageFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
        }
    }
}