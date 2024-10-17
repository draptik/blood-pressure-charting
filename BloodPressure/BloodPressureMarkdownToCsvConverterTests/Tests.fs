module Tests

open System
open System.IO
open BloodPressureMarkdownToCsvConverter.MarkdownConverter
open Xunit
open Swensen.Unquote

module InitialTDDTests =

    [<Fact>]
    let ``joining strings`` () =
        let strings = [ "a"; "b" ]
        let actual = strings |> String.concat "\n"
        let expected = "a\nb"
        test <@ actual = expected @>

    let isSuccess r =
        match r with
        | Ok _ -> true
        | Error _ -> false

    let isErrorWith r e =
        match r with
        | Ok _ -> false
        | Error err -> err = e

    let input = [
        "2024-10-15 Morning measurements"
        "- 11.00: 131/80 80"
        "- 12.00: 125/76 75 After lunch"
        "2024-10-16 Routine checks"
        "- 09.30: 118/74 70"
        "- 10.45: 122/78 72 Post-workout"
    ]

    [<Fact>]
    let ``initial happy case test`` () =
        let actual = tryConvertToCsv input

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
    let ``tryParseTimeAndMeasurement works`` (line: string) (expected: string) =
        let result = tryParseTimeAndMeasurement line
        match result with
        | Ok (time, systolic, diastolic, pulse, comment) ->
            // manually wrap the tuple in a string to simplify testing
            let actual = $"{time},{systolic},{diastolic},{pulse},{comment}"
            test <@ actual = expected @>
        | _ -> failwith "test failed"

// module ProcessFileWithVerifyTests =
//     let sampleFilePath = Path.Combine("SampleData", "test_input.txt")
//
//     [<Fact>]
//     let Run () =
//         VerifyChecks.Run()
//
//     [<Fact>]
//     let ``processFile should return correct output for sample input`` () =
//         // Act
//         let actualOutput = MarkdownImport.processFile sampleFilePath
//
//         // Use Verify to check the output
//         Verifier.verify(actualOutput)