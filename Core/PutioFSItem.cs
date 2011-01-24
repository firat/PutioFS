using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Putio;

namespace PutioFS.Core
{
    public class PutioFsItem
    {
        public readonly PutioFsDataProvider DataProvider;
        public String Name { get { return this.DataProvider.Name; } }
        public bool IsDirectory { get { return this.DataProvider.IsDirectory; } }
        public long Size
        {
            get
            {
                if (this.IsDirectory)
                    return 0;
                
                return this.DataProvider.Size;
            }
        }
        public PutioFolder Parent;

        public PutioFsItem(PutioFsDataProvider data_provider, PutioFolder parent)
        {
            this.DataProvider = data_provider;
        }
    }
}
