using Koinonia.Ponto.Domain;

namespace Koinonia.Ponto.Domain.Tests;

public class ClassificarHorasTestes
{
    [Fact]
    public void Exemplo_combinado_recorta_evento_e_deixa_extra_depois_dele()
    {
        var eventoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var trechos = ClassificarHoras.Executar(
            batidas: [new IntervaloMinutos(8 * 60, 19 * 60)],
            escala: [new IntervaloMinutos(8 * 60, 17 * 60)],
            eventos:
            [
                new EventoNoDia(eventoId, "Formatura", new IntervaloMinutos(14 * 60, 18 * 60), TratamentoCopiado.Banco(1m))
            ],
            percentualExtraComum: 50m);

        Assert.Equal(3, trechos.Count);

        Assert.Equal(Fatia.Regular, trechos[0].Fatia);
        Assert.Equal(8 * 60, trechos[0].Intervalo.Inicio);
        Assert.Equal(14 * 60, trechos[0].Intervalo.Fim);
        Assert.Null(trechos[0].MinutosCreditados);

        Assert.Equal(Fatia.Evento, trechos[1].Fatia);
        Assert.Equal(14 * 60, trechos[1].Intervalo.Inicio);
        Assert.Equal(18 * 60, trechos[1].Intervalo.Fim);
        Assert.Equal(eventoId, trechos[1].EventoId);
        Assert.Equal("Formatura", trechos[1].NomeEvento);
        Assert.Equal(240, trechos[1].MinutosTrabalhados);
        Assert.Equal(240, trechos[1].MinutosCreditados);
        Assert.Equal(1m, trechos[1].Tratamento!.Fator);

        Assert.Equal(Fatia.ExtraComum, trechos[2].Fatia);
        Assert.Equal(18 * 60, trechos[2].Intervalo.Inicio);
        Assert.Equal(19 * 60, trechos[2].Intervalo.Fim);
        Assert.Equal(50m, trechos[2].PercentualExtraComum);
        Assert.Null(trechos[2].MinutosCreditados);
    }

    [Fact]
    public void Sem_escala_o_tempo_fora_de_evento_e_extra_comum()
    {
        var trechos = ClassificarHoras.Executar(
            [new IntervaloMinutos(8 * 60, 10 * 60)],
            [],
            [],
            50m);

        var trecho = Assert.Single(trechos);
        Assert.Equal(Fatia.ExtraComum, trecho.Fatia);
        Assert.Equal(8 * 60, trecho.Intervalo.Inicio);
        Assert.Equal(10 * 60, trecho.Intervalo.Fim);
        Assert.Equal(50m, trecho.PercentualExtraComum);
    }

    [Fact]
    public void Vao_entre_batidas_nao_entra_na_conta()
    {
        var trechos = ClassificarHoras.Executar(
            [new IntervaloMinutos(8 * 60, 12 * 60), new IntervaloMinutos(13 * 60, 17 * 60)],
            [new IntervaloMinutos(8 * 60, 17 * 60)],
            [],
            50m);

        Assert.Equal(2, trechos.Count);
        Assert.Equal(12 * 60, trechos[0].Intervalo.Fim);
        Assert.Equal(13 * 60, trechos[1].Intervalo.Inicio);
        Assert.All(trechos, trecho => Assert.Equal(Fatia.Regular, trecho.Fatia));
    }

    [Fact]
    public void Minutos_contiguos_da_mesma_fatia_formam_um_trecho()
    {
        var trechos = ClassificarHoras.Executar(
            [new IntervaloMinutos(8 * 60, 12 * 60), new IntervaloMinutos(12 * 60, 17 * 60)],
            [new IntervaloMinutos(8 * 60, 17 * 60)],
            [],
            50m);

        var trecho = Assert.Single(trechos);
        Assert.Equal(8 * 60, trecho.Intervalo.Inicio);
        Assert.Equal(17 * 60, trecho.Intervalo.Fim);
    }

    [Fact]
    public void Fator_um_e_meio_arredonda_um_minuto_para_dois()
    {
        var trechos = ClassificarHoras.Executar(
            [new IntervaloMinutos(8 * 60, 8 * 60 + 1)],
            [],
            [new EventoNoDia(Guid.NewGuid(), "Plantão", new IntervaloMinutos(8 * 60, 9 * 60), TratamentoCopiado.Banco(1.5m))],
            0m);

        var trecho = Assert.Single(trechos);
        Assert.Equal(1, trecho.MinutosTrabalhados);
        Assert.Equal(2, trecho.MinutosCreditados);
    }

    [Fact]
    public void Hora_normal_de_evento_nao_credita_banco()
    {
        var trechos = ClassificarHoras.Executar(
            [new IntervaloMinutos(8 * 60, 9 * 60)],
            [new IntervaloMinutos(8 * 60, 17 * 60)],
            [new EventoNoDia(Guid.NewGuid(), "Conselho", new IntervaloMinutos(8 * 60, 9 * 60), TratamentoCopiado.HoraNormal())],
            50m);

        var trecho = Assert.Single(trechos);
        Assert.Equal(Fatia.Evento, trecho.Fatia);
        Assert.Equal(FormaDePagamento.HoraNormal, trecho.Tratamento!.Forma);
        Assert.Null(trecho.MinutosCreditados);
        Assert.Null(trecho.PercentualExtraComum);
    }

    [Fact]
    public void Cortar_divide_a_batida_na_meia_noite_de_Sao_Paulo()
    {
        var entrada = new DateTimeOffset(2026, 10, 6, 22, 0, 0, TimeSpan.FromHours(-3));
        var saida = new DateTimeOffset(2026, 10, 7, 2, 0, 0, TimeSpan.FromHours(-3));

        var pedacos = ClassificarHoras.Cortar(entrada, saida);

        Assert.Equal(2, pedacos.Count);
        Assert.Equal(new DateOnly(2026, 10, 6), pedacos[0].Dia);
        Assert.Equal(22 * 60, pedacos[0].Intervalo.Inicio);
        Assert.Equal(1440, pedacos[0].Intervalo.Fim);
        Assert.Equal(new DateOnly(2026, 10, 7), pedacos[1].Dia);
        Assert.Equal(0, pedacos[1].Intervalo.Inicio);
        Assert.Equal(2 * 60, pedacos[1].Intervalo.Fim);

        var noite = ClassificarHoras.Executar([pedacos[0].Intervalo], [], [], 50m);
        var madrugada = ClassificarHoras.Executar(
            [pedacos[1].Intervalo],
            [new IntervaloMinutos(0, 8 * 60)],
            [],
            50m);

        Assert.Equal(Fatia.ExtraComum, Assert.Single(noite).Fatia);
        Assert.Equal(Fatia.Regular, Assert.Single(madrugada).Fatia);
    }
}
