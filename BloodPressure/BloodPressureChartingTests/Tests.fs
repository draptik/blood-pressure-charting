module Tests

open System
open BloodPressureCharting
open Xunit
open Data

[<Fact>]
let ``Sample plot`` () =
    let input = Importer.importData "./SampleData/input_chatgpt.csv"
    let measurements = parseMeasurements input
    measurements |> plot