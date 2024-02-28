#!/bin/sh

CONSOLE_APP_PATH="./BloodPressureChartingConsole"
SAMPLE_DATA_PATH="./BloodPressureChartingTests/SampleData"
SAMPLE_DATA_FILE="input_chatgpt.csv"
SAMPLE_DATA="${SAMPLE_DATA_PATH}/${SAMPLE_DATA_FILE}"

dotnet run \
  --project "${CONSOLE_APP_PATH}" \
  --inputfile "${SAMPLE_DATA}"