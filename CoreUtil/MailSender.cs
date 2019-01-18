using System;
using System.Collections.Generic;
using System.Linq;
using ZTAMPZ_EMAIL_SWF.CoreUtil;
using System.Net.Mail;
using System.Net;
using System.Data;

namespace ZTAMPZ_EMAIL_SWF
{
    public class MailSender
    {
        public static void Send(string to, string SmtpServer, string Username, string Password, string Port, string EnableSSL, string EncryptedMethod, string SenderAddress, string subject, string body, string MimeType, IDbCommand Command)
        {
            string[] recipients = to.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            string errMessage = "";
            MailMessage mm = new MailMessage();
            try
            {
                string smtpServer = SmtpServer;
                string sender = Username;
                string password = Password;
                string port = Port;
                string enableSSl = EnableSSL;
                string encryptedMethod = EncryptedMethod;
                string senderAddress = SenderAddress;
                string mimeType = MimeType;
                for (int i = 0; i < recipients.Length; i++)
                {
                    try
                    {
                        mm.To.Add(new MailAddress(recipients[i].Trim()));
                    }
                    catch (Exception e2)
                    {
                        errMessage += e2.Message + Environment.NewLine;
                    }
                }
                mm.Subject = subject;
                mm.Body = body;
                mm.IsBodyHtml = mimeType == "text/plain" ? false : true;
                mm.From = new MailAddress(senderAddress);
                SmtpClient smtp = new SmtpClient();
                if (smtpServer != string.Empty)
                    smtp.Host = smtpServer;
                if (encryptedMethod != string.Empty)
                    ServicePointManager.SecurityProtocol = encryptedMethod.ToUpper() == "TLS" ? SecurityProtocolType.Tls : SecurityProtocolType.Ssl3;
                if (port != string.Empty)
                    smtp.Port = Convert.ToInt32(port);
                if (enableSSl != string.Empty)
                    smtp.EnableSsl = enableSSl == "Y" ? true : false;
                if (password != string.Empty)
                    smtp.Credentials = new System.Net.NetworkCredential(sender, password);
                Console.WriteLine("Sending Email......");

                smtp.Send(mm);
                Console.WriteLine("Email Sent.");
            }
            catch (System.Net.Mail.SmtpFailedRecipientException e3)
            {
                Console.Error.WriteLine(e3);

                List<string> failedRecipients = null;
                var es = e3 as SmtpFailedRecipientsException;
                if (es != null)
                {
                    errMessage = Helper.ConcatStringList(es.InnerExceptions.Select(ie => ie.Message), "\r\n");
                    failedRecipients = es.InnerExceptions.Select(ie => ie.FailedRecipient).ToList();
                }
                else
                {
                    errMessage = e3.Message;
                    if (e3 is SmtpFailedRecipientException)
                    {
                        failedRecipients = new List<string>() { ((SmtpFailedRecipientException)e3).FailedRecipient };
                    }
                }

                if (mm.To.Count == failedRecipients.Count)
                {
                    throw;
                }
                else
                {
                    DatabaseHelper.LogError(Command, e3);
                }
                return;
            }
        }
    }
}