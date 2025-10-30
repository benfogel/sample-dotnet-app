#!/bin/bash

# Check if a version argument is provided
if [ -z "$1" ]; then
  echo "Usage: ./build.sh <version>"
  exit 1
fi

VERSION=$1
IMAGE_NAME="gcr.io/cloud-alchemists-sandbox/kamek/sample-dotnet-app"

TAG="${IMAGE_NAME}:${VERSION}"

echo "Building Docker image ${TAG}..."

# Build the Docker image
docker build -t "${TAG}" .

echo "Build complete."

echo "Pushing Docker image ${TAG}..."
docker push "${TAG}"
echo "Push complete."
