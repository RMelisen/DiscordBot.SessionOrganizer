#!/bin/bash
set -e

DISCORD_TOKEN=$(jq --raw-output '.discord_token' /data/options.json)
REGISTER_GLOBALLY=$(jq --raw-output '.register_globally' /data/options.json)

export Discord__Token="$DISCORD_TOKEN"
export Discord__RegisterCommandsGlobally="$REGISTER_GLOBALLY"
export Database__Path="/data/ProjectSYNCS.db"
export DOTNET_ENVIRONMENT="Production"

exec /app/ProjectSYNCS
