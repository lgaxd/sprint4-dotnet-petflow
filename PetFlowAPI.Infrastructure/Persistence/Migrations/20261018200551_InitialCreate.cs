using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetFlowAPI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CLINIC",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NAME = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: false),
                    ADDRESS = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: true),
                    PHONE = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: true),
                    CNPJ = table.Column<string>(type: "VARCHAR2(18)", unicode: false, maxLength: 18, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLINIC", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "EVENT_TYPE",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NAME = table.Column<string>(type: "VARCHAR2(50)", unicode: false, maxLength: 50, nullable: false),
                    POINTS_REWARD = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CATEGORY = table.Column<string>(type: "VARCHAR2(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EVENT_TYPE", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "LOG_AUDITORIA_HEALTH_EVENT",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOME_USUARIO = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: false),
                    TIPO_OPERACAO = table.Column<string>(type: "VARCHAR2(10)", unicode: false, maxLength: 10, nullable: false),
                    DATA_HORA = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    VALORES_ANTERIORES = table.Column<string>(type: "VARCHAR2(4000)", unicode: false, maxLength: 4000, nullable: true),
                    VALORES_NOVOS = table.Column<string>(type: "VARCHAR2(4000)", unicode: false, maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOG_AUDITORIA_HEALTH_EVENT", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "LOG_ERROS",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    PROCEDURE_NAME = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: false),
                    USUARIO = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: false),
                    DATA_OCORRENCIA = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CODIGO_ERRO = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    MENSAGEM_ERRO = table.Column<string>(type: "VARCHAR2(4000)", unicode: false, maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOG_ERROS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "REWARD_ACTION",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NAME = table.Column<string>(type: "VARCHAR2(50)", unicode: false, maxLength: 50, nullable: false),
                    POINTS_VALUE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DESCRIPTION = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REWARD_ACTION", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "RISK_LEVEL",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NAME = table.Column<string>(type: "VARCHAR2(50)", unicode: false, maxLength: 50, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: true),
                    MIN_SCORE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    MAX_SCORE = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RISK_LEVEL", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SPECIES",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NAME = table.Column<string>(type: "VARCHAR2(50)", unicode: false, maxLength: 50, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SPECIES", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TUTOR",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ROLE = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "TUTOR"),
                    NAME = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: false),
                    EMAIL = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: false),
                    PHONE = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: true),
                    PASSWORD_HASH = table.Column<string>(type: "VARCHAR2(255)", unicode: false, maxLength: 255, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TUTOR", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PARTNER_DISCOUNT",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    CLINIC_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    PARTNER_NAME = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: false),
                    CATEGORY = table.Column<string>(type: "VARCHAR2(50)", unicode: false, maxLength: 50, nullable: false),
                    DISCOUNT_PERCENT = table.Column<decimal>(type: "DECIMAL(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PARTNER_DISCOUNT", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PARTNER_DISCOUNT_CLINIC_CLINIC_ID",
                        column: x => x.CLINIC_ID,
                        principalTable: "CLINIC",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PLAN",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    CLINIC_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    NAME = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: true),
                    PRICE = table.Column<decimal>(type: "DECIMAL(10,2)", precision: 10, scale: 2, nullable: false),
                    DURATION_DAYS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    POINTS_PER_EVENT = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PLAN", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PLAN_CLINIC_CLINIC_ID",
                        column: x => x.CLINIC_ID,
                        principalTable: "CLINIC",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ADDRESS",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TUTOR_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    STREET = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: false),
                    CITY = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: false),
                    STATE = table.Column<string>(type: "VARCHAR2(50)", unicode: false, maxLength: 50, nullable: false),
                    ZIP_CODE = table.Column<string>(type: "VARCHAR2(10)", unicode: false, maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADDRESS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ADDRESS_TUTOR_TUTOR_ID",
                        column: x => x.TUTOR_ID,
                        principalTable: "TUTOR",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PET",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TUTOR_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    SPECIES_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    NAME = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: false),
                    BREED = table.Column<string>(type: "VARCHAR2(50)", unicode: false, maxLength: 50, nullable: true),
                    BIRTH_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    WEIGHT = table.Column<decimal>(type: "DECIMAL(5,2)", precision: 5, scale: 2, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PET", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PET_SPECIES_SPECIES_ID",
                        column: x => x.SPECIES_ID,
                        principalTable: "SPECIES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PET_TUTOR_TUTOR_ID",
                        column: x => x.TUTOR_ID,
                        principalTable: "TUTOR",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "REWARD_POINT",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TUTOR_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    REWARD_ACTION_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    POINTS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    REFERENCE_TYPE = table.Column<string>(type: "VARCHAR2(50)", unicode: false, maxLength: 50, nullable: true),
                    REFERENCE_ID = table.Column<long>(type: "NUMBER(19)", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REWARD_POINT", x => x.ID);
                    table.ForeignKey(
                        name: "FK_REWARD_POINT_REWARD_ACTION_REWARD_ACTION_ID",
                        column: x => x.REWARD_ACTION_ID,
                        principalTable: "REWARD_ACTION",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_REWARD_POINT_TUTOR_TUTOR_ID",
                        column: x => x.TUTOR_ID,
                        principalTable: "TUTOR",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "COUPON_TEMPLATE",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    PARTNER_DISCOUNT_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    TITLE = table.Column<string>(type: "VARCHAR2(100)", unicode: false, maxLength: 100, nullable: false),
                    DISCOUNT_VALUE = table.Column<decimal>(type: "DECIMAL(10,2)", precision: 10, scale: 2, nullable: false),
                    DISCOUNT_TYPE = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    POINTS_REQUIRED = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COUPON_TEMPLATE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_COUPON_TEMPLATE_PARTNER_DISCOUNT_PARTNER_DISCOUNT_ID",
                        column: x => x.PARTNER_DISCOUNT_ID,
                        principalTable: "PARTNER_DISCOUNT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HEALTH_EVENT",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    PET_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    EVENT_TYPE_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    CLINIC_ID = table.Column<long>(type: "NUMBER(19)", nullable: true),
                    DESCRIPTION = table.Column<string>(type: "VARCHAR2(200)", unicode: false, maxLength: 200, nullable: true),
                    EVENT_DATE = table.Column<DateTime>(type: "DATE", nullable: false),
                    STATUS = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HEALTH_EVENT", x => x.ID);
                    table.ForeignKey(
                        name: "FK_HEALTH_EVENT_CLINIC_CLINIC_ID",
                        column: x => x.CLINIC_ID,
                        principalTable: "CLINIC",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HEALTH_EVENT_EVENT_TYPE_EVENT_TYPE_ID",
                        column: x => x.EVENT_TYPE_ID,
                        principalTable: "EVENT_TYPE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HEALTH_EVENT_PET_PET_ID",
                        column: x => x.PET_ID,
                        principalTable: "PET",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RISK_SCORE",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    PET_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    SCORE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    RISK_LEVEL_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    CALCULATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RISK_SCORE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RISK_SCORE_PET_PET_ID",
                        column: x => x.PET_ID,
                        principalTable: "PET",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RISK_SCORE_RISK_LEVEL_RISK_LEVEL_ID",
                        column: x => x.RISK_LEVEL_ID,
                        principalTable: "RISK_LEVEL",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SUBSCRIPTION",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    PET_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    PLAN_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    START_DATE = table.Column<DateTime>(type: "DATE", nullable: false),
                    END_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    STATUS = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SUBSCRIPTION", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SUBSCRIPTION_PET_PET_ID",
                        column: x => x.PET_ID,
                        principalTable: "PET",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SUBSCRIPTION_PLAN_PLAN_ID",
                        column: x => x.PLAN_ID,
                        principalTable: "PLAN",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "COUPON",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TEMPLATE_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    CODE = table.Column<string>(type: "VARCHAR2(50)", unicode: false, maxLength: 50, nullable: false),
                    STATUS = table.Column<string>(type: "VARCHAR2(20)", unicode: false, maxLength: 20, nullable: false),
                    EXPIRATION_DATE = table.Column<DateTime>(type: "DATE", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COUPON", x => x.ID);
                    table.ForeignKey(
                        name: "FK_COUPON_COUPON_TEMPLATE_TEMPLATE_ID",
                        column: x => x.TEMPLATE_ID,
                        principalTable: "COUPON_TEMPLATE",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "REDEEM",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TUTOR_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    COUPON_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    POINTS_USED = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REDEEM", x => x.ID);
                    table.ForeignKey(
                        name: "FK_REDEEM_COUPON_COUPON_ID",
                        column: x => x.COUPON_ID,
                        principalTable: "COUPON",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_REDEEM_TUTOR_TUTOR_ID",
                        column: x => x.TUTOR_ID,
                        principalTable: "TUTOR",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADDRESS_TUTOR_ID",
                table: "ADDRESS",
                column: "TUTOR_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CLINIC_CNPJ",
                table: "CLINIC",
                column: "CNPJ",
                unique: true,
                filter: "\"CNPJ\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_COUPON_CODE",
                table: "COUPON",
                column: "CODE",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_COUPON_TEMPLATE_ID",
                table: "COUPON",
                column: "TEMPLATE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_COUPON_TEMPLATE_PARTNER_DISCOUNT_ID",
                table: "COUPON_TEMPLATE",
                column: "PARTNER_DISCOUNT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_EVENT_TYPE_NAME",
                table: "EVENT_TYPE",
                column: "NAME",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HEALTH_EVENT_CLINIC_ID",
                table: "HEALTH_EVENT",
                column: "CLINIC_ID");

            migrationBuilder.CreateIndex(
                name: "IX_HEALTH_EVENT_EVENT_TYPE_ID",
                table: "HEALTH_EVENT",
                column: "EVENT_TYPE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_HEALTH_EVENT_PET_ID",
                table: "HEALTH_EVENT",
                column: "PET_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PARTNER_DISCOUNT_CLINIC_ID",
                table: "PARTNER_DISCOUNT",
                column: "CLINIC_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PET_SPECIES_ID",
                table: "PET",
                column: "SPECIES_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PET_TUTOR_ID",
                table: "PET",
                column: "TUTOR_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PLAN_CLINIC_ID",
                table: "PLAN",
                column: "CLINIC_ID");

            migrationBuilder.CreateIndex(
                name: "IX_REDEEM_COUPON_ID",
                table: "REDEEM",
                column: "COUPON_ID");

            migrationBuilder.CreateIndex(
                name: "IX_REDEEM_TUTOR_ID",
                table: "REDEEM",
                column: "TUTOR_ID");

            migrationBuilder.CreateIndex(
                name: "IX_REWARD_ACTION_NAME",
                table: "REWARD_ACTION",
                column: "NAME",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_REWARD_POINT_REWARD_ACTION_ID",
                table: "REWARD_POINT",
                column: "REWARD_ACTION_ID");

            migrationBuilder.CreateIndex(
                name: "IX_REWARD_POINT_TUTOR_ID",
                table: "REWARD_POINT",
                column: "TUTOR_ID");

            migrationBuilder.CreateIndex(
                name: "IX_RISK_LEVEL_NAME",
                table: "RISK_LEVEL",
                column: "NAME",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RISK_SCORE_PET_ID",
                table: "RISK_SCORE",
                column: "PET_ID");

            migrationBuilder.CreateIndex(
                name: "IX_RISK_SCORE_RISK_LEVEL_ID",
                table: "RISK_SCORE",
                column: "RISK_LEVEL_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SPECIES_NAME",
                table: "SPECIES",
                column: "NAME",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SUBSCRIPTION_PET_ID",
                table: "SUBSCRIPTION",
                column: "PET_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SUBSCRIPTION_PLAN_ID",
                table: "SUBSCRIPTION",
                column: "PLAN_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TUTOR_EMAIL",
                table: "TUTOR",
                column: "EMAIL",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADDRESS");

            migrationBuilder.DropTable(
                name: "HEALTH_EVENT");

            migrationBuilder.DropTable(
                name: "LOG_AUDITORIA_HEALTH_EVENT");

            migrationBuilder.DropTable(
                name: "LOG_ERROS");

            migrationBuilder.DropTable(
                name: "REDEEM");

            migrationBuilder.DropTable(
                name: "REWARD_POINT");

            migrationBuilder.DropTable(
                name: "RISK_SCORE");

            migrationBuilder.DropTable(
                name: "SUBSCRIPTION");

            migrationBuilder.DropTable(
                name: "EVENT_TYPE");

            migrationBuilder.DropTable(
                name: "COUPON");

            migrationBuilder.DropTable(
                name: "REWARD_ACTION");

            migrationBuilder.DropTable(
                name: "RISK_LEVEL");

            migrationBuilder.DropTable(
                name: "PET");

            migrationBuilder.DropTable(
                name: "PLAN");

            migrationBuilder.DropTable(
                name: "COUPON_TEMPLATE");

            migrationBuilder.DropTable(
                name: "SPECIES");

            migrationBuilder.DropTable(
                name: "TUTOR");

            migrationBuilder.DropTable(
                name: "PARTNER_DISCOUNT");

            migrationBuilder.DropTable(
                name: "CLINIC");
        }
    }
}
