﻿using System;
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
                    WriteLogEvent(new List<string> { $"{DateTime.Now} {recipientAddress} sending ok" }, ConsoleColor.Green);

                }
                catch (Exception)
                {
                    sendingOK = false;
                    WriteLogEvent(new List<string> { $"{DateTime.Now} {recipientAddress} sending fail" }, ConsoleColor.Red);
                }

            };
        }
        static bool WriteLogEvent(List<string> events, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            foreach (var item in events)
            {
                Console.WriteLine(item);
            }
            Console.ResetColor();

            try
            {
                File.AppendAllLines(logFile, events);

            }
            catch (IOException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{DateTime.Now}: Cant write event to log");
                Console.ResetColor();
                return false;
            }
            return true;
            
        }
        static void RecipientsListFormer()
        {
            int count = 0;
            Console.WriteLine("looking for new recipients in file");

            string[] tmpStrArray,
                     recipients = File.ReadAllLines(recipientsFile);
            
            for (int i = 0; i < recipients.Length; i++)
            {
                if (recipients[i].StartsWith("#")) continue;
                tmpStrArray = recipients[i].Split(' ');
                if (!tmpStrArray[0].Contains("@")) continue;

                if (!recipientsList.Contains(tmpStrArray[0]))//определяем новые адреса
                {
                    recipientsList.Add(tmpStrArray[0]);//TODO лучше сформировать временный лист новых адресов для немедленной рассылки, а затем очистить список и заново перечитать из файла
                    count++;
                }



            }
            

            WriteLogEvent(new List<string> { $"{DateTime.Now} mailing list was updated" }, ConsoleColor.Gray);
        }
        
        #region VARIABLES
            static List<string> recipientsList = new List<string>();
            static DateTime lastTimeRecipientsListChanged;
            static string mailsenderUserName = "",//TODO после введения объекта-рассыльщика отсюда поубирать лишнее
                          smtpServer = "smtp.mail.ru",
                          currentIP = "";
            static ushort smtpPort = 25;

            const string recipientsFile = "recipients.list",
                         logFile = "events.log";

        #endregion
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("The arguments is: mailUserName server:port\ne.g. user@server.com smtp.server.com:25");
            }
            else
            {
                if (TryParseArgs(args))
                {
                    mailsenderUserName = args[0];
                    Console.WriteLine($"Sender name: {mailsenderUserName}\nServer parameters: {args[1]}");
                    Console.WriteLine("Enter Password for e-mail sender:");
                    string mailPassword = GetPassword();
                    string mbNewIP;
                        //

                    recipientsList.Add(mailsenderUserName);
                    CheckingFiles();
                    while (true)
                    {
                        CheckRecipientsListUpdate();
                        mbNewIP = GetIP(out bool IPaddressOK);
                        if (currentIP != mbNewIP && IPaddressOK)
                        {
                            currentIP = mbNewIP;
                            WriteLogEvent(new List<string> { $"{DateTime.Now} your IP is {currentIP}" }, ConsoleColor.Gray);
                            foreach (string recipient in recipientsList)
                            {
                                SendMessage(mailsenderUserName, mailPassword, recipient, currentIP, out bool sendingOK);
                                if (!sendingOK)
                                {
                                    WriteLogEvent(new List<string> { $"{DateTime.Now} error sending e-mail. Check username, password and connection" }, ConsoleColor.Red);
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

        private static string GetPassword()
        {
            string password = Console.ReadLine();
            --Console.CursorTop;
            Console.CursorLeft = 0;

            foreach (var item in password)
            {
                Console.Write('*');
            }
            Console.WriteLine();
            return password;
        }

        private static bool TryParseArgs(string[] v)
        {
            if (v[0].Contains("@") && v[1].Contains(":"))
            {
                string[] str = v[1].Split(':');
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
