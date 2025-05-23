#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM shimat/ubuntu20-dotnet5-opencv4.5.3:20210821 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Bkl.ESPS/", "Bkl.ESPS/"]
COPY ["Bkl.Dst.Interfaces/", "Bkl.Dst.Interfaces/"]
COPY ["Bkl.Models/", "Bkl.Models/"]
COPY ["Bkl.Infrastructure/", "Bkl.Infrastructure/"]
COPY ["Yitter.IdGenerator/", "Yitter.IdGenerator/"]
COPY ["Bkl.ThermalSDK/", "Bkl.ThermalSDK/"]
COPY ["SiloClientShared/", "SiloClientShared/"]
RUN dotnet restore "Bkl.ESPS/Bkl.ESPS.csproj"
WORKDIR "/src/Bkl.ESPS"
RUN dotnet build "Bkl.ESPS.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bkl.ESPS.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bkl.ESPS.dll"]