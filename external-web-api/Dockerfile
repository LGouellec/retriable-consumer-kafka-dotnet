FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["*", "external-web-api/"]
RUN dotnet restore "external-web-api/external-web-api.csproj"
WORKDIR "/src/external-web-api"
RUN dotnet build "external-web-api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "external-web-api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "external-web-api.dll"]
