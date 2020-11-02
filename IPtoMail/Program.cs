using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Net;
using System.Net.Mail;
using System.IO;
using System.Collections.Generic;

namespace IPtoMail
{//TODO сохранить адрес для избежания повторной отправки на почту
    class Program
    {
        static string GetIP(out bool IPaddressOK)
        {
            try
            {
                var req = WebRequest.Create("http://checkip.dyndns.org");
                string reqstring;

                using (var reader = new StreamReader(req.GetResponse().GetResponseStream()))
                {
                    reqstring = reader.ReadToEnd();
                }
                string[] a = reqstring.Split(':');//TODO попробовать использовать regex при вычленении IP-адреса
                string a2 = a[1].Substring(1);
                a = a2.Split('<');
                IPaddressOK = true;
                return a[0].ToString();

            }
            catch (Exception)
            {
                IPaddressOK = false;
                return "";
            }

        }

        static void SendMessage(string mailUserName, string mailPassword, string recipientAddress, string body, out bool sendingOK)
        {
            var sender = new MailAddress(mailUserName);
            var recipient = new MailAddress(recipientAddress);

            using var message = new MailMessage(sender, recipient)
            {
                Subject = "New address",
                Body = body
            };

            using (var client = new SmtpClient(smtpServer, smtpPort))
            {
                client.Credentials = new NetworkCredential(mailUserName, mailPassword);
                client.EnableSsl = true;

                try
                {
                    client.Send(message);
                    sendingOK = true;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{DateTime.Now} {recipientAddress} sending ok");
                    Console.ResetColor();
                }
                catch (Exception)
                {
                    sendingOK = false;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{DateTime.Now} {recipientAddress} sending false");
                    Console.ResetColor();
                }

            };
        }

        static void RecipientsListFormer()
        {
Console.WriteLine("recipient list re-formed");
            recipientsList.Clear();
            recipientsList.Add(mailsenderUserName);
            string[] rec;
            string[] recipients = File.ReadAllLines("recipients.list");

            for (int i = 0; i < recipients.Length; i++)
            {
                if (recipients[i].StartsWith("#")) continue;
                rec = recipients[i].Split(' ');
                if (!rec[0].Contains("@")) continue;
                recipientsList.Add(rec[0]);
            }
        }
        
        #region VARIABLES
            static List<string> recipientsList = new List<string>();
            static DateTime lastTimeRecipientsListChanged;
            static string mailsenderUserName = "",
                          smtpServer = "smtp.mail.ru";
            static ushort smtpPort = 25;

            const string recipientsFile = "recipients.list",
                         logFile = "events.log";

        #endregion
        static void Main(string[] args)//args: mailUserName
        {
            if (args.Length != 2)
            {
                Console.WriteLine("The arguments is: mailUserName server:port\ne.g. user@server.com smtp.server.com:25");
            }
            else
            {
                mailsenderUserName = args[0];
                if (TryParseArgs(args[1]))
                {
                    CheckingFiles();
                    Console.WriteLine("Enter mailPassword:");
                    string mailPassword = Console.ReadLine();
                    Console.Clear();
                    string
                        currentIP = "",
                        mbNewIP;
                    while (true)
                    {
                        mbNewIP = GetIP(out bool IPaddressOK);
                        if (currentIP != mbNewIP && IPaddressOK)
                        {
                            currentIP = mbNewIP;
                            Console.WriteLine($"{DateTime.Now} your IP is {currentIP}");
                            CheckRecipientsListUpdate();

                            foreach (string recipient in recipientsList)
                            {
                                SendMessage(mailsenderUserName, mailPassword, recipient, currentIP, out bool sendingOK);
                                if (!sendingOK)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"{DateTime.Now} error sending e-mail. Check username, password and connection");
                                    Console.ResetColor();
                                }

                            }
                        }
                        Thread.Sleep(60000);
                    }

                }
                else
                {
                    Console.WriteLine("Wrong input parameters");
                }
            }

        }

        private static bool TryParseArgs(string v)
        {
            if (v.Contains(":"))
            {
                string[] str = v.Split(':');
                smtpServer = str[0];

                if (ushort.TryParse(str[1], out smtpPort))
                    return true;

            }
            
            return false;
        }

        private static void CheckRecipientsListUpdate()
        {
            if (lastTimeRecipientsListChanged != File.GetLastWriteTime(recipientsFile))
            {
                lastTimeRecipientsListChanged = File.GetLastWriteTime(recipientsFile);
                RecipientsListFormer();
            }
        }

        static void CheckingFiles()
        {
            if (!File.Exists(logFile))
            {
                using (File.Create(logFile));
            }



            if (!File.Exists(recipientsFile))
            {
                using (File.Create(recipientsFile));
                
            }
            else
            {
                RecipientsListFormer();
            }
            //CheckRecipientsListUpdate();
            lastTimeRecipientsListChanged = File.GetLastWriteTime(recipientsFile);
            


        }
    }
}
