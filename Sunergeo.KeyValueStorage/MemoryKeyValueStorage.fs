namespace Sunergeo.KeyValueStorage.Memory

open System
open Sunergeo
open Sunergeo.Core
open Sunergeo.KeyValueStorage

type MemoryKeyValueVersion = Guid
type MemoryKeyValueStore<'Key, 'Value>() =

    let mutable innerLockSemaphore:Map<string, MemoryKeyValueVersion> = Map.empty
    let mutable innerStore:Map<string, string> = Map.empty
    
    interface IKeyValueStore<'Key, 'Value, MemoryKeyValueVersion> with
        member this.Get
            (key: 'Key)
            :Result<('Value * MemoryKeyValueVersion) option, ReadError> = 
            
            let serializedKey = key |> KeyValueStoreModule.serialize
            
            lock 
                innerStore
                (fun _ ->
                    innerStore
                    |> Map.tryFind serializedKey
                    |> Option.map
                        (fun value ->
                            value |> KeyValueStoreModule.deserialize<'Value>,
                            innerLockSemaphore |> Map.find serializedKey
                        )
                    |> Result.Ok
                )
    
        member this.Create
            (key: 'Key)
            (value: 'Value)
            :Result<unit, WriteError> =

            let serializedKey =
                key
                |> KeyValueStoreModule.serialize

            let serializedValue =
                value
                |> KeyValueStoreModule.serialize
                
            lock
                innerStore
                (fun _ ->
                    if None = (innerLockSemaphore |> Map.tryFind serializedKey)  // where None = None, or Some x = Some x
                    then
                        innerStore <- innerStore |> Map.add serializedKey serializedValue
                        let newVersion = Guid.NewGuid().ToByteArray() |> MemoryKeyValueVersion
                        innerLockSemaphore <- innerLockSemaphore |> Map.add serializedKey newVersion
                        () |> Result.Ok
                    else
                        WriteError.InvalidVersion |> Result.Error
                )
    
        member this.Delete
            (key: 'Key)
            (version: MemoryKeyValueVersion)
            :Result<unit, WriteError> =
            let serializedKey =
                key
                |> KeyValueStoreModule.serialize
                
            lock
                innerStore
                (fun _ ->
                    if version = (innerLockSemaphore |> Map.find serializedKey)  // where None = None, or Some x = Some x
                    then
                        innerStore <- innerStore |> Map.remove serializedKey
                        innerLockSemaphore <- innerLockSemaphore |> Map.remove serializedKey
                        () |> Result.Ok
                    else
                        WriteError.InvalidVersion |> Result.Error
                )

        member this.Put
            (key: 'Key)
            (valueOverVersion: 'Value * MemoryKeyValueVersion)
            :Result<unit, WriteError> =

            let serializedKey =
                key
                |> KeyValueStoreModule.serialize

            let serializedValue =
                valueOverVersion
                |> fst
                |> KeyValueStoreModule.serialize

            let version = 
                valueOverVersion
                |> snd

            lock
                innerStore
                (fun _ ->
                    if version = (innerLockSemaphore |> Map.find serializedKey)  // where None = None, or Some x = Some x
                    then
                        innerStore <- innerStore |> Map.add serializedKey serializedValue
                        let newVersion = Guid.NewGuid().ToByteArray() |> MemoryKeyValueVersion
                        innerLockSemaphore <- innerLockSemaphore |> Map.add serializedKey newVersion
                        () |> Result.Ok
                    else
                        WriteError.InvalidVersion |> Result.Error
                )