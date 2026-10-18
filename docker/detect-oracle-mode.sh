#!/bin/sh
# Imprime "fiap" se o .env contiver credenciais REAIS do Oracle em ConnectionStrings__OracleConnection;
# caso contrário imprime "local" (sem .env, variável ausente/vazia ou ainda com os placeholders do .env.example).
#
# Uso:  sh docker/detect-oracle-mode.sh [diretório-do-.env]      (padrão: diretório atual)

env_file="${1:-.}/.env"

if [ ! -f "$env_file" ]; then
  echo local
  exit 0
fi

# Última ocorrência da chave (ignora comentários), removendo CR de arquivos com final de linha do Windows.
value=$(grep -E '^[[:space:]]*(export[[:space:]]+)?ConnectionStrings__OracleConnection[[:space:]]*=' "$env_file" \
        | tail -n 1 | sed -e 's/^[^=]*=//' -e 's/\r$//' -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//' -e 's/^"//' -e 's/"$//' -e "s/^'//" -e "s/'\$//")

case "$value" in
  "")                                   echo local ;;
  *SEU_RM*|*SUA_SENHA*|*SEU_USUARIO*)   echo local ;;   # placeholders do .env.example
  *)                                    echo fiap ;;
esac
