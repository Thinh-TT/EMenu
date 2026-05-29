FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY EMenu.Web/EMenu.Web.csproj EMenu.Web/
COPY EMenu.Application/EMenu.Application.csproj EMenu.Application/
COPY EMenu.Application.Abstractions/EMenu.Application.Abstractions.csproj EMenu.Application.Abstractions/
COPY EMenu.Domain/EMenu.Domain.csproj EMenu.Domain/
COPY EMenu.Infrastructure/EMenu.Infrastructure.csproj EMenu.Infrastructure/

RUN dotnet restore EMenu.Web/EMenu.Web.csproj

COPY . .

RUN dotnet publish EMenu.Web/EMenu.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080

ENTRYPOINT ["dotnet", "EMenu.Web.dll"]
