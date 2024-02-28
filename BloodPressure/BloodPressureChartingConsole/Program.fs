open Argu
open BloodPressureCharting

type CliArguments =
    | [<Mandatory>] InputFile of string: string
    
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | InputFile _ -> "specify the data file to use."

let parser = ArgumentParser.Create<CliArguments>(programName = "BloodPressureChartingConsole")

// Example usage with the sample data from the test project:
//
// dotnet run --input-file ../BloodPressureChartingTests/SampleData/input_chatgpt.csv
//
[<EntryPoint>]
let main argv =
    try
        let parseResult = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)
        
        let inputFile = parseResult.GetResult InputFile
        let importResult = Importer.importData inputFile

        match importResult with
        | Error e ->
            match e with
            | ImporterError ie ->
                match ie with
                | FileNotFound fnf ->
                    eprintf $"%s{fnf}"; 1
            | OtherError oe ->
                eprintf $"%s{oe}"; 1
        | Ok lines ->
            lines
            |> Data.parseMeasurements
            |> Data.plot
            0
    with :? ArguParseException as e ->
        eprintf $"%s{e.Message}"; 1
