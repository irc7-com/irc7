#!/bin/sh
set -e

IMAGE_NAME="jyonxo/irc7cs:latest"
CONTAINER_NAME="irc7cs"
HOST_MACHINE=$(hostname)

echo "Stopping and removing existing container: $CONTAINER_NAME"
docker stop $CONTAINER_NAME 2>/dev/null || true
docker rm $CONTAINER_NAME 2>/dev/null || true

echo "Starting new container: $CONTAINER_NAME"
docker run -d \
  -v /Users/sky/irc7_config:/incoming-config-cs \
  -e irc7_port=6667 \
  -e irc7_fqdn=$HOST_MACHINE \
  -e irc7_redis=host.docker.internal:6379 \
  -p 6667:6667 \
  --name $CONTAINER_NAME \
  $IMAGE_NAME

echo "Done. Container $CONTAINER_NAME is running."



