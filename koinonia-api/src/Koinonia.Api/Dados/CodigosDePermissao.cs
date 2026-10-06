namespace Koinonia.Api.Dados;

public static class CodigosDePermissao
{
    public static readonly string[] Telas = ["visao-geral", "ponto", "planejamento", "fechamento", "contas"];

    public static readonly string[] Acoes =
    [
        "registrar-propria-batida", "ver-ponto-de-outro", "cadastrar-funcionario", "definir-expediente",
        "publicar-evento", "configurar-pagamento", "fechar-mes", "reabrir-mes", "baixar-planilha", "gerir-contas"
    ];

    public static IEnumerable<string> Todas => Telas.Concat(Acoes);
}
