FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN mkdir /app
WORKDIR /app
RUN apt-get update && apt-get install -y curl
COPY bin/Release/net8.0 /app
ENTRYPOINT ["dotnet", "Bablomet.Marketdata.dll", "--environment=Production"]