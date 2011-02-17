using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Putio;

namespace PutioFS.Core
{
    public class PutioFolder : PutioFsItem
    {
        private List<PutioFolder> SubFolders;
        private List<PutioFile> Files;
        private DateTime LastContentUpdate;

        private Boolean Initialized = false;

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static PutioFolder GetRootFolder(PutioFileSystem fs)
        {
            return new PutioFolder(fs);
        }

        private PutioFolder(PutioFileSystem fs)
            : base(fs)
        {
        }

        public PutioFolder(PutioFsDataProvider data_provider, PutioFolder parent)
            : base(data_provider, parent)
        {
            if (!this.IsDirectory)
                throw new Exception("Can not create a directory entry for a non directory item.");
        }

        private bool ShouldUpdateContents()
        {
            //if (this.LastContentUpdate.AddSeconds(Constants.FOLDER_UPDATE_INTERVAL_SECONDS) < DateTime.Now)
            //{
            //    return true;
            //}

            // return false;
            return !this.Initialized;
        }

        private void UpdateContents()
        {
            if (this.ShouldUpdateContents())
            {
                lock(this)
                {
                    // Check again if somebody else with the lock already cached these...
                    if (this.ShouldUpdateContents())
                    {
                        
                        logger.Debug("Updating folder contents for {0}", this.Name);
                        this.SubFolders = new List<PutioFolder>();
                        this.Files = new List<PutioFile>();
                        foreach (Item item in this.DataProvider.GetFsItems(this))
                        {
                            if (item.IsDirectory)
                                this.SubFolders.Add(new PutioFolder(new PutioFsApiDataProvider(this.Fs, item), this));
                            else
                                this.Files.Add(new PutioFile(new PutioFsApiDataProvider(this.Fs, item), this));
                        }
                        this.LastContentUpdate = DateTime.Now;
                        this.Initialized = true;
                    }
                }
            }
        }

        public PutioFsItem GetItem(String name)
        {
            this.UpdateContents();
            foreach (PutioFolder folder in this.SubFolders)
            {
                if (folder.Name == name)
                    return folder;
            }

            foreach (PutioFile file in this.Files)
            {
                if (file.Name == name)
                    return file;
            }

            return null;
        }

        public PutioFile GetFile(String name)
        {
            this.UpdateContents();
            foreach (PutioFile file in this.Files)
            {
                if (file.Name == name)
                    return file;
            }

            return null;
        }

        public PutioFolder GetFolder(String name)
        {
            this.UpdateContents();
            foreach (PutioFolder folder in this.SubFolders)
            {
                if (folder.Name == name)
                    return folder;
            }

            return null;
        }

        public List<PutioFolder> GetFolders()
        {
            this.UpdateContents();
            return this.SubFolders;
        }

        public List<PutioFile> GetFiles()
        {
            this.UpdateContents();
            return this.Files;
        }
    }
}
