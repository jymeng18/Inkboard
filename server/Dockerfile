# build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app

COPY Inkboard.API/*.csproj  Inkboard.API/
COPY Inkboard.Application/*.csproj  Inkboard.Application/
COPY Inkboard.Domain/*.csproj  Inkboard.Domain/
COPY Inkboard.Infra/*.csproj  Inkboard.Infra/

RUN dotnet restore Inkboard.API/*.csproj -r linux-x64

COPY . .

# Publish stage
FROM build AS publish
RUN dotnet publish Inkboard.API/Inkboard.API.csproj \
    -c Release \
    -r linux-x64 \
    -o /app/publish \
    --no-restore

# Run stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

RUN apt-get update && apt-get install -y libgssapi-krb5-2

COPY --from=publish /app/publish .

EXPOSE 8080

CMD [ "dotnet", "Inkboard.API.dll" ]
