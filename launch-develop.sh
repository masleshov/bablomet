set echo off

root_path=$(dirname $(dirname $0))

docker compose -f "$root_path/docker-compose.yml" down -v
docker image rm $(docker images | grep "<none>" | awk '{print $3}')

# очистка старых сборок
rm -rf "$root_path/Bablomet.API/bin"
rm -rf "$root_path/Bablomet.API/obj"

rm -rf "$root_path/Bablomet.Marketdata/bin"
rm -rf "$root_path/Bablomet.Marketdata/obj"

rm -rf "$root_path/Bablomet.PRO.Telegram/bin"
rm -rf "$root_path/Bablomet.PRO.Telegram/obj"

# сборка API
api="$root_path/Bablomet.API/Bablomet.API.csproj"
dotnet clean $api; dotnet build $api -c Release; dotnet publish $api -c Release

# сборка Marketdata
marketdata="$root_path/Bablomet.Marketdata/Bablomet.Marketdata.csproj"
dotnet clean $marketdata; dotnet build $marketdata -c Release; dotnet publish $marketdata -c Release

# сборка Telegram-бота
telegram="$root_path/Bablomet.PRO.Telegram/Bablomet.PRO.Telegram.csproj"
dotnet clean $telegram; dotnet build $telegram -c Release; dotnet publish $telegram -c Release

# запуск docker-compose
docker compose -f "$root_path/docker-compose.yml" up -d --build
pkill -f MSBuild