using Koinonia.Api.Cadastro;
using Koinonia.Shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Koinonia.Api.Tests;

public class CadastroDeFuncionariosTestes : IDisposable
{
    private readonly string _arquivo = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
    private readonly CadastroDbContext _contexto;
    private readonly CadastroDeFuncionarios _cadastro;

    public CadastroDeFuncionariosTestes()
    {
        var options = new DbContextOptionsBuilder<CadastroDbContext>().UseSqlite($"Data Source={_arquivo}").Options;
        _contexto = new CadastroDbContext(options);
        _contexto.Database.Migrate();
        _cadastro = new CadastroDeFuncionarios(_contexto);
    }

    [Fact]
    public void Cadastrar_gera_id_e_guarda_o_nome()
    {
        var criado = _cadastro.Cadastrar("Ana");
        Assert.NotEqual(Guid.Empty, criado.Id.Valor);
        Assert.Equal("Ana", _cadastro.ObterNome(criado.Id));
        Assert.True(_cadastro.Existe(criado.Id));
    }

    [Fact]
    public void Nome_em_branco_e_recusado()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => _cadastro.Cadastrar("  "));
        Assert.Equal("Nome inválido.", erro.Message);
    }

    [Fact]
    public void Desconhecido_nao_existe()
    {
        Assert.False(_cadastro.Existe(FuncionarioId.Novo()));
    }

    public void Dispose()
    {
        _contexto.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_arquivo))
            File.Delete(_arquivo);
    }
}
