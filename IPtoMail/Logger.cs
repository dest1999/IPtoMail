using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IPtoMail
{
    static class Logger
    {
        static List<string> eventStorage = new List<string>();
        static bool eventStorageNotEmpty;

        public static bool WriteLogEvent(List<string> events, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            foreach (var item in events)
            {
                Console.WriteLine(item);
            }
            Console.ResetColor();

            if (eventStorage.Count != 0)
                foreach (var item in events)
                {
                    eventStorage.Add(item);
                }
            else
            {
                eventStorage = events;
            }

            try
            {
                File.AppendAllLines(Program.logFile, eventStorage);
            }
            catch (Exception)//TODO возможно, отложенная запись на случай ошибки?
            {

                eventStorageNotEmpty = true;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.WriteLine($"< < < < < \t{DateTime.Now}: Cant write events to log, events buffering\t > > > > >");
                Console.ResetColor();
                return false;
            }
            
            if (eventStorageNotEmpty)
            {
                eventStorageNotEmpty = false;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{DateTime.Now} Event log accessible, events queue wrote to log successful");
                Console.ResetColor();
            }
            eventStorage.Clear(); //если запись в файл успешна
            return true;

        }

    }
}
