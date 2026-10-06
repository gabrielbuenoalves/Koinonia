using Koinonia.Planejamento.Domain;
using Koinonia.Shared;

namespace Koinonia.Planejamento.Application;

public sealed record EventoLido(Guid Id, string Nome, DateTimeOffset Inicio, DateTimeOffset Fim, TratamentoDeEvento Tratamento);

public sealed record LeituraDoDia(
    IReadOnlyList<IntervaloDeEscala> Escala,
    IReadOnlyList<EventoLido> Eventos,
    decimal PercentualExtraComum);

public interface IRepositorioDePlanejamento
{
    void SalvarEscala(EscalaDoDia escala);
    void RemoverEscala(FuncionarioId funcionarioId, DateOnly dia);
    void SalvarEvento(Evento evento);
    Evento? ObterEvento(Guid id);
    IReadOnlyList<Evento> ListarEventos();
    void SalvarPercentual(PercentualExtraComum percentual);
    LeituraDoDia Ler(FuncionarioId funcionarioId, DateOnly dia);
}

public interface ILeituraDePlanejamento
{
    LeituraDoDia Ler(FuncionarioId funcionarioId, DateOnly dia);
}

public sealed class ServicoDePlanejamento : ILeituraDePlanejamento
{
    private readonly IRepositorioDePlanejamento _repositorio;

    public ServicoDePlanejamento(IRepositorioDePlanejamento repositorio) => _repositorio = repositorio;

    public void PublicarEscala(FuncionarioId funcionarioId, DateOnly dia, IReadOnlyList<IntervaloDeEscala> intervalos)
        => _repositorio.SalvarEscala(EscalaDoDia.Publicar(funcionarioId, dia, intervalos));

    public void RemoverEscala(FuncionarioId funcionarioId, DateOnly dia)
        => _repositorio.RemoverEscala(funcionarioId, dia);

    public Guid PublicarEvento(string nome, DateTimeOffset inicio, DateTimeOffset fim, IReadOnlyCollection<FuncionarioId> escalados, TratamentoDeEvento tratamento)
    {
        var evento = Evento.Criar(nome, inicio, fim, escalados, tratamento);
        GradeDeEventos.GarantirSemConflito(evento, _repositorio.ListarEventos());
        _repositorio.SalvarEvento(evento);
        return evento.Id;
    }

    public void AlterarEvento(Guid id, string nome, DateTimeOffset inicio, DateTimeOffset fim, IReadOnlyCollection<FuncionarioId> escalados, TratamentoDeEvento tratamento)
    {
        var evento = _repositorio.ObterEvento(id) ?? throw new ViolacaoDeRegra("Intervalo inválido.");
        evento.Alterar(nome, inicio, fim, escalados, tratamento);
        GradeDeEventos.GarantirSemConflito(evento, _repositorio.ListarEventos());
        _repositorio.SalvarEvento(evento);
    }

    public void DefinirPercentual(decimal valor)
        => _repositorio.SalvarPercentual(PercentualExtraComum.Definir(valor));

    public LeituraDoDia Ler(FuncionarioId funcionarioId, DateOnly dia) => _repositorio.Ler(funcionarioId, dia);
}
