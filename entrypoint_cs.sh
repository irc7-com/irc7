#!/bin/sh
set -e

# Copy host JSON config files into the container's working directory if mounted
if [ -d /incoming-config-cs ]; then
    cp /incoming-config-cs/* /app/
fi

: "${irc7_port:?Environment variable irc7_port is required}"
: "${irc7_fqdn:?Environment variable irc7_fqdn is required}"

set -- /app/irc7cs --ip 0.0.0.0 --port "$irc7_port" --fqdn "$irc7_fqdn"
[ -n "$irc7_redis" ] && set -- "$@" --redis "$irc7_redis"
[ -n "$irc7_name"  ] && set -- "$@" --name  "$irc7_name"

exec "$@"
