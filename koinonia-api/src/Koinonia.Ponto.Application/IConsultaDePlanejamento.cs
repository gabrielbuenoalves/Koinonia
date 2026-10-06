using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Ponto.Application;

public sealed record ConsultaDoDia(
    IReadOnlyList<IntervaloMinutos> Escala,
    IReadOnlyList<EventoNoDia> Eventos,
    decimal PercentualExtraComum);

public interface IConsultaDePlanejamento
{
    ConsultaDoDia Obter(FuncionarioId funcionarioId, DateOnly dia);
}

public interface IRelogio
{
    DateTimeOffset Agora();
}

public interface IRepositorioDePonto
{
    IReadOnlyList<Batida> ListarBatidas(FuncionarioId funcionarioId);
    void Salvar(Batida batida);
    void Remover(Guid batidaId);
    DiaDePonto ObterOuAbrir(FuncionarioId funcionarioId, DateOnly dia);
    void Salvar(DiaDePonto dia);
    IReadOnlyList<DiaDePonto> ListarAprovados(DateOnly inicio, DateOnly fim);
}

public sealed record Espelho(bool Aprovado, IReadOnlyList<TrechoGravado> Trechos);
