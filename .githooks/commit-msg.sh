#!/bin/sh

# Commit message file
commit_msg_file=$1
commit_msg=$(cat "$commit_msg_file")

# Regex: Must have Jira key like PROJ-123 followed by description
regex='^[A-Z][A-Z0-9]+-[0-9]+[[:space:]]+.+'

if ! echo "$commit_msg" | grep -Eq "$regex"; then
  echo "❌ Commit message rejected."
  echo "➡️ Format must be: JIRA-123 <meaningful description>"
  echo "Example: PROJ-456 Fix login issue"
  exit 1
fi
