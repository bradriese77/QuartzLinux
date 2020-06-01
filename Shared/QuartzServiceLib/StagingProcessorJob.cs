using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuartzServiceLib
{
    [Export("QuartzServiceJob", typeof(IQuartzServiceJob))]
    public abstract class StagingProcessorJob<T>:QuartzServiceJob<T>
    {
     
        public StagingProcessorJob()
        {


        }

        private string[] emailTo;

        public string[] EmailTo
        {
            get { return emailTo; }
            set { emailTo = value; }
        }
        public string processFolder;
        private string archiveFolder;
        private string errorFolder;
        private string wildCard;

        public string WildCard
        {
            get { return wildCard; }
            set { wildCard = value; }
        }

        public string ArchiveFolder
        {
            get { return archiveFolder; }
            set { archiveFolder = value; }
        }

        public string ErrorFolder
        {
            get { return errorFolder; }
            set { errorFolder = value; }
        }
        public string ProcessFolder
        {
            get { return processFolder; }
            set { processFolder = value; }
        }
        public virtual void ProcessFile(string FilePath)
        {

            throw new NotImplementedException();
        }
        public void ProcessStagingFolder()
        {
            int Errors = 0;
            Exception LastException = null;
            Logger.Info(string.Format("Processing {0} for {1}", ProcessFolder, WildCard));
            foreach (var FilePath in Directory.GetFiles(ProcessFolder, WildCard))
            {
                FileInfo info = new FileInfo(FilePath);
                Logger.Info(string.Format("Processing {0}", FilePath));
                string ErrorPath = Path.Combine(ErrorFolder, info.Name);
                string ArchivePath = Path.Combine(ArchiveFolder, info.Name);
                try
                {
                    ProcessFile(info.FullName);

                    if (!string.IsNullOrEmpty(ArchiveFolder))
                    {
                        LogMove(FilePath, ArchivePath, true);
                    }
                }
                catch (Exception ex)
                {
                    LastException = ex;
                    Logger.Error(ex);
                    Errors++;

                    if (!string.IsNullOrEmpty(ErrorFolder))
                    {
                        LogMove(FilePath, ErrorPath, true);
                    }
                }

            }
            if (Errors > 0)
            {
                string ErrorMsg = string.Format("{0} completed with {1} Errors\r\n{2}", this.GetType().Name, Errors, LastException.ToString());
                if(EmailTo!=null && EmailTo.Length>0)SendEmail(EmailTo, string.Format("Error(s) Processing {0}", this.GetType().Name), ErrorMsg, new string[] { });
                throw new Exception(string.Format("{0} Error(s)", Errors));
            }

        }

    }
}
