FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FNBReservation.Portal/FNBReservation.Portal.csproj", "FNBReservation.Portal/"]
COPY ["FNBReservation.Core/FNBReservation.Core.csproj", "FNBReservation.Core/"]
COPY ["FNBReservation.Shared/FNBReservation.Shared.csproj", "FNBReservation.Shared/"]
RUN dotnet restore "FNBReservation.Portal/FNBReservation.Portal.csproj"
COPY . .
WORKDIR "/src/FNBReservation.Portal"
RUN dotnet build "FNBReservation.Portal.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FNBReservation.Portal.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FNBReservation.Portal.dll"]