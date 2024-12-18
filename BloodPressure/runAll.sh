#!/bin/bash
#
# Example usage:
#
# ./runAll.sh
#
# or
#
# ./runAll.sh /path/to/some/real/data.md

set -euo pipefail

CHARTING_CONSOLE_APP_PATH="./src/BloodPressureChartingConsole"
CONVERTER_CONSOLE_APP_PATH="./src/BloodPressureMarkdownToCsvConverterConsole"

# Default values
SAMPLE_DATA_PATH="./tests/BloodPressureMarkdownToCsvConverterTests/SampleData"
SAMPLE_DATA_FILE="test_input.txt"
SAMPLE_DATA="${SAMPLE_DATA_PATH}/${SAMPLE_DATA_FILE}"

# When no arguments are provided on the command line, use the sample data set.
# Otherwise use the provided file name as input.
if [ $# -eq 0 ]; then
    INPUT_DATA="${SAMPLE_DATA}"
else
    INPUT_DATA="${1}"
fi

# Check if input data file exists
if [ ! -f "${INPUT_DATA}" ]; then
    echo "Error: Input file '${INPUT_DATA}' does not exist." >&2
    exit 1
fi

CSV_DATA="${INPUT_DATA%.*}.csv"

# Convert the markdownfile to csv
dotnet run \
    --project "${CONVERTER_CONSOLE_APP_PATH}" \
    --markdownfile "${INPUT_DATA}" \
    --csvfile "${CSV_DATA}"

# Run the dotnet F# console application
dotnet run \
    --project "${CHARTING_CONSOLE_APP_PATH}" \
    --inputfile "${CSV_DATA}"
