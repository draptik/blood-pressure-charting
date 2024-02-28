# Blood Pressure Data Import

To record my blood pressure values I currently use a markdown file with the format:

```markdown
2024-02-22 Do
- 09.30: 123/80 82
- 12.00: 121/90 92
2024-02-23 Fr
- 06.30: 133/90 93
- 11.15: 151/91 91
```

This format is easy to input, and is synchronized using Nextcloud.

I copy the files `convert_md_to_csv.awk` and `create-csv.sh` to the folder containing my data. Then I adapt the file names in the file `create-csv.sh` to match my needs.
