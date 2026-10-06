using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using Koinonia.Ponto.Application;
using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Ponto.Application.Tests;

public class ExportadorDeFechamentoTestes
{
    private readonly FuncionarioId _ana = FuncionarioId.Novo();

    [Fact]
    public void Periodo_sem_dia_aprovado_gera_so_o_cabecalho()
    {
        var arquivo = ExportadorDeFechamento.Gerar([], id => "Ana", FormatoDeFechamento.Csv);
        var (detalhe, totais) = LerZip(arquivo.Conteudo);
        Assert.Equal(ExportadorDeFechamento.Cabecalho, detalhe.Trim());
        Assert.Equal(ExportadorDeFechamento.Cabecalho, totais.Trim());
    }

    [Fact]
    public void Csv_separa_extra_por_percentual_e_preenche_credito_so_no_banco()
    {
        var dia = DiaAprovado();
        var arquivo = ExportadorDeFechamento.Gerar([dia], id => "Ana", FormatoDeFechamento.Csv);
        var (detalhe, totais) = LerZip(arquivo.Conteudo);

        Assert.Contains("Ana;2026-10-06;08:00;14:00;regular;;;;;360;", detalhe);
        Assert.Contains("Ana;2026-10-06;14:00;18:00;evento;Formatura;banco de horas;;1;240;240", detalhe);
        Assert.Contains("Ana;2026-10-06;18:00;19:00;extra comum;;;50;;60;", detalhe);
        Assert.Contains("extra comum;;;50;;60;", totais);
        Assert.DoesNotContain("2026-10-07", detalhe);
        Assert.Equal("fechamento.zip", arquivo.NomeDoArquivo);
    }

    [Fact]
    public void Excel_tem_abas_Detalhe_e_Totais()
    {
        var arquivo = ExportadorDeFechamento.Gerar([DiaAprovado()], id => "Ana", FormatoDeFechamento.Excel);
        using var pasta = new XLWorkbook(new MemoryStream(arquivo.Conteudo));
        Assert.NotNull(pasta.Worksheet("Detalhe"));
        Assert.NotNull(pasta.Worksheet("Totais"));
        Assert.Equal("funcionário", pasta.Worksheet("Detalhe").Cell(1, 1).GetString());
        Assert.Equal("fechamento.xlsx", arquivo.NomeDoArquivo);
    }

    private DiaDePonto DiaAprovado()
    {
        var dia = DiaDePonto.Abrir(_ana, new DateOnly(2026, 10, 6));
        dia.Aprovar(ClassificarHoras.Executar(
            [new IntervaloMinutos(8 * 60, 19 * 60)],
            [new IntervaloMinutos(8 * 60, 17 * 60)],
            [new EventoNoDia(Guid.NewGuid(), "Formatura", new IntervaloMinutos(14 * 60, 18 * 60), TratamentoCopiado.Banco(1m))],
            50m));
        return dia;
    }

    private static (string Detalhe, string Totais) LerZip(byte[] conteudo)
    {
        using var zip = new ZipArchive(new MemoryStream(conteudo), ZipArchiveMode.Read);
        return (Ler(zip, "detalhe.csv"), Ler(zip, "totais.csv"));
    }

    private static string Ler(ZipArchive zip, string nome)
    {
        var entrada = zip.GetEntry(nome) ?? throw new InvalidOperationException(nome);
        using var leitor = new StreamReader(entrada.Open(), Encoding.UTF8);
        return leitor.ReadToEnd();
    }
}
