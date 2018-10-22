using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScannerX
{
    class Program
    {
        static void Main(string[] args)
        {
            Scanner scanner = new Scanner(2000);                                            //A new scanner with maximum 2000 sockets in parallel.
            scanner.Timeout = 2000;                                                         //Timeout for TCP connecting.
            scanner.OnResult += Scanner_OnResult;                                           //Fires when successfully connects to an endpoint.
            scanner.Start();                                                                //Start scanning.
            for (int i = 1; i < 10000; i++)
            {                                                                               //Add endpoints to the scan list.
                scanner.Add(new IPEndPoint(IPAddress.Parse("127.0.0.1"), i));               //We can add before or after calling Start(), 
            }                                                                               //as long as Stop() has not been called.

            while (scanner.Count != 0)                                                      //Track status.
            {
                Thread.Sleep(50);
                lock (scanner)
                {
                    Console.Write("Remaining: " + scanner.Count.ToString() + "      \r");   
                }
            }
            scanner.Stop();                                                                 //Stop scanning.

            Console.WriteLine("Mission accomplished.");
            Console.ReadKey();
        }

        private static void Scanner_OnResult(Scanner from, IPEndPoint ep)
        {
            lock (from)
            {
                Console.Write("                                                       \r");
                Console.WriteLine(ep.ToString());
            }
        }
    }
}
