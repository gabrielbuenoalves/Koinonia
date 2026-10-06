using Koinonia.Api.Dados;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Koinonia.Api.Tests;

public class SemeadorDeRolesTestes : IDisposable
{
    private readonly string _arquivo = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
    private readonly KoinoniaDbContext _contexto;

    public SemeadorDeRolesTestes()
    {
        var options = new DbContextOptionsBuilder<KoinoniaDbContext>().UseSqlite($"Data Source={_arquivo}").Options;
        _contexto = new KoinoniaDbContext(options);
        _contexto.Database.Migrate();
    }

    [Fact]
    public void Semear_cria_funcionario_e_gestao()
    {
        SemeadorDeRoles.Semear(_contexto);

        var funcionario = _contexto.Roles.Include(item => item.Permissoes).Single(item => item.Nome == "Funcionário");
        var gestao = _contexto.Roles.Include(item => item.Permissoes).Single(item => item.Nome == "Gestão");

        Assert.Equal(["ponto", "registrar-propria-batida", "visao-geral"], funcionario.Permissoes.Select(item => item.Codigo).Order().ToArray());
        Assert.Equal(CodigosDePermissao.Todas.Order().ToArray(), gestao.Permissoes.Select(item => item.Codigo).Order().ToArray());
    }

    [Fact]
    public void Semear_de_novo_nao_duplica()
    {
        SemeadorDeRoles.Semear(_contexto);
        SemeadorDeRoles.Semear(_contexto);
        Assert.Equal(2, _contexto.Roles.Count());
    }

    public void Dispose()
    {
        _contexto.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_arquivo))
            File.Delete(_arquivo);
    }
}
