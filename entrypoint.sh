#!/bin/sh
set -e

if [ -n "$irc7d_redis" ]; then
    echo "Starting with Redis mode..."
    exec /app/output/irc7 \
        --type "$irc7d_type" \
        --ip 0.0.0.0 \
        --port "$irc7d_port" \
        --fqdn "$irc7d_fqdn" \
        --redis "$irc7d_redis"
else
    echo "Starting with Server mode..."
    exec /app/output/irc7 \
        --type "$irc7d_type" \
        --ip 0.0.0.0 \
        --port "$irc7d_port" \
        --fqdn "$irc7d_fqdn" \
        --server "$irc7d_server"
fi
