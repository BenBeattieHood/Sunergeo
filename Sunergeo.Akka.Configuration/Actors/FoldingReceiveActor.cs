using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunergeo.Akka.Actors
{
    public class FoldingReceiveActor<TState, TEvent> : global::Akka.Actor.ReceiveActor
    {
        public FoldingReceiveActor(
            TState initialState,
            Func<TState, TEvent, TState> fold
            )
        {
            var state = initialState;
            Receive<TEvent>(e => state = fold(state, e));
        }

        public static global::Akka.Actor.IActorRef Create<TState, TEvent>(
            global::Akka.Actor.ActorSystem system,
            TState initialState,
            Func<TState, TEvent, TState> fold,
            string id = null
            )
        {
            var props = global::Akka.Actor.Props.Create(() => new FoldingReceiveActor<TState, TEvent>(initialState, fold));
            return system.ActorOf(props, id);
        }
    }
}
