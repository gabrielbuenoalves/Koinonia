using Koinonia.Shared;

namespace Koinonia.Ponto.Domain;

public sealed record TrechoGravado(
    int Inicio,
    int Fim,
    Fatia Fatia,
    Guid? EventoId,
    string? NomeEvento,
    FormaDePagamento? Forma,
    decimal? Percentual,
    decimal? Fator,
    int MinutosTrabalhados,
    int? MinutosCreditados)
{
    public static TrechoGravado De(TrechoClassificado trecho)
    {
        var percentual = trecho.Fatia == Fatia.ExtraComum
            ? trecho.PercentualExtraComum
            : trecho.Tratamento?.Percentual;
        return new TrechoGravado(
            trecho.Intervalo.Inicio,
            trecho.Intervalo.Fim,
            trecho.Fatia,
            trecho.EventoId,
            trecho.NomeEvento,
            trecho.Tratamento?.Forma,
            percentual,
            trecho.Tratamento?.Fator,
            trecho.MinutosTrabalhados,
            trecho.MinutosCreditados);
    }
}

public sealed class DiaDePonto
{
    private readonly List<TrechoGravado> _trechos = [];

    private DiaDePonto(FuncionarioId funcionarioId, DateOnly dia)
    {
        FuncionarioId = funcionarioId;
        Dia = dia;
    }

    public FuncionarioId FuncionarioId { get; }
    public DateOnly Dia { get; }
    public bool Aprovado { get; private set; }
    public IReadOnlyList<TrechoGravado> Trechos => _trechos;

    public static DiaDePonto Abrir(FuncionarioId funcionarioId, DateOnly dia) => new(funcionarioId, dia);

    public static DiaDePonto Reidratar(FuncionarioId funcionarioId, DateOnly dia, bool aprovado, IEnumerable<TrechoGravado> trechos)
    {
        var diaDePonto = new DiaDePonto(funcionarioId, dia);
        if (!aprovado)
            return diaDePonto;
        diaDePonto._trechos.AddRange(trechos);
        diaDePonto.Aprovado = true;
        return diaDePonto;
    }

    public void Aprovar(IReadOnlyList<TrechoClassificado> trechos)
    {
        if (Aprovado)
            throw new ViolacaoDeRegra("Aprovar um dia já aprovado é recusado.");
        _trechos.Clear();
        _trechos.AddRange(trechos.Select(TrechoGravado.De));
        Aprovado = true;
    }

    public void Reabrir()
    {
        if (!Aprovado)
            throw new ViolacaoDeRegra("Reabrir um dia aberto é recusado.");
        _trechos.Clear();
        Aprovado = false;
    }
}
