using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Microsoft.AspNet.SignalR;

namespace Sunergeo.AkkaSignalr.Consumer
{
    public class Polling : ReceiveActor
    {
        private readonly Consumer<string, string> _consumer;

        public Polling(IActorRef worker, string group, string servers)
        {
            _consumer = new Consumer<string, string>(
                BuildConfiguration(group, servers),
                new StringDeserializer(Encoding.UTF8),
                new StringDeserializer(Encoding.UTF8)
            );

            _consumer.OnMessage += (_, msg) =>
            {
                Console.WriteLine($"{msg.Partition}:{msg.Offset}");
                worker.Tell(new Worker.Work(msg.Value, msg.Partition, msg.Offset.Value));
            };

            _consumer.OnConsumeError += (a, b) => Console.WriteLine("boom");

            _consumer.Subscribe(new List<string> { "turtle10k"});

            Receive<Poll>(p =>
            {
                Console.WriteLine("Polling");
                var chatterthing = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();

                chatterthing.Clients.All.broadcastMessage("Polling");

                _consumer.Poll(TimeSpan.FromSeconds(5));
                Self.Tell(p);
            });
        }

        protected override void PreStart()
        {
            Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(100), Self, new Poll(),
                ActorRefs.Nobody);
            base.PreStart();
        }

        protected override void PostStop()
        {
            _consumer?.Dispose();
            base.PostStop();
        }

        public class Poll
        {
        }

        public static Props Props(IActorRef worker, string group, string servers)
        {
            return Akka.Actor.Props.Create(() => new Polling(worker, group, servers));
        }

        private static IEnumerable<KeyValuePair<string, object>> BuildConfiguration(string group, string servers)
        {
            return new Dictionary<string, object>
            {
                {"group.id", group},
                {"enable.auto.commit", true},
                {"auto.commit.interval.ms", 5000},
                {"statistics.interval.ms", 60000},
                {"bootstrap.servers", servers},
                {
                    "default.topic.config",
                    new Dictionary<string, object>
                    {
                        {"auto.offset.reset", "smallest"}
                    }
                }
            };
        }
    }
}