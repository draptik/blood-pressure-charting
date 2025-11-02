module Tests

open System
open System.IO
open BloodPressureMarkdownToCsvConverter
open BloodPressureMarkdownToCsvConverter.MarkdownConverter
open Xunit
open VerifyXunit
open Swensen.Unquote

module InitialTDDTests =

    [<Fact>]
    let ``joining strings`` () =
        let strings = [ "a"; "b" ]
        let actual = strings |> String.concat "\n"
        let expected = "a\nb"
        test <@ actual = expected @>

    [<Fact>]
    let ``initial happy case test`` () =
        // Arrange
        let input = [
            "2024-10-15 Morning measurements"
            "- 11.00: 131/80 80"
            "- 12.00: 125/76 75 After lunch"
            "2024-10-16 Routine checks"
            "- 09.30: 118/74 70"
            "- 10.45: 122/78 72 Post-workout"
        ]

        // Act
        let actual = tryConvertToCsv input

        // Assert
        let expected =
            @"2024-10-15-11:00,131,80,80,Morning measurements
2024-10-15-12:00,125,76,75,Morning measurements After lunch
2024-10-16-09:30,118,74,70,Routine checks
2024-10-16-10:45,122,78,72,Routine checks Post-workout"

        match actual with
        | Ok a -> test <@ a = expected @>
        | Error _ -> failwith "should not happen"

    [<Fact>]
    let ``save some random text to a file and read it again`` () =
        let randomText = $"test-{DateTime.Now}"

        // NOTE:
        // The folder "IoTmpFolder" has to contain at least one file (ie `.gitkeep`).
        // The file has to have the attribute "Copy always" in the fsproj file.
        let file = Path.Combine("IoTmpFolder", "somefile.txt")

        File.WriteAllText(file, randomText)
        let actual = File.ReadAllText(file)
        test <@ actual = randomText @>

    [<Theory>]
    [<InlineData("8.00: 131/80 80", "8:00,131,80,80,")>]
    [<InlineData("11:00: 131/80 80", "11:00,131,80,80,")>]
    [<InlineData("12.00: 125/76 75 some comment", "12:00,125,76,75,some comment")>]
    [<InlineData("11:00 131/80 80", "11:00,131,80,80,")>]
    let ``tryParseTimeAndMeasurement works`` (line: string) (expected: string) =
        let result = tryParseTimeAndMeasurement line

        match result with
        | Ok(time, systolic, diastolic, pulse, comment) ->
            // manually wrap the tuple in a string to simplify testing
            let actual = $"{time},{systolic},{diastolic},{pulse},{comment}"
            test <@ actual = expected @>
        | _ -> failwith "test failed"

module MarkdownToCsvConversionTests =
    let sampleFilePath = Path.Combine("SampleData", "test_input.txt")

    // This checks the Verify conventions.
    // For details, see: https://github.com/VerifyTests/Verify?tab=readme-ov-file#conventions-check
    [<Fact>]
    let Run () = VerifyChecks.Run()

    [<Fact>]
    let ``tryConvertToCsv with valid input should return correct output`` () =
        // Arrange
        let input = File.ReadAllLines sampleFilePath |> List.ofArray

        // Act
        let result = tryConvertToCsv input

        // Assert
        match result with
        | Ok actual -> Verifier.verify actual
        | Error e -> failwith $"should not happen: {e}"

    let invalidSampleData: obj[] list = [
        [| [ "24-10-11" ]; InvalidDayFormat "Invalid day format: '24-10-11'" |]
        [| [ "x24-10-11" ]; InvalidDayFormat "Invalid day format: 'x24-10-11'" |]
        [|
            [ "- 8:00: 120/80 50" ]
            TimeLineMissingCorrespondingDayLine "Line is missing day information: '- 8:00: 120/80 50'"
        |]
        [|
            [ "2024-10-11" ]
            FinalDayLineWithoutMeasurements "Final day line has no measurement: '2024-10-11'"
        |]
        [|
            [ "2024-11-11"; "2024-11-12" ]
            PreviousDayLineWithoutMeasurements "Line has no measurement: '2024-11-11'"
        |]
        [|
            [ "2024-12-11"; "- 8.00: 123/89 78"; "2024-12-12" ]
            FinalDayLineWithoutMeasurements "Final day line has no measurement: '2024-12-12'"
        |]
        [|
            [ "2024-01-01"; "- invalid" ]
            InvalidTimeAndMeasurementFormat "Invalid time/measurement format: 'invalid'"
        |]
    ]

    [<Theory>]
    [<MemberData(nameof invalidSampleData)>]
    let ``tryConvertToCsv with invalid input should return correct output`` (lines, expected) =
        match tryConvertToCsv lines with
        | Ok o -> failwith $"should not happen: {o}"
        | Error error -> test <@ error = expected @>