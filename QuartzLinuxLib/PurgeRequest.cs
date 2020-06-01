using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppUtilityLib;

namespace QuartzServiceLib
{
    public class PurgeRequest : AppUtility, IPurgeRequest
    {
        public string Name { get; set; }
        public string Folder { get; set; }
        public string WildCard { get; set; }
        public int? DaysBack { get; set; }
        public bool Recurse { get; set; }
        public bool RemoveEmptySubFolders { get; set; }
        public PurgeType Type { get; set; }
        public bool FakePurge { get; set; }

        public PurgeRequest()
        {
            FakePurge = false;
        }
        public enum PurgeType
        {
            File,
            Folder
        }
        public void Purge()
        {
            Logger.Info(string.Format("Running {0} Purge", Name));
            switch (Type)
            {
                case PurgeType.File:
                    PurgeFiles(Folder);
                    break;
                case PurgeType.Folder:
                    PurgeFolders(Folder);
                    break;
            }


        }
        public void PurgeFiles(string Folder)
        {
            Logger.Info(string.Format("Purging Files {1} from {0} DaysBack {2} Recurse {3}", Folder, WildCard, DaysBack, Recurse));
            List<Exception> Exceptions = new List<Exception>();
            foreach (string FileName in Directory.GetFiles(Folder,WildCard))
            {
                try
                {
                     FileInfo info = new FileInfo(FileName);
                     if (!DaysBack.HasValue || info.LastWriteTime < DateTime.Now.AddDays(-DaysBack.Value))
                     {

                         if (FakePurge)
                         {
                             Logger.Info(string.Format("Faking Delete {0}", FileName));
                         }
                         else
                             LogDelete(FileName);
                     }
                }
                catch(Exception ex)
                {
                    Exceptions.Add(ex);

                }

            }
            if(Recurse)
            {
                foreach (string SubFolder in Directory.GetDirectories(Folder))
                {
                    try
                    {
                        PurgeFiles(SubFolder);
                    }
                    catch (Exception ex)
                    {
                        Exceptions.Add(ex);
                    }
                    if (RemoveEmptySubFolders && Directory.GetFiles(SubFolder, "*").Count() == 0 && Directory.GetDirectories(SubFolder, "*").Count() == 0)
                    {
                        try
                        {
                            if (FakePurge)
                            {
                                Logger.Info(string.Format("Faking DeleteDirectory {0},0", SubFolder));
                            }
                            else LogDeleteDirectory(SubFolder, false);
                        }
                        catch (Exception ex)
                        {
                            Exceptions.Add(ex);
                        }
                    }
                }

            }
            if (Exceptions.Count > 0) throw new AggregateException(Exceptions);
        }
        public void PurgeFolders(string Folder)
        {
            Logger.Info(string.Format("Purging Folders {1} from {0} {2}", Folder, WildCard, DaysBack));
            List<Exception> Exceptions = new List<Exception>();
            foreach (string SubFolder in Directory.GetDirectories(Folder))
            {
                try
                {
                    DirectoryInfo info = new DirectoryInfo(SubFolder);
                    if (!DaysBack.HasValue || info.LastWriteTime < DateTime.Now.AddDays(-DaysBack.Value))
                    {

                        if (FakePurge)
                        {
                            Logger.Info(string.Format("Faking DeleteDirectory {0},{1}", SubFolder, Recurse));
                        }
                        else
                            LogDeleteDirectory(SubFolder, Recurse);
                    }

                }
                catch (Exception ex)
                {
                    Exceptions.Add(ex);
                }
            }
        }
    }
}
