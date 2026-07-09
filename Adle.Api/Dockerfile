# Multi-stage build: React SPA + ASP.NET Core API in one single-origin image.
# Build context is the repo root (the API references sibling net8.0 projects):
#   docker build -f Adle.Api/Dockerfile -t adle-api .

# --- 1. Frontend build -------------------------------------------------------
FROM node:20-alpine AS frontend
WORKDIR /fe
COPY adle-web/package.json adle-web/package-lock.json ./
RUN npm ci
COPY adle-web/ ./
# Emit into a local dist (override the host-oriented outDir from vite.config).
RUN npx vite build --outDir dist --emptyOutDir

# --- 2. API publish ----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Adle.Api/Adle.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- 3. Runtime --------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
COPY --from=frontend /fe/dist ./wwwroot
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Adle.Api.dll"]
