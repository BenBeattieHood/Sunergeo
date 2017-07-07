using System;
using Aerospike.Client;
using Akka.Actor;
using Microsoft.AspNet.SignalR;

namespace Sunergeo.AkkaSignalr.Consumer
{
    public class Worker : ReceiveActor
    {
        //private readonly AerospikeClient _aerospikeClient;
        
        public Worker(ServerDetails aeroSpike)
        {
           // _aerospikeClient = new AerospikeClient(aeroSpike.Server, aeroSpike.Port);
            
            Receive<Work>(w =>
            {
                Console.WriteLine(w.Item);
                //var key = new Key("test", "test", w.Partition.ToString());
                //var bin = new Bin("item", w.Item);

                var chatterthing = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
                chatterthing.Clients.All.broadcastMessage(w.Item);

                //_aerospikeClient.Put(null, key, bin);
            });
        }

        protected override void PostStop()
        {
           // _aerospikeClient?.Dispose();
            base.PostStop();
        }

        public class Work
        {
            public Work(string item, int partition, long offset)
            {
                Item = item;
                Partition = partition;
                Offset = offset;
            }

            public string Item { get; }
            public int Partition { get; }
            public long Offset { get; }
        }

        public class ServerDetails
        {
            public ServerDetails(string server, int port)
            {
                Server = server;
                Port = port;
            }

            public string Server { get; }
            public int Port { get; }
        }

        public static Props Props(ServerDetails aeroSpike)
        {
            return Akka.Actor.Props.Create(() => new Worker(aeroSpike));
        }
    }
}