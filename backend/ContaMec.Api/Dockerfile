# ContaMec.Api (.NET 8) para Render u otro hosting con Docker.
# Build:  docker build -t contamec-api backend/ContaMec.Api
# Run:    docker run -p 8080:8080 -e ConnectionStrings__DefaultConnection="..." -e Jwt__Key="..." contamec-api

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ContaMec.Api.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
# Render indica el puerto en la variable PORT; sin ella se usa 8080.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet ContaMec.Api.dll"]
