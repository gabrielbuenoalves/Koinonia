using Koinonia.Ponto.Application;
using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Ponto.Infrastructure;

public sealed class RepositorioDePonto : IRepositorioDePonto
{
    private readonly PontoDbContext _contexto;

    public RepositorioDePonto(PontoDbContext contexto) => _contexto = contexto;

    public IReadOnlyList<Batida> ListarBatidas(FuncionarioId funcionarioId)
        => _contexto.Batidas.Where(item => item.FuncionarioId == funcionarioId.Valor).ToList()
            .Select(item => Batida.Reidratar(item.Id, funcionarioId, item.Entrada, item.Saida)).ToList();

    public void Salvar(Batida batida)
    {
        var row = _contexto.Batidas.FirstOrDefault(item => item.Id == batida.Id);
        if (row is null)
        {
            row = new BatidaRow { Id = batida.Id };
            _contexto.Batidas.Add(row);
        }

        row.FuncionarioId = batida.FuncionarioId.Valor;
        row.Entrada = batida.Entrada;
        row.Saida = batida.Saida;
        _contexto.SaveChanges();
    }

    public void Remover(Guid batidaId)
    {
        var row = _contexto.Batidas.FirstOrDefault(item => item.Id == batidaId);
        if (row is null)
            return;
        _contexto.Batidas.Remove(row);
        _contexto.SaveChanges();
    }

    public DiaDePonto ObterOuAbrir(FuncionarioId funcionarioId, DateOnly dia)
    {
        var row = _contexto.Dias.FirstOrDefault(item => item.FuncionarioId == funcionarioId.Valor && item.Dia == dia);
        if (row is null)
            return DiaDePonto.Abrir(funcionarioId, dia);
        var trechos = _contexto.Trechos.Where(item => item.DiaId == row.Id).ToList().Select(Mapear);
        return DiaDePonto.Reidratar(funcionarioId, dia, row.Aprovado, trechos);
    }

    public void Salvar(DiaDePonto dia)
    {
        var row = _contexto.Dias.FirstOrDefault(item => item.FuncionarioId == dia.FuncionarioId.Valor && item.Dia == dia.Dia);
        if (row is { Aprovado: true } && dia.Aprovado)
            throw new ViolacaoDeRegra("Aprovar um dia já aprovado é recusado.");

        if (row is null)
        {
            row = new DiaRow { Id = Guid.NewGuid(), FuncionarioId = dia.FuncionarioId.Valor, Dia = dia.Dia };
            _contexto.Dias.Add(row);
        }

        row.Aprovado = dia.Aprovado;
        _contexto.Trechos.RemoveRange(_contexto.Trechos.Where(item => item.DiaId == row.Id));
        _contexto.Trechos.AddRange(dia.Trechos.Select(item => new TrechoRow
        {
            Id = Guid.NewGuid(),
            DiaId = row.Id,
            Inicio = item.Inicio,
            Fim = item.Fim,
            Fatia = (int)item.Fatia,
            EventoId = item.EventoId,
            NomeEvento = item.NomeEvento,
            Forma = item.Forma is null ? null : (int)item.Forma,
            Percentual = item.Percentual,
            Fator = item.Fator,
            MinutosTrabalhados = item.MinutosTrabalhados,
            MinutosCreditados = item.MinutosCreditados
        }));
        _contexto.SaveChanges();
    }

    public IReadOnlyList<DiaDePonto> ListarAprovados(DateOnly inicio, DateOnly fim)
        => _contexto.Dias.Where(item => item.Aprovado && item.Dia >= inicio && item.Dia <= fim).ToList()
            .Select(item => ObterOuAbrir(FuncionarioId.De(item.FuncionarioId), item.Dia)).ToList();

    private static TrechoGravado Mapear(TrechoRow item) => new(
        item.Inicio,
        item.Fim,
        (Fatia)item.Fatia,
        item.EventoId,
        item.NomeEvento,
        item.Forma is null ? null : (FormaDePagamento)item.Forma,
        item.Percentual,
        item.Fator,
        item.MinutosTrabalhados,
        item.MinutosCreditados);
}
