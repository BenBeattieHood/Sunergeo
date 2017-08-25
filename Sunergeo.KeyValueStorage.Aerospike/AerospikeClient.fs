namespace Sunergeo.KeyValueStorage.Aerospike

open System
open Sunergeo.KeyValueStorage

type AerospikeReadError =
    Timeout
    | Error of string

type AerospikeWriteError =
    Timeout
    | InvalidVersion
    | Error of String

// A TL;DR of write versions is here https://discuss.aerospike.com/t/locking-a-record-in-aerospike/2152/3
type AerospikeClient(config: KeyValueStoreConfig) =
   
    let valueColumnName = "value"
    let db = "test"
    
    let client = new Aerospike.Client.AerospikeClient(config.Uri.Host, config.Uri.Port)

    let createKey 
        (key: string)
        :Aerospike.Client.Key =
        Aerospike.Client.Key(db, config.TableName, key)

    let createWritePolicy
        (generation: int)
        (generationPolicy: Aerospike.Client.GenerationPolicy)
        :Aerospike.Client.WritePolicy =
        let writePolicy = Aerospike.Client.WritePolicy()
        writePolicy.generation <- generation
        writePolicy.generationPolicy <- Aerospike.Client.GenerationPolicy.EXPECT_GEN_EQUAL
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
            | _ as ex -> 
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

            let writePolicy = createWritePolicy 0 Aerospike.Client.GenerationPolicy.EXPECT_GEN_EQUAL

            let binValue = Aerospike.Client.Bin(valueColumnName, value)
            client.Put(writePolicy, keySet, binValue)
            |> Result.Ok 
        with
            | _ as ex -> 
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

            let writePolicy = createWritePolicy version Aerospike.Client.GenerationPolicy.EXPECT_GEN_EQUAL

            let binValue = Aerospike.Client.Bin(valueColumnName, value)
            client.Put(writePolicy, keySet, binValue)
            |> Result.Ok 
        with
            | _ as ex -> 
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

            let writePolicy = createWritePolicy version Aerospike.Client.GenerationPolicy.EXPECT_GEN_EQUAL
            
            client.Delete(writePolicy, keySet)
            |> ignore
            |> Result.Ok 
        with
            | _ as ex -> 
                ex.Message
                |> AerospikeWriteError.Error |> Result.Error
                
    interface System.IDisposable with
        member this.Dispose() =
            client.Close()
            client.Dispose()