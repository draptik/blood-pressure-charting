module Tests

open System
open BloodPressureCharting
open Xunit
open Data

let isOk r =
    match r with
    | Ok x -> x
    | _ -> failwith "is not ok!"

(*
    We are abusing unit tests to run the program and generate the plot.
 *)
[<Fact>]
let ``Sample plot`` () =
    let validInput = "./SampleData/input_chatgpt.csv"
    
    validInput
    |> Importer.importData 
    |> isOk
    |> parseMeasurements
    |> plot