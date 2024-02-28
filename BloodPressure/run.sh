#!/bin/sh

CONSOLE_APP_PATH="./BloodPressureChartingConsole"

# Default values
SAMPLE_DATA_PATH="./BloodPressureChartingTests/SampleData"
SAMPLE_DATA_FILE="input_chatgpt.csv"
SAMPLE_DATA="${SAMPLE_DATA_PATH}/${SAMPLE_DATA_FILE}"

# When no arguments are provided on the command line, use the sample data set.
# Otherwhise use the provided file name as input.
if [ $# -eq 0 ]; then
	INPUT_DATA="${SAMPLE_DATA}"
else
	INPUT_DATA="${1}"
fi

# Run the dotnet F# console application
dotnet run \
	--project "${CONSOLE_APP_PATH}" \
	--inputfile "${INPUT_DATA}"

