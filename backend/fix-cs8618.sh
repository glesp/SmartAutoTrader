#!/bin/bash

CS8618_LOG="cs8618-warnings.txt"

if [ ! -f "$CS8618_LOG" ]; then
  echo "❌ File $CS8618_LOG not found!"
  exit 1
fi

echo "🔍 Scanning $CS8618_LOG for affected files..."

# Get unique list of .cs files mentioned in the CS8618 log
FILES=$(grep -oE '[^ ]+\.cs' "$CS8618_LOG" | sort -u)

if [ -z "$FILES" ]; then
  echo "✅ No files with CS8618 issues found."
  exit 0
fi

for file in $FILES; do
  if [ -f "$file" ]; then
    echo "📄 Processing $file"

    perl -pi -e 's/(public\s+string\s+\w+\s*\{\s*get;\s*set;\s*\})/$1 = "";/g' "$file"
    perl -pi -e 's/(public\s+int\s+\w+\s*\{\s*get;\s*set;\s*\})/$1 = 0;/g' "$file"
    perl -pi -e 's/(public\s+bool\s+\w+\s*\{\s*get;\s*set;\s*\})/$1 = false;/g' "$file"
    perl -pi -e 's/(public\s+DateTime\s+\w+\s*\{\s*get;\s*set;\s*\})/$1 = DateTime.MinValue;/g' "$file"
    perl -pi -e 's/(public\s+double\s+\w+\s*\{\s*get;\s*set;\s*\})/$1 = 0.0;/g' "$file"
    perl -pi -e 's/(public\s+decimal\s+\w+\s*\{\s*get;\s*set;\s*\})/$1 = 0.0m;/g' "$file"
  else
    echo "⚠️  File not found: $file"
  fi
done

echo "✅ All CS8618 properties patched with default values."
