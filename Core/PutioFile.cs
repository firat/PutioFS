using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PutioFS.Core
{
    /// <summary>
    ///  Represents a remote file on Put.io servers.
    ///  Calling the Open() method returns a PutioFileHandle which
    ///  should be closed explicitly by calling its Close() method.
    ///  <see cref="PutioFileHandle"/>
    /// </summary>
    public class PutioFile : PutioFsItem
    {
        public readonly LocalFileCache Cache;

        public PutioFile(PutioFsDataProvider data_provider, PutioFolder parent)
            : base(data_provider, parent)
        {
            this.Cache = new LocalFileCache(this);
        }

        /// <summary>
        /// This creates a new PutioFileHandle and returns it.
        /// It should later be closed explicitly.
        /// </summary>
        /// <returns>PutioFileHandle</returns>
        public PutioFileHandle Open()
        {
            this.Cache.Init();
            return new PutioFileHandle(this);
        }

    }  
}
