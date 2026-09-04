FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/VietAIS.Gplx.slnx backend/
COPY backend/src/Gplx.Api/Gplx.Api.csproj backend/src/Gplx.Api/
COPY backend/tests/Gplx.Api.Tests/Gplx.Api.Tests.csproj backend/tests/Gplx.Api.Tests/
RUN dotnet restore backend/VietAIS.Gplx.slnx

COPY backend backend
COPY data data
RUN dotnet publish backend/src/Gplx.Api/Gplx.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
COPY --from=build /src/data /app/data
ENTRYPOINT ["dotnet", "Gplx.Api.dll"]
