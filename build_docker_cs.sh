#!/bin/sh
set -e

: "${IRCX_SSPI_TOKEN:?Environment variable IRCX_SSPI_TOKEN is required}"

docker buildx build \
  --file Dockerfile.cs \
  --platform linux/arm64 \
  --secret id=IRCX_SSPI_TOKEN,env=IRCX_SSPI_TOKEN \
  --tag jyonxo/irc7cs:latest \
  --load \
  .

