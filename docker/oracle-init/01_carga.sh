#!/bin/bash
# Carrega o schema e os dados do PetFlow como o usuário PETFLOW (o mesmo que a API usa).
#
# Por que este script existe: a imagem gvenzl executa os arquivos *.sql de /container-entrypoint-initdb.d como SYS,
# o que criaria as tabelas no schema SYS e deixaria o schema PETFLOW vazio ("table or view does not exist").
# Aqui os mesmos .sql são executados com sqlplus conectado como PETFLOW.

PETFLOW_CONN="petflow/${APP_USER_PASSWORD}@//localhost:1521/FREEPDB1"

petflow_run_sql() {
  local file="$1" log="/tmp/$(basename "$1").log"
  echo "CONTAINER: [petflow] executando ${file} como PETFLOW (log completo em ${log})"
  sqlplus -s "${PETFLOW_CONN}" > "${log}" 2>&1 <<EOF
SET DEFINE OFF
@${file}
EXIT
EOF
}

petflow_run_sql /seed/2TDSPX_CodigoSql_PetFlow.sql
petflow_run_sql /seed/03_EF_Baseline.sql
# Opcional: pacotes PL/SQL da disciplina de banco (descomente o volume correspondente no docker-compose.yml)
[ -f /seed/01_PetFlow_Sprint4_Pacotes.sql ] && petflow_run_sql /seed/01_PetFlow_Sprint4_Pacotes.sql

tables=$(sqlplus -s "${PETFLOW_CONN}" <<EOF
SET HEADING OFF FEEDBACK OFF PAGESIZE 0
SELECT COUNT(*) FROM user_tables WHERE table_name IN ('TUTOR','PET','CLINIC','COUPON','REDEEM');
EXIT
EOF
)
tables=$(echo "${tables}" | tr -d '[:space:]')

if [ "${tables}" = "5" ]; then
  echo "CONTAINER: [petflow] OK - schema PETFLOW criado e carregado."
else
  echo "CONTAINER: [petflow] ERRO - tabelas principais não encontradas no schema PETFLOW (encontradas: ${tables:-0}). Veja /tmp/*.sql.log"
fi
