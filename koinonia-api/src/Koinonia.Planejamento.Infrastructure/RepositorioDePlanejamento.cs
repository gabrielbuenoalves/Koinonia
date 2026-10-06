using Koinonia.Planejamento.Application;
using Koinonia.Planejamento.Domain;
using Koinonia.Shared;

namespace Koinonia.Planejamento.Infrastructure;

public sealed class RepositorioDePlanejamento : IRepositorioDePlanejamento
{
    private readonly PlanejamentoDbContext _contexto;

    public RepositorioDePlanejamento(PlanejamentoDbContext contexto) => _contexto = contexto;

    public void SalvarEscala(EscalaDoDia escala)
    {
        RemoverEscala(escala.FuncionarioId, escala.Dia);
        var row = new EscalaRow
        {
            Id = Guid.NewGuid(),
            FuncionarioId = escala.FuncionarioId.Valor,
            Dia = escala.Dia,
            Intervalos = escala.Intervalos.Select(item => new IntervaloEscalaRow
            {
                Id = Guid.NewGuid(),
                Inicio = item.Inicio,
                Fim = item.Fim
            }).ToList()
        };
        _contexto.Escalas.Add(row);
        _contexto.SaveChanges();
    }

    public void RemoverEscala(FuncionarioId funcionarioId, DateOnly dia)
    {
        var existente = _contexto.Escalas.FirstOrDefault(item => item.FuncionarioId == funcionarioId.Valor && item.Dia == dia);
        if (existente is null)
            return;
        _contexto.Escalas.Remove(existente);
        _contexto.SaveChanges();
    }

    public void SalvarEvento(Evento evento)
    {
        var row = _contexto.Eventos.FirstOrDefault(item => item.Id == evento.Id);
        if (row is null)
        {
            row = new EventoRow { Id = evento.Id };
            _contexto.Eventos.Add(row);
        }

        row.Nome = evento.Nome;
        row.Inicio = evento.Inicio;
        row.Fim = evento.Fim;
        row.Forma = (int)evento.Tratamento.Forma;
        row.Percentual = evento.Tratamento.Percentual;
        row.Fator = evento.Tratamento.Fator;
        _contexto.Escalados.RemoveRange(_contexto.Escalados.Where(item => item.EventoId == evento.Id));
        _contexto.Escalados.AddRange(evento.Escalados.Select(item => new EventoEscaladoRow
        {
            EventoId = evento.Id,
            FuncionarioId = item.Valor
        }));
        _contexto.SaveChanges();
    }

    public Evento? ObterEvento(Guid id)
    {
        var row = _contexto.Eventos.FirstOrDefault(item => item.Id == id);
        return row is null ? null : Mapear(row);
    }

    public IReadOnlyList<Evento> ListarEventos()
        => _contexto.Eventos.ToList().Select(Mapear).ToList();

    public void SalvarPercentual(PercentualExtraComum percentual)
    {
        var row = _contexto.Percentual.FirstOrDefault(item => item.Id == 1);
        if (row is null)
        {
            row = new PercentualRow { Id = 1 };
            _contexto.Percentual.Add(row);
        }

        row.Valor = percentual.Valor;
        _contexto.SaveChanges();
    }

    public LeituraDoDia Ler(FuncionarioId funcionarioId, DateOnly dia)
    {
        var escala = _contexto.Escalas.FirstOrDefault(item => item.FuncionarioId == funcionarioId.Valor && item.Dia == dia);
        var intervalos = escala is null
            ? []
            : _contexto.Intervalos.Where(item => item.EscalaId == escala.Id)
                .OrderBy(item => item.Inicio)
                .Select(item => new IntervaloDeEscala(item.Inicio, item.Fim))
                .ToList();

        var eventos = _contexto.Eventos
            .Where(item => _contexto.Escalados.Any(escalado => escalado.EventoId == item.Id && escalado.FuncionarioId == funcionarioId.Valor))
            .Select(item => new EventoLido(
                item.Id,
                item.Nome,
                item.Inicio,
                item.Fim,
                new TratamentoDeEvento((FormaDePagamentoDoEvento)item.Forma, item.Percentual, item.Fator)))
            .ToList();

        var percentual = _contexto.Percentual.FirstOrDefault(item => item.Id == 1)?.Valor ?? 0m;
        return new LeituraDoDia(intervalos, eventos, percentual);
    }

    private Evento Mapear(EventoRow row)
    {
        var escalados = _contexto.Escalados.Where(item => item.EventoId == row.Id).Select(item => FuncionarioId.De(item.FuncionarioId)).ToList();
        var tratamento = new TratamentoDeEvento((FormaDePagamentoDoEvento)row.Forma, row.Percentual, row.Fator);
        return Evento.Reidratar(row.Id, row.Nome, row.Inicio, row.Fim, escalados, tratamento);
    }
}
