ARG irc7d_port=6667
ARG irc7d_fqdn=localhost
ARG irc7d_type=ACS
ARG irc7d_server=127.0.0.1:6667

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
RUN \
    git clone https://github.com/IRC7/IRC7.git && \
RUN \
    #git clone https://github.com/IRC7/IRC7.git && \
    dotnet publish IRC7/Irc.Daemon \
      /p:PublishTrimmed=false \
      /p:PublishSingleFile=true \
      -c Release \
      -o ./output
RUN mv ./output/Irc7d ./output/irc7

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