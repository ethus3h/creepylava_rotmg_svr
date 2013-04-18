using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using wServer.realm;
using System.Net.NetworkInformation;
using wServer.networking;
using System.Globalization;
using db;

namespace wServer
{
    static class Program
    {
        internal static SimpleSettings Settings;

        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.Name = "Server Entry Point";

            using (Settings = new SimpleSettings("wServer"))
            {
                RealmManager manager = new RealmManager();
                manager.Initialize();

                Server server = new Server(manager, 2050);
                PolicyServer policy = new PolicyServer();

                Console.CancelKeyPress += (sender, e) => e.Cancel = true;

                policy.Start();
                server.Start();
                Console.WriteLine("Listening at port 2050...");

                while (Console.ReadKey(true).Key != ConsoleKey.Escape) ;

                Console.WriteLine("Terminating...");
                server.Stop();
                policy.Stop();
                manager.Terminate();
            }
        }
    }
}
