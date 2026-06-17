FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish ProjectSYNCS.csproj \
    -c Release \
    -r linux-arm64 \
    --self-contained true \
    -o /app

FROM debian:bookworm-slim AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       bash jq ca-certificates libssl3 zlib1g \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
COPY run.sh /run.sh
RUN chmod +x /run.sh /app/ProjectSYNCS
ENTRYPOINT ["/run.sh"]
