FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Restore from the project file alone, so the NuGet layer survives source edits and version
# bumps (the csproj only reads config.yaml when it exists). Rebuilt only when the csproj changes.
COPY ProjectSYNCS.csproj .
RUN dotnet restore ProjectSYNCS.csproj -r linux-arm64 -p:SelfContained=true
COPY . .
RUN dotnet publish ProjectSYNCS.csproj \
    -c Release \
    -r linux-arm64 \
    --self-contained true \
    --no-restore \
    -o /app

# Her body on the Minecraft server (MinecraftBody/, driven by MinecraftBodyService): its npm tree,
# installed from the lockfile alone so this layer too survives source edits. The CI runner is native
# arm64, so the packages are the Pi's.
FROM node:22-bookworm-slim AS body
WORKDIR /body
COPY MinecraftBody/package.json MinecraftBody/package-lock.json ./
RUN npm ci --omit=dev --no-audit --no-fund
COPY MinecraftBody/index.js MinecraftBody/fight.js ./

FROM debian:bookworm-slim AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       bash jq ca-certificates libssl3 zlib1g libicu72 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
# Node is one self-contained binary; the slim base already has the C++ runtime it links against.
COPY --from=body /usr/local/bin/node /usr/local/bin/node
COPY --from=body /body /app/body
COPY run.sh /run.sh
RUN chmod +x /run.sh /app/ProjectSYNCS
ENTRYPOINT ["/run.sh"]
