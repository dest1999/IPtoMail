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
        public static bool WriteLogEvent(List<string> events, ConsoleColor color = ConsoleColor.Gray)
        {
            //TODO оформить систему логирования как отдельный класс (static), и => дергать приямо из методов
            Console.ForegroundColor = color;
            foreach (var item in events)
            {
                Console.WriteLine(item);
            }
            Console.ResetColor();

            try
            {
                File.AppendAllLines(Program.logFile, events);

            }
            catch (IOException)//TODO возможно, отложенная запись на случай ошибки?
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine($"< < < < < \t{DateTime.Now}: Cant write event to log\t > > > > >");
                Console.ResetColor();
                return false;
            }
            return true;

        }

    }
}
