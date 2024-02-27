# Blood Pressure Plotting

Some experiments on how to visualize blood pressure measurements...

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

For further analysis, I can convert the format to a CSV using awk:

```
awk '/^[0-9]{4}-[0-9]{2}-[0-9]{2}/{date=$1; next} {print date, $1, $2, $3, $4}' data.txt
```
