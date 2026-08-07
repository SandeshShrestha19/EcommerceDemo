FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV DOTNET_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# copy solution file
COPY ["ECommerceGraphQL.sln", "./"]

# copy all project files
COPY ["src/API/ECommerce.API/ECommerce.API.csproj", "src/API/ECommerce.API/"]
COPY ["src/Application/ECommerce.Application/ECommerce.Application.csproj", "src/Application/ECommerce.Application/"]
COPY ["src/Core/ECommerce.Domain/ECommerce.Domain.csproj", "src/Core/ECommerce.Domain/"]
COPY ["src/Infrastructure/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj", "src/Infrastructure/ECommerce.Infrastructure/"]

# restore once! ← fixed
RUN dotnet restore "src/API/ECommerce.API/ECommerce.API.csproj"

# copy everything else
COPY . .

# build
WORKDIR "/src/src/API/ECommerce.API"
RUN dotnet build "ECommerce.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ECommerce.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish

FROM base AS final

RUN apt-get update && \
  apt-get install -y libgssapi-krb5-2 && \
  rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ECommerce.API.dll"]