# Build context is the REPOSITORY ROOT, not this folder:
#     docker build -f server/ZeroHour.Server/Dockerfile .
#
# The server references shared/ZeroHour.Sim, and a Docker build cannot reach outside its
# context. Building from this directory would fail on the project reference, so compose
# sets `context: .` with `dockerfile:` pointing here.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first, on their own layer. Restore is the slow step and it only depends on
# these; copying the whole source before restoring would re-download every package on any
# source edit.
COPY shared/ZeroHour.Sim/ZeroHour.Sim.csproj shared/ZeroHour.Sim/
COPY server/ZeroHour.Server/ZeroHour.Server.csproj server/ZeroHour.Server/
RUN dotnet restore server/ZeroHour.Server/ZeroHour.Server.csproj

COPY shared/ shared/
COPY server/ server/

# The determinism guards are a CI concern, not an image-build concern, so this publishes
# without running tests. CI is where a stray float gets caught.
RUN dotnet publish server/ZeroHour.Server/ZeroHour.Server.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# The ASP.NET runtime image ships neither curl nor wget, so a HEALTHCHECK written against
# either would fail forever and mark the container permanently unhealthy — which reads like
# a broken app rather than a missing binary. Installing curl deliberately, before the user
# switch below, is the cost of having the healthcheck mean something.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

# Non-root. APP_UID is defined by the base image; a compromised process should not own
# the filesystem it runs on.
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "ZeroHour.Server.dll"]
