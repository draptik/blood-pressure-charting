module Tests

open System.IO
open Xunit
open BloodPressureImporter

module ProcessFileTests =

    let writeTestFile (filePath: string) (content: string) =
        File.WriteAllText(filePath, content)

    [<Fact>]
    let ``processFile should return correct output for sample input`` () =
        // Arrange
        let tempFilePath = "test_input.txt"
        let sampleInput = """
2024-10-15
- 11.00: 131/80 80
- 12.00: 125/76 75
2024-10-16
- 09.30: 118/74 70
"""
        let expectedOutput =
            """2024-10-15-11:00 131 80 80
2024-10-15-12:00 125 76 75
2024-10-16-09:30 118 74 70"""

        // Write the sample input to a temporary file
        writeTestFile tempFilePath sampleInput

        // Act
        let actualOutput = MarkdownImport.processFile tempFilePath

        // Assert
        Assert.Equal(expectedOutput, actualOutput)

        // Clean up
        File.Delete(tempFilePath)