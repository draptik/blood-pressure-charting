#!/bin/sh

input_file="input_raw.md"
output_file="input.csv"

awk_script="convert_md_to_csv.awk"

awk -f \
	$awk_script \
	$input_file \
	>$output_file
