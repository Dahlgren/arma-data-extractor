FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build
WORKDIR /source

# copy and publish app and libraries
COPY . .
RUN dotnet publish -c release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./ArmaDataExtractor"]
