FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything at once
COPY . .

# Create a PublishSingleFile.targets file to exclude content from modules
RUN echo '<Project><PropertyGroup><PublishWithAspNetCoreTargetManifest>false</PublishWithAspNetCoreTargetManifest></PropertyGroup></Project>' > Directory.Build.props

# Modify the Authentication API project to exclude content files
RUN echo '<Project><PropertyGroup><EnableDefaultContentItems>false</EnableDefaultContentItems></PropertyGroup></Project>' > /src/FNBReservation.Modules.Authentication.API/Directory.Build.props

# Restore the MainAPI project
WORKDIR "/src/FNBReservation.MainAPI"
RUN dotnet restore

# Build and publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false /p:ErrorOnDuplicatePublishOutputFiles=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/FNBReservation.MainAPI/appsettings*.json .
ENTRYPOINT ["dotnet", "FNBReservation.MainAPI.dll"]