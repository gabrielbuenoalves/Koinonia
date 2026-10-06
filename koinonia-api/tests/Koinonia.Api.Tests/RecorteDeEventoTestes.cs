using Koinonia.Api;
using Koinonia.Ponto.Domain;

namespace Koinonia.Api.Tests;

public class RecorteDeEventoTestes
{
    [Fact]
    public void Evento_que_cruza_a_meia_noite_vira_um_pedaco_em_cada_dia()
    {
        var inicio = new DateTimeOffset(2026, 10, 6, 22, 0, 0, TimeSpan.FromHours(-3));
        var fim = new DateTimeOffset(2026, 10, 7, 2, 0, 0, TimeSpan.FromHours(-3));

        var noite = ConsultaDePlanejamentoAdapter.Recortar(inicio, fim, new DateOnly(2026, 10, 6));
        var madrugada = ConsultaDePlanejamentoAdapter.Recortar(inicio, fim, new DateOnly(2026, 10, 7));

        Assert.Equal(new IntervaloMinutos(22 * 60, 1440), noite);
        Assert.Equal(new IntervaloMinutos(0, 2 * 60), madrugada);
    }

    [Fact]
    public void Dia_sem_intersecao_devolve_nulo()
    {
        var inicio = new DateTimeOffset(2026, 10, 6, 14, 0, 0, TimeSpan.FromHours(-3));
        var fim = new DateTimeOffset(2026, 10, 6, 18, 0, 0, TimeSpan.FromHours(-3));
        Assert.Null(ConsultaDePlanejamentoAdapter.Recortar(inicio, fim, new DateOnly(2026, 10, 7)));
    }
}
