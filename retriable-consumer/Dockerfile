FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["retriable-consumer/retriable-consumer.csproj", "retriable-consumer/"]
RUN dotnet restore "retriable-consumer/retriable-consumer.csproj"
COPY . .
WORKDIR "/src/retriable-consumer"
RUN dotnet build "retriable-consumer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "retriable-consumer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "retriable-consumer.dll"]
