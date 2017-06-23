module Api

open Freya.Core
open Freya.Machines.Http
open Freya.Types.Http
open Freya.Routers.Uri.Template

let name_ = Route.atom_ "name"

let name =
    freya {
        let! nameO = Freya.Optic.get name_

        match nameO with
        | Some name -> return name
        | None -> return "World" }

let sayHello =
    freya {
        let! name = name

        return Represent.text (sprintf "Hello, %s!" name) }

let helloMachine =
    freyaMachine {
        methods [GET; HEAD; OPTIONS]
        handleOk sayHello }

let root =
    freyaRouter {
        resource "/hello{/name}" helloMachine }
