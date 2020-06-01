using log4net;
using log4net.Config;
using Quartz;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace QuartzServiceLib
{
    [Export("QuartzServiceJob", typeof(IQuartzServiceJob))]
    public class EmailJob : QuartzServiceJob<EmailJob>
    {
        public EmailJob()
        {
            JobTypeCronExpression = "59 59 23 31 12 ? 2099";
            JobDataMap map = new JobDataMap();
            map.Add("From", "");
            map.Add("To", "");
            map.Add("Body", "");
            map.Add("IsHtml", "true");
            map.Add("Subject", "");
            map.Add("FilePaths", "");
            JobDetail.SetJobData(map);
            LoadConfigInfo();
        }
        public string[] validFromEmails;
        
        public string [] ValidFromEmails
        {
            get
            {
                if (validFromEmails==null)
                {
                    validFromEmails = GetSetting("ValidFromEmails", "devnotifications@mom365.com").Split(',');
                }
                return validFromEmails;
            }
            set => validFromEmails = value;

        }
        public void SendEMail(IJobExecutionContext context)
        {
            if (context.JobDetail != null)
            {

                string From = context.JobDetail.JobDataMap["From"].ToString();
                string[] Tos = context.JobDetail.JobDataMap["To"].ToString().Split(';');
                string Body = context.JobDetail.JobDataMap["Body"].ToString();
                bool IsHtml = context.JobDetail.JobDataMap["IsHtml"].ToString() == "true";
                string Subject = context.JobDetail.JobDataMap["Subject"].ToString();
                string[] FilePaths = context.JobDetail.JobDataMap["FilePaths"].ToString().Split(',');
                SendMail(Tos, Body, IsHtml, Subject, FilePaths, From);
            }
        }
        public void SendMail(string []Tos,string Body,bool IsHtml,string Subject,string []FilePaths = null, string From=null)
        {
            if (!string.IsNullOrEmpty(From))
            {
                System.Net.Mail.MailAddress m = new System.Net.Mail.MailAddress(From);
                if(!ValidFromEmails.Contains(m.Address.ToLower()))
                {
                    throw new Exception($"{m.Address} is not a valid Email Sender for this Class");
                }
            }

            AlternateView Message;


            if (IsHtml)
            {
                Message = AlternateView.CreateAlternateViewFromString(Body, new ContentType("text/html"));
            }
            else
            {
                Message = AlternateView.CreateAlternateViewFromString(Body);
            }

            if (Tos.Length > 0 && !string.IsNullOrEmpty(Body))
             /   SendEmail(Tos, Subject, Message, FilePaths,From);
        }

        public override void ExecuteJob(IJobExecutionContext context)
        {
            List<Exception> Exceptions = new List<Exception>();

            SendEMail(context);
            
            if (Exceptions.Count > 0) throw new AggregateException(Exceptions);
        }


    }
}
