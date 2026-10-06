namespace Koinonia.Api.Dados;

public sealed class Usuario
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string SenhaHash { get; set; } = "";
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public sealed class Role
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public List<PermissaoDaRole> Permissoes { get; set; } = [];
}

public sealed class PermissaoDaRole
{
    public Guid RoleId { get; set; }
    public string Codigo { get; set; } = "";
    public Role Role { get; set; } = null!;
}

public sealed class Sessao
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string TokenHash { get; set; } = "";
    public Usuario Usuario { get; set; } = null!;
}

public sealed class Funcionario
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public int MinutosContratados { get; set; }
    public decimal Salario { get; set; }
    public Usuario Usuario { get; set; } = null!;
}

public sealed class Expediente
{
    public Guid Id { get; set; }
    public DateOnly InicioVigencia { get; set; }
    public DateOnly FimVigencia { get; set; }
    public List<IntervaloExpediente> Intervalos { get; set; } = [];
}

public sealed class IntervaloExpediente
{
    public Guid Id { get; set; }
    public Guid ExpedienteId { get; set; }
    public int Inicio { get; set; }
    public int Fim { get; set; }
    public Expediente Expediente { get; set; } = null!;
}

public sealed class Configuracao
{
    public int Id { get; set; }
    public decimal? Percentual { get; set; }
    public int? PrazoDias { get; set; }
}

public sealed class Evento
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public DateTimeOffset Inicio { get; set; }
    public DateTimeOffset Fim { get; set; }
    public string Regra { get; set; } = "";
    public decimal? Percentual { get; set; }
    public int? PrazoVencimentoDias { get; set; }
    public List<ParticipanteEvento> Participantes { get; set; } = [];
}

public sealed class ParticipanteEvento
{
    public Guid EventoId { get; set; }
    public Guid FuncionarioId { get; set; }
    public Evento Evento { get; set; } = null!;
    public Funcionario Funcionario { get; set; } = null!;
}

public sealed class Batida
{
    public Guid Id { get; set; }
    public Guid FuncionarioId { get; set; }
    public DateTimeOffset Entrada { get; set; }
    public DateTimeOffset? Saida { get; set; }
    public Funcionario Funcionario { get; set; } = null!;
}

public sealed class Fechamento
{
    public Guid Id { get; set; }
    public Guid FuncionarioId { get; set; }
    public int Ano { get; set; }
    public int Mes { get; set; }
    public string EscolhaFalta { get; set; } = "";
    public decimal Salario { get; set; }
    public int MinutosContratados { get; set; }
    public decimal ValorHora { get; set; }
    public int MinutosNormais { get; set; }
    public int MinutosFalta { get; set; }
    public int MinutosCobertos { get; set; }
    public int MinutosDescontados { get; set; }
    public decimal Desconto { get; set; }
    public int MinutosExtra { get; set; }
    public decimal PercentualExtra { get; set; }
    public decimal ValorExtra { get; set; }
    public decimal ValorEventos { get; set; }
    public decimal ValorBancoPago { get; set; }
    public decimal Total { get; set; }
    public Funcionario Funcionario { get; set; } = null!;
    public List<LinhaEventoFechamento> Linhas { get; set; } = [];
}

public sealed class LinhaEventoFechamento
{
    public Guid Id { get; set; }
    public Guid FechamentoId { get; set; }
    public Guid? EventoId { get; set; }
    public string Nome { get; set; } = "";
    public string Regra { get; set; } = "";
    public decimal? Percentual { get; set; }
    public int Minutos { get; set; }
    public decimal? Valor { get; set; }
    public int? MinutosCreditados { get; set; }
    public Fechamento Fechamento { get; set; } = null!;
}

public sealed class MovimentoBanco
{
    public Guid Id { get; set; }
    public Guid FuncionarioId { get; set; }
    public string Tipo { get; set; } = "";
    public int Minutos { get; set; }
    public decimal? ValorHora { get; set; }
    public DateOnly? VenceEm { get; set; }
    public DateOnly? Dia { get; set; }
    public Guid? EventoId { get; set; }
    public Guid? CreditoId { get; set; }
    public Guid? FechamentoId { get; set; }
    public decimal? Valor { get; set; }
    public Funcionario Funcionario { get; set; } = null!;
}
