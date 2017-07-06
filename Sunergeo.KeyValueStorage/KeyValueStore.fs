namespace Sunergeo.KeyValueStorage

open System
open Aerospike.Client
open System.IO
open System.Runtime.Serialization.Json
open System.Text

type KeyValueStorageConfig = {
    Uri: Uri
    Logger: Sunergeo.Logging.Logger
    TableName: string
}

type AerospikeReadError =
    Timeout
    | Error of string

type AerospikeWriteError =
    Timeout
    | InvalidVersion
    | Error of String


// A TL;DR of write versions is here https://discuss.aerospike.com/t/locking-a-record-in-aerospike/2152/3
type Aerospike(config: KeyValueStorageConfig) =
   
    let valueColumnName = "value"
    let db = "test"
    
    let client = new AerospikeClient(config.Uri.Host, config.Uri.Port)

    let createKey 
        (key: string)
        :Key =
        Key(db, config.TableName, key)

    let createWritePolicy
        (generation: int)
        (generationPolicy: GenerationPolicy)
        :WritePolicy =
        let writePolicy = WritePolicy()
        writePolicy.generation <- generation
        writePolicy.generationPolicy <- GenerationPolicy.EXPECT_GEN_EQUAL
        writePolicy
            

    member this.Get
        (
            key: string
        )
        : Result<(string * int) option, AerospikeReadError> =
        try  // (Database Table Row)

            let keySet = key |> createKey
            
            client.Get(null, keySet, valueColumnName)
            |> Option.ofObj
            |> Option.map
                (fun readRec ->
                    let value = readRec.GetValue(valueColumnName) |> Option.ofObj
                    match value with
                    | Some value -> value :?> string, readRec.generation
                    | None -> failwith "Boom"
                )
            |> Result.Ok

        with
            | :? _ as ex -> 
                ex.Message
                |> AerospikeReadError.Error |> Result.Error
    
    member this.Create
        (
            key: string,
            value: string
        )
        :Result<unit, AerospikeWriteError> =
        try
            let keySet = key |> createKey

            let writePolicy = createWritePolicy 0 GenerationPolicy.EXPECT_GEN_EQUAL

            let binValue = new Bin(valueColumnName, value)
            client.Put(writePolicy, keySet, binValue)
            |> Result.Ok 
        with
            | :? _ as ex -> 
                ex.Message
                |> AerospikeWriteError.Error |> Result.Error

    member this.Delete
        (
            key: string,
            version: int
        )
        :Result<unit, AerospikeWriteError> =
        try
            let keySet = key |> createKey

            let writePolicy = createWritePolicy version GenerationPolicy.EXPECT_GEN_EQUAL
            
            client.Delete(writePolicy, keySet)
            |> ignore
            |> Result.Ok 
        with
            | :? _ as ex -> 
                ex.Message
                |> AerospikeWriteError.Error |> Result.Error

    member this.Put
        (
            key: string,
            value: string,
            version: int
        )
        :Result<unit, AerospikeWriteError> =
        try
            let keySet = key |> createKey

            let writePolicy = createWritePolicy version GenerationPolicy.EXPECT_GEN_EQUAL

            let binValue = new Bin(valueColumnName, value)
            client.Put(writePolicy, keySet, binValue)
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

module KeyValueStoreModule =

    let serialize
        (value: 'a)
        : string =
        use ms = new MemoryStream() 
        (new DataContractJsonSerializer(typeof<'a>)).WriteObject(ms, value) 
        Encoding.Default.GetString(ms.ToArray()) 
        

    let deserialize<'a>
        (serializedValue: string)
        : 'a =
        use ms = new MemoryStream(ASCIIEncoding.Default.GetBytes(serializedValue))
        let obj = (new DataContractJsonSerializer(typeof<'a>)).ReadObject(ms) 
        obj :?> 'a


type KeyValueStore<'Key, 'Value when 'Key : comparison>(config: KeyValueStorageConfig) = 

    let innerStore = new Aerospike(config)

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