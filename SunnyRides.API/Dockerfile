FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SunnyRides.Model/SunnyRides.Model.csproj", "SunnyRides.Model/"]
COPY ["SunnyRides.Services/SunnyRides.Services.csproj", "SunnyRides.Services/"]
COPY ["SunnyRides.API/SunnyRides.API.csproj", "SunnyRides.API/"]
RUN dotnet restore "SunnyRides.API/SunnyRides.API.csproj"
COPY . .
RUN dotnet publish "SunnyRides.API/SunnyRides.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SunnyRides.API.dll"]