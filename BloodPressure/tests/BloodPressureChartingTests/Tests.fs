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

let validInput = "./SampleData/input_valid.csv"
let invalidInput = "./SampleData/input_invalid_systolic.csv"

[<Fact>]
let ``invalid data should be handled`` () =
  invalidInput
  |> Importer.tryImportData
  |> takeOk
  |> tryParseMeasurements
  |> shouldBeError

[<Fact>]
let ``generated chart is correct`` () =
  validInput
  |> Importer.tryImportData
  |> takeOk
  |> tryParseMeasurements
  |> takeOk
  |> generateChart
  |> toSvg
  |> Verifier.verifyXml