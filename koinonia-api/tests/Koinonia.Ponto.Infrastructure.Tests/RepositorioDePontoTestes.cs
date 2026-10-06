using Koinonia.Ponto.Domain;
using Koinonia.Ponto.Infrastructure;
using Koinonia.Shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Koinonia.Ponto.Infrastructure.Tests;

public class RepositorioDePontoTestes : IDisposable
{
    private readonly string _arquivo = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
    private readonly PontoDbContext _contexto;
    private readonly RepositorioDePonto _repositorio;
    private readonly FuncionarioId _pessoa = FuncionarioId.Novo();

    public RepositorioDePontoTestes()
    {
        var options = new DbContextOptionsBuilder<PontoDbContext>().UseSqlite($"Data Source={_arquivo}").Options;
        _contexto = new PontoDbContext(options);
        _contexto.Database.Migrate();
        _repositorio = new RepositorioDePonto(_contexto);
    }

    [Fact]
    public void Dia_aprovado_volta_com_os_numeros_copiados()
    {
        var agora = new DateTimeOffset(2026, 10, 6, 20, 0, 0, TimeSpan.FromHours(-3));
        var batida = Batida.Abrir(_pessoa, agora.AddHours(-2), agora);
        batida.Fechar(agora.AddHours(-1), agora);
        _repositorio.Salvar(batida);

        var dia = DiaDePonto.Abrir(_pessoa, new DateOnly(2026, 10, 6));
        dia.Aprovar(
        [
            new TrechoClassificado(new IntervaloMinutos(18 * 60, 19 * 60), Fatia.ExtraComum, null, null, null, 50m)
        ]);
        _repositorio.Salvar(dia);

        using var outro = new PontoDbContext(new DbContextOptionsBuilder<PontoDbContext>().UseSqlite($"Data Source={_arquivo}").Options);
        var lido = new RepositorioDePonto(outro).ObterOuAbrir(_pessoa, new DateOnly(2026, 10, 6));

        Assert.True(lido.Aprovado);
        Assert.Equal(50m, Assert.Single(lido.Trechos).Percentual);
        Assert.Single(new RepositorioDePonto(outro).ListarAprovados(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)));
    }

    [Fact]
    public void Segunda_aprovacao_do_mesmo_dia_e_recusada()
    {
        var dia = new DateOnly(2026, 10, 6);
        _repositorio.Salvar(DiaAprovado(dia));

        var erro = Assert.Throws<ViolacaoDeRegra>(() => _repositorio.Salvar(DiaAprovado(dia)));
        Assert.Equal("Aprovar um dia já aprovado é recusado.", erro.Message);

        var guardado = _repositorio.ObterOuAbrir(_pessoa, dia);
        Assert.True(guardado.Aprovado);
        Assert.Equal(50m, Assert.Single(guardado.Trechos).Percentual);

        var reaberto = DiaDePonto.Reidratar(_pessoa, dia, true, guardado.Trechos);
        reaberto.Reabrir();
        _repositorio.Salvar(reaberto);
        Assert.False(_repositorio.ObterOuAbrir(_pessoa, dia).Aprovado);
    }

    [Fact]
    public void Nao_ha_chave_estrangeira_para_planejamento()
    {
        var modelo = _contexto.Model.GetEntityTypes().SelectMany(item => item.GetForeignKeys());
        Assert.DoesNotContain(modelo, chave => chave.PrincipalEntityType.Name.Contains("Planejamento") || chave.PrincipalEntityType.Name.Contains("Evento"));
    }

    private DiaDePonto DiaAprovado(DateOnly dia)
    {
        var aberto = DiaDePonto.Abrir(_pessoa, dia);
        aberto.Aprovar(
        [
            new TrechoClassificado(new IntervaloMinutos(18 * 60, 19 * 60), Fatia.ExtraComum, null, null, null, 50m)
        ]);
        return aberto;
    }

    public void Dispose()
    {
        _contexto.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_arquivo))
            File.Delete(_arquivo);
    }
}
