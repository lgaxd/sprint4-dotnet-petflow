#!/bin/sh
# Sobe o PetFlow escolhendo o modo pelo .env (ver docker-compose.yml).
#
#   ./start.sh                 # .env com credenciais FIAP -> só a API | sem credenciais -> API + Oracle + MongoDB locais
#   ./start.sh --with-mongo    # no modo FIAP, sobe também o MongoDB em contêiner (anotações dos pets)
#   ./start.sh down            # derruba o que foi iniciado (use "down -v" para apagar também os dados locais)

cd "$(dirname "$0")" || exit 1

if docker compose version >/dev/null 2>&1; then
  COMPOSE="docker compose"
elif command -v docker-compose >/dev/null 2>&1; then
  COMPOSE="docker-compose"
else
  echo "Docker Compose não encontrado (nem 'docker compose' nem 'docker-compose')." >&2
  exit 1
fi

case "$1" in
  down) shift; exec $COMPOSE --profile fiap down "$@" ;;
esac

mode=$(sh docker/detect-oracle-mode.sh .)

if [ "$mode" = "fiap" ]; then
  if [ "$1" = "--with-mongo" ]; then
    echo ">> .env com credenciais do Oracle da FIAP: subindo a API + MongoDB (sem Oracle local)."
    exec $COMPOSE up --build api-fiap mongo-fiap
  fi
  echo ">> .env com credenciais do Oracle da FIAP: subindo SOMENTE a API (sem Oracle/MongoDB locais)."
  echo ">> Anotações dos pets (MongoDB) ficam indisponíveis, a menos que o .env defina FIAP_MONGO_CONNECTION ou você use --with-mongo."
  exec $COMPOSE up --build api-fiap
fi

echo ">> Sem credenciais da FIAP no .env: subindo API + Oracle + MongoDB locais (a 1ª vez leva alguns minutos)."
exec $COMPOSE up --build
