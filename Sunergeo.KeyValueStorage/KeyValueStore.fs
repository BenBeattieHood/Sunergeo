namespace Sunergeo.KeyValueStorage

open System

type KeyValueStorageConfig = {
    uri: string // placeholder
    logger: Sunergeo.Logging.Logger
}

type AerospikeReadError =
    Timeout
    | Error of string

type AerospikeWriteError =
    Timeout
    | InvalidVersion
    | Error of string


// This type is just a placeholder to represent the approx API offered by Aerospike. A TL;DR of write versions is here https://discuss.aerospike.com/t/locking-a-record-in-aerospike/2152/3
type KeyValueVersion = Guid
type Aerospike() =

    let mutable innerLockSemaphore:Map<string, KeyValueVersion> = Map.empty
    let mutable innerStore:Map<string, string> = Map.empty

    member this.Get
        (
            key: string
        )
        :Async<Result<(string * KeyValueVersion) option, AerospikeReadError>> =
        async {
            return lock 
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
        }

    member this.Put
        (
            key: string,
            value: string,
            version: KeyValueVersion option
        )
        :Async<Result<unit, AerospikeWriteError>> =
        async {
            return lock
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
        }


type ReadError =
    Timeout
    | Error of string

type WriteError = 
    Timeout
    | InvalidVersion
    | Error of string

type KeyValueStore<'Key, 'Value when 'Key : comparison>(config: KeyValueStorageConfig) = 

    let innerStore = Aerospike()

    let serialize
        (value: 'a)
        : string =
        Sunergeo.Core.Todo.todo()

    let deserialize
        (serializedValue: string)
        : 'a =
        Sunergeo.Core.Todo.todo()

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
        
    member this.Get
        (key: 'Key)
        :Async<Result<('Value * KeyValueVersion) option, ReadError>> = 
        async {
            let serializedKey = key |> serialize
            let! serializedValueAndVersion = innerStore.Get serializedKey

            return 
                serializedValueAndVersion
                |> ResultModule.bimap
                    (Option.map (fst >> deserialize))
                    toReadError
        }

    member this.Put
        (key: 'Key)
        (version: KeyValueVersion option)
        (value: 'Value)
        :Async<Result<unit, WriteError>> =
        async {
            let serializedKey = key |> serialize
            let serializedValue = value |> serialize
            let! result = innerStore.Put(serializedKey, serializedValue, version)

            return 
                result
                |> ResultModule.mapFailure
                    toWriteError
        }
