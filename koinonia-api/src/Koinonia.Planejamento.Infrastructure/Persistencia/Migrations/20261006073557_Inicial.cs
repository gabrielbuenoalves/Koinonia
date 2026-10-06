using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinonia.Planejamento.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Escalas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FuncionarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dia = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Escalas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Eventos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Inicio = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Fim = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Forma = table.Column<int>(type: "INTEGER", nullable: false),
                    Percentual = table.Column<decimal>(type: "TEXT", nullable: true),
                    Fator = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eventos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PercentualExtraComum",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Valor = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PercentualExtraComum", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntervalosEscala",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EscalaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Inicio = table.Column<int>(type: "INTEGER", nullable: false),
                    Fim = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntervalosEscala", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntervalosEscala_Escalas_EscalaId",
                        column: x => x.EscalaId,
                        principalTable: "Escalas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventosEscalados",
                columns: table => new
                {
                    EventoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FuncionarioId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosEscalados", x => new { x.EventoId, x.FuncionarioId });
                    table.ForeignKey(
                        name: "FK_EventosEscalados_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Escalas_FuncionarioId_Dia",
                table: "Escalas",
                columns: new[] { "FuncionarioId", "Dia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntervalosEscala_EscalaId",
                table: "IntervalosEscala",
                column: "EscalaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosEscalados");

            migrationBuilder.DropTable(
                name: "IntervalosEscala");

            migrationBuilder.DropTable(
                name: "PercentualExtraComum");

            migrationBuilder.DropTable(
                name: "Eventos");

            migrationBuilder.DropTable(
                name: "Escalas");
        }
    }
}
