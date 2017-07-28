namespace Sunergeo.KeyValueStorage

open System
open System.IO
open System.Runtime.Serialization.Json
open System.Text

type KeyValueStoreConfig = {
    Uri: Uri
    Logger: Sunergeo.Logging.Logger
    TableName: string
}

type ReadError =
    Timeout
    | Error of string

type WriteError = 
    Timeout
    | InvalidVersion
    | Error of string


// Maybe move to FsPickler for speed (https://github.com/neuecc/ZeroFormatter#performance)
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


type IKeyValueStore<'Key, 'Value, 'Version when 'Version : comparison> = 

    abstract member Get: 'Key -> Result<('Value * 'Version) option, ReadError>
    
    abstract member Create: 'Key -> 'Value -> Result<unit, WriteError>
    
    abstract member Delete: 'Key -> 'Version -> Result<unit, WriteError>

    abstract member Put: 'Key -> ('Value * 'Version) -> Result<unit, WriteError>