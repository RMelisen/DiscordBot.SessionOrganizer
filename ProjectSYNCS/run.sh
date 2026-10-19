#!/bin/bash
set -e

DISCORD_TOKEN=$(jq --raw-output '.discord_token' /data/options.json)
REGISTER_GLOBALLY=$(jq --raw-output '.register_globally' /data/options.json)

export Discord__Token="$DISCORD_TOKEN"
export Discord__RegisterCommandsGlobally="$REGISTER_GLOBALLY"
export Database__Path="/data/ProjectSYNCS.db"
export DOTNET_ENVIRONMENT="Production"

# Her body on the Minecraft server (MinecraftBodyService): off while minecraft_host is empty.
# The options are new, so an existing install may lack them: every read has a fallback.
export Minecraft__Host="$(jq --raw-output '.minecraft_host // empty' /data/options.json)"
export Minecraft__Port="$(jq --raw-output '.minecraft_port // 25565' /data/options.json)"
export Minecraft__Username="$(jq --raw-output '.minecraft_username // "SYNCS"' /data/options.json)"
export Minecraft__Owner="$(jq --raw-output '.minecraft_owner // empty' /data/options.json)"

exec /app/ProjectSYNCS
