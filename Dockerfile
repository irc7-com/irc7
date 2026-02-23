ARG irc7d_port=6667
ARG irc7d_fqdn=localhost
ARG irc7d_type=ACS
ARG irc7d_server=127.0.0.1:6667

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
RUN \
    git clone https://github.com/IRC7/IRC7.git && \
    dotnet publish IRC7/Irc.Daemon \
      --self-contained true \
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
ENV irc7d_port=${irc7d_port}
ENV irc7d_fqdn=${irc7d_fqdn}
ENV irc7d_type=${irc7d_type}
ENV irc7d_server=${irc7d_server}
CMD /app/output/irc7 --type $irc7d_type --ip 0.0.0.0 --port $irc7d_port --fqdn $irc7d_fqdn --server $irc7d_server
EXPOSE ${irc7d_port}