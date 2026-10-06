using Koinonia.Shared;

namespace Koinonia.Planejamento.Domain;

public enum FormaDePagamentoDoEvento { HoraNormal, Adicional, BancoDeHoras }

public sealed record TratamentoDeEvento(FormaDePagamentoDoEvento Forma, decimal? Percentual, decimal? Fator)
{
    public static TratamentoDeEvento HoraNormal() => new(FormaDePagamentoDoEvento.HoraNormal, null, null);

    public static TratamentoDeEvento Adicional(decimal percentual)
    {
        if (percentual < 0)
            throw new ViolacaoDeRegra("Percentual menor que zero é recusado.");
        return new TratamentoDeEvento(FormaDePagamentoDoEvento.Adicional, percentual, null);
    }

    public static TratamentoDeEvento Banco(decimal fator)
    {
        if (fator <= 0)
            throw new ViolacaoDeRegra("Fator menor ou igual a zero é recusado.");
        return new TratamentoDeEvento(FormaDePagamentoDoEvento.BancoDeHoras, null, fator);
    }
}

public sealed class Evento
{
    private readonly HashSet<FuncionarioId> _escalados;

    private Evento(Guid id, string nome, DateTimeOffset inicio, DateTimeOffset fim, IEnumerable<FuncionarioId> escalados, TratamentoDeEvento tratamento)
    {
        Id = id;
        Nome = nome;
        Inicio = inicio;
        Fim = fim;
        Tratamento = tratamento;
        _escalados = escalados.ToHashSet();
    }

    public Guid Id { get; }
    public string Nome { get; private set; }
    public DateTimeOffset Inicio { get; private set; }
    public DateTimeOffset Fim { get; private set; }
    public TratamentoDeEvento Tratamento { get; private set; }
    public IReadOnlySet<FuncionarioId> Escalados => _escalados;

    public static Evento Criar(string nome, DateTimeOffset inicio, DateTimeOffset fim, IReadOnlyCollection<FuncionarioId> escalados, TratamentoDeEvento tratamento)
    {
        ValidarFim(inicio, fim);
        return new Evento(Guid.NewGuid(), ValidarNome(nome), inicio, fim, ValidarEscalados(escalados), tratamento);
    }

    public static Evento Reidratar(Guid id, string nome, DateTimeOffset inicio, DateTimeOffset fim, IEnumerable<FuncionarioId> escalados, TratamentoDeEvento tratamento)
        => new(id, nome, inicio, fim, escalados, tratamento);

    public void Alterar(string nome, DateTimeOffset inicio, DateTimeOffset fim, IReadOnlyCollection<FuncionarioId> escalados, TratamentoDeEvento tratamento)
    {
        var nomeValidado = ValidarNome(nome);
        var fimValidado = ValidarFim(inicio, fim);
        var escaladosValidados = ValidarEscalados(escalados).ToList();

        Nome = nomeValidado;
        Inicio = inicio;
        Fim = fimValidado;
        Tratamento = tratamento;
        _escalados.Clear();
        foreach (var escalado in escaladosValidados)
            _escalados.Add(escalado);
    }

    public bool ConflitaCom(Evento outro)
    {
        if (Id == outro.Id)
            return false;
        if (Inicio >= outro.Fim || outro.Inicio >= Fim)
            return false;
        return _escalados.Overlaps(outro._escalados);
    }

    private static string ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ViolacaoDeRegra("Intervalo inválido.");
        return nome.Trim();
    }

    private static DateTimeOffset ValidarFim(DateTimeOffset inicio, DateTimeOffset fim)
    {
        if (fim <= inicio)
            throw new ViolacaoDeRegra("Saída anterior à entrada.");
        return fim;
    }

    private static IReadOnlyCollection<FuncionarioId> ValidarEscalados(IReadOnlyCollection<FuncionarioId> escalados)
    {
        if (escalados.Count == 0)
            throw new ViolacaoDeRegra("Intervalo inválido.");
        return escalados;
    }
}

public static class GradeDeEventos
{
    public static void GarantirSemConflito(Evento candidato, IEnumerable<Evento> existentes)
    {
        foreach (var existente in existentes)
        {
            if (candidato.ConflitaCom(existente))
                throw new ViolacaoDeRegra("Dois eventos com interseção de duração maior que zero, para a mesma pessoa, são recusados.");
        }
    }
}
