FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SunnyRides.Model/SunnyRides.Model.csproj", "SunnyRides.Model/"]
COPY ["SunnyRides.Services/SunnyRides.Services.csproj", "SunnyRides.Services/"]
COPY ["SunnyRides.API/SunnyRides.API.csproj", "SunnyRides.API/"]
RUN dotnet restore "SunnyRides.API/SunnyRides.API.csproj"
COPY . .
RUN dotnet publish "SunnyRides.API/SunnyRides.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Model preporuke koristi ML.NET, cija je nativna biblioteka za matricnu faktorizaciju
# (LIBMF) gradjena sa OpenMP-om. Runtime image ga nema, pa bi se bez ovoga treniranje
# srusilo tek u kontejneru - lokalno na Windowsu radi i bez toga.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgomp1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SunnyRides.API.dll"]