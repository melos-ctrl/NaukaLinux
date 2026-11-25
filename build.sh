#!/bin/bash

set -euo pipefail

if [ ! -f Frontend/.env.production ]; then
  echo "Brak Frontend/.env.production (VITE_TURNSTILE_SITE_KEY=<prawdziwy klucz>)"; exit 1
fi

rm -rf out deploy.tgz
mkdir out

echo "==> Backend"
dotnet publish Backend/Backend.csproj -c Release -r linux-x64 --self-contained -o out/api
rm -rf out/api/wwwroot/avatars 

echo "==> Migracje bazy"
dotnet ef migrations bundle --project Backend/Backend.csproj --startup-project Backend/Backend.csproj \
  -r linux-x64 --self-contained -o out/efbundle --force

echo "==> Frontend"
(cd Frontend && npm ci && npm run build)
cp -r Frontend/dist out/dist

echo "==> Paczka"
tar -czf deploy.tgz -C out .

echo "Gotowe: deploy.tgz"
echo "Dalej:  scp deploy.tgz serwer:/opt/naukalinux/  &&  ssh -t serwer 'sudo /opt/naukalinux/deploy.sh'"