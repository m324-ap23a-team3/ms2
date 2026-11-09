FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Ms2.Api/Ms2.Api.csproj src/Ms2.Api/
RUN dotnet restore src/Ms2.Api/Ms2.Api.csproj
COPY src/ src/
RUN dotnet publish src/Ms2.Api/Ms2.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Ms2.Api.dll"]
