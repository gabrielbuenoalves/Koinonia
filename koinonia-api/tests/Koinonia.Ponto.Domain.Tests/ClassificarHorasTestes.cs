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
}
