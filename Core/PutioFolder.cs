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

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public PutioFolder(PutioFsDataProvider data_provider, PutioFolder parent)
            : base(data_provider, parent)
        {
            if (!this.IsDirectory)
                throw new Exception("Can not create a directory entry for a non directory item.");
        }

        private bool ShouldUpdateContents()
        {
            if (this.LastContentUpdate == null)
                return true;

            if (this.LastContentUpdate.AddSeconds(Constants.FOLDER_UPDATE_INTERVAL_SECONDS) < DateTime.Now)
            {
                return true;
            }

            return false;
        }

        private void UpdateContent()
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
                                this.SubFolders.Add(new PutioFolder(new PutioFsApiDataProvider(item), this));
                            else
                                this.Files.Add(new PutioFile(new PutioFsApiDataProvider(item), this));
                        }
                        this.LastContentUpdate = DateTime.Now;
                    }
                }
            }
        }

        public PutioFsItem GetItem(String name)
        {
            this.UpdateContent();
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
            this.UpdateContent();
            foreach (PutioFile file in this.Files)
            {
                if (file.Name == name)
                    return file;
            }

            return null;
        }

        public PutioFolder GetFolder(String name)
        {
            this.UpdateContent();
            foreach (PutioFolder folder in this.SubFolders)
            {
                if (folder.Name == name)
                    return folder;
            }

            return null;
        }

        public List<PutioFolder> GetFolders()
        {
            this.UpdateContent();
            return this.SubFolders;
        }

        public List<PutioFile> GetFiles()
        {
            this.UpdateContent();
            return this.Files;
        }
    }
}
