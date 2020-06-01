using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppUtilityLib
{
    public class Emailer:AppUtility
    {
        private string smtpHost;
        private string smtpUser;
        private string smtpPwd;
        private string smtpFrom;
    
        public Emailer()
        {

            SmtpFrom = string.Format(GetSetting("SmtpFrom", ""),Environment.MachineName);
            SmtpHost = GetSetting("SmtpHost","scprdsmtp01");
            SmtpUser = GetSetting("SmtpUser", "");
            SmtpPwd = GetSetting("SmtpPwd", "");
        }
        public string SmtpHost
        {
            get
            {
                return smtpHost;
            }

            set
            {
                smtpHost = value;
            }
        }
        public string SmtpUser
        {
            get
            {
                return smtpUser;
            }

            set
            {
                smtpUser = value;
            }
        }

        public string SmtpPwd
        {
            get
            {
                return smtpPwd;
            }

            set
            {
                smtpPwd = value;
            }
        }

        public string SmtpFrom
        {
            get
            {
                return smtpFrom;
            }

            set
            {
                smtpFrom = value;
            }
        }
        public void SendEmail(string[] Tos, string Subject, System.Net.Mail.AlternateView Message, string[] FilePaths,string From=null)
        {
            Logger.Info(string.Format("SendEmail To: {0} Subject: {1} Message: {2}",string.Join(";", Tos),Subject,Message.ToString()));
            System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
            foreach (string To in Tos)
            {
                System.Net.Mail.MailAddress m = new System.Net.Mail.MailAddress(To);
                message.To.Add(m);
            }
            message.Subject = Subject;
            if(From==null)
            message.From = new System.Net.Mail.MailAddress(SmtpFrom);
            else message.From = new System.Net.Mail.MailAddress(From);
            message.AlternateViews.Add(Message);
            foreach (string FilePath in FilePaths)
                if (!string.IsNullOrEmpty(FilePath))
                    message.Attachments.Add(new System.Net.Mail.Attachment(FilePath));
            System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient(SmtpHost);
            smtp.Credentials = new System.Net.NetworkCredential(SmtpUser, SmtpPwd);
            smtp.Send(message);
        }


        public void SendEmail(string[] Tos, string Subject, string Message, string[] FilePaths)
        {
             Logger.Info(string.Format("SendEmail To: {0} Subject: {1} Message: {2} FilePaths: {3}",string.Join(";", Tos),Subject,Message.ToString(),string.Join(",",FilePaths.ToArray())));
            System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
            foreach (string To in Tos)
            {
                System.Net.Mail.MailAddress m = new System.Net.Mail.MailAddress(To);
                message.To.Add(m);
            }
            message.Subject = Subject;
            message.From = new System.Net.Mail.MailAddress(SmtpFrom);
            message.Body = Message;
            foreach (string FilePath in FilePaths)
                if (!string.IsNullOrEmpty(FilePath))
                    message.Attachments.Add(new System.Net.Mail.Attachment(FilePath));
            System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient(SmtpHost);
            smtp.Credentials = new System.Net.NetworkCredential(SmtpUser, SmtpPwd);
            smtp.Send(message);
        }
        public void SendEmail(string To, string Subject, string Message, string FilePath)
        {
            SendEmail(new string[] { To }, Subject, Message, new string[] { FilePath });
        }
    }
}
