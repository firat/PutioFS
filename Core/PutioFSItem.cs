using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Putio;

namespace PutioFS.Core
{
    public class PutioFSItem
    {
        public Item ApiItem;
        public Api Api;
        public PutioFolder Parent;
        public bool IsDirectory { get { return this.ApiItem.IsDirectory; } }
        public int Id { get { return Int32.Parse(this.ApiItem.Id); } }
        public String Name
        {
            get
            {
                String tmp = this.ApiItem.Name;
                foreach (char illegal_char in Constants.ILLEGAL_CHARACTERS)
                {
                    tmp = tmp.Replace(illegal_char, '_');
                }
                return tmp;
            }
        }

        public PutioFSItem(Api api, Item api_item, PutioFolder parent)
        {
            if (parent == null && api_item.Id != "0")
                throw new ApiException(api_item, "Only root folder can have null as parent.");

            this.Api = api;
            this.ApiItem = api_item;
            this.Parent = parent;
        }
    }
}
