module ExperimentalTests

open System
open BloodPressureMarkdownToCsvConverter.MarkdownConverter
open FsCheck.Xunit
open Xunit
open FsCheck
open Xunit.Abstractions

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

    type MeasurementStringArb =
        static member MeasurementString() = measurementStringArb

    [<Property(Arbitrary = [| typeof<MeasurementStringArb> |])>]
    let ``Generated string matches expected format - V4`` (generatedString: string) =
        generatedString |> tryParseTimeAndMeasurement |> Result.isOk

    [<Property(Arbitrary = [| typeof<MeasurementStringArb> |], Verbose = true)>]
    let ``Generated string matches expected format - V5 - verbosity`` (generatedString: string) =
        generatedString |> tryParseTimeAndMeasurement |> Result.isOk

    type FactWithVerboseOutputTests(output: ITestOutputHelper) =

        let write result =
            output.WriteLine $"The actual result was: '{result}'"

        [<Fact>]
        let ``Generated string matches expected format - V6 - verbosity`` () =
            let property (generatedString: string) =
                write generatedString // <- Use `IOutputHelper` for verbose output
                generatedString |> tryParseTimeAndMeasurement |> Result.isOk

            Prop.forAll measurementStringArb property |> Check.QuickThrowOnFailure