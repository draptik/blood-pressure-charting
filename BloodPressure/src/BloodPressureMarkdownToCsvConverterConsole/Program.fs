open Argu
open BloodPressureMarkdownToCsvConverter
open System.IO

type CliArguments =
    | [<Mandatory>] MarkdownFile of string: string
    | [<Mandatory>] CsvFile of string: string

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | MarkdownFile _ -> "specify the markdown file to use."
            | CsvFile _ -> "specify the csv file to use as target."

let parser =
    ArgumentParser.Create<CliArguments>(programName = "BloodPressureImporterConsole")

[<EntryPoint>]
let main argv =
    try
        let parsedResult = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)

        let markdownFile = parsedResult.GetResult MarkdownFile
        let csvFile = parsedResult.GetResult CsvFile

        let markdownContent = File.ReadAllLines markdownFile |> List.ofArray

        match MarkdownConverter.tryConvertToCsv markdownContent with
        | Error e ->
            eprintf $"%A{e}"
            1
        | Ok content ->
            File.WriteAllText(csvFile, content)
            0

    with :? ArguParseException as e ->
        eprintf $"%s{e.Message}"
        1