# ─────────────────────────────────────────────────────────────
# Stage 1 – Build native AOT binary
# Requires clang and zlib for the ILC (IL Compiler) linker step.
# ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Install native AOT build prerequisites
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        clang \
        zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .

# Docker injects TARGETARCH (amd64 | arm64) automatically in multi-platform builds.
# Map it to the corresponding .NET RID.
ARG TARGETARCH
RUN DOTNET_RID="linux-${TARGETARCH}" \
    && if [ "$TARGETARCH" = "amd64" ]; then DOTNET_RID="linux-x64"; \
       elif [ "$TARGETARCH" = "arm64" ]; then DOTNET_RID="linux-arm64"; \
       fi \
    && dotnet publish Irc.Daemon/Irc.Daemon.csproj \
        -c Release \
        -r "$DOTNET_RID" \
        --self-contained true \
        -p:PublishAot=true \
        -o /app/output \
    && mv /app/output/Irc7d /app/output/irc7

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0
WORKDIR /app/output/
COPY --from=build /app/output /app/output
ARG irc7d_port
ARG irc7d_fqdn
ARG irc7d_type
ARG irc7d_server
ARG irc7d_redis
ENV irc7d_port=${irc7d_port}
ENV irc7d_fqdn=${irc7d_fqdn}
ENV irc7d_type=${irc7d_type}
ENV irc7d_server=${irc7d_server}
ENV irc7d_redis=${irc7d_redis}

# Add entrypoint script
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
ENTRYPOINT ["/entrypoint.sh"]
EXPOSE ${irc7d_port}