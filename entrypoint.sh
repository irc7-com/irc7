#!/bin/sh
set -e

# Copy host JSON config files into the container's app output directory if they exist
set -- /incoming-config/*.json
if [ -f "$1" ]; then
    cp /incoming-config/*.json /app/output/
fi

: "${irc7d_type:?Environment variable irc7d_type is required}"
: "${irc7d_port:?Environment variable irc7d_port is required}"
: "${irc7d_fqdn:?Environment variable irc7d_fqdn is required}"

if [ -n "$irc7d_redis" ]; then
    echo "Starting with Redis mode..."
    exec /app/output/irc7 \
        --type "$irc7d_type" \
        --ip 0.0.0.0 \
        --port "$irc7d_port" \
        --fqdn "$irc7d_fqdn" \
        --redis "$irc7d_redis"
else
    : "${irc7d_server:?Environment variable irc7d_server is required}"
    echo "Starting with Server mode..."
    exec /app/output/irc7 \
        --type "$irc7d_type" \
        --ip 0.0.0.0 \
        --port "$irc7d_port" \
        --fqdn "$irc7d_fqdn" \
        --server "$irc7d_server"
fi
