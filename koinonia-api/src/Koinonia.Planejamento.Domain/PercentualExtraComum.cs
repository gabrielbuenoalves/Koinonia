using Koinonia.Shared;

namespace Koinonia.Planejamento.Domain;

public sealed record PercentualExtraComum(decimal Valor)
{
    public static PercentualExtraComum Definir(decimal valor)
    {
        if (valor < 0)
            throw new ViolacaoDeRegra("Percentual menor que zero é recusado.");
        return new PercentualExtraComum(valor);
    }
}
