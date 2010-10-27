using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Putio;

namespace PutioFS.Core
{
    public class PutioFolder : PutioFSItem
    {
        private List<PutioFolder> SubFolders;
        private List<PutioFile> Files;

        public PutioFolder(Api api, Item api_item, PutioFolder parent)
            : base(api, api_item, parent)
        {
            if (!api_item.IsDirectory)
                throw new ApiException(api_item, "Can not create a directory entry in the FS for a non directory item.");
        }

        private void UpdateContent()
        {
            if (this.SubFolders == null)
            {
                lock (this)
                {
                    // Check again if somebody else with the lock already cached these...
                    if (this.SubFolders == null)
                    {
                        this.SubFolders = new List<PutioFolder>();
                        this.Files = new List<PutioFile>();
                        foreach (Item item in this.Api.GetItems(this.ApiItem.Id))
                        {
                            if (item.IsDirectory)
                                this.SubFolders.Add(new PutioFolder(this.Api, item, this));
                            else
                                this.Files.Add(new PutioFile(this.Api, item, this));
                        }
                    }
                }
            }
        }

        public PutioFSItem GetItem(String name)
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
