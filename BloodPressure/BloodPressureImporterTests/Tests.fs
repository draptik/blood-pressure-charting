module Tests

open System.IO
open VerifyXunit
open Xunit
open BloodPressureImporter

module ProcessFileWithVerifyTests =
    let sampleFilePath = Path.Combine("SampleData", "test_input.txt")

    [<Fact>]
    let Run () =
        VerifyChecks.Run()

    [<Fact>]
    let ``processFile should return correct output for sample input`` () =
        // Act
        let actualOutput = MarkdownImport.processFile sampleFilePath

        // Use Verify to check the output
        Verifier.verify(actualOutput)
