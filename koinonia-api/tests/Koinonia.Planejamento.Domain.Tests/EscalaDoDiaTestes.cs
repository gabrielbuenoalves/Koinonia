using Koinonia.Planejamento.Domain;
using Koinonia.Shared;

namespace Koinonia.Planejamento.Domain.Tests;

public class EscalaDoDiaTestes
{
    [Fact]
    public void Publicar_aceita_intervalos_que_apenas_se_encostam()
    {
        var escala = EscalaDoDia.Publicar(
            FuncionarioId.Novo(),
            new DateOnly(2026, 10, 6),
            [new IntervaloDeEscala(8 * 60, 12 * 60), new IntervaloDeEscala(12 * 60, 18 * 60)]);

        Assert.Equal(2, escala.Intervalos.Count);
    }

    [Fact]
    public void Publicar_recusa_intersecao()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => EscalaDoDia.Publicar(
            FuncionarioId.Novo(),
            new DateOnly(2026, 10, 6),
            [new IntervaloDeEscala(8 * 60, 13 * 60), new IntervaloDeEscala(12 * 60, 18 * 60)]));

        Assert.Equal("Intervalos de escala com interseção de duração maior que zero, no mesmo dia, são recusados.", erro.Message);
    }

    [Fact]
    public void Publicar_recusa_lista_vazia()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => EscalaDoDia.Publicar(
            FuncionarioId.Novo(),
            new DateOnly(2026, 10, 6),
            []));

        Assert.Equal("Intervalo inválido.", erro.Message);
    }
}
