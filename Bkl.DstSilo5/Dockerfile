#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Bkl.DstSilo5/", "Bkl.DstSilo5/"]
COPY ["Bkl.Dst.Grains/", "Bkl.Dst.Grains/"]
COPY ["Bkl.Dst.Interfaces/", "Bkl.Dst.Interfaces/"]
COPY ["Bkl.Models/", "Bkl.Models/"]
COPY ["Bkl.Infrastructure/", "Bkl.Infrastructure/"]
COPY ["Yitter.IdGenerator/", "Yitter.IdGenerator/"]
COPY ["Bkl.ThermalSDK/", "Bkl.ThermalSDK/"]
RUN dotnet restore "Bkl.DstSilo5/Bkl.DstSilo5.csproj" 
WORKDIR "/src/Bkl.DstSilo5"
RUN dotnet build "Bkl.DstSilo5.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bkl.DstSilo5.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 11000
EXPOSE 21000
EXPOSE 4980
ENTRYPOINT ["dotnet", "Bkl.DstSilo5.dll"]