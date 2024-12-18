module Tests

open System
open Swensen.Unquote
open Xunit
open BloodPressureCharting
open Data

let takeOk r =
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

    validInput |> Importer.tryImportData |> takeOk |> tryParseMeasurements |> takeOk
// |> plot // <- uncomment this, run the test, and see the plot in your default browser!

[<Fact>]
let ``invalid data should be handled`` () =
    let invalidInput = "./SampleData/input_invalid_systolic.csv"

    invalidInput
    |> Importer.tryImportData
    |> takeOk
    |> tryParseMeasurements
    |> shouldBeError

[<Fact>]
let ``generated chart is correct`` () =
    let validInput = "./SampleData/input_chatgpt.csv"

    validInput
    |> Importer.tryImportData
    |> takeOk
    |> tryParseMeasurements
    |> takeOk
    |> generateChart
    |> toSvg
    |> Verifier.verifyXml