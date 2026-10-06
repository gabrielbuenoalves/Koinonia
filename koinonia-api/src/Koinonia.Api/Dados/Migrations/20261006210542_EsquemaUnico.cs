using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinonia.Api.Dados.Migrations
{
    /// <inheritdoc />
    public partial class EsquemaUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configuracao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Percentual = table.Column<string>(type: "TEXT", nullable: true),
                    PrazoDias = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Evento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Inicio = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Fim = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Regra = table.Column<string>(type: "TEXT", nullable: false),
                    Percentual = table.Column<string>(type: "TEXT", nullable: true),
                    PrazoVencimentoDias = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Expediente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InicioVigencia = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    FimVigencia = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expediente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntervaloExpediente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExpedienteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Inicio = table.Column<int>(type: "INTEGER", nullable: false),
                    Fim = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntervaloExpediente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntervaloExpediente_Expediente_ExpedienteId",
                        column: x => x.ExpedienteId,
                        principalTable: "Expediente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PermissaoDaRole",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissaoDaRole", x => new { x.RoleId, x.Codigo });
                    table.ForeignKey(
                        name: "FK_PermissaoDaRole_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    SenhaHash = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuario_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Funcionario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MinutosContratados = table.Column<int>(type: "INTEGER", nullable: false),
                    Salario = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Funcionario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Funcionario_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sessao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessao_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Batida",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FuncionarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Entrada = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Saida = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Batida", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Batida_Funcionario_FuncionarioId",
                        column: x => x.FuncionarioId,
                        principalTable: "Funcionario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Fechamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FuncionarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ano = table.Column<int>(type: "INTEGER", nullable: false),
                    Mes = table.Column<int>(type: "INTEGER", nullable: false),
                    EscolhaFalta = table.Column<string>(type: "TEXT", nullable: false),
                    Salario = table.Column<string>(type: "TEXT", nullable: false),
                    MinutosContratados = table.Column<int>(type: "INTEGER", nullable: false),
                    ValorHora = table.Column<string>(type: "TEXT", nullable: false),
                    MinutosNormais = table.Column<int>(type: "INTEGER", nullable: false),
                    MinutosFalta = table.Column<int>(type: "INTEGER", nullable: false),
                    MinutosCobertos = table.Column<int>(type: "INTEGER", nullable: false),
                    MinutosDescontados = table.Column<int>(type: "INTEGER", nullable: false),
                    Desconto = table.Column<string>(type: "TEXT", nullable: false),
                    MinutosExtra = table.Column<int>(type: "INTEGER", nullable: false),
                    PercentualExtra = table.Column<string>(type: "TEXT", nullable: false),
                    ValorExtra = table.Column<string>(type: "TEXT", nullable: false),
                    ValorEventos = table.Column<string>(type: "TEXT", nullable: false),
                    ValorBancoPago = table.Column<string>(type: "TEXT", nullable: false),
                    Total = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fechamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fechamento_Funcionario_FuncionarioId",
                        column: x => x.FuncionarioId,
                        principalTable: "Funcionario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParticipanteEvento",
                columns: table => new
                {
                    EventoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FuncionarioId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipanteEvento", x => new { x.EventoId, x.FuncionarioId });
                    table.ForeignKey(
                        name: "FK_ParticipanteEvento_Evento_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Evento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParticipanteEvento_Funcionario_FuncionarioId",
                        column: x => x.FuncionarioId,
                        principalTable: "Funcionario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LinhaEventoFechamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FechamentoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Regra = table.Column<string>(type: "TEXT", nullable: false),
                    Percentual = table.Column<string>(type: "TEXT", nullable: true),
                    Minutos = table.Column<int>(type: "INTEGER", nullable: false),
                    Valor = table.Column<string>(type: "TEXT", nullable: true),
                    MinutosCreditados = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinhaEventoFechamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinhaEventoFechamento_Fechamento_FechamentoId",
                        column: x => x.FechamentoId,
                        principalTable: "Fechamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovimentoBanco",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FuncionarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Minutos = table.Column<int>(type: "INTEGER", nullable: false),
                    ValorHora = table.Column<string>(type: "TEXT", nullable: true),
                    VenceEm = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Dia = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EventoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreditoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FechamentoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Valor = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentoBanco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentoBanco_Evento_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Evento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentoBanco_Fechamento_FechamentoId",
                        column: x => x.FechamentoId,
                        principalTable: "Fechamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentoBanco_Funcionario_FuncionarioId",
                        column: x => x.FuncionarioId,
                        principalTable: "Funcionario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentoBanco_MovimentoBanco_CreditoId",
                        column: x => x.CreditoId,
                        principalTable: "MovimentoBanco",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Configuracao",
                columns: new[] { "Id", "Percentual", "PrazoDias" },
                values: new object[] { 1, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Batida_FuncionarioId",
                table: "Batida",
                column: "FuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Fechamento_FuncionarioId_Ano_Mes",
                table: "Fechamento",
                columns: new[] { "FuncionarioId", "Ano", "Mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Funcionario_UsuarioId",
                table: "Funcionario",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntervaloExpediente_ExpedienteId",
                table: "IntervaloExpediente",
                column: "ExpedienteId");

            migrationBuilder.CreateIndex(
                name: "IX_LinhaEventoFechamento_FechamentoId",
                table: "LinhaEventoFechamento",
                column: "FechamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentoBanco_CreditoId",
                table: "MovimentoBanco",
                column: "CreditoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentoBanco_EventoId",
                table: "MovimentoBanco",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentoBanco_FechamentoId",
                table: "MovimentoBanco",
                column: "FechamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentoBanco_FuncionarioId",
                table: "MovimentoBanco",
                column: "FuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipanteEvento_FuncionarioId",
                table: "ParticipanteEvento",
                column: "FuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Nome",
                table: "Role",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessao_TokenHash",
                table: "Sessao",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessao_UsuarioId",
                table: "Sessao",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_Email",
                table: "Usuario",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_RoleId",
                table: "Usuario",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Batida");

            migrationBuilder.DropTable(
                name: "Configuracao");

            migrationBuilder.DropTable(
                name: "IntervaloExpediente");

            migrationBuilder.DropTable(
                name: "LinhaEventoFechamento");

            migrationBuilder.DropTable(
                name: "MovimentoBanco");

            migrationBuilder.DropTable(
                name: "ParticipanteEvento");

            migrationBuilder.DropTable(
                name: "PermissaoDaRole");

            migrationBuilder.DropTable(
                name: "Sessao");

            migrationBuilder.DropTable(
                name: "Expediente");

            migrationBuilder.DropTable(
                name: "Fechamento");

            migrationBuilder.DropTable(
                name: "Evento");

            migrationBuilder.DropTable(
                name: "Funcionario");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "Role");
        }
    }
}
