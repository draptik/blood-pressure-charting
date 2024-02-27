module Tests

open System
open BloodPressureCharting
open Xunit
open Data

(*
    We are abusing unit tests to run the program and generate the plot.
*)
[<Fact>]
let ``Sample plot`` () =
    let input = Importer.importData "./SampleData/input_chatgpt.csv"
    let measurements = parseMeasurements input
    measurements |> plot