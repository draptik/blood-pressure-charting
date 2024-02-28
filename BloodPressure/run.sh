#!/bin/sh

CONSOLE_APP_PATH="./BloodPressureChartingConsole"
SAMPLE_DATA_PATH="./BloodPressureChartingTests/SampleData"
SAMPLE_DATA_FILE="input_chatgpt.csv"
SAMPLE_DATA="${SAMPLE_DATA_PATH}/${SAMPLE_DATA_FILE}"

if [ $# -eq 0 ]; then
  INPUT_DATA="${SAMPLE_DATA}"
else
  INPUT_DATA="${1}"
fi

dotnet run \
  --project "${CONSOLE_APP_PATH}" \
  --inputfile "${INPUT_DATA}"