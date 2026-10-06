using Koinonia.Ponto.Application;
using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Ponto.Application.Tests;

public class ServicoDePontoTestes
{
    private readonly DateTimeOffset _agora = new(2026, 10, 6, 20, 0, 0, TimeSpan.FromHours(-3));
    private readonly FuncionarioId _pessoa = FuncionarioId.Novo();
    private readonly DateOnly _dia = new(2026, 10, 6);
    private readonly ConsultaFalsa _consulta = new();
    private readonly RepositorioFalso _repositorio = new();
    private readonly ServicoDePonto _servico;

    public ServicoDePontoTestes()
    {
        _servico = new ServicoDePonto(_consulta, _repositorio, new RelogioFixo(_agora));
    }

    [Fact]
    public void Previa_nao_grava_trecho()
    {
        FecharJornada();
        var espelho = _servico.ObterEspelho(_pessoa, _dia);

        Assert.False(espelho.Aprovado);
        Assert.Equal(3, espelho.Trechos.Count);
        Assert.Equal(0, _repositorio.SalvamentosDeDia);
    }

    [Fact]
    public void Aprovacao_copia_os_numeros_e_ignora_mudanca_posterior_do_plano()
    {
        FecharJornada();
        _servico.Aprovar(_pessoa, _dia);
        _consulta.Percentual = 80m;

        var espelho = _servico.ObterEspelho(_pessoa, _dia);
        var extra = espelho.Trechos.Single(item => item.Fatia == Fatia.ExtraComum);

        Assert.True(espelho.Aprovado);
        Assert.Equal(50m, extra.Percentual);
        Assert.Equal(1, _repositorio.SalvamentosDeDia);
    }

    [Fact]
    public void Entrada_sem_saida_impede_a_aprovacao()
    {
        _repositorio.Batidas.Add(Batida.Abrir(_pessoa, _agora.AddHours(-1), _agora));
        var erro = Assert.Throws<ViolacaoDeRegra>(() => _servico.Aprovar(_pessoa, _dia));
        Assert.Equal("Aprovar com entrada sem saída é recusado.", erro.Message);
        Assert.Equal(0, _repositorio.SalvamentosDeDia);
    }

    [Fact]
    public void Sem_intervalo_fechado_a_aprovacao_e_recusada()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => _servico.Aprovar(_pessoa, _dia));
        Assert.Equal("Aprovar sem intervalo fechado é recusado.", erro.Message);
    }

    [Fact]
    public void Batida_em_dia_aprovado_e_recusada()
    {
        FecharJornada();
        _servico.Aprovar(_pessoa, _dia);
        var erro = Assert.Throws<ViolacaoDeRegra>(() => _servico.RegistrarEntrada(_pessoa, _agora.AddMinutes(-30)));
        Assert.Equal("Batida em dia aprovado é recusada até a gestão reabrir.", erro.Message);
    }

    [Fact]
    public void Intervalo_que_cruza_outro_fechado_e_recusado()
    {
        _servico.RegistrarEntrada(_pessoa, _agora.AddHours(-4));
        var id = _repositorio.Batidas.Single().Id;
        _servico.RegistrarSaida(id, _pessoa, _agora.AddHours(-1));

        _servico.RegistrarEntrada(_pessoa, _agora.AddHours(-3));
        var segunda = _repositorio.Batidas.Single(item => item.Aberta).Id;
        var erro = Assert.Throws<ViolacaoDeRegra>(() => _servico.RegistrarSaida(segunda, _pessoa, _agora.AddMinutes(-30)));
        Assert.Equal("Intervalo que cruza outro intervalo já fechado da mesma pessoa é recusado.", erro.Message);
    }

    private void FecharJornada()
    {
        var entrada = new DateTimeOffset(2026, 10, 6, 8, 0, 0, TimeSpan.FromHours(-3));
        var saida = new DateTimeOffset(2026, 10, 6, 19, 0, 0, TimeSpan.FromHours(-3));
        _servico.RegistrarEntrada(_pessoa, entrada);
        _servico.RegistrarSaida(_repositorio.Batidas.Single().Id, _pessoa, saida);
    }

    private sealed class RelogioFixo(DateTimeOffset agora) : IRelogio
    {
        public DateTimeOffset Agora() => agora;
    }

    private sealed class ConsultaFalsa : IConsultaDePlanejamento
    {
        public decimal Percentual { get; set; } = 50m;

        public ConsultaDoDia Obter(FuncionarioId funcionarioId, DateOnly dia) => new(
            [new IntervaloMinutos(8 * 60, 17 * 60)],
            [new EventoNoDia(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Formatura", new IntervaloMinutos(14 * 60, 18 * 60), TratamentoCopiado.Banco(1m))],
            Percentual);
    }

    private sealed class RepositorioFalso : IRepositorioDePonto
    {
        public List<Batida> Batidas { get; } = [];
        public DiaDePonto? Dia { get; private set; }
        public int SalvamentosDeDia { get; private set; }

        public IReadOnlyList<Batida> ListarBatidas(FuncionarioId funcionarioId)
            => Batidas.Where(item => item.FuncionarioId == funcionarioId).ToList();

        public void Salvar(Batida batida)
        {
            Batidas.RemoveAll(item => item.Id == batida.Id);
            Batidas.Add(batida);
        }

        public void Remover(Guid batidaId) => Batidas.RemoveAll(item => item.Id == batidaId);

        public DiaDePonto ObterOuAbrir(FuncionarioId funcionarioId, DateOnly dia)
            => Dia ?? DiaDePonto.Abrir(funcionarioId, dia);

        public void Salvar(DiaDePonto dia)
        {
            SalvamentosDeDia++;
            Dia = dia;
        }

        public IReadOnlyList<DiaDePonto> ListarAprovados(DateOnly inicio, DateOnly fim)
            => Dia is { Aprovado: true } && Dia.Dia >= inicio && Dia.Dia <= fim ? [Dia] : [];
    }
}
