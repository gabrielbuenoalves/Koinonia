using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Ponto.Application;

public sealed class ServicoDePonto
{
    private readonly IConsultaDePlanejamento _consulta;
    private readonly IRepositorioDePonto _repositorio;
    private readonly IRelogio _relogio;

    public ServicoDePonto(IConsultaDePlanejamento consulta, IRepositorioDePonto repositorio, IRelogio relogio)
    {
        _consulta = consulta;
        _repositorio = repositorio;
        _relogio = relogio;
    }

    public Espelho ObterEspelho(FuncionarioId funcionarioId, DateOnly dia)
    {
        var diaDePonto = _repositorio.ObterOuAbrir(funcionarioId, dia);
        if (diaDePonto.Aprovado)
            return new Espelho(true, diaDePonto.Trechos);

        var consulta = _consulta.Obter(funcionarioId, dia);
        var trechos = ClassificarHoras.Executar(
            IntervalosFechados(funcionarioId, dia),
            consulta.Escala,
            consulta.Eventos,
            consulta.PercentualExtraComum);
        return new Espelho(false, trechos.Select(TrechoGravado.De).ToList());
    }

    public void Aprovar(FuncionarioId funcionarioId, DateOnly dia)
    {
        var batidas = BatidasDoDia(funcionarioId, dia);
        if (batidas.Any(item => item.Aberta))
            throw new ViolacaoDeRegra("Aprovar com entrada sem saída é recusado.");
        if (!batidas.Any(item => !item.Aberta))
            throw new ViolacaoDeRegra("Aprovar sem intervalo fechado é recusado.");

        var diaDePonto = _repositorio.ObterOuAbrir(funcionarioId, dia);
        var consulta = _consulta.Obter(funcionarioId, dia);
        var trechos = ClassificarHoras.Executar(
            IntervalosFechados(funcionarioId, dia),
            consulta.Escala,
            consulta.Eventos,
            consulta.PercentualExtraComum);
        diaDePonto.Aprovar(trechos);
        _repositorio.Salvar(diaDePonto);
    }

    public void Reabrir(FuncionarioId funcionarioId, DateOnly dia)
    {
        var diaDePonto = _repositorio.ObterOuAbrir(funcionarioId, dia);
        diaDePonto.Reabrir();
        _repositorio.Salvar(diaDePonto);
    }

    public Guid RegistrarEntrada(FuncionarioId funcionarioId, DateTimeOffset entrada)
    {
        var agora = _relogio.Agora();
        var dia = ClassificarHoras.DiaCivil(entrada);
        GarantirDiaAberto(funcionarioId, dia);
        if (_repositorio.ListarBatidas(funcionarioId).Any(item => item.Aberta))
            throw new ViolacaoDeRegra("Já existe entrada sem saída.");
        var batida = Batida.Abrir(funcionarioId, entrada, agora);
        _repositorio.Salvar(batida);
        return batida.Id;
    }

    public void RegistrarSaida(Guid batidaId, FuncionarioId funcionarioId, DateTimeOffset saida)
    {
        var batida = Obter(batidaId, funcionarioId);
        var agora = _relogio.Agora();
        if (saida > agora)
            throw new ViolacaoDeRegra("Entrada ou saída no futuro é recusada.");
        if (saida <= batida.Entrada)
            throw new ViolacaoDeRegra("Saída anterior à entrada.");
        foreach (var pedaco in ClassificarHoras.Cortar(batida.Entrada, saida))
            GarantirDiaAberto(funcionarioId, pedaco.Dia);
        GarantirSemIntersecao(funcionarioId, batida.Entrada, saida, batida.Id);
        batida.Fechar(saida, agora);
        _repositorio.Salvar(batida);
    }

    public void RemoverBatida(Guid batidaId, FuncionarioId funcionarioId)
    {
        var batida = Obter(batidaId, funcionarioId);
        if (batida.Saida is null)
        {
            GarantirDiaAberto(funcionarioId, ClassificarHoras.DiaCivil(batida.Entrada));
        }
        else
        {
            foreach (var pedaco in ClassificarHoras.Cortar(batida.Entrada, batida.Saida.Value))
                GarantirDiaAberto(funcionarioId, pedaco.Dia);
        }

        _repositorio.Remover(batidaId);
    }

    private Batida Obter(Guid batidaId, FuncionarioId funcionarioId)
        => _repositorio.ListarBatidas(funcionarioId).FirstOrDefault(item => item.Id == batidaId)
           ?? throw new ViolacaoDeRegra("Funcionário desconhecido é recusado no host.");

    private void GarantirDiaAberto(FuncionarioId funcionarioId, DateOnly dia)
    {
        if (_repositorio.ObterOuAbrir(funcionarioId, dia).Aprovado)
            throw new ViolacaoDeRegra("Batida em dia aprovado é recusada até a gestão reabrir.");
    }

    private void GarantirSemIntersecao(FuncionarioId funcionarioId, DateTimeOffset entrada, DateTimeOffset saida, Guid ignorar)
    {
        foreach (var outra in _repositorio.ListarBatidas(funcionarioId))
        {
            if (outra.Id == ignorar || outra.Saida is null)
                continue;
            if (entrada < outra.Saida && outra.Entrada < saida)
                throw new ViolacaoDeRegra("Intervalo que cruza outro intervalo já fechado da mesma pessoa é recusado.");
        }
    }

    private List<Batida> BatidasDoDia(FuncionarioId funcionarioId, DateOnly dia)
        => _repositorio.ListarBatidas(funcionarioId).Where(batida =>
            ClassificarHoras.DiaCivil(batida.Entrada) == dia
            || (batida.Saida is not null && ClassificarHoras.Cortar(batida.Entrada, batida.Saida.Value).Any(pedaco => pedaco.Dia == dia)))
            .ToList();

    private List<IntervaloMinutos> IntervalosFechados(FuncionarioId funcionarioId, DateOnly dia)
        => BatidasDoDia(funcionarioId, dia)
            .Where(item => item.Saida is not null)
            .SelectMany(item => ClassificarHoras.Cortar(item.Entrada, item.Saida!.Value))
            .Where(item => item.Dia == dia)
            .Select(item => item.Intervalo)
            .ToList();
}
