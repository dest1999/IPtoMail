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
        {//TODO ?вместо метода ввести класс, передавать имя-пароль в конструкторе?
            var sender = new MailAddress(mailUserName);
            var recipient = new MailAddress(recipientAddress);

            using var message = new MailMessage(sender, recipient)
            {
                Subject = "New address",
                Body = body
            };

            using (var client = new SmtpClient("smtp.mail.ru", 25))
            {
                client.Credentials = new NetworkCredential(mailUserName, mailPassword);
                client.EnableSsl = true;

                try
                {
                    client.Send(message);
                    sendingOK = true;
                }
                catch (Exception)
                {
                    sendingOK = false;
                }

            };
        }

        static void RecipientsListFormer()
        {
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

        static List<string> recipientsList = new List<string>();
        static DateTime lastTimeRecipientsListChanged;
        static string mailsenderUserName = "";

        const string recipientsFile = "recipients.list",
                     logFile = "events.log";
        static void Main(string[] args)//args: mailUserName
        {
            if (args.Length != 1)
            {
                Console.WriteLine("The arguments is: mailUserName");
            }
            else
            {
                mailsenderUserName = args[0];
                CheckingFiles();
                Console.WriteLine("Enter mailPassword:");
                string mailPassword = Console.ReadLine();
                Console.Clear();
                string
                    currentIP = "",
                    mbNewIP;
                while (true)
                {
                    CheckRecipientsListUpdate();
                    mbNewIP = GetIP(out bool IPaddressOK);
                    if (currentIP != mbNewIP && IPaddressOK)
                    {
                        currentIP = mbNewIP;
                        Console.WriteLine($"{DateTime.Now} your IP is {currentIP}");
                        //CheckRecipientsListUpdate();

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
            if (!File.Exists(recipientsFile))
            {
                using(File.Create(recipientsFile))
                
                lastTimeRecipientsListChanged = File.GetLastWriteTime(recipientsFile);
            }
            else CheckRecipientsListUpdate();

            if (!File.Exists(logFile))
            {
                using (File.Create(logFile));

            }

            //lastTimeRecipientsListChanged = File.GetLastWriteTime(recipientsFile);
        }
    }
}
