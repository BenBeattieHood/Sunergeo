namespace Sunergeo.KeyValueStorage.Memory

open Sunergeo
open Sunergeo.Core
open Sunergeo.KeyValueStorage

type KeyValueVersion = Guid
type MemoryKeyValueStore<'Key, 'Value when 'Key : comparison>() =

    let mutable innerLockSemaphore:Map<'Key, KeyValueVersion> = Map.empty
    let mutable innerStore:Map<'Key, string> = Map.empty
    
    interface IKeyValueStore<'Key, 'Value, KeyValueVersion> with
        member this.Get
            (key: 'Key)
            :Result<('Value * KeyValueVersion) option, ReadError> = 
            let asd = 
                lock 
                    innerStore
                    (fun _ ->
                        innerStore
                        |> Map.tryFind key
                        |> Option.map
                            (fun value ->
                                value,
                                innerLockSemaphore |> Map.find key
                            )
                        |> Result.Ok
                    )
                //let serializedKey = key |> KeyValueStoreModule.serialize
                //let serializedValueAndVersion = innerStore.Get serializedKey
            
                //serializedValueAndVersion
                //|> ResultModule.bimap
                //    (fun x ->
                //        match x with
                //        | Some (serializedValue, version) -> 
                //            let value = KeyValueStoreModule.deserialize<'Value> serializedValue
                //            (value, version)
                //            |> Some
                //        | None -> None
                //    )
                //    toReadError
            ()
    
        member this.Create
            (key: 'Key)
            (value: 'Value)
            :Result<unit, WriteError> =
                //let serializedKey =
                //    key
                //    |> KeyValueStoreModule.serialize
                //let serializedValue =
                //    value
                //    |> KeyValueStoreModule.serialize
                //let result = innerStore.Create(serializedKey, serializedValue)
            
                //result
                //|> ResultModule.mapFailure
                //    toWriteError
            ()
    
        member this.Delete
            (key: 'Key)
            (version: KeyValueVersion)
            :Result<unit, WriteError> =
                //let serializedKey =
                //    key
                //    |> KeyValueStoreModule.serialize
                //let result = innerStore.Delete(serializedKey, generation)
            
                //result
                //|> ResultModule.mapFailure
                //    toWriteError
            ()

        member this.Put
            (key: 'Key)
            (valueOverVersion: 'Value * KeyValueVersion)
            :Result<unit, WriteError> =
            lock
                innerStore
                (fun _ ->
                    if version = (innerLockSemaphore |> Map.tryFind key)  // where None = None, or Some x = Some x
                    then
                        innerStore <- innerStore |> Map.add key value
                        innerLockSemaphore <- innerLockSemaphore |> Map.add key Guid.Empty
                        () |> Result.Ok
                    else
                        AerospikeWriteError.InvalidVersion |> Result.Error
                )
                //let serializedKey =
                //    key
                //    |> KeyValueStoreModule.serialize
                //let serializedValue =
                //    valueOverVersion
                //    |> fst
                //    |> KeyValueStoreModule.serialize
                //let generation =
                //    valueOverVersion
                //    |> snd
                //let result = innerStore.Put(serializedKey, serializedValue, generation)
            
                //result
                //|> ResultModule.mapFailure
                //    toWriteError
