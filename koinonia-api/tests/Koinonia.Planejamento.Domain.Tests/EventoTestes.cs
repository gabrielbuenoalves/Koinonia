using Koinonia.Planejamento.Domain;
using Koinonia.Shared;

namespace Koinonia.Planejamento.Domain.Tests;

public class EventoTestes
{
    private static readonly DateTimeOffset Inicio = new(2026, 10, 6, 14, 0, 0, TimeSpan.FromHours(-3));
    private static readonly DateTimeOffset Fim = new(2026, 10, 6, 18, 0, 0, TimeSpan.FromHours(-3));

    [Fact]
    public void Adicional_sem_percentual_e_recusado()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => TratamentoDeEvento.Adicional(-1m));
        Assert.Equal("Percentual menor que zero é recusado.", erro.Message);
    }

    [Fact]
    public void Banco_sem_fator_valido_e_recusado()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => TratamentoDeEvento.Banco(0m));
        Assert.Equal("Fator menor ou igual a zero é recusado.", erro.Message);
    }

    [Fact]
    public void Hora_normal_nao_carrega_numero()
    {
        var tratamento = TratamentoDeEvento.HoraNormal();
        Assert.Equal(FormaDePagamentoDoEvento.HoraNormal, tratamento.Forma);
        Assert.Null(tratamento.Percentual);
        Assert.Null(tratamento.Fator);
    }

    [Fact]
    public void Eventos_que_apenas_se_encostam_para_a_mesma_pessoa_sao_validos()
    {
        var pessoa = FuncionarioId.Novo();
        var primeiro = Evento.Criar("A", Inicio, Fim, [pessoa], TratamentoDeEvento.HoraNormal());
        var segundo = Evento.Criar("B", Fim, Fim.AddHours(1), [pessoa], TratamentoDeEvento.HoraNormal());

        GradeDeEventos.GarantirSemConflito(segundo, [primeiro]);
    }

    [Fact]
    public void Intersecao_para_a_mesma_pessoa_e_recusada()
    {
        var pessoa = FuncionarioId.Novo();
        var primeiro = Evento.Criar("A", Inicio, Fim, [pessoa], TratamentoDeEvento.Adicional(50m));
        var segundo = Evento.Criar("B", Inicio.AddHours(1), Fim.AddHours(1), [pessoa], TratamentoDeEvento.Banco(1m));

        var erro = Assert.Throws<ViolacaoDeRegra>(() => GradeDeEventos.GarantirSemConflito(segundo, [primeiro]));
        Assert.Equal("Dois eventos com interseção de duração maior que zero, para a mesma pessoa, são recusados.", erro.Message);
    }

    [Fact]
    public void Alterar_repete_a_recusa_de_intersecao()
    {
        var pessoa = FuncionarioId.Novo();
        var primeiro = Evento.Criar("A", Inicio, Fim, [pessoa], TratamentoDeEvento.HoraNormal());
        var segundo = Evento.Criar("B", Fim, Fim.AddHours(2), [pessoa], TratamentoDeEvento.HoraNormal());

        var erro = Assert.Throws<ViolacaoDeRegra>(() =>
        {
            segundo.Alterar("B", Inicio.AddHours(1), Fim.AddHours(1), [pessoa], TratamentoDeEvento.HoraNormal());
            GradeDeEventos.GarantirSemConflito(segundo, [primeiro]);
        });

        Assert.Equal("Dois eventos com interseção de duração maior que zero, para a mesma pessoa, são recusados.", erro.Message);
    }

    [Fact]
    public void Alterar_com_fim_anterior_ao_inicio_e_recusado_sem_alterar_estado()
    {
        var pessoa = FuncionarioId.Novo();
        var evento = Evento.Criar("Original", Inicio, Fim, [pessoa], TratamentoDeEvento.HoraNormal());

        var erro = Assert.Throws<ViolacaoDeRegra>(() =>
            evento.Alterar("Novo", Fim, Inicio, [pessoa], TratamentoDeEvento.Adicional(10m)));

        Assert.Equal("Saída anterior à entrada.", erro.Message);
        Assert.Equal("Original", evento.Nome);
        Assert.Equal(Inicio, evento.Inicio);
        Assert.Equal(Fim, evento.Fim);
        Assert.Single(evento.Escalados);
        Assert.Contains(pessoa, evento.Escalados);
    }

    [Fact]
    public void Alterar_passando_os_escalados_do_proprio_evento_mantem_a_escala()
    {
        var pessoa = FuncionarioId.Novo();
        var evento = Evento.Criar("Original", Inicio, Fim, [pessoa], TratamentoDeEvento.HoraNormal());

        evento.Alterar("Novo", Inicio, Fim, evento.Escalados, TratamentoDeEvento.Adicional(10m));

        Assert.Equal("Novo", evento.Nome);
        Assert.Single(evento.Escalados);
        Assert.Contains(pessoa, evento.Escalados);
    }

    [Fact]
    public void Alterar_com_escalados_vazios_e_recusado_sem_alterar_estado()
    {
        var pessoa = FuncionarioId.Novo();
        var evento = Evento.Criar("Original", Inicio, Fim, [pessoa], TratamentoDeEvento.HoraNormal());

        var erro = Assert.Throws<ViolacaoDeRegra>(() =>
            evento.Alterar("Novo", Inicio, Fim, [], TratamentoDeEvento.HoraNormal()));

        Assert.Equal("Intervalo inválido.", erro.Message);
        Assert.Equal("Original", evento.Nome);
        Assert.Equal(Inicio, evento.Inicio);
        Assert.Equal(Fim, evento.Fim);
        Assert.Single(evento.Escalados);
        Assert.Contains(pessoa, evento.Escalados);
    }
}
