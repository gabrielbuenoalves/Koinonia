using Koinonia.Planejamento.Application;
using Koinonia.Planejamento.Domain;
using Koinonia.Ponto.Application;
using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Api;

public sealed class ConsultaDePlanejamentoAdapter : IConsultaDePlanejamento
{
    private readonly ILeituraDePlanejamento _leitura;

    public ConsultaDePlanejamentoAdapter(ILeituraDePlanejamento leitura) => _leitura = leitura;

    public ConsultaDoDia Obter(FuncionarioId funcionarioId, DateOnly dia)
    {
        var leitura = _leitura.Ler(funcionarioId, dia);
        var eventos = new List<EventoNoDia>();
        foreach (var evento in leitura.Eventos)
        {
            var recorte = Recortar(evento.Inicio, evento.Fim, dia);
            if (recorte is null)
                continue;
            eventos.Add(new EventoNoDia(evento.Id, evento.Nome, recorte.Value, Mapear(evento.Tratamento)));
        }

        return new ConsultaDoDia(
            leitura.Escala.Select(item => new IntervaloMinutos(item.Inicio, item.Fim)).ToList(),
            eventos,
            leitura.PercentualExtraComum);
    }

    public static IntervaloMinutos? Recortar(DateTimeOffset inicio, DateTimeOffset fim, DateOnly dia)
    {
        var zona = ClassificarHoras.Zona;
        var inicioDia = new DateTimeOffset(dia.ToDateTime(TimeOnly.MinValue), zona.GetUtcOffset(dia.ToDateTime(TimeOnly.MinValue)));
        var fimDia = inicioDia.AddDays(1);
        var a = inicio > inicioDia ? inicio : inicioDia;
        var b = fim < fimDia ? fim : fimDia;
        if (b <= a)
            return null;

        var aLocal = TimeZoneInfo.ConvertTime(a, zona);
        var bLocal = TimeZoneInfo.ConvertTime(b, zona);
        var inicioMinuto = aLocal.Hour * 60 + aLocal.Minute;
        var fimMinuto = b >= fimDia ? 1440 : bLocal.Hour * 60 + bLocal.Minute;
        return fimMinuto <= inicioMinuto ? null : new IntervaloMinutos(inicioMinuto, fimMinuto);
    }

    private static TratamentoCopiado Mapear(TratamentoDeEvento tratamento) => tratamento.Forma switch
    {
        FormaDePagamentoDoEvento.HoraNormal => TratamentoCopiado.HoraNormal(),
        FormaDePagamentoDoEvento.Adicional => TratamentoCopiado.Adicional(tratamento.Percentual!.Value),
        FormaDePagamentoDoEvento.BancoDeHoras => TratamentoCopiado.Banco(tratamento.Fator!.Value),
        _ => throw new ViolacaoDeRegra("Evento sem percentual, quando o tratamento é adicional, é recusado.")
    };
}
