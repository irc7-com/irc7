#!/bin/bash

# Set the image and container name
HOST_MACHINE="metal"
IMAGE_NAME="jyonxo/irc7d"
CONTAINER_NAME="irc7d_cs"

# Pull the latest version of the image (commented to build local)
#echo "Pulling the latest image: $IMAGE_NAME"
#docker pull $IMAGE_NAME

# Stop and remove the existing container
echo "Stopping and removing existing container: $CONTAINER_NAME"
docker stop $CONTAINER_NAME
docker rm $CONTAINER_NAME

# Run the new container with the updated image
echo "Starting new container..."
docker run -d \
  -v /Users/sky/irc7_config:/incoming-config \
  -e irc7d_type=ACS \
  -e irc7d_port=6667 \
  -e irc7d_fqdn=$HOST_MACHINE \
  -e irc7d_server=$HOST_MACHINE:6667 \
  -e irc7d_redis=host.docker.internal:6379 \
  -p 6667:6667 \
  --name $CONTAINER_NAME \
  $IMAGE_NAME

echo "Update complete. Container $CONTAINER_NAME is running with the latest image."