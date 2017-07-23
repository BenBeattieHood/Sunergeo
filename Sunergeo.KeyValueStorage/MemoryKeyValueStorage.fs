namespace Sunergeo.KeyValueStorage.Memory

type KeyValueVersion = Guid
type MemoryKeyValueStore() =

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