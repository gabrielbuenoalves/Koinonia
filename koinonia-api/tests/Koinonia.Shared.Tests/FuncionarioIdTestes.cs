using Koinonia.Shared;

namespace Koinonia.Shared.Tests;

public class FuncionarioIdTestes
{
    [Fact]
    public void Novo_gera_guid_preenchido()
    {
        var id = FuncionarioId.Novo();
        Assert.NotEqual(Guid.Empty, id.Valor);
    }

    [Fact]
    public void De_recusa_guid_vazio()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => FuncionarioId.De(Guid.Empty));
        Assert.Equal("Identificador vazio.", erro.Message);
    }
}
