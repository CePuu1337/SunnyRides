FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SunnyRides.Model/SunnyRides.Model.csproj", "SunnyRides.Model/"]
COPY ["SunnyRides.Services/SunnyRides.Services.csproj", "SunnyRides.Services/"]
COPY ["SunnyRides.Subscriber/SunnyRides.Subscriber.csproj", "SunnyRides.Subscriber/"]
RUN dotnet restore "SunnyRides.Subscriber/SunnyRides.Subscriber.csproj"
COPY . .
RUN dotnet publish "SunnyRides.Subscriber/SunnyRides.Subscriber.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SunnyRides.Subscriber.dll"]