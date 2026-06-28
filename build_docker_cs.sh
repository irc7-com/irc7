#!/bin/sh
set -e

: "${GITHUB_TOKEN:?Environment variable GITHUB_TOKEN is required}"

docker buildx build \
  --file Dockerfile.cs \
  --platform linux/arm64 \
  --secret id=GITHUB_TOKEN,env=GITHUB_TOKEN \
  --tag jyonxo/irc7cs:latest \
  --load \
  .
