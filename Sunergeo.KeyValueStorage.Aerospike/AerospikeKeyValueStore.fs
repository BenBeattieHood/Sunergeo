namespace Sunergeo.KeyValueStorage.Aerospike

open Sunergeo
open Sunergeo.Core
open Sunergeo.KeyValueStorage


type AerospikeKeyValueStore<'Key, 'Value when 'Key : comparison>(config: KeyValueStoreConfig) = 

    let innerStore = new AerospikeClient(config)

    let toReadError
        (aerospikeError: AerospikeReadError)
        : ReadError =
        match aerospikeError with
        | AerospikeReadError.Timeout -> ReadError.Timeout
        | AerospikeReadError.Error error -> ReadError.Error error
        
    let toWriteError
        (aerospikeError: AerospikeWriteError)
        : WriteError =
        match aerospikeError with
        | AerospikeWriteError.Timeout -> WriteError.Timeout
        | AerospikeWriteError.InvalidVersion -> WriteError.InvalidVersion
        | AerospikeWriteError.Error error -> WriteError.Error error
        
    interface IKeyValueStore<'Key, 'Value, int> with
        member this.Get
            (key: 'Key)
            :Result<('Value * int) option, ReadError> = 
                let serializedKey = key |> KeyValueStoreModule.serialize
                let serializedValueAndVersion = innerStore.Get serializedKey
            
                serializedValueAndVersion
                |> ResultModule.bimap
                    (fun x ->
                        match x with
                        | Some (serializedValue, version) -> 
                            let value = KeyValueStoreModule.deserialize<'Value> serializedValue
                            (value, version)
                            |> Some
                        | None -> None
                    )
                    toReadError
    
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
                let result = innerStore.Create(serializedKey, serializedValue)
            
                result
                |> ResultModule.mapFailure
                    toWriteError
    
        member this.Delete
            (key: 'Key)
            (generation: int)
            :Result<unit, WriteError> =
                let serializedKey =
                    key
                    |> KeyValueStoreModule.serialize
                let result = innerStore.Delete(serializedKey, generation)
            
                result
                |> ResultModule.mapFailure
                    toWriteError

        member this.Put
            (key: 'Key)
            (valueOverVersion: 'Value * int)
            :Result<unit, WriteError> =
                let serializedKey =
                    key
                    |> KeyValueStoreModule.serialize
                let serializedValue =
                    valueOverVersion
                    |> fst
                    |> KeyValueStoreModule.serialize
                let generation =
                    valueOverVersion
                    |> snd
                let result = innerStore.Put(serializedKey, serializedValue, generation)
            
                result
                |> ResultModule.mapFailure
                    toWriteError

    interface System.IDisposable with
        member this.Dispose() =
            (innerStore :> System.IDisposable).Dispose()