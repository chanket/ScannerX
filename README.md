# ScannerX
My port scanner in C#, multithreading and asynchronous.

# Example
    Scanner scanner = new Scanner(2000);                                            //A new scanner with maximum 2000 sockets.
    scanner.Timeout = 2000;                                                         //Timeout 2000ms for TCP connecting.
    scanner.OnResult += Scanner_OnResult;                                           //Fires when successfully connects to an endpoint.

    for (int i = 1; i < 10000; i++)
    {                                                                               //Add endpoints of 127.0.0.1 to the scan list.
        scanner.Add(new IPEndPoint(IPAddress.Loopback, i));                         //We can add before or after calling Start(), 
    }                                                                               //as long as Stop() has not been called.

    scanner.Start();                                                                //Start scanning.
    while (scanner.Count != 0)
    {
        Thread.Sleep(50);
        lock (scanner)
        {
            Console.Write("Remaining: " + scanner.Count.ToString() + "      \r");   //Track status.
        }
    }
    scanner.Stop();                                                                 //Stop scanning.

    Console.WriteLine("Mission accomplished.");
    Console.ReadKey();
