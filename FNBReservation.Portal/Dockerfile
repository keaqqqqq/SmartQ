FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["F&BReservation.Portal/F&BReservation.Portal.csproj", "F&BReservation.Portal/"]
COPY ["F&BReservation.Core/F&BReservation.Core.csproj", "F&BReservation.Core/"]
COPY ["F&BReservation.Shared/F&BReservation.Shared.csproj", "F&BReservation.Shared/"]
RUN dotnet restore "F&BReservation.Portal/F&BReservation.Portal.csproj"
COPY . .
WORKDIR "/src/F&BReservation.Portal"
RUN dotnet build "F&BReservation.Portal.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "F&BReservation.Portal.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "F&BReservation.Portal.dll"]