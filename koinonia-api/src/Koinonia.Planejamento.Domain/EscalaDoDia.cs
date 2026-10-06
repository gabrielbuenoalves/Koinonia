using Koinonia.Shared;

namespace Koinonia.Planejamento.Domain;

public readonly record struct IntervaloDeEscala
{
    public int Inicio { get; }
    public int Fim { get; }

    public IntervaloDeEscala(int Inicio, int Fim)
    {
        if (Fim <= Inicio || Inicio < 0 || Fim > 1440)
            throw new ViolacaoDeRegra("Intervalo inválido.");
        this.Inicio = Inicio;
        this.Fim = Fim;
    }

    public bool Intersecta(IntervaloDeEscala outro) => Inicio < outro.Fim && outro.Inicio < Fim;
}

public sealed class EscalaDoDia
{
    private EscalaDoDia(FuncionarioId funcionarioId, DateOnly dia, IReadOnlyList<IntervaloDeEscala> intervalos)
    {
        FuncionarioId = funcionarioId;
        Dia = dia;
        Intervalos = intervalos;
    }

    public FuncionarioId FuncionarioId { get; }
    public DateOnly Dia { get; }
    public IReadOnlyList<IntervaloDeEscala> Intervalos { get; }

    public static EscalaDoDia Publicar(FuncionarioId funcionarioId, DateOnly dia, IReadOnlyList<IntervaloDeEscala> intervalos)
    {
        if (intervalos.Count == 0)
            throw new ViolacaoDeRegra("Intervalo inválido.");

        for (var i = 0; i < intervalos.Count; i++)
        {
            for (var j = i + 1; j < intervalos.Count; j++)
            {
                if (intervalos[i].Intersecta(intervalos[j]))
                    throw new ViolacaoDeRegra("Intervalos de escala com interseção de duração maior que zero, no mesmo dia, são recusados.");
            }
        }

        return new EscalaDoDia(funcionarioId, dia, intervalos.OrderBy(item => item.Inicio).ToList());
    }
}
