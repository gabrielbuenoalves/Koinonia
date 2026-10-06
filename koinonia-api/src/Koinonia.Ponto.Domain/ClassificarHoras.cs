using Koinonia.Shared;

namespace Koinonia.Ponto.Domain;

public enum Fatia { Regular, ExtraComum, Evento }

public enum FormaDePagamento { HoraNormal, Adicional, BancoDeHoras }

public sealed record TratamentoCopiado(FormaDePagamento Forma, decimal? Percentual, decimal? Fator)
{
    public static TratamentoCopiado HoraNormal() => new(FormaDePagamento.HoraNormal, null, null);

    public static TratamentoCopiado Adicional(decimal percentual)
    {
        if (percentual < 0)
            throw new ViolacaoDeRegra("Percentual menor que zero é recusado.");
        return new TratamentoCopiado(FormaDePagamento.Adicional, percentual, null);
    }

    public static TratamentoCopiado Banco(decimal fator)
    {
        if (fator <= 0)
            throw new ViolacaoDeRegra("Fator menor ou igual a zero é recusado.");
        return new TratamentoCopiado(FormaDePagamento.BancoDeHoras, null, fator);
    }

    public int? MinutosCreditados(int minutosTrabalhados)
    {
        if (Forma != FormaDePagamento.BancoDeHoras || Fator is null)
            return null;
        return (int)Math.Round(minutosTrabalhados * Fator.Value, MidpointRounding.AwayFromZero);
    }
}

public readonly record struct IntervaloMinutos
{
    public int Inicio { get; }
    public int Fim { get; }

    public IntervaloMinutos(int Inicio, int Fim)
    {
        if (Fim <= Inicio || Inicio < 0 || Fim > 1440)
            throw new ViolacaoDeRegra("Intervalo inválido.");
        this.Inicio = Inicio;
        this.Fim = Fim;
    }

    public bool Cobre(int minuto) => minuto >= Inicio && minuto < Fim;

    public bool Intersecta(IntervaloMinutos outro) => Inicio < outro.Fim && outro.Inicio < Fim;
}

public sealed record EventoNoDia(Guid Id, string Nome, IntervaloMinutos Intervalo, TratamentoCopiado Tratamento);

public sealed record TrechoClassificado(
    IntervaloMinutos Intervalo,
    Fatia Fatia,
    Guid? EventoId,
    string? NomeEvento,
    TratamentoCopiado? Tratamento,
    decimal? PercentualExtraComum)
{
    public int MinutosTrabalhados => Intervalo.Fim - Intervalo.Inicio;
    public int? MinutosCreditados => Tratamento?.MinutosCreditados(MinutosTrabalhados);
}

public static class ClassificarHoras
{
    public static IReadOnlyList<TrechoClassificado> Executar(
        IReadOnlyList<IntervaloMinutos> batidas,
        IReadOnlyList<IntervaloMinutos> escala,
        IReadOnlyList<EventoNoDia> eventos,
        decimal percentualExtraComum)
    {
        if (percentualExtraComum < 0)
            throw new ViolacaoDeRegra("Percentual menor que zero é recusado.");

        var fatiaPorMinuto = new Fatia?[1440];
        var eventoPorMinuto = new int?[1440];

        for (var minuto = 0; minuto < 1440; minuto++)
        {
            if (!batidas.Any(batida => batida.Cobre(minuto)))
                continue;

            var indiceEvento = IndiceDoEvento(eventos, minuto);
            if (indiceEvento >= 0)
            {
                fatiaPorMinuto[minuto] = Fatia.Evento;
                eventoPorMinuto[minuto] = indiceEvento;
                continue;
            }

            fatiaPorMinuto[minuto] = escala.Count > 0 && escala.Any(item => item.Cobre(minuto))
                ? Fatia.Regular
                : Fatia.ExtraComum;
        }

        var trechos = new List<TrechoClassificado>();
        var cursor = 0;
        while (cursor < 1440)
        {
            if (fatiaPorMinuto[cursor] is null)
            {
                cursor++;
                continue;
            }

            var inicio = cursor;
            var fatia = fatiaPorMinuto[cursor]!.Value;
            var indice = eventoPorMinuto[cursor];
            while (cursor < 1440 && fatiaPorMinuto[cursor] == fatia && eventoPorMinuto[cursor] == indice)
                cursor++;

            EventoNoDia? evento = indice is int encontrado ? eventos[encontrado] : null;
            trechos.Add(new TrechoClassificado(
                new IntervaloMinutos(inicio, cursor),
                fatia,
                evento?.Id,
                evento?.Nome,
                fatia == Fatia.Evento ? evento!.Tratamento : null,
                fatia == Fatia.ExtraComum ? percentualExtraComum : null));
        }

        return trechos;
    }

    private static int IndiceDoEvento(IReadOnlyList<EventoNoDia> eventos, int minuto)
    {
        for (var i = 0; i < eventos.Count; i++)
        {
            if (eventos[i].Intervalo.Cobre(minuto))
                return i;
        }

        return -1;
    }
}
