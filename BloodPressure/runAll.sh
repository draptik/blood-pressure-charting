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
SAMPLE_MARKDOWN_DATA_PATH="./tests/BloodPressureMarkdownToCsvConverterTests/SampleData"
SAMPLE_MARKDOWN_DATA_FILE="test_input.md"
SAMPLE_MARKDOWN_DATA="${SAMPLE_MARKDOWN_DATA_PATH}/${SAMPLE_MARKDOWN_DATA_FILE}"

# When no arguments are provided on the command line, use the sample data set.
# Otherwise use the provided markdown file name as input.
if [ $# -eq 0 ]; then
    MARKDOWN_DATA="${SAMPLE_MARKDOWN_DATA}"
else
    MARKDOWN_DATA="${1}"
fi

# Check if input markdown data file exists
if [ ! -f "${MARKDOWN_DATA}" ]; then
    echo "Error: Input file '${MARKDOWN_DATA}' does not exist." >&2
    exit 1
fi

# Convert the markdown file to csv
CSV_DATA="${MARKDOWN_DATA%.*}.csv"
dotnet run \
    --project "${CONVERTER_CONSOLE_APP_PATH}" \
    --markdownfile "${MARKDOWN_DATA}" \
    --csvfile "${CSV_DATA}"

# Run the dotnet F# console application with the csv data
dotnet run \
    --project "${CHARTING_CONSOLE_APP_PATH}" \
    --inputfile "${CSV_DATA}"
