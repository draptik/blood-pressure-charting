module Tests

open System
open Swensen.Unquote
open Xunit
open BloodPressureCharting
open Data

let isOk r =
    match r with
    | Ok x -> x
    | _ -> failwith "is not ok!"

let shouldBeError r =
    match r with
    | Error _ -> true =! true
    | _ -> true =! false

(*
    We are abusing unit tests to run the program and generate the plot.
 *)
[<Fact>]
let ``Sample plot`` () =
    let validInput = "./SampleData/input_chatgpt.csv"

    validInput
    |> Importer.tryImportData
    |> isOk
    |> tryParseMeasurements
    |> isOk

[<Fact>]
let ``invalid data should be handled`` () =
    let validInput = "./SampleData/input_invalid_systolic.csv"

    validInput
    |> Importer.tryImportData
    |> isOk
    |> tryParseMeasurements
    |> shouldBeError