namespace Sunergeo.KeyValueStorage

open System

type KeyValueStorageConfig = {
    uri: string // placeholder
}

type AerospikeWriteError =
    Timeout
    | InvalidToken


// This type is just a stub to represent the approx API offered by Aerospike. A TL;DR of write tokens is here https://discuss.aerospike.com/t/locking-a-record-in-aerospike/2152/3
type AerospikeToken = Guid
type Aerospike() =

    let mutable innerLockSemaphore:Map<string, AerospikeToken> = Map.empty
    let mutable innerStore:Map<string, string> = Map.empty

    member this.Get
        (key: string)
        :Async<(string * AerospikeToken) option> =
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
                )
        }

    member this.Put
        (key: string)
        (value: string)
        (token: AerospikeToken)
        :Async<Result<unit, AerospikeWriteError>> =
        async {
            return lock
                innerStore
                (fun _ ->
                    if innerLockSemaphore.Item(key) = token
                    then 
                        innerStore <- innerStore |> Map.add key value
                        innerLockSemaphore <- innerLockSemaphore |> Map.add key Guid.Empty
                        () |> Result.Ok
                    else
                        AerospikeWriteError.InvalidToken |> Result.Error
                )
        }

type WriteError = 
    Timeout
    | Error of string

type KeyValueStore<'TValue>(config: KeyValueStorageConfig) = 

    let innerLockSemaphore = Aerospike()
    let innerStore = Aerospike()

    let serialize
        (value: 'a)
        : string =
        ""

    let deserialize
        (serializedValue: string)
        : 'a =
        Unchecked.defaultof<'a>

    member this.Get
        (key: string)
        :Async<'TValue option> = 
        async {
            let! serializedValueAndToken = innerStore.Get key
            return serializedValueAndToken |> Option.map
                (fun (serializedValue, token) ->
                    serializedValue |> deserialize
                )
        }

    member this.BeginWrite
        (key: string)
        (value: 'TValue)
        :Async<Result<unit, WriteError>> =
        async {
            let! lockAndToken = innerLockSemaphore.Get key
            return 
                match lockAndToken with
                | None ->
                    
        }
