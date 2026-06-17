FROM debian:bookworm-slim

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       bash jq ca-certificates libssl3 zlib1g \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY app/ .
COPY run.sh /run.sh
RUN chmod +x /run.sh /app/ProjectSYNCS

ENTRYPOINT ["/run.sh"]
