using System;
using Akka.Actor;
using Microsoft.Owin.Hosting;
using Microsoft.AspNet.SignalR;

namespace Sunergeo.AkkaSignalr.Consumer
{
    public class Program
    {
        static void Main()
        {
            var url = "http://localhost:10080";

            using (WebApp.Start<Startup>(url))
            {
                Console.WriteLine("Server running on {0}", url);
                //var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
                //hubContext.Clients.All.Send("Hello?", "Hi!");

                //var chatHub = new ChatHub();
                //chatHub.Send("faa", "Faa");

                var system = ActorSystem.Create("TuneUp");

                var worker = system.ActorOf(Worker.Props(new Worker.ServerDetails("localhost", 3000)), "worker");
                system.ActorOf(Polling.Props(worker, "tuneup", "localhost:9092"), "poll");


                Console.ReadKey();


            }


        }

        public static IDisposable StartAkkaAndSignalr()
        {
            var url = "http://localhost:10080";
            var webapp = WebApp.Start<Startup>(url);
            Console.WriteLine("Server running on {0}", url);

            var system = ActorSystem.Create("TuneUp");

            var worker = system.ActorOf(Worker.Props(new Worker.ServerDetails("localhost", 3000)), "worker");
            system.ActorOf(Polling.Props(worker, "tuneup", "localhost:9092"), "poll");

            return webapp;
        }
    }
}