namespace Sunergeo.KeyValueStorage

open System
open Aerospike.Client

type KeyValueStorageConfig = {
    uri: string // placeholder
    port: int
    logger: Sunergeo.Logging.Logger
}

type AerospikeReadError =
    Timeout
    | Error of string

type AerospikeWriteError =
    Timeout
    | InvalidVersion
    | Error of String


// This type is just a placeholder to represent the approx API offered by Aerospike. A TL;DR of write versions is here https://discuss.aerospike.com/t/locking-a-record-in-aerospike/2152/3
type Aerospike(config: KeyValueStorageConfig) =
   
    let valueColumnName = "value"
    
    let client = new AerospikeClient(config.uri, config.port)

    member this.Get
        (
            key: string
        )
        : Result<(string option * int) option, AerospikeReadError> =
        try  // (Database Table Row)

            let keySet = new Key("test", "TableOne", key)
            
            client.Get(null, keySet, valueColumnName)
            |> Option.ofObj
            |> Option.map
                (fun readRec ->
                    readRec.GetValue(valueColumnName) |> Option.ofObj |> Option.map string,
                    readRec.generation
                )
            |> Result.Ok

        with
            | :? _ as ex -> 
                ex.Message
                |> AerospikeReadError.Error |> Result.Error

    member this.Put
        (
            key: string,
            value: string option,
            generation: int
        )
        :Result<unit, AerospikeWriteError> =
        try  // (Database Table Row)
            let keySet = new Key("test", "TableOne", key)

            let writePolicy = WritePolicy()
            writePolicy.generation <- generation
            writePolicy.generationPolicy <- GenerationPolicy.EXPECT_GEN_EQUAL  

            match value with
            | Some value ->
                let values = [| Bin(valueColumnName, value) |]
                client.Put(writePolicy, keySet, values)
            | None ->
                client.Delete(writePolicy, keySet)
                |> ignore
            |> Result.Ok
        with
            | :? _ as ex -> 
                ex.Message
                |> AerospikeWriteError.Error |> Result.Error
                
    interface System.IDisposable with
        member this.Dispose() =
            client.Close()
            client.Dispose()

type ReadError =
    Timeout
    | Error of string

type WriteError = 
    Timeout
    | InvalidVersion
    | Error of string

type KeyValueStore<'Key, 'Value when 'Key : comparison>(config: KeyValueStorageConfig) = 

    let innerStore = new Aerospike(config)

    let serialize
        (value: 'a)
        : string =
        Sunergeo.Core.Todo.todo()   // use http://www.fssnip.net/1l/title/Convert-an-object-to-json-and-json-to-object

    let deserialize
        (serializedValue: string)
        : 'a =
        Sunergeo.Core.Todo.todo()   // use http://www.fssnip.net/1l/title/Convert-an-object-to-json-and-json-to-object

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
        :Result<('Value * int) option, ReadError> = 
            let serializedKey = key |> serialize
            let serializedValueAndVersion = innerStore.Get serializedKey
            
            serializedValueAndVersion
            |> ResultModule.bimap
                (fun (serializedValue, version) ->          // this won't build yet, but I think we need to change aerospike's response to fit this - let's discuss
                    let value = serializedValue |> deserialize
                    value, version
                )
                toReadError

    member this.Put
        (key: 'Key)
        (valueOverVersion: ('Value option) * int)
        :Result<unit, WriteError> =
            let value, version = valueOverVersion
            let serializedKey = key |> serialize
            let serializedValue = value |> Option.map serialize
            let result = innerStore.Put(serializedKey, serializedValue, version)
            
            result
            |> ResultModule.mapFailure
                toWriteError

    interface System.IDisposable with
        member this.Dispose() =
            (innerStore :> System.IDisposable).Dispose()