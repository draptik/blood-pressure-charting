#!/usr/bin/awk -f
#
# Here is an example input:
#
#2024-02-22 Do
#- 11.00: 131/80 80
#- 21.00: 126/69 80
#2024-02-23 Fr
#- 07.00: 129/78 86
#
# The expected output is:
#
#2024-02-22-11:00 131 80 80
#2024-02-22-21:00 126 69 80
#2024-02-23-07:00 129 78 86

BEGIN {
  # Actions to be performed before processing the input
}

# This skips the first line
NR == 1 { next }

# Match the date line and store the date (format: YYYY-MM-DD)
# In case this matches, skip to the next line
/^[0-9]{4}-[0-9]{2}-[0-9]{2}/ {
  date = $1
  next # <- this skips to the next line!
}

{
  # This is one of those lines which was not matched by the previous rule
  #
  # Here is an example line:
  #
  # - 11.00: 131/80 80
  #
  # The fields are split by spaces by default.
  #
  # '$1' is the '-' symbol, so we start from $2
  time = $2
  pressure = $3
  pulse = $4

  # Format the timestamp
  # 'sub' replaces inline!
  sub(/:$/, "", time) # Remove trailing colon
  sub(/\./, ":", time);  # Replace dot with colon
  timestamp = date "-" time

  # Split the pressure field into two variables
  # The pressure is in the format: systolic/diastolic
  split(pressure, bloodPressure, "/")
  systolic = bloodPressure[1]
  diastolic = bloodPressure[2]

  # Print the formatted data (it is currently separated by spaces)
  print timestamp, systolic, diastolic, pulse
}

END {
  # Actions to be performed after processing the input
}
