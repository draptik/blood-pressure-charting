open Argu
open BloodPressureCharting

type CliArguments =
    | [<Mandatory>] InputFile of string: string

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | InputFile _ -> "specify the data file to use."

let parser =
    ArgumentParser.Create<CliArguments>(programName = "BloodPressureChartingConsole")

// Example usage with the sample data from the test project:
//
// dotnet run --inputfile ../BloodPressureChartingTests/SampleData/input_chatgpt.csv
//
[<EntryPoint>]
let main argv =
    try
        let parseResult = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)

        let inputFile = parseResult.GetResult InputFile
        let importResult = Importer.tryImportData inputFile

        // Note to self: `eprintf` prints to STDERR...
        match importResult with
        | Error e ->
            match e with
            | ImporterError ie ->
                match ie with
                | FileNotFound fnf ->
                    eprintf $"%s{fnf}"
                    1
            | InvalidDateFormatError idfe ->
                eprintf $"%s{idfe}"
                1
            | NotAnIntError naie ->
                eprintf $"%s{naie}"
                1
            | OtherError oe ->
                eprintf $"%s{oe}"
                1
        | Ok lines ->
            let parsed = lines |> Data.tryParseMeasurements

            match parsed with
            | Error ep ->
                let errorMessage = ep |> string
                eprintf $"%s{errorMessage}"
                1
            | Ok data ->
                data |> Data.showInBrowser
                0

    with :? ArguParseException as e ->
        eprintf $"%s{e.Message}"
        1