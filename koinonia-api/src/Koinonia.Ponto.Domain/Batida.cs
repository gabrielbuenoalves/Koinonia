using Koinonia.Shared;

namespace Koinonia.Ponto.Domain;

public sealed class Batida
{
    private Batida() { }

    public Guid Id { get; private set; }
    public FuncionarioId FuncionarioId { get; private set; }
    public DateTimeOffset Entrada { get; private set; }
    public DateTimeOffset? Saida { get; private set; }
    public bool Aberta => Saida is null;

    public static Batida Abrir(FuncionarioId funcionarioId, DateTimeOffset entrada, DateTimeOffset agora)
    {
        if (entrada > agora)
            throw new ViolacaoDeRegra("Entrada ou saída no futuro é recusada.");
        return new Batida { Id = Guid.NewGuid(), FuncionarioId = funcionarioId, Entrada = entrada };
    }

    public static Batida Reidratar(Guid id, FuncionarioId funcionarioId, DateTimeOffset entrada, DateTimeOffset? saida)
        => new() { Id = id, FuncionarioId = funcionarioId, Entrada = entrada, Saida = saida };

    public void Fechar(DateTimeOffset saida, DateTimeOffset agora)
    {
        if (saida > agora)
            throw new ViolacaoDeRegra("Entrada ou saída no futuro é recusada.");
        if (saida <= Entrada)
            throw new ViolacaoDeRegra("Saída anterior à entrada.");
        Saida = saida;
    }
}
