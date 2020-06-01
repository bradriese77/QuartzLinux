using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuartzServiceLib
{
    [Export("QuartzServiceJob", typeof(IQuartzServiceJob))]
    public class PurgeJob:QuartzServiceJob<PurgeJob>
    {

        public PurgeJob()
        {
           // JobTypeCronExpression = "0 0 8 * * ?";
            JobTypeCronExpression = "59 59 23 31 12 ? 2099";

            
            LoadConfigInfo();
        }
        public List<IPurgeRequest> purgeRequests;
        public List<IPurgeRequest> PurgeRequests
        {
            get
            {
                if(purgeRequests==null)
                {
                    PurgeRequestSection section = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).Sections["PurgeRequestSection"] as PurgeRequestSection;
                    if (section == null) return new List<IPurgeRequest>();
                    return new List<IPurgeRequest>(section.PurgeRequests.Cast<IPurgeRequest>());
                }

                return purgeRequests;
            }
            set
            {
                purgeRequests=value;
            }

        }
        public override void ExecuteJob()
        {
            List<Exception> Exceptions = new List<Exception>();

            foreach (IPurgeRequest r in PurgeRequests)
            {

                try
                {
                    PurgeRequest p = new PurgeRequest() { Name = r.Name, DaysBack = r.DaysBack, FakePurge = r.FakePurge, Folder = r.Folder, Recurse = r.Recurse, Type = r.Type, WildCard = r.WildCard, RemoveEmptySubFolders = r.RemoveEmptySubFolders };
                    p.Purge();
                }
                catch (Exception ex)
                {
                    Exceptions.Add(ex);
                }

            }
            if (Exceptions.Count > 0) throw new AggregateException(Exceptions);
        }

    }
}
