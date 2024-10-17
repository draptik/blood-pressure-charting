namespace BloodPressureImporter

open System
open System.IO
open System.Text.RegularExpressions

module MarkdownImport =

    let extractDate (line: string) =
        // Match the date line (format: YYYY-MM-DD)
        let datePattern = @"^\d{4}-\d{2}-\d{2}"
        if Regex.IsMatch(line, datePattern) then
            Regex.Match(line, datePattern).Value
        else
            ""

    let formatTime (time: string) =
        time.Replace(":", "").Replace(".", ":")

    let extractPressure (pressure: string) =
        // Split the pressure into systolic and diastolic
        let pressureParts = pressure.Split('/')
        if pressureParts.Length = 2 then
            let systolic = pressureParts[0]
            let diastolic = pressureParts[1]
            (systolic, diastolic)
        else
            ("", "")

    let formatTimestamp date time =
        let formattedTime = formatTime time
        $"{date}-{formattedTime}"

    let formatOutput timestamp systolic diastolic pulse =
        $"{timestamp} {systolic} {diastolic} {pulse}"

    let processLine (date: string) (line: string) =
        let fields = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
        if fields.Length >= 4 then
            let time = fields[1]
            let pressure = fields[2]
            let pulse = fields[3]

            // Get formatted timestamp
            let timestamp = formatTimestamp date time

            // Get systolic and diastolic pressure values
            let systolic, diastolic = extractPressure pressure

            // Return the final formatted output string
            formatOutput timestamp systolic diastolic pulse
        else
            ""

    let processFile filePath =
        let mutable date = ""

        // Use a list to collect all processed lines
        let results =
            File.ReadLines(filePath)
            |> Seq.fold (fun acc line ->
                // Check if the line contains a date
                let newDate = extractDate line
                if newDate <> "" then
                    date <- newDate
                    acc // Continue accumulating without adding a new entry
                elif date <> "" then
                    // Only process lines if a date has been established
                    let output = processLine date line
                    if output <> "" then
                        acc @ [output] // Add the processed line to the accumulator
                    else
                        acc
                else
                    acc
            ) []

        // Join all results into a single string
        String.concat "\n" results
    // Usage

    let filePath = "input.txt" // Replace with your actual input file path
    let output = processFile filePath
    printfn $"%s{output}"
