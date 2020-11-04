using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;

namespace IPtoMail
{
    class MailSender
    {
        readonly MailAddress mailUserName;
        readonly string mailPassword;
        readonly string smtpServer;
        readonly ushort smtpPort;
        readonly bool useSSL;

        public MailSender(string senderName, string mailPassword, string smtpServer, ushort smtpPort, bool useSSL)
        {
            this.mailUserName = new MailAddress(senderName);
            this.mailPassword = mailPassword;
            this.smtpServer = smtpServer;
            this.smtpPort = smtpPort;
            this.useSSL = useSSL;
        }
        
        public (List<string> stringsToLog, bool sendedOK) SendMessage(List<string> recipientsList, string body)
        {
            bool sendedOK = true;
            int failes = 0;
            List<string> failedAddresses = new List<string>();
            List<string> outString = new List<string>();

            foreach (var address in recipientsList)
            {
                var recipient = new MailAddress(address);

                using var message = new MailMessage(this.mailUserName, recipient)
                {
                    Subject = "New address",
                    Body = body
                };

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.Credentials = new NetworkCredential(this.mailUserName.ToString(), this.mailPassword);
                    client.EnableSsl = this.useSSL;

                    try
                    {
                        client.Send(message);
                    }
                    catch (Exception)
                    {
                        failes++;
                        failedAddresses.Add(address);
                        sendedOK = false;
                    }
                };
            }
            
            if (sendedOK)
            {
                outString.Add($"{DateTime.Now} sended to {recipientsList.Count}, no errors");
            }
            else
            {
                outString.Add($"{DateTime.Now} sended to {recipientsList.Count - failes}, no errors");
                outString.Add($"\t\terrors to send: {failes}, to addresses");
                foreach (var item in failedAddresses)
                {
                    outString.Add($"\t\t{item}");
                }
            }
            return (outString, sendedOK);
        }

        public (string stringToLog, bool sendedOK) SendMessage (string recipientAddress, string body)
        {
            var recipient = new MailAddress(recipientAddress);

            using var message = new MailMessage(this.mailUserName, recipient)
            {
                Subject = "New address",
                Body = body
            };

            using (var client = new SmtpClient(smtpServer, smtpPort))
            {
                client.Credentials = new NetworkCredential(this.mailUserName.ToString(), this.mailPassword);
                client.EnableSsl = this.useSSL;

                try
                {
                    client.Send(message);
                    return ($"{DateTime.Now} {recipientAddress} sending ok", true);
                }
                catch (Exception)
                {
                    return ($"{DateTime.Now} {recipientAddress} sending fail", false);
                }

            };
        }
    }
}
