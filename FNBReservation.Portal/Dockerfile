FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything at once
COPY . .

# Build and publish Portal
WORKDIR "/src/FNBReservation.Portal"
RUN dotnet restore "FNBReservation.Portal.csproj"
RUN dotnet build "FNBReservation.Portal.csproj" -c Release -o /app/build
RUN dotnet publish "FNBReservation.Portal.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FNBReservation.Portal.dll"]