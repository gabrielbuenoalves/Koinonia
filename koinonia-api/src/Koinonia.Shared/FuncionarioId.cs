namespace Koinonia.Shared;

public readonly record struct FuncionarioId(Guid Valor)
{
    public static FuncionarioId Novo() => new(Guid.NewGuid());

    public static FuncionarioId De(Guid valor)
    {
        if (valor == Guid.Empty)
            throw new ViolacaoDeRegra("Identificador vazio.");
        return new FuncionarioId(valor);
    }
}

public sealed class ViolacaoDeRegra : Exception
{
    public ViolacaoDeRegra(string mensagem) : base(mensagem) { }
}
