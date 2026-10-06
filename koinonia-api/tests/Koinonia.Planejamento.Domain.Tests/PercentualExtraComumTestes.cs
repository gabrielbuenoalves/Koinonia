using Koinonia.Planejamento.Domain;
using Koinonia.Shared;

namespace Koinonia.Planejamento.Domain.Tests;

public class PercentualExtraComumTestes
{
    [Fact]
    public void Zero_e_valido()
    {
        Assert.Equal(0m, PercentualExtraComum.Definir(0m).Valor);
    }

    [Fact]
    public void Negativo_e_recusado()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => PercentualExtraComum.Definir(-1m));
        Assert.Equal("Percentual menor que zero é recusado.", erro.Message);
    }
}
