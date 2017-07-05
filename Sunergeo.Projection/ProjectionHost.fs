namespace Sunergeo.Projection

open Sunergeo.Core

open System
open System.Reflection

open Orleankka                       // base types of Orleankka
open Orleankka.FSharp                // additional API layer for F#
open Orleankka.FSharp.Runtime        // Actor base class defined here

// https://github.com/OrleansContrib/Orleankka/wiki/Getting-Started-F%23-(ver-1.0)

type Projector<'PartitionId, 'Events, 'State when 'PartitionId : comparison>(create: EventSourceInitItem<'PartitionId> -> 'State, fold: 'State -> 'Events -> 'State) =
    inherit Actor<EventLogItem<'PartitionId, 'Events>>()

    let mutable state: 'State option = None

    override this.Receive event = 
        task {
            let newState = 
                match event, state with
                | Init metadata, None ->
                    metadata 
                    |> create
                    |> Some

                | Event event, Some state ->
                    event
                    |> fold state 
                    |> Some

                | Init metadata, Some state -> 
                    failwith (sprintf "Invalid state (%O) to apply init (%O) onto" state metadata)

                | Event event, None ->
                    failwith (sprintf "Cannot apply event (%O) to empty state" event)

            state <- newState
            return nothing
        }

type ProjectionHostConfig = {
    Assemblies: Assembly list   // todo: remove
    InstanceId: InstanceId
    KafkaUri: Uri
}

open Kafunk

type ProjectionHost<'PartitionId, 'Events, 'State when 'PartitionId : comparison>(config: ProjectionHostConfig) = 
    let topic = 
        config.InstanceId 
        |> Utils.toTopic<'State>

    let kafkaConsumerGroupName = topic

    let system =
        config.Assemblies           /// I don't think this'll work for us
        |> Array.ofList
        |> ActorSystem.createPlayground
        //|> ActorSystem.bootstrapper

    let kafkaConnection =
        config.KafkaUri.ToString()
        |> Kafka.connHost
        
    let kafkaConsumer = 
        ConsumerConfig.create (kafkaConsumerGroupName, topic)
        |> Consumer.create kafkaConnection

    let kafkaConsumer = 
        Consumer.consume kafkaConsumer
            (fun (consumerState:ConsumerState) (consumerMessageSet:ConsumerMessageSet) -> 
                async {
                    //for message in consumerMessageSet.messageSet.messages do
                    //    message.message.value
                    printfn "member_id=%s topic=%s partition=%i" consumerState.memberId consumerMessageSet.topic consumerMessageSet.partition
                    do! Consumer.commitOffsets kafkaConsumer (ConsumerMessageSet.commitPartitionOffsets consumerMessageSet) 
                }
            )
        |> Async.RunSynchronously

    member this.X = "F#"
