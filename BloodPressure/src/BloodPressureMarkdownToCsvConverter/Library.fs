namespace BloodPressureMarkdownToCsvConverter

open System
open System.Text.RegularExpressions

type MarkdownImportError =
    | InvalidDayFormat of string
    | InvalidTimeAndMeasurementFormat of string

// NOTE:
// This is "only" a Markdown to CSV converter.
// I'll try not to apply domain knowledge here..
//
// TODO:
// - Update plotting function to include comments
// - Add config file for default location of data
module MarkdownConverter =

    type LineType =
        | DayLine of string * string
        | TimeLine of string

    // TODO: Test
    //
    // Example inputs:
    //
    //  8.00: 131/80 80
    //  12.00: 125/76 75 some comment
    //  11:00: 131/80 80
    let tryParseTimeAndMeasurement (line: string) =
        let timeSeparator = "[:.]"
        let bpSeparator = "/"

        let regex =
            Regex(
                $"""
                (?<time> \d{{1,2}} {timeSeparator} \d{{2}}):\s*       # Time in HH:MM or H.MM format
                (?<systolic>\d+){bpSeparator}                         # Systolic pressure
                (?<diastolic>\d+)\s+                                  # Diastolic pressure
                (?<pulse>\d+)                                         # Pulse
                (?:\s(?<comment>.+))?                                 # Optional comment
                """,
                RegexOptions.IgnorePatternWhitespace ||| RegexOptions.Compiled
            )

        let matches = regex.Match line

        if matches.Success then
            let time = matches.Groups["time"].Value.Replace('.', ':')
            let systolic = int matches.Groups["systolic"].Value
            let diastolic = int matches.Groups["diastolic"].Value
            let pulse = int matches.Groups["pulse"].Value

            let comment =
                if matches.Groups["comment"].Success then
                    matches.Groups["comment"].Value
                else
                    ""

            Ok(time, systolic, diastolic, pulse, comment)
        else
            Error(InvalidTimeAndMeasurementFormat $"Invalid time/measurement format: {line}")

    // TODO: Test
    let tryParseDay (line: string) =
        let parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)

        if parts.Length > 0 && DateTime.TryParse(parts[0]) |> fst then
            let day = parts[0].Trim()
            let comment = if parts.Length > 1 then parts[1].Trim() else ""
            Ok(day, comment)
        else
            Error(InvalidDayFormat $"Invalid day format: {line}")

    let tryProcessData
        (currentDay: string, currentComment: string)
        (line: string)
        : Result<LineType, MarkdownImportError> =
        if line.StartsWith("- ") then
            tryParseTimeAndMeasurement (line.Substring(2))
            |> Result.bind (fun resultValue ->
                let time, systolic, diastolic, pulse, comment = resultValue
                let timestamp = $"{currentDay}-{time}"

                let fullComment =
                    if comment = "" then currentComment
                    else if currentComment = "" then comment
                    else $"{currentComment} {comment}"

                Ok(TimeLine($"{timestamp},{systolic},{diastolic},{pulse},{fullComment}")))
        else
            tryParseDay line
            |> Result.bind (fun (day, comment) -> Ok(DayLine(day, comment)))

    // This is where the magic happens
    let rec tryProcessLines (currentDay: string, currentComment: string) (lines: string list) (acc: string list) =
        match lines with
        | [] -> Ok(acc |> List.rev |> String.concat "\n")
        | line :: rest ->
            tryProcessData (currentDay, currentComment) line
            |> Result.bind (function
                | DayLine(day, comment) when rest <> [] -> tryProcessLines (day, comment) rest acc
                | TimeLine csvLine -> tryProcessLines (currentDay, currentComment) rest (csvLine :: acc)
                | _ -> failwith "Ups - unhandled")

    // Ignore empty lines, and lines starting with `#` and `<!--`
    let sanitizeLines (lines: string list) =
        lines
        |> List.filter (fun line ->
            not (
                String.IsNullOrWhiteSpace(line)
                || line.TrimStart().StartsWith("#")
                || line.TrimStart().StartsWith("<!--")
            ))

    // This is the public API.
    // It returns a result object.
    // Which contains either the CSV as a single string, or an error message ("First error -> Bye")
    let tryConvertToCsv (lines: string list) : Result<string, MarkdownImportError> =
        let sanitizedLines = sanitizeLines lines
        tryProcessLines ("", "") sanitizedLines []