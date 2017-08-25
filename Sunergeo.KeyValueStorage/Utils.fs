module Sunergeo.KeyValueStorage.KeyValueStorageUtils

open Sunergeo.KeyValueStorage

type ReadWriteError =
    | ReadError of ReadError
    | WriteError of WriteError

let createOrPut
    (store: IKeyValueStore<'key, 'value, 'version>)
    (key: 'key)
    (value: 'value)
    : Result<unit, ReadWriteError> =

    ResultModule.result {
        let! version = 
            store.Get key
            |> ResultModule.bimap
                (Option.map snd)
                ReadWriteError.ReadError

        return!
            match version with
            | None ->
                store.Create key value
            | Some version ->
                store.Put key (value, version)
            |> ResultModule.mapFailure ReadWriteError.WriteError
    }
