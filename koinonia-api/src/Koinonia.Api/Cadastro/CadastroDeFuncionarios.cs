using Koinonia.Shared;

namespace Koinonia.Api.Cadastro;

public sealed record FuncionarioCadastrado(FuncionarioId Id, string Nome);

public sealed class CadastroDeFuncionarios
{
    private readonly CadastroDbContext _contexto;

    public CadastroDeFuncionarios(CadastroDbContext contexto) => _contexto = contexto;

    public FuncionarioCadastrado Cadastrar(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ViolacaoDeRegra("Nome inválido.");
        var row = new Funcionario { Id = Guid.NewGuid(), Nome = nome.Trim() };
        _contexto.Funcionarios.Add(row);
        _contexto.SaveChanges();
        return new FuncionarioCadastrado(FuncionarioId.De(row.Id), row.Nome);
    }

    public bool Existe(FuncionarioId id) => _contexto.Funcionarios.Any(item => item.Id == id.Valor);

    public IReadOnlyList<FuncionarioCadastrado> Listar()
        => _contexto.Funcionarios.OrderBy(item => item.Nome).Select(item => new FuncionarioCadastrado(FuncionarioId.De(item.Id), item.Nome)).ToList();

    public string ObterNome(FuncionarioId id)
        => _contexto.Funcionarios.Where(item => item.Id == id.Valor).Select(item => item.Nome).FirstOrDefault()
           ?? throw new ViolacaoDeRegra("Funcionário desconhecido é recusado no host.");
}
