module Tests

open System
open System.IO
open BloodPressureMarkdownToCsvConverter
open BloodPressureMarkdownToCsvConverter.MarkdownConverter
open FsCheck.Xunit
open Xunit
open FsCheck
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
    let ``tryParseTimeAndMeasurement works`` (line: string) (expected: string) =
        let result = tryParseTimeAndMeasurement line

        match result with
        | Ok(time, systolic, diastolic, pulse, comment) ->
            // manually wrap the tuple in a string to simplify testing
            let actual = $"{time},{systolic},{diastolic},{pulse},{comment}"
            test <@ actual = expected @>
        | _ -> failwith "test failed"

module LearningPropertyBasedTesting =

    let reverse xs = xs |> List.rev

    [<Fact>]
    let ``Reversing a list twice gives the original list`` () =
        let property (list: int list) = list |> reverse |> reverse = list
        Check.QuickThrowOnFailure property

    [<Fact>]
    let ``Sum of integers is commutative`` () =
        let generator = Gen.sized (fun _ -> Gen.two (Gen.choose (0, 100)))

        let property (a, b) = a + b = b + a

        Prop.forAll (generator |> Arb.fromGen) property |> Check.QuickThrowOnFailure

    [<Property>]
    let ``Reversing a list twice gives the original list - xunit version`` (list: int list) =
        list |> reverse |> reverse = list

    type CustomType = { A: int; B: string }

    let customTypeGenerator =
        Gen.map2
            (fun a b -> { A = a; B = b })
            (Gen.choose (1, 100))
            (Gen.listOf (Gen.elements [ 'a' .. 'z' ])
             |> Gen.map (fun chars -> String(chars |> Array.ofList)))

    let customTypeArb = Arb.fromGen customTypeGenerator

    [<Fact>]
    let ``CustomType has consistent string length`` () =
        let property (ct: CustomType) = ct.B.Length >= 0 // A trivial property just for illustration

        Prop.forAll customTypeArb property |> Check.QuickThrowOnFailure

    [<Property>]
    let ``Division by a non-zero number does not throw`` (x: int, y: int) =
        y <> 0
        ==> lazy
            (x / y |> ignore
             true)

    let constrainedCustomTypeGen =
        Gen.sized (fun _ ->
            Gen.map2
                (fun a b -> { A = a; B = b })
                (Gen.choose (1, 100))
                (Gen.listOfLength 10 (Gen.elements [ 'a' .. 'z' ])
                 |> Gen.map (fun chars -> String(chars |> Array.ofList))))

    let constrainedCustomTypeArb = Arb.fromGen constrainedCustomTypeGen

    [<Fact>]
    let ``CustomType satisfies constraint`` () =
        let property (ct: CustomType) = ct.A > 0 && ct.B.Length <= 10 // Example property for validation

        Prop.forAll constrainedCustomTypeArb property |> Check.QuickThrowOnFailure



module TryParseTimeAndMeasurementPropertyBasedTests =

    let timeGen =
        Gen.map2 (fun h m -> $"%d{h}:%02d{m}") (Gen.choose (0, 23)) (Gen.choose (0, 59))

    let bloodPressureGen =
        Gen.map2 (fun systolic diastolic -> $"%d{systolic}/%d{diastolic}") (Gen.choose (90, 200)) (Gen.choose (60, 120))

    let heartRateGen = Gen.choose (50, 150) |> Gen.map string

    let commentGen =
        Gen.listOfLength 10 (Gen.elements [ 'a' .. 'z' ])
        |> Gen.map (fun chars -> String(chars |> Array.ofList))

    let measurementStringGen =
        Gen.map4
            (fun time bp hr comment -> $"%s{time}: %s{bp} %s{hr} %s{comment}")
            timeGen
            bloodPressureGen
            heartRateGen
            commentGen

    let measurementStringArb = Arb.fromGen measurementStringGen

    [<Fact>]
    let ``Generated string matches expected format - V1`` () =
        let property (generatedString: string) =
            Text.RegularExpressions.Regex.IsMatch(
                generatedString,
                @"^\d{1,2}:\d{2}: \d{2,3}/\d{2,3} \d{2,3} [a-z]{10}$"
            )

        Prop.forAll measurementStringArb property |> Check.QuickThrowOnFailure

    [<Fact>]
    let ``Generated string matches expected format - V2`` () =
        let property (generatedString: string) =
            let result = tryParseTimeAndMeasurement generatedString
            result.IsOk

        Prop.forAll measurementStringArb property |> Check.QuickThrowOnFailure

    [<Fact>]
    let ``Generated string matches expected format - V3`` () =
        let property (generatedString: string) =
            generatedString |> tryParseTimeAndMeasurement |> Result.isOk

        Prop.forAll measurementStringArb property |> Check.QuickThrowOnFailure

    type FullStringArb =
        static member FullString() = measurementStringArb

    [<Property(Arbitrary = [| typeof<FullStringArb> |])>]
    let ``Generated string matches expected format - V4`` (generatedString: string) =
        generatedString |> tryParseTimeAndMeasurement |> Result.isOk

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