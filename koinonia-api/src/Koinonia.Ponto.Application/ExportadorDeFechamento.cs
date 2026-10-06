using System.Globalization;
using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Ponto.Application;

public enum FormatoDeFechamento { Csv, Excel }

public sealed record ArquivoDeFechamento(string NomeDoArquivo, string TipoConteudo, byte[] Conteudo);

public static class ExportadorDeFechamento
{
    public const string Cabecalho = "funcionário;dia;início;fim;fatia;evento;tratamento;percentual;fator;minutos trabalhados;minutos creditados";
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: true);

    public static ArquivoDeFechamento Gerar(IReadOnlyList<DiaDePonto> dias, Func<FuncionarioId, string> nome, FormatoDeFechamento formato)
    {
        var detalhe = dias
            .SelectMany(dia => dia.Trechos.Select(trecho => Linha(nome(dia.FuncionarioId), dia.Dia, trecho)))
            .OrderBy(item => item[0]).ThenBy(item => item[1]).ThenBy(item => item[2])
            .ToList();
        var totais = detalhe
            .GroupBy(item => (item[0], item[4], item[5], item[6], item[7], item[8]))
            .Select(grupo => new[]
            {
                grupo.Key.Item1, "", "", "", grupo.Key.Item2, grupo.Key.Item3, grupo.Key.Item4, grupo.Key.Item5, grupo.Key.Item6,
                grupo.Sum(item => int.Parse(item[9], Cultura)).ToString(Cultura),
                SomarCredito(grupo)
            })
            .ToList();

        return formato == FormatoDeFechamento.Csv ? Csv(detalhe, totais) : Excel(detalhe, totais);
    }

    private static string[] Linha(string nome, DateOnly dia, TrechoGravado trecho) =>
    [
        nome,
        dia.ToString("yyyy-MM-dd"),
        Formatar(trecho.Inicio),
        Formatar(trecho.Fim),
        FatiaTexto(trecho.Fatia),
        trecho.NomeEvento ?? "",
        FormaTexto(trecho.Forma),
        trecho.Percentual?.ToString(Cultura) ?? "",
        trecho.Fator?.ToString(Cultura) ?? "",
        trecho.MinutosTrabalhados.ToString(Cultura),
        trecho.MinutosCreditados?.ToString(Cultura) ?? ""
    ];

    private static string SomarCredito(IEnumerable<string[]> grupo)
    {
        var valores = grupo.Select(item => item[10]).Where(item => item.Length > 0).Select(item => int.Parse(item, Cultura)).ToList();
        return valores.Count == 0 ? "" : valores.Sum().ToString(Cultura);
    }

    private static string Formatar(int minuto)
        => minuto == 1440 ? "24:00" : TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minuto)).ToString("HH:mm");

    private static string FatiaTexto(Fatia fatia) => fatia switch
    {
        Fatia.Regular => "regular",
        Fatia.ExtraComum => "extra comum",
        Fatia.Evento => "evento",
        _ => ""
    };

    private static string FormaTexto(FormaDePagamento? forma) => forma switch
    {
        FormaDePagamento.HoraNormal => "hora normal",
        FormaDePagamento.Adicional => "adicional",
        FormaDePagamento.BancoDeHoras => "banco de horas",
        _ => ""
    };

    private static ArquivoDeFechamento Csv(IReadOnlyList<string[]> detalhe, IReadOnlyList<string[]> totais)
    {
        using var memoria = new MemoryStream();
        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            Escrever(zip, "detalhe.csv", detalhe);
            Escrever(zip, "totais.csv", totais);
        }

        return new ArquivoDeFechamento("fechamento.zip", "application/zip", memoria.ToArray());
    }

    private static void Escrever(ZipArchive zip, string nome, IReadOnlyList<string[]> linhas)
    {
        var entrada = zip.CreateEntry(nome);
        using var escritor = new StreamWriter(entrada.Open(), Utf8);
        escritor.Write(Cabecalho);
        escritor.Write('\n');
        foreach (var linha in linhas)
        {
            escritor.Write(string.Join(';', linha));
            escritor.Write('\n');
        }
    }

    private static ArquivoDeFechamento Excel(IReadOnlyList<string[]> detalhe, IReadOnlyList<string[]> totais)
    {
        using var pasta = new XLWorkbook();
        Preencher(pasta.Worksheets.Add("Detalhe"), detalhe);
        Preencher(pasta.Worksheets.Add("Totais"), totais);
        using var memoria = new MemoryStream();
        pasta.SaveAs(memoria);
        return new ArquivoDeFechamento(
            "fechamento.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            memoria.ToArray());
    }

    private static void Preencher(IXLWorksheet aba, IReadOnlyList<string[]> linhas)
    {
        var colunas = Cabecalho.Split(';');
        for (var coluna = 0; coluna < colunas.Length; coluna++)
            aba.Cell(1, coluna + 1).Value = colunas[coluna];
        for (var linha = 0; linha < linhas.Count; linha++)
        {
            for (var coluna = 0; coluna < linhas[linha].Length; coluna++)
                aba.Cell(linha + 2, coluna + 1).Value = linhas[linha][coluna];
        }
    }
}
