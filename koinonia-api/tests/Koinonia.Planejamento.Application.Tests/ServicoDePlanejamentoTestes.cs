using Koinonia.Planejamento.Application;
using Koinonia.Planejamento.Domain;
using Koinonia.Planejamento.Infrastructure;
using Koinonia.Shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Koinonia.Planejamento.Application.Tests;

public class ServicoDePlanejamentoTestes : IDisposable
{
    private readonly string _arquivo = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
    private readonly PlanejamentoDbContext _contexto;
    private readonly ServicoDePlanejamento _servico;

    public ServicoDePlanejamentoTestes()
    {
        var options = new DbContextOptionsBuilder<PlanejamentoDbContext>()
            .UseSqlite($"Data Source={_arquivo}")
            .Options;
        _contexto = new PlanejamentoDbContext(options);
        _contexto.Database.Migrate();
        _servico = new ServicoDePlanejamento(new RepositorioDePlanejamento(_contexto));
    }

    [Fact]
    public void Remover_escala_faz_a_leitura_voltar_sem_intervalos()
    {
        var pessoa = FuncionarioId.Novo();
        var dia = new DateOnly(2026, 10, 6);
        _servico.PublicarEscala(pessoa, dia, [new IntervaloDeEscala(8 * 60, 17 * 60)]);
        _servico.RemoverEscala(pessoa, dia);

        var leitura = _servico.Ler(pessoa, dia);

        Assert.Empty(leitura.Escala);
        Assert.Equal(0m, leitura.PercentualExtraComum);
    }

    [Fact]
    public void Publicar_de_novo_substitui_os_intervalos()
    {
        var pessoa = FuncionarioId.Novo();
        var dia = new DateOnly(2026, 10, 6);
        _servico.PublicarEscala(pessoa, dia, [new IntervaloDeEscala(8 * 60, 12 * 60)]);
        _servico.PublicarEscala(pessoa, dia, [new IntervaloDeEscala(13 * 60, 17 * 60)]);

        var leitura = _servico.Ler(pessoa, dia);

        var intervalo = Assert.Single(leitura.Escala);
        Assert.Equal(13 * 60, intervalo.Inicio);
    }

    [Fact]
    public void Evento_publicado_aparece_na_leitura_de_quem_esta_escalado()
    {
        var pessoa = FuncionarioId.Novo();
        var outro = FuncionarioId.Novo();
        var inicio = new DateTimeOffset(2026, 10, 6, 14, 0, 0, TimeSpan.FromHours(-3));
        var fim = new DateTimeOffset(2026, 10, 6, 18, 0, 0, TimeSpan.FromHours(-3));
        _servico.PublicarEvento("Formatura", inicio, fim, [pessoa], TratamentoDeEvento.Banco(1m));

        var daPessoa = _servico.Ler(pessoa, new DateOnly(2026, 10, 6));
        var doOutro = _servico.Ler(outro, new DateOnly(2026, 10, 6));

        Assert.Equal("Formatura", Assert.Single(daPessoa.Eventos).Nome);
        Assert.Empty(doOutro.Eventos);
    }

    [Fact]
    public void Definir_percentual_muda_a_leitura()
    {
        _servico.DefinirPercentual(50m);
        var leitura = _servico.Ler(FuncionarioId.Novo(), new DateOnly(2026, 10, 6));
        Assert.Equal(50m, leitura.PercentualExtraComum);
    }

    [Fact]
    public void Alterar_evento_mantendo_o_mesmo_escalado_persiste_o_novo_nome()
    {
        var pessoa = FuncionarioId.Novo();
        var inicio = new DateTimeOffset(2026, 10, 6, 14, 0, 0, TimeSpan.FromHours(-3));
        var fim = new DateTimeOffset(2026, 10, 6, 18, 0, 0, TimeSpan.FromHours(-3));
        var tratamento = TratamentoDeEvento.Banco(1m);
        var id = _servico.PublicarEvento("Formatura", inicio, fim, [pessoa], tratamento);

        _servico.AlterarEvento(id, "Colação", inicio, fim, [pessoa], tratamento);

        var leitura = _servico.Ler(pessoa, new DateOnly(2026, 10, 6));
        var evento = Assert.Single(leitura.Eventos);
        Assert.Equal("Colação", evento.Nome);
        Assert.Equal(id, evento.Id);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_arquivo))
            File.Delete(_arquivo);
    }
}
