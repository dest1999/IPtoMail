using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Net;
using System.Net.Mail;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace IPtoMail
{
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

        static List<string> RecipientsListFormer()
        {
            //Console.WriteLine("looking for new recipients in file");
            List<string> mbNewRecipientsList = new List<string>(){ recipientsList[0] };
            List<string> forImmediatelySending = new List<string>();
            string[] tmpStrArray,
                     recipientsAddresses = File.ReadAllLines(recipientsFile);

            foreach (var recipient in recipientsAddresses)
            {
                if (recipient.StartsWith("#")) continue;
                tmpStrArray = recipient.Split(' ');
                if (!tmpStrArray[0].Contains("@")) continue;
                mbNewRecipientsList.Add(tmpStrArray[0]);

                if (!recipientsList.Contains(tmpStrArray[0]))//определяем новые адреса
                {
                    forImmediatelySending.Add(tmpStrArray[0]);
                }
            }

            if (mbNewRecipientsList.Count != recipientsList.Count)//если кол-во адресатов изменилось
            {
                recipientsList = mbNewRecipientsList;
                Logger.WriteLogEvent(new List<string> { $"{DateTime.Now} mailing list was updated" });

            }

            return forImmediatelySending;

        }
        
        #region VARIABLES
            static List<string> recipientsList = new List<string>();
            static DateTime lastTimeRecipientsListChanged;

            public const string recipientsFile = "recipients.list",
                                logFile = "events.log";

        #endregion
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("The arguments is: mailUserName server:port ssl\neg user@server.com smtp.server.com:25 ssl(if use SSL)\nor user@server.com smtp.server.com:25");
            }
            else
            {
                if (TryParseArgs(args, out string smtpServer, out ushort smtpPort, out bool useSSL))
                {
                    string  mbNewIP,
                            currentIP = "",
                            mailsenderUserName = args[0];

                    Console.WriteLine($"Sender name: {mailsenderUserName}\nServer parameters: {args[1]}");
                    Console.WriteLine("Enter Password for e-mail sender:");
                    
                    MailSender sender = new MailSender(mailsenderUserName, GetPassword(), smtpServer, smtpPort, useSSL);
                    recipientsList.Add(mailsenderUserName);
                    CheckingFiles();
                    while (true)
                    {
                        if (CheckRecipientsListUpdate(out List<string> listToSendImmed))
                        {//в список добавлены новые адреса
                            sender.SendMessage(listToSendImmed, currentIP);

                        }
                        mbNewIP = GetIP(out bool IPaddressOK);
                        if (currentIP != mbNewIP && IPaddressOK)
                        {
                            currentIP = mbNewIP;
                            Logger.WriteLogEvent(new List<string> { $"{DateTime.Now} your IP is {currentIP}" });

                            sender.SendMessage(recipientsList, currentIP);

                        }
                        Console.Title = $"IP is {currentIP}, last IP check: {DateTime.Now}";
                        Thread.Sleep(60000);
                    }
                }
                else
                {
                    Console.WriteLine("Wrong input parameters");
                }
            }
        }

        private static bool TryParseArgs(string[] v, out string smtpServer, out ushort smtpPort, out bool ssl)
        {
            smtpServer = null;
            smtpPort = 0;
            ssl = false;

            try
            {
                if (v[2].ToLower() == "ssl")
                    ssl = true;
            }
            catch (Exception)
            {
                ssl = false;
            }

            if (v[0].Contains("@") && v[1].Contains(":"))
            {
                string[] str = v[1].Split(':');
                smtpServer = str[0];
                if (ushort.TryParse(str[1], out smtpPort))
                    return true;
            }
            return false;
        }

        private static string GetPassword()
        {
            string password = Console.ReadLine();
            --Console.CursorTop;
            Console.CursorLeft = 0;

            foreach (var _ in password)
            {
                Console.Write('*');
            }
            Console.WriteLine();
            return password;
        }
        private static bool CheckRecipientsListUpdate(out List<string> listToSendImmed)
        {
            listToSendImmed = null;
            if (lastTimeRecipientsListChanged != File.GetLastWriteTime(recipientsFile))
            {//есть изменения в файле
                lastTimeRecipientsListChanged = File.GetLastWriteTime(recipientsFile);
                listToSendImmed = RecipientsListFormer();
                if (listToSendImmed.Count != 0)
                {//в список адресов добавлены новые, необходимо разослать им немедленно
                    return true;
                }
                return false;
            }
            else
            {//нет изменений в файле
                return false;
            }
        }

        static void CheckingFiles()
        {
            if (!File.Exists(logFile))//возможно этот блок убрать: файл создавать при первой записи в лог
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
