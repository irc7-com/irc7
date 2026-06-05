#!/bin/sh
set -e

IMAGE_NAME="redis:latest"
CONTAINER_NAME="irc7_redis"

echo "Stopping and removing existing container: $CONTAINER_NAME"
docker stop $CONTAINER_NAME 2>/dev/null || true
docker rm $CONTAINER_NAME 2>/dev/null || true

echo "Starting new container: $CONTAINER_NAME"
docker run -d \
  -p 6397:6379 \
  --name $CONTAINER_NAME \
  $IMAGE_NAME

echo "Done. Container $CONTAINER_NAME is running."

