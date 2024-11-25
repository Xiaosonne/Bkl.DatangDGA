
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["Bkl.Inspection/", "Bkl.Inspection/"]
COPY ["Bkl.Infrastructure/", "Bkl.Infrastructure/"]
COPY ["Bkl.Models/", "Bkl.Models/"]
COPY ["Yitter.IdGenerator/", "Yitter.IdGenerator/"]
COPY ["BklAPIShare/", "BklAPIShare/"]

RUN dotnet restore "Bkl.Inspection/Bkl.Inspection.csproj"
COPY . .
WORKDIR "/src/Bkl.Inspection"
RUN dotnet build "Bkl.Inspection.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bkl.Inspection.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bkl.Inspection.dll"]