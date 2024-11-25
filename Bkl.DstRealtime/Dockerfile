#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Bkl.DstRealtime/", "Bkl.DstRealtime/"]
COPY ["Bkl.Dst.Grains/", "Bkl.Dst.Grains/"]
COPY ["Bkl.Dst.Interfaces/", "Bkl.Dst.Interfaces/"]
COPY ["Bkl.Models/", "Bkl.Models/"]
COPY ["Bkl.Infrastructure/", "Bkl.Infrastructure/"]
COPY ["Yitter.IdGenerator/", "Yitter.IdGenerator/"]
COPY ["Bkl.ThermalSDK/", "Bkl.ThermalSDK/"]
COPY ["SiloClientShared/", "SiloClientShared/"]
RUN dotnet restore "Bkl.DstRealtime/Bkl.DstRealtime.csproj"
WORKDIR "/src/Bkl.DstRealtime"
RUN dotnet build "Bkl.DstRealtime.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bkl.DstRealtime.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bkl.DstRealtime.dll"]