using Koinonia.Api.Dados;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Koinonia.Api.Tests;

public class EsquemaTestes : IDisposable
{
    private readonly string _arquivo = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
    private readonly KoinoniaDbContext _contexto;

    public EsquemaTestes()
    {
        var options = new DbContextOptionsBuilder<KoinoniaDbContext>().UseSqlite($"Data Source={_arquivo}").Options;
        _contexto = new KoinoniaDbContext(options);
        _contexto.Database.Migrate();
    }

    [Fact]
    public void Migra_as_tabelas_do_spec()
    {
        var tabelas = _contexto.Database
            .SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite%' ORDER BY name")
            .ToList();

        Assert.Equal(
            new[]
            {
                "Batida", "Configuracao", "Evento", "Expediente", "Fechamento", "Funcionario",
                "IntervaloExpediente", "LinhaEventoFechamento", "MovimentoBanco", "ParticipanteEvento",
                "PermissaoDaRole", "Role", "Sessao", "Usuario"
            },
            tabelas.Where(nome => nome is not "__EFMigrationsHistory" and not "__EFMigrationsLock").ToArray());
    }

    [Fact]
    public void Email_de_usuario_e_unico_e_funcionario_aponta_para_um_usuario()
    {
        var indices = _contexto.Database
            .SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type = 'index' AND sql LIKE '%UNIQUE%'")
            .ToList();
        Assert.Contains(indices, nome => nome.Contains("Email", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(indices, nome => nome.Contains("UsuarioId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Configuracao_nasce_uma_linha_vazia()
    {
        var linha = Assert.Single(_contexto.Configuracoes);
        Assert.Null(linha.Percentual);
        Assert.Null(linha.PrazoDias);
    }

    [Fact]
    public void Chave_estrangeira_recusa_funcionario_sem_usuario()
    {
        _contexto.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
        var erro = Record.Exception(() => _contexto.Database.ExecuteSqlRaw(
            "INSERT INTO Funcionario (Id, UsuarioId, MinutosContratados, Salario) VALUES ({0}, {1}, 9600, '3000')",
            Guid.NewGuid(),
            Guid.NewGuid()));
        Assert.NotNull(erro);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_arquivo))
            File.Delete(_arquivo);
    }
}
