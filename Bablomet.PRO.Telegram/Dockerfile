FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN mkdir /app
WORKDIR /app
COPY bin/Release/net8.0 /app
ENTRYPOINT ["dotnet", "Bablomet.PRO.Telegram.dll", "--environment=Production"]