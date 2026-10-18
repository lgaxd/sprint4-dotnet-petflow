-- =============================================================
-- PetFlow - SPRINT 4 - EMPACOTAMENTO DE PROCEDURES, FUNCTIONS E TRIGGER
-- Materia: Mastering Relational and Non-Relational Database
-- =============================================================
-- OBJETIVO (requisito oficial da Sprint 4):
--   "Atualizar a codificacao com empacotamento dos processos,
--    visando melhor organizacao e dinamismo. Funcoes, procedimentos
--    e gatilhos devem ser empacotados para garantir modularidade e
--    reutilizacao. O empacotamento deve seguir boas praticas,
--    agrupando os objetos de maneira logica e acessivel."
--
-- ESTE SCRIPT PRESSUPOE que o script da Sprint 3
-- (2TDSPX_CodigoSql_PetFlow.sql) ja foi executado, pois reutiliza
-- as tabelas e a tabela LOG_ERROS criadas por ele. As procedures e
-- functions "soltas" criadas na Sprint 3 sao substituidas por
-- versoes DENTRO de pacotes; caso o schema ja tenha os objetos
-- soltos, este script os recria como membros de pacote (o objeto
-- solto antigo pode ser removido com DROP PROCEDURE/DROP FUNCTION
-- apos a validacao, se necessario).
--
-- DECISAO DE IMPLEMENTACAO (agrupamento logico dos objetos):
--   1) PKG_PETFLOW_CADASTRO   -> as 17 procedures de carga/insercao
--      (uma por tabela), que representam o processo de cadastro do
--      dominio PetFlow.
--   2) PKG_PETFLOW_RELATORIO  -> as 2 functions e as 2 procedures de
--      relatorio da Sprint 3 (conversao para JSON, validacao de
--      senha, relatorio com JOIN e relatorio com subtotal/total).
--   3) PKG_PETFLOW_AUDITORIA  -> a logica de auditoria que antes
--      estava 100% dentro do corpo do trigger foi extraida para uma
--      procedure de pacote (SP_REGISTRAR_AUDITORIA_HEALTH_EVENT).
--
-- OBSERVACAO IMPORTANTE SOBRE O TRIGGER:
--   O Oracle PL/SQL NAO permite declarar um TRIGGER dentro de um
--   PACKAGE (trigger e sempre um objeto de schema independente,
--   nunca um membro de pacote). Por isso trg_audit_health_event
--   continua existindo como objeto de schema separado, porem seu
--   corpo foi reduzido a uma unica chamada para a procedure
--   empacotada PKG_PETFLOW_AUDITORIA.sp_registrar_auditoria_health_event,
--   preservando a exigencia de modularidade e reuso dentro do que a
--   linguagem permite.
--
-- NOTA: o pacote PKG_PETFLOW_GAMIFICATION, chamado pela aplicacao Java,
-- esta no script 04_PetFlow_Sprint4_Pacote_Gamificacao.sql.
--
-- ORDEM DE EXECUCAO deste script:
--   1) PKG_PETFLOW_CADASTRO   (spec + body)
--   2) PKG_PETFLOW_RELATORIO  (spec + body)
--   3) PKG_PETFLOW_AUDITORIA  (spec + body)
--   4) Recriacao do trigger trg_audit_health_event chamando o pacote
--   5) Bloco de smoke test (chama 1 procedure de cada pacote)
-- =============================================================


-- #############################################################
-- 1) PACOTE PKG_PETFLOW_CADASTRO
--    Agrupa as 17 procedures de insercao/carga de dados (uma por
--    tabela do dominio), antes soltas na Sprint 3.
-- #############################################################

CREATE OR REPLACE PACKAGE pkg_petflow_cadastro AS
  -- ---------------------------------------------------------
  -- Empacota as procedures de insercao de cada tabela do
  -- dominio PetFlow. Cada procedure mantem, sem alteracoes de
  -- comportamento, a mesma assinatura, as mesmas validacoes e o
  -- mesmo tratamento de excecoes (log em LOG_ERROS +
  -- RAISE_APPLICATION_ERROR) que possuia na Sprint 3.
  -- ---------------------------------------------------------

  PROCEDURE sp_insert_species (
  p_name        IN species.name%TYPE,
  p_description IN species.description%TYPE
);

  PROCEDURE sp_insert_event_type (
  p_name          IN event_type.name%TYPE,
  p_points_reward IN event_type.points_reward%TYPE,
  p_category      IN event_type.category%TYPE
);

  PROCEDURE sp_insert_reward_action (
  p_name         IN reward_action.name%TYPE,
  p_points_value IN reward_action.points_value%TYPE,
  p_description  IN reward_action.description%TYPE
);

  PROCEDURE sp_insert_risk_level (
  p_name        IN risk_level.name%TYPE,
  p_description IN risk_level.description%TYPE,
  p_min_score   IN risk_level.min_score%TYPE,
  p_max_score   IN risk_level.max_score%TYPE
);

  PROCEDURE sp_insert_tutor (
  p_role          IN tutor.role%TYPE DEFAULT 'TUTOR',
  p_name          IN tutor.name%TYPE,
  p_email         IN tutor.email%TYPE,
  p_phone         IN tutor.phone%TYPE,
  p_password_hash IN tutor.password_hash%TYPE
);

  PROCEDURE sp_insert_address (
  p_tutor_id IN address.tutor_id%TYPE,
  p_street   IN address.street%TYPE,
  p_city     IN address.city%TYPE,
  p_state    IN address.state%TYPE,
  p_zip_code IN address.zip_code%TYPE
);

  PROCEDURE sp_insert_clinic (
  p_name    IN clinic.name%TYPE,
  p_address IN clinic.address%TYPE,
  p_phone   IN clinic.phone%TYPE,
  p_cnpj    IN clinic.cnpj%TYPE
);

  PROCEDURE sp_insert_pet (
  p_tutor_id   IN pet.tutor_id%TYPE,
  p_species_id IN pet.species_id%TYPE,
  p_name       IN pet.name%TYPE,
  p_breed      IN pet.breed%TYPE,
  p_birth_date IN pet.birth_date%TYPE,
  p_weight     IN pet.weight%TYPE
);

  PROCEDURE sp_insert_plan (
  p_clinic_id        IN plan.clinic_id%TYPE,
  p_name             IN plan.name%TYPE,
  p_description      IN plan.description%TYPE,
  p_price            IN plan.price%TYPE,
  p_duration_days    IN plan.duration_days%TYPE,
  p_points_per_event IN plan.points_per_event%TYPE
);

  PROCEDURE sp_insert_subscription (
  p_pet_id     IN subscription.pet_id%TYPE,
  p_plan_id    IN subscription.plan_id%TYPE,
  p_start_date IN subscription.start_date%TYPE,
  p_end_date   IN subscription.end_date%TYPE,
  p_status     IN subscription.status%TYPE
);

  PROCEDURE sp_insert_health_event (
  p_pet_id        IN health_event.pet_id%TYPE,
  p_event_type_id IN health_event.event_type_id%TYPE,
  p_clinic_id     IN health_event.clinic_id%TYPE,
  p_description   IN health_event.description%TYPE,
  p_event_date    IN health_event.event_date%TYPE,
  p_status        IN health_event.status%TYPE
);

  PROCEDURE sp_insert_reward_point (
  p_tutor_id         IN reward_point.tutor_id%TYPE,
  p_reward_action_id IN reward_point.reward_action_id%TYPE,
  p_points           IN reward_point.points%TYPE,
  p_reference_type   IN reward_point.reference_type%TYPE,
  p_reference_id     IN reward_point.reference_id%TYPE
);

  PROCEDURE sp_insert_partner_discount (
  p_clinic_id        IN partner_discount.clinic_id%TYPE,
  p_partner_name     IN partner_discount.partner_name%TYPE,
  p_category         IN partner_discount.category%TYPE,
  p_discount_percent IN partner_discount.discount_percent%TYPE
);

  PROCEDURE sp_insert_coupon_template (
  p_partner_discount_id IN coupon_template.partner_discount_id%TYPE,
  p_title               IN coupon_template.title%TYPE,
  p_discount_value      IN coupon_template.discount_value%TYPE,
  p_discount_type       IN coupon_template.discount_type%TYPE,
  p_points_required     IN coupon_template.points_required%TYPE
);

  PROCEDURE sp_insert_coupon (
  p_template_id     IN coupon.template_id%TYPE,
  p_code            IN coupon.code%TYPE,
  p_status          IN coupon.status%TYPE,
  p_expiration_date IN coupon.expiration_date%TYPE
);

  PROCEDURE sp_insert_redeem (
  p_tutor_id   IN redeem.tutor_id%TYPE,
  p_coupon_id  IN redeem.coupon_id%TYPE,
  p_points_used IN redeem.points_used%TYPE
);

  PROCEDURE sp_insert_risk_score (
  p_pet_id        IN risk_score.pet_id%TYPE,
  p_score         IN risk_score.score%TYPE,
  p_risk_level_id IN risk_score.risk_level_id%TYPE
);

END pkg_petflow_cadastro;
/


CREATE OR REPLACE PACKAGE BODY pkg_petflow_cadastro AS

PROCEDURE sp_insert_species (
  p_name        IN species.name%TYPE,
  p_description IN species.description%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_nome_nulo   EXCEPTION;
  ex_duplicado   EXCEPTION;
  PRAGMA EXCEPTION_INIT(ex_duplicado, -1);
BEGIN
  v_usuario := USER;
  IF p_name IS NULL THEN
    RAISE ex_nome_nulo;
  END IF;

  INSERT INTO species (name, description)
  VALUES (p_name, p_description);

  COMMIT;

EXCEPTION
  WHEN ex_nome_nulo THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_species', v_usuario, -20001, 'Nome da especie nao pode ser nulo');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20001, 'Nome da especie nao pode ser nulo');

  WHEN ex_duplicado THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_species', v_usuario, -1, 'Especie ja cadastrada: ' || p_name);
    COMMIT;
    RAISE_APPLICATION_ERROR(-20002, 'Especie ja cadastrada: ' || p_name);

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_species', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_species;

PROCEDURE sp_insert_event_type (
  p_name          IN event_type.name%TYPE,
  p_points_reward IN event_type.points_reward%TYPE,
  p_category      IN event_type.category%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_pontos_invalidos EXCEPTION;
  ex_duplicado        EXCEPTION;
  PRAGMA EXCEPTION_INIT(ex_duplicado, -1);
BEGIN
  v_usuario := USER;
  IF p_points_reward < 0 THEN
    RAISE ex_pontos_invalidos;
  END IF;

  INSERT INTO event_type (name, points_reward, category)
  VALUES (p_name, p_points_reward, p_category);

  COMMIT;

EXCEPTION
  WHEN ex_pontos_invalidos THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_event_type', v_usuario, -20003, 'Pontos nao podem ser negativos');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20003, 'Pontos nao podem ser negativos');

  WHEN ex_duplicado THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_event_type', v_usuario, -1, 'Tipo de evento ja existe: ' || p_name);
    COMMIT;
    RAISE_APPLICATION_ERROR(-20004, 'Tipo de evento ja existe: ' || p_name);

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_event_type', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_event_type;

PROCEDURE sp_insert_reward_action (
  p_name         IN reward_action.name%TYPE,
  p_points_value IN reward_action.points_value%TYPE,
  p_description  IN reward_action.description%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_duplicado       EXCEPTION;
  ex_pontos_invalidos EXCEPTION;
  PRAGMA EXCEPTION_INIT(ex_duplicado, -1);
BEGIN
  v_usuario := USER;
  IF p_points_value <= 0 THEN
    RAISE ex_pontos_invalidos;
  END IF;

  INSERT INTO reward_action (name, points_value, description)
  VALUES (p_name, p_points_value, p_description);

  COMMIT;

EXCEPTION
  WHEN ex_pontos_invalidos THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_reward_action', v_usuario, -20005, 'Valor dos pontos deve ser maior que zero');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20005, 'Valor dos pontos deve ser maior que zero');

  WHEN ex_duplicado THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_reward_action', v_usuario, -1, 'Acao de recompensa ja existe: ' || p_name);
    COMMIT;
    RAISE_APPLICATION_ERROR(-20006, 'Acao de recompensa ja existe: ' || p_name);

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_reward_action', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_reward_action;

PROCEDURE sp_insert_risk_level (
  p_name        IN risk_level.name%TYPE,
  p_description IN risk_level.description%TYPE,
  p_min_score   IN risk_level.min_score%TYPE,
  p_max_score   IN risk_level.max_score%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_intervalo_invalido EXCEPTION;
  ex_duplicado          EXCEPTION;
  PRAGMA EXCEPTION_INIT(ex_duplicado, -1);
BEGIN
  v_usuario := USER;
  IF p_min_score > p_max_score THEN
    RAISE ex_intervalo_invalido;
  END IF;

  INSERT INTO risk_level (name, description, min_score, max_score)
  VALUES (p_name, p_description, p_min_score, p_max_score);

  COMMIT;

EXCEPTION
  WHEN ex_intervalo_invalido THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_risk_level', v_usuario, -20007, 'Score minimo nao pode ser maior que o maximo');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20007, 'Score minimo nao pode ser maior que o maximo');

  WHEN ex_duplicado THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_risk_level', v_usuario, -1, 'Nivel de risco ja existe: ' || p_name);
    COMMIT;
    RAISE_APPLICATION_ERROR(-20008, 'Nivel de risco ja existe: ' || p_name);

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_risk_level', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_risk_level;

PROCEDURE sp_insert_tutor (
  p_role          IN tutor.role%TYPE DEFAULT 'TUTOR',
  p_name          IN tutor.name%TYPE,
  p_email         IN tutor.email%TYPE,
  p_phone         IN tutor.phone%TYPE,
  p_password_hash IN tutor.password_hash%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_email_nulo EXCEPTION;
  ex_duplicado  EXCEPTION;
  ex_role_invalido EXCEPTION;
  PRAGMA EXCEPTION_INIT(ex_duplicado, -1);
BEGIN
  v_usuario := USER;
  
  -- Nova validação para ROLE
  IF p_role NOT IN ('ADMIN', 'TUTOR') THEN
    RAISE ex_role_invalido;
  END IF;
  
  IF p_email IS NULL THEN
    RAISE ex_email_nulo;
  END IF;

  INSERT INTO tutor (role, name, email, phone, password_hash)
  VALUES (p_role, p_name, p_email, p_phone, p_password_hash);

  COMMIT;

EXCEPTION
  WHEN ex_email_nulo THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_tutor', v_usuario, -20009, 'Email do tutor nao pode ser nulo');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20009, 'Email do tutor nao pode ser nulo');

  WHEN ex_duplicado THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_tutor', v_usuario, -1, 'Email ja cadastrado: ' || p_email);
    COMMIT;
    RAISE_APPLICATION_ERROR(-20010, 'Email ja cadastrado: ' || p_email);
  
  WHEN ex_role_invalido THEN  -- <-- NOVA EXCEÇÃO
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_tutor', v_usuario, -20035, 'Role invalida. Use ADMIN ou TUTOR');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20035, 'Role invalida. Use ADMIN ou TUTOR');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_tutor', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_tutor;

PROCEDURE sp_insert_address (
  p_tutor_id IN address.tutor_id%TYPE,
  p_street   IN address.street%TYPE,
  p_city     IN address.city%TYPE,
  p_state    IN address.state%TYPE,
  p_zip_code IN address.zip_code%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_tutor_invalido EXCEPTION;
  ex_cep_invalido   EXCEPTION;
  v_count           NUMBER;
BEGIN
  v_usuario := USER;
  SELECT COUNT(*) INTO v_count FROM tutor WHERE id = p_tutor_id;
  IF v_count = 0 THEN
    RAISE ex_tutor_invalido;
  END IF;

  IF LENGTH(p_zip_code) < 8 THEN
    RAISE ex_cep_invalido;
  END IF;

  INSERT INTO address (tutor_id, street, city, state, zip_code)
  VALUES (p_tutor_id, p_street, p_city, p_state, p_zip_code);

  COMMIT;

EXCEPTION
  WHEN ex_tutor_invalido THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_address', v_usuario, -20011, 'Tutor nao encontrado: ID ' || p_tutor_id);
    COMMIT;
    RAISE_APPLICATION_ERROR(-20011, 'Tutor nao encontrado');

  WHEN ex_cep_invalido THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_address', v_usuario, -20012, 'CEP invalido: ' || p_zip_code);
    COMMIT;
    RAISE_APPLICATION_ERROR(-20012, 'CEP invalido');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_address', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_address;

PROCEDURE sp_insert_clinic (
  p_name    IN clinic.name%TYPE,
  p_address IN clinic.address%TYPE,
  p_phone   IN clinic.phone%TYPE,
  p_cnpj    IN clinic.cnpj%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_duplicado EXCEPTION;
  ex_nome_nulo EXCEPTION;
  PRAGMA EXCEPTION_INIT(ex_duplicado, -1);
BEGIN
  v_usuario := USER;
  IF p_name IS NULL THEN
    RAISE ex_nome_nulo;
  END IF;

  INSERT INTO clinic (name, address, phone, cnpj)
  VALUES (p_name, p_address, p_phone, p_cnpj);

  COMMIT;

EXCEPTION
  WHEN ex_nome_nulo THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_clinic', v_usuario, -20013, 'Nome da clinica nao pode ser nulo');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20013, 'Nome da clinica nao pode ser nulo');

  WHEN ex_duplicado THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_clinic', v_usuario, -1, 'CNPJ ja cadastrado: ' || p_cnpj);
    COMMIT;
    RAISE_APPLICATION_ERROR(-20014, 'CNPJ ja cadastrado');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_clinic', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_clinic;

PROCEDURE sp_insert_pet (
  p_tutor_id   IN pet.tutor_id%TYPE,
  p_species_id IN pet.species_id%TYPE,
  p_name       IN pet.name%TYPE,
  p_breed      IN pet.breed%TYPE,
  p_birth_date IN pet.birth_date%TYPE,
  p_weight     IN pet.weight%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_nome_nulo  EXCEPTION;
  ex_peso_invalido EXCEPTION;
  v_count       NUMBER;
BEGIN
  v_usuario := USER;
  IF p_name IS NULL THEN
    RAISE ex_nome_nulo;
  END IF;

  IF p_weight IS NOT NULL AND p_weight <= 0 THEN
    RAISE ex_peso_invalido;
  END IF;

  INSERT INTO pet (tutor_id, species_id, name, breed, birth_date, weight)
  VALUES (p_tutor_id, p_species_id, p_name, p_breed, p_birth_date, p_weight);

  COMMIT;

EXCEPTION
  WHEN ex_nome_nulo THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_pet', v_usuario, -20015, 'Nome do pet nao pode ser nulo');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20015, 'Nome do pet nao pode ser nulo');

  WHEN ex_peso_invalido THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_pet', v_usuario, -20016, 'Peso do pet deve ser maior que zero');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20016, 'Peso do pet deve ser maior que zero');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_pet', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_pet;

PROCEDURE sp_insert_plan (
  p_clinic_id        IN plan.clinic_id%TYPE,
  p_name             IN plan.name%TYPE,
  p_description      IN plan.description%TYPE,
  p_price            IN plan.price%TYPE,
  p_duration_days    IN plan.duration_days%TYPE,
  p_points_per_event IN plan.points_per_event%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_preco_invalido    EXCEPTION;
  ex_duracao_invalida  EXCEPTION;
BEGIN
  v_usuario := USER;
  IF p_price <= 0 THEN
    RAISE ex_preco_invalido;
  END IF;

  IF p_duration_days <= 0 THEN
    RAISE ex_duracao_invalida;
  END IF;

  INSERT INTO plan (clinic_id, name, description, price, duration_days, points_per_event)
  VALUES (p_clinic_id, p_name, p_description, p_price, p_duration_days, p_points_per_event);

  COMMIT;

EXCEPTION
  WHEN ex_preco_invalido THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_plan', v_usuario, -20017, 'Preco do plano deve ser maior que zero');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20017, 'Preco do plano deve ser maior que zero');

  WHEN ex_duracao_invalida THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_plan', v_usuario, -20018, 'Duracao do plano deve ser maior que zero');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20018, 'Duracao do plano deve ser maior que zero');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_plan', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_plan;

PROCEDURE sp_insert_subscription (
  p_pet_id     IN subscription.pet_id%TYPE,
  p_plan_id    IN subscription.plan_id%TYPE,
  p_start_date IN subscription.start_date%TYPE,
  p_end_date   IN subscription.end_date%TYPE,
  p_status     IN subscription.status%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_data_invalida EXCEPTION;
  ex_status_invalido EXCEPTION;
BEGIN
  v_usuario := USER;
  IF p_end_date IS NOT NULL AND p_end_date < p_start_date THEN
    RAISE ex_data_invalida;
  END IF;

  IF p_status NOT IN ('ATIVO','ENCERRADO','CANCELADO','EXPIRADO') THEN
    RAISE ex_status_invalido;
  END IF;

  INSERT INTO subscription (pet_id, plan_id, start_date, end_date, status)
  VALUES (p_pet_id, p_plan_id, p_start_date, p_end_date, p_status);

  COMMIT;

EXCEPTION
  WHEN ex_data_invalida THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_subscription', v_usuario, -20019, 'Data de fim nao pode ser anterior a data de inicio');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20019, 'Data de fim invalida');

  WHEN ex_status_invalido THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_subscription', v_usuario, -20020, 'Status invalido: ' || p_status);
    COMMIT;
    RAISE_APPLICATION_ERROR(-20020, 'Status invalido. Use: ATIVO, ENCERRADO, CANCELADO ou EXPIRADO');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_subscription', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_subscription;

PROCEDURE sp_insert_health_event (
  p_pet_id        IN health_event.pet_id%TYPE,
  p_event_type_id IN health_event.event_type_id%TYPE,
  p_clinic_id     IN health_event.clinic_id%TYPE,
  p_description   IN health_event.description%TYPE,
  p_event_date    IN health_event.event_date%TYPE,
  p_status        IN health_event.status%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_data_futura   EXCEPTION;
  ex_status_invalido EXCEPTION;
BEGIN
  v_usuario := USER;
  IF p_event_date > SYSDATE THEN
    RAISE ex_data_futura;
  END IF;

  IF p_status NOT IN ('AGENDADO','REALIZADO','CANCELADO') THEN
    RAISE ex_status_invalido;
  END IF;

  INSERT INTO health_event (pet_id, event_type_id, clinic_id, description, event_date, status)
  VALUES (p_pet_id, p_event_type_id, p_clinic_id, p_description, p_event_date, p_status);

  COMMIT;

EXCEPTION
  WHEN ex_data_futura THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_health_event', v_usuario, -20021, 'Data do evento nao pode ser futura');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20021, 'Data do evento nao pode ser futura');

  WHEN ex_status_invalido THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_health_event', v_usuario, -20022, 'Status invalido: ' || p_status);
    COMMIT;
    RAISE_APPLICATION_ERROR(-20022, 'Status invalido');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_health_event', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_health_event;

PROCEDURE sp_insert_reward_point (
  p_tutor_id         IN reward_point.tutor_id%TYPE,
  p_reward_action_id IN reward_point.reward_action_id%TYPE,
  p_points           IN reward_point.points%TYPE,
  p_reference_type   IN reward_point.reference_type%TYPE,
  p_reference_id     IN reward_point.reference_id%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_pontos_invalidos EXCEPTION;
  ex_referencia_nula  EXCEPTION;
BEGIN
  v_usuario := USER;
  IF p_points <= 0 THEN
    RAISE ex_pontos_invalidos;
  END IF;

  IF p_reference_type IS NULL THEN
    RAISE ex_referencia_nula;
  END IF;

  INSERT INTO reward_point (tutor_id, reward_action_id, points, reference_type, reference_id)
  VALUES (p_tutor_id, p_reward_action_id, p_points, p_reference_type, p_reference_id);

  COMMIT;

EXCEPTION
  WHEN ex_pontos_invalidos THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_reward_point', v_usuario, -20023, 'Pontos devem ser maiores que zero');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20023, 'Pontos devem ser maiores que zero');

  WHEN ex_referencia_nula THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_reward_point', v_usuario, -20024, 'Tipo de referencia nao pode ser nulo');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20024, 'Tipo de referencia nao pode ser nulo');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_reward_point', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_reward_point;

PROCEDURE sp_insert_partner_discount (
  p_clinic_id        IN partner_discount.clinic_id%TYPE,
  p_partner_name     IN partner_discount.partner_name%TYPE,
  p_category         IN partner_discount.category%TYPE,
  p_discount_percent IN partner_discount.discount_percent%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_desconto_invalido EXCEPTION;
  ex_nome_nulo         EXCEPTION;
BEGIN
  v_usuario := USER;
  IF p_partner_name IS NULL THEN
    RAISE ex_nome_nulo;
  END IF;

  IF p_discount_percent <= 0 OR p_discount_percent > 100 THEN
    RAISE ex_desconto_invalido;
  END IF;

  INSERT INTO partner_discount (clinic_id, partner_name, category, discount_percent)
  VALUES (p_clinic_id, p_partner_name, p_category, p_discount_percent);

  COMMIT;

EXCEPTION
  WHEN ex_nome_nulo THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_partner_discount', v_usuario, -20025, 'Nome do parceiro nao pode ser nulo');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20025, 'Nome do parceiro nao pode ser nulo');

  WHEN ex_desconto_invalido THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_partner_discount', v_usuario, -20026, 'Desconto deve estar entre 1 e 100');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20026, 'Percentual de desconto invalido');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_partner_discount', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_partner_discount;

PROCEDURE sp_insert_coupon_template (
  p_partner_discount_id IN coupon_template.partner_discount_id%TYPE,
  p_title               IN coupon_template.title%TYPE,
  p_discount_value      IN coupon_template.discount_value%TYPE,
  p_discount_type       IN coupon_template.discount_type%TYPE,
  p_points_required     IN coupon_template.points_required%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_tipo_invalido      EXCEPTION;
  ex_pontos_invalidos   EXCEPTION;
BEGIN
  v_usuario := USER;
  IF p_discount_type NOT IN ('PERCENTUAL','VALOR_FIXO') THEN
    RAISE ex_tipo_invalido;
  END IF;

  IF p_points_required <= 0 THEN
    RAISE ex_pontos_invalidos;
  END IF;

  INSERT INTO coupon_template (partner_discount_id, title, discount_value, discount_type, points_required)
  VALUES (p_partner_discount_id, p_title, p_discount_value, p_discount_type, p_points_required);

  COMMIT;

EXCEPTION
  WHEN ex_tipo_invalido THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_coupon_template', v_usuario, -20027, 'Tipo de desconto invalido: ' || p_discount_type);
    COMMIT;
    RAISE_APPLICATION_ERROR(-20027, 'Use PERCENTUAL ou VALOR_FIXO');

  WHEN ex_pontos_invalidos THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_coupon_template', v_usuario, -20028, 'Pontos necessarios devem ser maiores que zero');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20028, 'Pontos necessarios invalidos');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_coupon_template', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_coupon_template;

PROCEDURE sp_insert_coupon (
  p_template_id     IN coupon.template_id%TYPE,
  p_code            IN coupon.code%TYPE,
  p_status          IN coupon.status%TYPE,
  p_expiration_date IN coupon.expiration_date%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_duplicado    EXCEPTION;
  ex_status_invalido EXCEPTION;
  PRAGMA EXCEPTION_INIT(ex_duplicado, -1);
BEGIN
  v_usuario := USER;
  IF p_status NOT IN ('DISPONIVEL','RESGATADO','UTILIZADO') THEN
    RAISE ex_status_invalido;
  END IF;

  INSERT INTO coupon (template_id, code, status, expiration_date)
  VALUES (p_template_id, p_code, p_status, p_expiration_date);

  COMMIT;

EXCEPTION
  WHEN ex_status_invalido THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_coupon', v_usuario, -20029, 'Status de cupom invalido: ' || p_status);
    COMMIT;
    RAISE_APPLICATION_ERROR(-20029, 'Status invalido');

  WHEN ex_duplicado THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_coupon', v_usuario, -1, 'Codigo de cupom ja existe: ' || p_code);
    COMMIT;
    RAISE_APPLICATION_ERROR(-20030, 'Codigo de cupom duplicado');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_coupon', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_coupon;

PROCEDURE sp_insert_redeem (
  p_tutor_id   IN redeem.tutor_id%TYPE,
  p_coupon_id  IN redeem.coupon_id%TYPE,
  p_points_used IN redeem.points_used%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_pontos_invalidos EXCEPTION;
  ex_cupom_indisponivel EXCEPTION;
  v_status VARCHAR2(20);
BEGIN
  v_usuario := USER;
  IF p_points_used <= 0 THEN
    RAISE ex_pontos_invalidos;
  END IF;

  SELECT status INTO v_status FROM coupon WHERE id = p_coupon_id;
  IF v_status != 'DISPONIVEL' THEN
    RAISE ex_cupom_indisponivel;
  END IF;

  INSERT INTO redeem (tutor_id, coupon_id, points_used)
  VALUES (p_tutor_id, p_coupon_id, p_points_used);

  UPDATE coupon SET status = 'RESGATADO' WHERE id = p_coupon_id;

  COMMIT;

EXCEPTION
  WHEN ex_pontos_invalidos THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_redeem', v_usuario, -20031, 'Pontos usados devem ser maiores que zero');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20031, 'Pontos invalidos');

  WHEN ex_cupom_indisponivel THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_redeem', v_usuario, -20032, 'Cupom nao esta disponivel para resgate');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20032, 'Cupom indisponivel');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_redeem', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_redeem;

PROCEDURE sp_insert_risk_score (
  p_pet_id        IN risk_score.pet_id%TYPE,
  p_score         IN risk_score.score%TYPE,
  p_risk_level_id IN risk_score.risk_level_id%TYPE
) AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);
  ex_score_invalido EXCEPTION;
  ex_nivel_invalido EXCEPTION;
  v_count NUMBER;
BEGIN
  v_usuario := USER;
  IF p_score < 0 OR p_score > 100 THEN
    RAISE ex_score_invalido;
  END IF;

  SELECT COUNT(*) INTO v_count FROM risk_level WHERE id = p_risk_level_id;
  IF v_count = 0 THEN
    RAISE ex_nivel_invalido;
  END IF;

  INSERT INTO risk_score (pet_id, score, risk_level_id)
  VALUES (p_pet_id, p_score, p_risk_level_id);

  COMMIT;

EXCEPTION
  WHEN ex_score_invalido THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_risk_score', v_usuario, -20033, 'Score deve estar entre 0 e 100');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20033, 'Score invalido');

  WHEN ex_nivel_invalido THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_risk_score', v_usuario, -20034, 'Nivel de risco nao encontrado: ID ' || p_risk_level_id);
    COMMIT;
    RAISE_APPLICATION_ERROR(-20034, 'Nivel de risco invalido');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_insert_risk_score', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_insert_risk_score;

END pkg_petflow_cadastro;
/


-- #############################################################
-- 2) PACOTE PKG_PETFLOW_RELATORIO
--    Agrupa as 2 functions e as 2 procedures de relatorio da
--    Sprint 3 (conversao manual para JSON, validacao de senha,
--    relatorio via JOIN e relatorio com subtotal/total manual).
-- #############################################################

CREATE OR REPLACE PACKAGE pkg_petflow_relatorio AS
  -- ---------------------------------------------------------
  -- Empacota as functions e procedures de regra de negocio e
  -- relatorio da Sprint 3. Mesma assinatura, mesmas validacoes e
  -- mesmo tratamento de excecoes das versoes originais.
  -- sp_relatorio_pets_json usa internamente
  -- fn_pet_tutor_to_json (chamada direta ao membro do pacote,
  -- sem qualificacao, pois ambos estao no mesmo pacote).
  -- ---------------------------------------------------------

  FUNCTION fn_pet_tutor_to_json (
  p_pet_id      IN pet.id%TYPE,
  p_pet_name    IN pet.name%TYPE,
  p_breed       IN pet.breed%TYPE,
  p_weight      IN pet.weight%TYPE,
  p_tutor_name  IN tutor.name%TYPE,
  p_species_nm  IN species.name%TYPE
) RETURN VARCHAR2;

  FUNCTION fn_validar_senha (
  p_senha IN VARCHAR2
) RETURN VARCHAR2;

  PROCEDURE sp_relatorio_pets_json;

  PROCEDURE sp_relatorio_pontos_categoria;

END pkg_petflow_relatorio;
/


CREATE OR REPLACE PACKAGE BODY pkg_petflow_relatorio AS

FUNCTION fn_pet_tutor_to_json (
  p_pet_id      IN pet.id%TYPE,
  p_pet_name    IN pet.name%TYPE,
  p_breed       IN pet.breed%TYPE,
  p_weight      IN pet.weight%TYPE,
  p_tutor_name  IN tutor.name%TYPE,
  p_species_nm  IN species.name%TYPE
) RETURN VARCHAR2
AS
  v_usuario   VARCHAR2(100);
  v_sqlcode   NUMBER;
  v_sqlerrm   VARCHAR2(4000);
  v_json      VARCHAR2(4000);
  v_breed     VARCHAR2(60);
  v_weight    VARCHAR2(20);

  ex_id_nulo    EXCEPTION;
  ex_nome_nulo  EXCEPTION;
  ex_json_grande EXCEPTION;
BEGIN
  v_usuario := USER;

  -- Excecao 1: identificador obrigatorio
  IF p_pet_id IS NULL THEN
    RAISE ex_id_nulo;
  END IF;

  -- Excecao 2: campos textuais obrigatorios para montar o JSON
  IF p_pet_name IS NULL OR p_tutor_name IS NULL THEN
    RAISE ex_nome_nulo;
  END IF;

  -- Escape manual de aspas duplas nos campos de texto (regra basica de JSON)
  v_breed  := REPLACE(NVL(p_breed, ''), '"', '\"');
  v_weight := CASE WHEN p_weight IS NULL THEN 'null' ELSE TO_CHAR(p_weight) END;

  v_json :=
    '{'                                                              ||
    '"pet_id":'      || p_pet_id                                     || ',' ||
    '"pet_name":"'   || REPLACE(p_pet_name, '"', '\"')  || '",'      ||
    '"breed":"'      || v_breed                          || '",'    ||
    '"weight":'      || v_weight                                     || ',' ||
    '"tutor_name":"' || REPLACE(p_tutor_name, '"', '\"') || '",'     ||
    '"species":"'    || REPLACE(NVL(p_species_nm,''), '"', '\"') || '"' ||
    '}';

  -- Excecao 3: protecao contra JSON fora do tamanho suportado pela variavel
  IF LENGTH(v_json) > 4000 THEN
    RAISE ex_json_grande;
  END IF;

  RETURN v_json;

EXCEPTION
  WHEN ex_id_nulo THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('fn_pet_tutor_to_json', v_usuario, -20040, 'ID do pet nao pode ser nulo');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20040, 'ID do pet nao pode ser nulo');

  WHEN ex_nome_nulo THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('fn_pet_tutor_to_json', v_usuario, -20041, 'Nome do pet ou do tutor nao pode ser nulo');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20041, 'Nome do pet ou do tutor nao pode ser nulo');

  WHEN ex_json_grande THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('fn_pet_tutor_to_json', v_usuario, -20042, 'JSON gerado excede o tamanho maximo suportado');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20042, 'JSON gerado excede o tamanho maximo suportado');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('fn_pet_tutor_to_json', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END fn_pet_tutor_to_json;

FUNCTION fn_validar_senha (
  p_senha IN VARCHAR2
) RETURN VARCHAR2
AS
  v_usuario VARCHAR2(100);
  v_sqlcode NUMBER;
  v_sqlerrm VARCHAR2(4000);

  ex_senha_nula       EXCEPTION;
  ex_senha_curta      EXCEPTION;
  ex_senha_sem_numero EXCEPTION;
  ex_senha_sem_letra  EXCEPTION;
BEGIN
  v_usuario := USER;

  -- Excecao 1
  IF p_senha IS NULL THEN
    RAISE ex_senha_nula;
  END IF;

  -- Excecao 2
  IF LENGTH(p_senha) < 8 THEN
    RAISE ex_senha_curta;
  END IF;

  -- Excecao 3
  IF NOT REGEXP_LIKE(p_senha, '[0-9]') THEN
    RAISE ex_senha_sem_numero;
  END IF;

  -- Excecao 4
  IF NOT REGEXP_LIKE(p_senha, '[A-Za-z]') THEN
    RAISE ex_senha_sem_letra;
  END IF;

  RETURN 'SENHA_VALIDA';

EXCEPTION
  WHEN ex_senha_nula THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('fn_validar_senha', v_usuario, -20050, 'Senha nao pode ser nula');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20050, 'Senha nao pode ser nula');

  WHEN ex_senha_curta THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('fn_validar_senha', v_usuario, -20051, 'Senha deve ter no minimo 8 caracteres');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20051, 'Senha deve ter no minimo 8 caracteres');

  WHEN ex_senha_sem_numero THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('fn_validar_senha', v_usuario, -20052, 'Senha deve conter ao menos um numero');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20052, 'Senha deve conter ao menos um numero');

  WHEN ex_senha_sem_letra THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('fn_validar_senha', v_usuario, -20053, 'Senha deve conter ao menos uma letra');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20053, 'Senha deve conter ao menos uma letra');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('fn_validar_senha', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END fn_validar_senha;

PROCEDURE sp_relatorio_pets_json
AS
  v_usuario     VARCHAR2(100);
  v_sqlcode     NUMBER;
  v_sqlerrm     VARCHAR2(4000);
  v_json        VARCHAR2(4000);
  v_encontrou   BOOLEAN := FALSE;

  ex_sem_dados       EXCEPTION;
  ex_falha_conversao EXCEPTION;

  CURSOR c_pets IS
    SELECT
      p.id      AS pet_id,
      p.name    AS pet_name,
      p.breed   AS breed,
      p.weight  AS weight,
      t.name    AS tutor_name,
      s.name    AS species_name
    FROM pet p
    JOIN tutor   t ON t.id = p.tutor_id
    JOIN species s ON s.id = p.species_id
    ORDER BY t.name, p.name;
BEGIN
  v_usuario := USER;

  DBMS_OUTPUT.PUT_LINE('==============================================');
  DBMS_OUTPUT.PUT_LINE('PROCEDURE 1: PETS + TUTORES + ESPECIES (JSON)');
  DBMS_OUTPUT.PUT_LINE('==============================================');

  FOR r IN c_pets LOOP
    v_encontrou := TRUE;

    BEGIN
      v_json := fn_pet_tutor_to_json(
                  p_pet_id     => r.pet_id,
                  p_pet_name   => r.pet_name,
                  p_breed      => r.breed,
                  p_weight     => r.weight,
                  p_tutor_name => r.tutor_name,
                  p_species_nm => r.species_name
                );
    EXCEPTION
      WHEN OTHERS THEN
        RAISE ex_falha_conversao;
    END;

    DBMS_OUTPUT.PUT_LINE(v_json);
  END LOOP;

  -- Excecao: cursor sem nenhuma linha retornada
  IF NOT v_encontrou THEN
    RAISE ex_sem_dados;
  END IF;

EXCEPTION
  WHEN ex_sem_dados THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_relatorio_pets_json', v_usuario, -20060, 'Nenhum registro encontrado para o JOIN pet/tutor/species');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20060, 'Nenhum registro encontrado para gerar o JSON');

  WHEN ex_falha_conversao THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_relatorio_pets_json', v_usuario, -20061, 'Falha ao converter linha para JSON via fn_pet_tutor_to_json');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20061, 'Falha ao converter linha para JSON');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_relatorio_pets_json', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_relatorio_pets_json;

PROCEDURE sp_relatorio_pontos_categoria
AS
  v_usuario         VARCHAR2(100);
  v_sqlcode         NUMBER;
  v_sqlerrm         VARCHAR2(4000);

  v_tutor_atual     tutor.name%TYPE := NULL;
  v_acao_atual      reward_action.name%TYPE := NULL;
  v_soma_combo      NUMBER := 0;
  v_subtotal_tutor  NUMBER := 0;
  v_total_geral     NUMBER := 0;
  v_primeira_linha  BOOLEAN := TRUE;
  v_encontrou       BOOLEAN := FALSE;

  ex_sem_dados       EXCEPTION;
  ex_valor_negativo  EXCEPTION;

  -- Cursor ja ordenado pela hierarquia das categorias (tutor, acao).
  -- A soma da combinacao (tutor, acao) e feita linha a linha, dentro
  -- do proprio bloco PL/SQL (controle de quebra manual), sem usar
  -- ROLLUP/CUBE/GROUPING SETS.
  CURSOR c_fatos IS
    SELECT
      t.name  AS tutor_name,
      ra.name AS acao_name,
      rp.points AS pontos
    FROM reward_point rp
    JOIN tutor         t  ON t.id  = rp.tutor_id
    JOIN reward_action ra ON ra.id = rp.reward_action_id
    ORDER BY t.name, ra.name;

  PROCEDURE imprimir_combo IS
  BEGIN
    IF v_tutor_atual IS NOT NULL AND v_acao_atual IS NOT NULL THEN
      DBMS_OUTPUT.PUT_LINE(
        RPAD(v_tutor_atual, 20) ||
        RPAD(v_acao_atual, 30) ||
        LPAD(v_soma_combo, 12)
      );
    END IF;
  END imprimir_combo;

BEGIN
  v_usuario := USER;

  DBMS_OUTPUT.PUT_LINE('==============================================');
  DBMS_OUTPUT.PUT_LINE('PROCEDURE 2: PONTOS POR TUTOR E ACAO (SEM ROLLUP)');
  DBMS_OUTPUT.PUT_LINE('==============================================');
  DBMS_OUTPUT.PUT_LINE(RPAD('Tutor',20) || RPAD('Acao',30) || LPAD('Pontos',12));
  DBMS_OUTPUT.PUT_LINE(RPAD('-',62,'-'));

  FOR r IN c_fatos LOOP
    v_encontrou := TRUE;

    -- Pontos negativos so sao validos para a acao RESGATE_CUPOM (debito de resgate)
    IF r.pontos < 0 AND r.acao_name <> 'RESGATE_CUPOM' THEN
      RAISE ex_valor_negativo;
    END IF;

    -- Quebra de combo (tutor + acao mudou): imprime combo anterior
    IF NOT v_primeira_linha AND (r.tutor_name != v_tutor_atual OR r.acao_name != v_acao_atual) THEN
      imprimir_combo;
      v_soma_combo := 0;
    END IF;

    -- Quebra de tutor (primeira categoria mudou): imprime subtotal
    IF NOT v_primeira_linha AND r.tutor_name != v_tutor_atual THEN
      DBMS_OUTPUT.PUT_LINE(
        RPAD('  Sub Total ' || v_tutor_atual, 50) ||
        LPAD(v_subtotal_tutor, 12)
      );
      DBMS_OUTPUT.PUT_LINE(RPAD('-',62,'-'));
      v_subtotal_tutor := 0;
    END IF;

    v_tutor_atual    := r.tutor_name;
    v_acao_atual     := r.acao_name;
    v_soma_combo      := v_soma_combo      + r.pontos;
    v_subtotal_tutor  := v_subtotal_tutor  + r.pontos;
    v_total_geral     := v_total_geral     + r.pontos;
    v_primeira_linha  := FALSE;
  END LOOP;

  IF NOT v_encontrou THEN
    RAISE ex_sem_dados;
  END IF;

  -- Imprime o ultimo combo e o ultimo subtotal pendentes
  imprimir_combo;
  DBMS_OUTPUT.PUT_LINE(
    RPAD('  Sub Total ' || v_tutor_atual, 50) ||
    LPAD(v_subtotal_tutor, 12)
  );
  DBMS_OUTPUT.PUT_LINE(RPAD('=',62,'='));
  DBMS_OUTPUT.PUT_LINE(RPAD('Total Geral', 50) || LPAD(v_total_geral, 12));

EXCEPTION
  WHEN ex_sem_dados THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_relatorio_pontos_categoria', v_usuario, -20070, 'Nenhum registro encontrado em reward_point');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20070, 'Nenhum registro encontrado em reward_point');

  WHEN ex_valor_negativo THEN
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_relatorio_pontos_categoria', v_usuario, -20071, 'Valor de pontos negativo encontrado na tabela de fatos');
    COMMIT;
    RAISE_APPLICATION_ERROR(-20071, 'Valor de pontos negativo encontrado');

  WHEN OTHERS THEN
    v_sqlcode := SQLCODE;
    v_sqlerrm := SQLERRM;
    INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
    VALUES ('sp_relatorio_pontos_categoria', v_usuario, v_sqlcode, v_sqlerrm);
    COMMIT;
    RAISE;
END sp_relatorio_pontos_categoria;

END pkg_petflow_relatorio;
/


-- #############################################################
-- 3) PACOTE PKG_PETFLOW_AUDITORIA
--    Concentra a logica de auditoria de HEALTH_EVENT que antes
--    estava embutida diretamente no corpo do trigger.
--    CORRECAO (Sprint 4): o trigger passa a enviar os valores
--    :OLD e :NEW separadamente, de modo que VALORES_ANTERIORES
--    registre de fato o estado anterior do registro (antes, em
--    UPDATE, os valores "anteriores" eram copiados de :NEW).
-- #############################################################

CREATE OR REPLACE PACKAGE pkg_petflow_auditoria AS

  -- Registra em LOG_AUDITORIA_HEALTH_EVENT uma operacao de
  -- INSERT, UPDATE ou DELETE ocorrida em HEALTH_EVENT.
  -- Chamada exclusivamente pelo trigger trg_audit_health_event
  -- (FOR EACH ROW), que repassa os valores :OLD e :NEW ja
  -- resolvidos em parametros simples. Em INSERT os parametros
  -- p_old_* chegam NULL; em DELETE os p_new_* chegam NULL.
  PROCEDURE sp_registrar_auditoria_health_event (
    p_tipo_operacao     IN VARCHAR2,
    p_old_id            IN NUMBER,
    p_old_pet_id        IN NUMBER,
    p_old_event_type_id IN NUMBER,
    p_old_status        IN VARCHAR2,
    p_old_event_date    IN DATE,
    p_new_id            IN NUMBER,
    p_new_pet_id        IN NUMBER,
    p_new_event_type_id IN NUMBER,
    p_new_status        IN VARCHAR2,
    p_new_event_date    IN DATE
  );

END pkg_petflow_auditoria;
/


CREATE OR REPLACE PACKAGE BODY pkg_petflow_auditoria AS

  -- Funcao privada do pacote: monta o texto padronizado de valores.
  FUNCTION fn_montar_valores (
    p_id            IN NUMBER,
    p_pet_id        IN NUMBER,
    p_event_type_id IN NUMBER,
    p_status        IN VARCHAR2,
    p_event_date    IN DATE
  ) RETURN VARCHAR2 IS
  BEGIN
    RETURN 'ID=' || p_id ||
           ';PET_ID=' || p_pet_id ||
           ';EVENT_TYPE_ID=' || p_event_type_id ||
           ';STATUS=' || p_status ||
           ';EVENT_DATE=' || TO_CHAR(p_event_date, 'YYYY-MM-DD');
  END fn_montar_valores;

  PROCEDURE sp_registrar_auditoria_health_event (
    p_tipo_operacao     IN VARCHAR2,
    p_old_id            IN NUMBER,
    p_old_pet_id        IN NUMBER,
    p_old_event_type_id IN NUMBER,
    p_old_status        IN VARCHAR2,
    p_old_event_date    IN DATE,
    p_new_id            IN NUMBER,
    p_new_pet_id        IN NUMBER,
    p_new_event_type_id IN NUMBER,
    p_new_status        IN VARCHAR2,
    p_new_event_date    IN DATE
  ) AS
    v_usuario          VARCHAR2(100);
    v_sqlcode          NUMBER;
    v_sqlerrm          VARCHAR2(4000);
    v_valores_antigos  VARCHAR2(4000);
    v_valores_novos    VARCHAR2(4000);
    ex_tipo_invalido   EXCEPTION;
  BEGIN
    v_usuario := USER;

    -- Excecao 1: tipo de operacao fora do dominio esperado
    IF p_tipo_operacao NOT IN ('INSERT','UPDATE','DELETE') THEN
      RAISE ex_tipo_invalido;
    END IF;

    IF p_tipo_operacao IN ('UPDATE','DELETE') THEN
      v_valores_antigos := fn_montar_valores(p_old_id, p_old_pet_id, p_old_event_type_id,
                                             p_old_status, p_old_event_date);
    END IF;

    IF p_tipo_operacao IN ('INSERT','UPDATE') THEN
      v_valores_novos := fn_montar_valores(p_new_id, p_new_pet_id, p_new_event_type_id,
                                           p_new_status, p_new_event_date);
    END IF;

    INSERT INTO log_auditoria_health_event (
      nome_usuario, tipo_operacao, data_hora, valores_anteriores, valores_novos
    ) VALUES (
      USER, p_tipo_operacao, SYSTIMESTAMP, v_valores_antigos, v_valores_novos
    );

  EXCEPTION
    WHEN ex_tipo_invalido THEN
      INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
      VALUES ('sp_registrar_auditoria_health_event', v_usuario, -20080,
              'Tipo de operacao invalido: ' || p_tipo_operacao);
      COMMIT;
      RAISE_APPLICATION_ERROR(-20080, 'Tipo de operacao de auditoria invalido');

    WHEN OTHERS THEN
      v_sqlcode := SQLCODE;
      v_sqlerrm := SQLERRM;
      INSERT INTO log_erros (procedure_name, usuario, codigo_erro, mensagem_erro)
      VALUES ('sp_registrar_auditoria_health_event', v_usuario, v_sqlcode, v_sqlerrm);
      COMMIT;
      RAISE;
  END sp_registrar_auditoria_health_event;

END pkg_petflow_auditoria;
/


-- #############################################################
-- 4) RECRIACAO DO TRIGGER, AGORA DELEGANDO PARA O PACOTE
--    trg_audit_health_event continua sendo um objeto de schema
--    (exigencia da linguagem), mas toda a logica de montagem da
--    auditoria passa a residir em PKG_PETFLOW_AUDITORIA.
-- #############################################################

CREATE OR REPLACE TRIGGER trg_audit_health_event
AFTER INSERT OR UPDATE OR DELETE ON health_event
FOR EACH ROW
DECLARE
  v_tipo VARCHAR2(6);
BEGIN
  IF INSERTING THEN
    v_tipo := 'INSERT';
  ELSIF UPDATING THEN
    v_tipo := 'UPDATE';
  ELSE
    v_tipo := 'DELETE';
  END IF;

  pkg_petflow_auditoria.sp_registrar_auditoria_health_event(
    p_tipo_operacao     => v_tipo,
    p_old_id            => :OLD.id,
    p_old_pet_id        => :OLD.pet_id,
    p_old_event_type_id => :OLD.event_type_id,
    p_old_status        => :OLD.status,
    p_old_event_date    => :OLD.event_date,
    p_new_id            => :NEW.id,
    p_new_pet_id        => :NEW.pet_id,
    p_new_event_type_id => :NEW.event_type_id,
    p_new_status        => :NEW.status,
    p_new_event_date    => :NEW.event_date
  );
END trg_audit_health_event;
/


-- #############################################################
-- 5) SMOKE TEST - valida que os pacotes foram criados e
--    funcionam, chamando 1 membro de cada pacote.
--    (Nao substitui a demonstracao completa exigida no video;
--    serve apenas como verificacao rapida pos-deploy.)
-- #############################################################

SET SERVEROUTPUT ON;

-- Teste PKG_PETFLOW_RELATORIO (funcoes)
DECLARE
  v_json  VARCHAR2(4000);
  v_senha VARCHAR2(50);
BEGIN
  v_json := pkg_petflow_relatorio.fn_pet_tutor_to_json(
              1, 'Rex', 'Labrador', 28.5, 'Ana Lima', 'Cachorro'
            );
  DBMS_OUTPUT.PUT_LINE('PKG_PETFLOW_RELATORIO.fn_pet_tutor_to_json: ' || v_json);

  v_senha := pkg_petflow_relatorio.fn_validar_senha('SenhaForte123');
  DBMS_OUTPUT.PUT_LINE('PKG_PETFLOW_RELATORIO.fn_validar_senha: ' || v_senha);
END;
/

-- Teste PKG_PETFLOW_RELATORIO (procedures de relatorio)
BEGIN
  pkg_petflow_relatorio.sp_relatorio_pets_json;
  pkg_petflow_relatorio.sp_relatorio_pontos_categoria;
END;
/

-- Teste PKG_PETFLOW_CADASTRO (uma insercao simples de dominio)
BEGIN
  pkg_petflow_cadastro.sp_insert_species('Roedor', 'Especie de teste do pacote (Sprint 4)');
END;
/

-- Teste PKG_PETFLOW_AUDITORIA (via trigger, com INSERT/UPDATE/DELETE reais)
INSERT INTO health_event (pet_id, event_type_id, clinic_id, description, event_date, status)
VALUES (2, 4, 2, 'Check-up de rotina - teste pacote auditoria Sprint 4', DATE '2026-05-01', 'REALIZADO');
COMMIT;

UPDATE health_event
SET status = 'CANCELADO'
WHERE description = 'Check-up de rotina - teste pacote auditoria Sprint 4';
COMMIT;

DELETE FROM health_event
WHERE description = 'Check-up de rotina - teste pacote auditoria Sprint 4';
COMMIT;

SELECT id, nome_usuario, tipo_operacao, data_hora, valores_anteriores, valores_novos
FROM log_auditoria_health_event
ORDER BY id DESC
FETCH FIRST 3 ROWS ONLY;

SELECT procedure_name, usuario, data_ocorrencia, codigo_erro, mensagem_erro
FROM log_erros
ORDER BY id DESC
FETCH FIRST 5 ROWS ONLY;
