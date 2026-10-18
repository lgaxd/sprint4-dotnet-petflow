#!/bin/sh
# Executado pelo serviço "guard" do docker-compose (ver comentário lá). Falha — e com isso impede a subida do
# Oracle/MongoDB locais — quando o .env contém credenciais do Oracle da FIAP.

mode=$(sh /work/docker/detect-oracle-mode.sh /work)

if [ "$mode" = "fiap" ]; then
  cat <<'MSG'

  ┌────────────────────────────────────────────────────────────────────────────┐
  │  O .env contém credenciais do Oracle da FIAP (ConnectionStrings__OracleConnection).
  │  Nesse caso NÃO é para subir o Oracle/MongoDB locais — suba apenas a API:
  │
  │      ./start.sh                  (detecta sozinho)    ou
  │      docker compose up --build api-fiap [mongo-fiap]
  │
  │  Para usar os bancos locais em contêiner: apague/renomeie o .env (ou deixe os placeholders).
  └────────────────────────────────────────────────────────────────────────────┘

MSG
  exit 1
fi

echo "guard: nenhuma credencial FIAP no .env -> subindo Oracle e MongoDB locais."
