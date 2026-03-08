#!/bin/bash
set -e

# Copy host JSON config files into the container's app output directory if they exist
if [ -d /incoming-config ] && [ "$(ls -A /incoming-config/*.json 2>/dev/null)" ]; then
    cp /incoming-config/*.json /app/output/
fi

: "${irc7d_type:?Environment variable irc7d_type is required}"
: "${irc7d_port:?Environment variable irc7d_port is required}"
: "${irc7d_fqdn:?Environment variable irc7d_fqdn is required}"
: "${irc7d_server:?Environment variable irc7d_server is required}"

exec /app/output/irc7 --type "$irc7d_type" --ip 0.0.0.0 --port "$irc7d_port" --fqdn "$irc7d_fqdn" --server "$irc7d_server"
