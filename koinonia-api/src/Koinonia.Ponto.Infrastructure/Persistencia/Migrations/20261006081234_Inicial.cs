using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinonia.Ponto.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Batidas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FuncionarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Entrada = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Saida = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Batidas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiasDePonto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FuncionarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dia = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Aprovado = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiasDePonto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trechos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DiaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Inicio = table.Column<int>(type: "INTEGER", nullable: false),
                    Fim = table.Column<int>(type: "INTEGER", nullable: false),
                    Fatia = table.Column<int>(type: "INTEGER", nullable: false),
                    EventoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    NomeEvento = table.Column<string>(type: "TEXT", nullable: true),
                    Forma = table.Column<int>(type: "INTEGER", nullable: true),
                    Percentual = table.Column<decimal>(type: "TEXT", nullable: true),
                    Fator = table.Column<decimal>(type: "TEXT", nullable: true),
                    MinutosTrabalhados = table.Column<int>(type: "INTEGER", nullable: false),
                    MinutosCreditados = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trechos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trechos_DiasDePonto_DiaId",
                        column: x => x.DiaId,
                        principalTable: "DiasDePonto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiasDePonto_FuncionarioId_Dia",
                table: "DiasDePonto",
                columns: new[] { "FuncionarioId", "Dia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trechos_DiaId",
                table: "Trechos",
                column: "DiaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Batidas");

            migrationBuilder.DropTable(
                name: "Trechos");

            migrationBuilder.DropTable(
                name: "DiasDePonto");
        }
    }
}
