using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuartzServiceLib
{
    public class PurgeRequestSection : ConfigurationSection
    {
        [ConfigurationProperty("PurgeRequests", IsDefaultCollection = true)]
        public PurgeRequestCollection PurgeRequests
        {
            get { return (PurgeRequestCollection)this["PurgeRequests"]; }
            set { this["PurgeRequests"] = value; }
        }
    }

    [ConfigurationCollection(typeof(PurgeRequestElement), AddItemName = "PurgeRequest")]
    public class PurgeRequestCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new PurgeRequestElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((PurgeRequestElement)element).Name;
        }

    }


    public class PurgeRequestElement : ConfigurationElement, IPurgeRequest
    {

        [ConfigurationProperty("Name", DefaultValue = "", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)this["Name"]; }
            set { this["Name"] = value; }
        }
        [ConfigurationProperty("Type", DefaultValue = "File", IsRequired = true)]
        public PurgeRequest.PurgeType Type
        {
            get { return (PurgeRequest.PurgeType)this["Type"]; }
            set { this["Type"] = value; }
        }
        [ConfigurationProperty("Folder", DefaultValue = "", IsRequired = true)]
        public string Folder
        {
            get { return (string)this["Folder"]; }
            set { this["Folder"] = value; }
        }
        [ConfigurationProperty("FakePurge", DefaultValue = false, IsRequired = false)]
        public bool FakePurge
        {
            get { return (bool)this["FakePurge"]; }
            set { this["FakePurge"] = value; }
        }
        [ConfigurationProperty("Recurse", DefaultValue = false, IsRequired = false)]
        public bool Recurse
        {
            get { return (bool)this["Recurse"]; }
            set { this["Recurse"] = value; }
        }
        [ConfigurationProperty("RemoveEmptySubFolders", DefaultValue = false, IsRequired = false)]
        public bool RemoveEmptySubFolders
        {
            get { return (bool)this["RemoveEmptySubFolders"]; }
            set { this["RemoveEmptySubFolders"] = value; }
        }
        [ConfigurationProperty("DaysBack", DefaultValue = null, IsRequired = false)]
        public int? DaysBack
        {
            get { return (int?)this["DaysBack"]; }
            set { this["DaysBack"] = value; }
        }

        [ConfigurationProperty("WildCard", DefaultValue = "", IsRequired = true)]
        public string WildCard
        {
            get { return (string)this["WildCard"]; }
            set { this["WildCard"] = value; }
        }


        public void Purge()
        {
            throw new NotImplementedException();
        }

        public void PurgeFiles(string Folder)
        {
            throw new NotImplementedException();
        }

        public void PurgeFolders(string Folder)
        {
            throw new NotImplementedException();
        }
    }  
}
