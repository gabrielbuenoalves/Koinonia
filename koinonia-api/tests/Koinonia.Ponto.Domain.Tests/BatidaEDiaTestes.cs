using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Ponto.Domain.Tests;

public class BatidaEDiaTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 6, 20, 0, 0, TimeSpan.FromHours(-3));
    private static readonly FuncionarioId Pessoa = FuncionarioId.Novo();

    [Fact]
    public void Entrada_no_futuro_e_recusada()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => Batida.Abrir(Pessoa, Agora.AddMinutes(1), Agora));
        Assert.Equal("Entrada ou saída no futuro é recusada.", erro.Message);
    }

    [Fact]
    public void Saida_anterior_a_entrada_e_recusada()
    {
        var batida = Batida.Abrir(Pessoa, Agora.AddHours(-2), Agora);
        var erro = Assert.Throws<ViolacaoDeRegra>(() => batida.Fechar(Agora.AddHours(-3), Agora));
        Assert.Equal("Saída anterior à entrada.", erro.Message);
    }

    [Fact]
    public void Aprovar_duas_vezes_e_recusado()
    {
        var dia = DiaDePonto.Abrir(Pessoa, new DateOnly(2026, 10, 6));
        dia.Aprovar([]);
        var erro = Assert.Throws<ViolacaoDeRegra>(() => dia.Aprovar([]));
        Assert.Equal("Aprovar um dia já aprovado é recusado.", erro.Message);
    }

    [Fact]
    public void Reabrir_dia_aberto_e_recusado()
    {
        var dia = DiaDePonto.Abrir(Pessoa, new DateOnly(2026, 10, 6));
        var erro = Assert.Throws<ViolacaoDeRegra>(dia.Reabrir);
        Assert.Equal("Reabrir um dia aberto é recusado.", erro.Message);
    }

    [Fact]
    public void Aprovar_copia_o_trecho_e_reabrir_remove_a_copia()
    {
        var dia = DiaDePonto.Abrir(Pessoa, new DateOnly(2026, 10, 6));
        var trecho = new TrechoClassificado(
            new IntervaloMinutos(8 * 60, 9 * 60),
            Fatia.ExtraComum,
            null,
            null,
            null,
            50m);

        dia.Aprovar([trecho]);
        Assert.Equal(50m, Assert.Single(dia.Trechos).Percentual);

        dia.Reabrir();
        Assert.False(dia.Aprovado);
        Assert.Empty(dia.Trechos);
    }
}
