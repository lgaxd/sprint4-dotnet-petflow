-- =============================================================
-- PetFlow - SPRINT 4 - BASELINE DAS MIGRACOES DO ENTITY FRAMEWORK CORE
--
-- QUANDO USAR:
--   O schema foi criado pelo script 2TDSPX_CodigoSql_PetFlow.sql (e nao pelo
--   "dotnet ef database update"). As tabelas ja existem; este script apenas
--   registra a migracao InitialCreate como "ja aplicada", para que futuras
--   migracoes do EF Core (dotnet ef database update) rodem normalmente
--   sem tentar recriar as tabelas.
--
-- QUANDO NAO USAR:
--   Em um schema vazio, rode apenas:  dotnet ef database update
--   (a migracao InitialCreate cria todas as tabelas, identidades e FKs).
--
-- O MigrationId deve ser identico ao nome do arquivo da migracao em
-- PetFlowAPI.Infrastructure/Persistence/Migrations/*_InitialCreate.cs
-- =============================================================

DECLARE
  v_existe NUMBER;
BEGIN
  SELECT COUNT(*) INTO v_existe FROM user_tables WHERE table_name = '__EFMigrationsHistory';
  IF v_existe = 0 THEN
    EXECUTE IMMEDIATE 'CREATE TABLE "__EFMigrationsHistory" (
        "MigrationId"    NVARCHAR2(150) NOT NULL,
        "ProductVersion" NVARCHAR2(32)  NOT NULL,
        CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
      )';
  END IF;
END;
/

MERGE INTO "__EFMigrationsHistory" h
USING (SELECT '20261018200551_InitialCreate' AS id FROM dual) s
ON (h."MigrationId" = s.id)
WHEN NOT MATCHED THEN
  INSERT ("MigrationId", "ProductVersion") VALUES (s.id, '8.0.13');
COMMIT;
