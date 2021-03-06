﻿using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Net;
using System.Net.Mail;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace IPtoMail
{
    class Program
    {
        static WebClient webClient = new WebClient();
        static string GetIP(out bool IPaddressOK)
        {
            string IP;
            try
            {
                IP = webClient.DownloadString("http://checkip.dyndns.org");
            }
            catch (Exception)
            {
                IPaddressOK = false;
                return "";
            }

            IP = new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}").Matches(IP)[0].ToString();
            IPaddressOK = true;
            return IP;
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
                                logFile = "events.log",
                                passFile = "password.txt",
                                strToPassfile = "First string in this file is for your password (not password for e-mail) to secure storage password for e-mail";

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
                    recipientsList.Add(mailsenderUserName);
                    Logger.Start();
                    CheckingFiles();
                    MailSender sender = new MailSender(mailsenderUserName, GetPassword(), smtpServer, smtpPort, useSSL);
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
                        Console.Title = $"IP is {currentIP}, last IP check: {DateTime.Now.ToShortTimeString() }";
                        //Thread.Sleep(60000);
                        Thread.Sleep(TimeSpan.FromMinutes(1));
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
            if (File.Exists(passFile) && !File.ReadAllText(passFile).StartsWith(strToPassfile) )
            {//можно применить автоматизацию
                string[] stringsInPassFile = File.ReadAllLines(passFile);
                
                if (stringsInPassFile.Length == 1 && stringsInPassFile[0].Length != 0) //1-я заполнена, 2-я нет. Запросить, зашифровать и сохранить пароль
                {
                    string passwordForMail = GetPasswordFromKeyboard();
                    string encryptedPassword = "\n" + EncryptPassword(passwordForMail, stringsInPassFile[0] );
                    
                    File.AppendAllText(passFile, encryptedPassword); //сдесь записывается encryptedPassword в файл 2-й строкой
                    //Console.WriteLine("1-я заполнена, 2-я нет. Можно зашифровать и сохранить пароль");
                    
                    return passwordForMail;
                }
                
                if (stringsInPassFile.Length >= 2 && stringsInPassFile[0].Length != 0 && stringsInPassFile[1].Length != 0) //есть две не пустые строки, пробуем расшифровать
                {
                    return DecryptPassword(stringsInPassFile[1], stringsInPassFile[0]);
                }
            }
            //автоматизация невозможна
            return GetPasswordFromKeyboard();
            
            static string GetPasswordFromKeyboard()
            {
                Console.WriteLine("Enter Password for e-mail sender:");
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
        }

        private static string DecryptPassword(string encrText, string passKey)
        {
            byte[] tmpStr = Encoding.ASCII.GetBytes(passKey); //testing
            byte[] bytesToDecrypt = Convert.FromBase64String(encrText),
                   key = Encoding.Unicode.GetBytes(passKey),
                   iv = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53 };
            string pass;
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor (aes.Key, aes.IV);
                
                using (MemoryStream mStream = new MemoryStream(bytesToDecrypt))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(mStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cryptoStream))
                        {
                            
                            pass = sr.ReadToEnd();//в окончательной версии обернуть в try/catch для обработки исключения неверной расшифровки
                        }
                    }
                }
            }
            return pass;
        }

        private static string EncryptPassword(string plainText, string passKey)
        {
            //Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes();
            byte[] key = Encoding.Unicode.GetBytes(passKey),
                   iv = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53 }; //aes.IV
            plainText = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(plainText));
            
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                
                using (MemoryStream mStream = new MemoryStream())
                {
                    using(CryptoStream cryptoStream = new CryptoStream(mStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cryptoStream))
                        {
                            sw.Write(plainText);
                        }
                        return Convert.ToBase64String(mStream.ToArray());
                    }
                }
            }
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
            try
            {
                if (!File.Exists(passFile))
                {
                    File.AppendAllText(passFile, strToPassfile);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Read file {passFile} for instructions to automate");
                    Console.ResetColor();
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Cant create file {passFile}, automation imposible");
            }

            if (!File.Exists(recipientsFile))
            {
                try
                {
                    using (File.Create(recipientsFile))
                        Console.WriteLine($"Now you can add adresses to file {recipientsFile}");
                    lastTimeRecipientsListChanged = File.GetLastWriteTime(recipientsFile);
                }
                catch (Exception)
                {
                    Console.WriteLine($"Cant create file {recipientsFile}");
                }

            }
            else
            {
                lastTimeRecipientsListChanged = File.GetLastWriteTime(recipientsFile);
                RecipientsListFormer();
            }

        }
    }
}
