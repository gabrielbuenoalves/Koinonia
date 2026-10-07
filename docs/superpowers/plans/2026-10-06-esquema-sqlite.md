# Esquema SQLite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Criar no SQLite atual o esquema único do spec `docs/superpowers/specs/2026-10-06-banco-de-dados-design.md`, sem desligar o ponto que já roda.

**Architecture:** Um `KoinoniaDbContext` novo no host grava as tabelas do spec no mesmo arquivo `koinonia.db`. Os três contextos antigos continuam migrando as tabelas velhas. O código de ponto, escala e fechamento de hoje não muda neste plano. Login, cálculo e planilha ficam para os planos seguintes, nesta ordem: contas e sessão, depois jornada, depois fechamento.

**Tech Stack:** .NET 10, EF Core 10.0.12, SQLite, xUnit.

## Global Constraints

- Provedor: SQLite. Connection string atual: `Data Source=koinonia.db`.
- Fuso do domínio, quando o cálculo chegar: `America/Sao_Paulo`. Este plano não calcula jornada.
- Domínio continua sem referência a `Microsoft.EntityFrameworkCore`.
- Aplicação de Planejamento e de Ponto continua sem referência ao Entity Framework.
- Ponto não referencia Planejamento, e o inverso também.
- Tabelas novas usam o nome do spec, no singular: `Usuario`, `Role`, `PermissaoDaRole`, `Sessao`, `Funcionario`, `Expediente`, `IntervaloExpediente`, `Configuracao`, `Evento`, `ParticipanteEvento`, `Batida`, `Fechamento`, `LinhaEventoFechamento`, `MovimentoBanco`.
- Não dropar `Funcionarios`, `Escalas`, `Eventos`, `Batidas`, `DiasDePonto` nem `Trechos`.
- `Configuracao` nasce uma linha só, com percentual e prazo vazios.
- Roles semeadas, como dado: `Funcionário` com `visao-geral`, `ponto`, `registrar-propria-batida`. `Gestão` com as cinco telas e todas as ações.
- Códigos de tela: `visao-geral`, `ponto`, `planejamento`, `fechamento`, `contas`.
- Códigos de ação: `registrar-propria-batida`, `ver-ponto-de-outro`, `cadastrar-funcionario`, `definir-expediente`, `publicar-evento`, `configurar-pagamento`, `fechar-mes`, `reabrir-mes`, `baixar-planilha`, `gerir-contas`.
- Chave estrangeira ligada. Apagar role com usuário é recusado pelo banco. `Funcionario.UsuarioId` é único.

---

### Task 1: Contexto e tabelas

**Files:**
- Create: `koinonia-api/src/Koinonia.Api/Dados/CodigosDePermissao.cs`
- Create: `koinonia-api/src/Koinonia.Api/Dados/Entidades.cs`
- Create: `koinonia-api/src/Koinonia.Api/Dados/KoinoniaDbContext.cs`
- Create: `koinonia-api/src/Koinonia.Api/Dados/Migrations/` (gerada pelo EF)
- Modify: `koinonia-api/src/Koinonia.Api/Program.cs`
- Test: `koinonia-api/tests/Koinonia.Api.Tests/EsquemaTestes.cs`

**Interfaces:**
- Consumes: nada
- Produces: `KoinoniaDbContext`, entidades em `Koinonia.Api.Dados`, `CodigosDePermissao.Todas`

- [ ] **Step 1: Write the failing test**

Create `koinonia-api/tests/Koinonia.Api.Tests/EsquemaTestes.cs`:

```csharp
using Koinonia.Api.Dados;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Koinonia.Api.Tests;

public class EsquemaTestes : IDisposable
{
    private readonly string _arquivo = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
    private readonly KoinoniaDbContext _contexto;

    public EsquemaTestes()
    {
        var options = new DbContextOptionsBuilder<KoinoniaDbContext>().UseSqlite($"Data Source={_arquivo}").Options;
        _contexto = new KoinoniaDbContext(options);
        _contexto.Database.Migrate();
    }

    [Fact]
    public void Migra_as_tabelas_do_spec()
    {
        var tabelas = _contexto.Database
            .SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite%' ORDER BY name")
            .ToList();

        Assert.Equal(
            new[]
            {
                "Batida", "Configuracao", "Evento", "Expediente", "Fechamento", "Funcionario",
                "IntervaloExpediente", "LinhaEventoFechamento", "MovimentoBanco", "ParticipanteEvento",
                "PermissaoDaRole", "Role", "Sessao", "Usuario"
            },
            tabelas.Where(nome => nome != "__EFMigrationsHistory").ToArray());
    }

    [Fact]
    public void Email_de_usuario_e_unico_e_funcionario_aponta_para_um_usuario()
    {
        var indices = _contexto.Database
            .SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type = 'index' AND sql LIKE '%UNIQUE%'")
            .ToList();
        Assert.Contains(indices, nome => nome.Contains("Email", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(indices, nome => nome.Contains("UsuarioId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Configuracao_nasce_uma_linha_vazia()
    {
        var linha = Assert.Single(_contexto.Configuracoes);
        Assert.Null(linha.Percentual);
        Assert.Null(linha.PrazoDias);
    }

    [Fact]
    public void Chave_estrangeira_recusa_funcionario_sem_usuario()
    {
        _contexto.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
        var erro = Record.Exception(() => _contexto.Database.ExecuteSqlRaw(
            "INSERT INTO Funcionario (Id, UsuarioId, MinutosContratados, Salario) VALUES ({0}, {1}, 9600, '3000')",
            Guid.NewGuid(),
            Guid.NewGuid()));
        Assert.NotNull(erro);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_arquivo))
            File.Delete(_arquivo);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run from `koinonia-api`:

```bash
dotnet test tests/Koinonia.Api.Tests/Koinonia.Api.Tests.csproj --filter EsquemaTestes --nologo
```

Expected: FAIL because `Koinonia.Api.Dados` does not exist.

- [ ] **Step 3: Write minimal implementation**

Create `koinonia-api/src/Koinonia.Api/Dados/CodigosDePermissao.cs`:

```csharp
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
```

Create `koinonia-api/src/Koinonia.Api/Dados/Entidades.cs`:

```csharp
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
```

Create `koinonia-api/src/Koinonia.Api/Dados/KoinoniaDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Koinonia.Api.Dados;

public sealed class KoinoniaDbContext : DbContext
{
    public KoinoniaDbContext(DbContextOptions<KoinoniaDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<PermissaoDaRole> Permissoes => Set<PermissaoDaRole>();
    public DbSet<Sessao> Sessoes => Set<Sessao>();
    public DbSet<Funcionario> Funcionarios => Set<Funcionario>();
    public DbSet<Expediente> Expedientes => Set<Expediente>();
    public DbSet<IntervaloExpediente> IntervalosExpediente => Set<IntervaloExpediente>();
    public DbSet<Configuracao> Configuracoes => Set<Configuracao>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<ParticipanteEvento> Participantes => Set<ParticipanteEvento>();
    public DbSet<Batida> Batidas => Set<Batida>();
    public DbSet<Fechamento> Fechamentos => Set<Fechamento>();
    public DbSet<LinhaEventoFechamento> LinhasEvento => Set<LinhaEventoFechamento>();
    public DbSet<MovimentoBanco> MovimentosBanco => Set<MovimentoBanco>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entidade =>
        {
            entidade.ToTable("Usuario");
            entidade.HasIndex(item => item.Email).IsUnique();
            entidade.HasOne(item => item.Role).WithMany().HasForeignKey(item => item.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(entidade =>
        {
            entidade.ToTable("Role");
            entidade.HasIndex(item => item.Nome).IsUnique();
        });

        modelBuilder.Entity<PermissaoDaRole>(entidade =>
        {
            entidade.ToTable("PermissaoDaRole");
            entidade.HasKey(item => new { item.RoleId, item.Codigo });
            entidade.HasOne(item => item.Role).WithMany(item => item.Permissoes).HasForeignKey(item => item.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Sessao>(entidade =>
        {
            entidade.ToTable("Sessao");
            entidade.HasIndex(item => item.TokenHash).IsUnique();
            entidade.HasOne(item => item.Usuario).WithMany().HasForeignKey(item => item.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Funcionario>(entidade =>
        {
            entidade.ToTable("Funcionario");
            entidade.Property(item => item.Salario).HasConversion<string>();
            entidade.HasIndex(item => item.UsuarioId).IsUnique();
            entidade.HasOne(item => item.Usuario).WithMany().HasForeignKey(item => item.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Expediente>(entidade => entidade.ToTable("Expediente"));

        modelBuilder.Entity<IntervaloExpediente>(entidade =>
        {
            entidade.ToTable("IntervaloExpediente");
            entidade.HasOne(item => item.Expediente).WithMany(item => item.Intervalos).HasForeignKey(item => item.ExpedienteId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Configuracao>(entidade =>
        {
            entidade.ToTable("Configuracao");
            entidade.Property(item => item.Percentual).HasConversion<string>();
            entidade.HasData(new Configuracao { Id = 1 });
        });

        modelBuilder.Entity<Evento>(entidade =>
        {
            entidade.ToTable("Evento");
            entidade.Property(item => item.Percentual).HasConversion<string>();
        });

        modelBuilder.Entity<ParticipanteEvento>(entidade =>
        {
            entidade.ToTable("ParticipanteEvento");
            entidade.HasKey(item => new { item.EventoId, item.FuncionarioId });
            entidade.HasOne(item => item.Evento).WithMany(item => item.Participantes).HasForeignKey(item => item.EventoId).OnDelete(DeleteBehavior.Cascade);
            entidade.HasOne(item => item.Funcionario).WithMany().HasForeignKey(item => item.FuncionarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Batida>(entidade =>
        {
            entidade.ToTable("Batida");
            entidade.HasOne(item => item.Funcionario).WithMany().HasForeignKey(item => item.FuncionarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Fechamento>(entidade =>
        {
            entidade.ToTable("Fechamento");
            entidade.HasIndex(item => new { item.FuncionarioId, item.Ano, item.Mes }).IsUnique();
            entidade.Property(item => item.Salario).HasConversion<string>();
            entidade.Property(item => item.ValorHora).HasConversion<string>();
            entidade.Property(item => item.Desconto).HasConversion<string>();
            entidade.Property(item => item.PercentualExtra).HasConversion<string>();
            entidade.Property(item => item.ValorExtra).HasConversion<string>();
            entidade.Property(item => item.ValorEventos).HasConversion<string>();
            entidade.Property(item => item.ValorBancoPago).HasConversion<string>();
            entidade.Property(item => item.Total).HasConversion<string>();
            entidade.HasOne(item => item.Funcionario).WithMany().HasForeignKey(item => item.FuncionarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LinhaEventoFechamento>(entidade =>
        {
            entidade.ToTable("LinhaEventoFechamento");
            entidade.Property(item => item.Percentual).HasConversion<string>();
            entidade.Property(item => item.Valor).HasConversion<string>();
            entidade.HasOne(item => item.Fechamento).WithMany(item => item.Linhas).HasForeignKey(item => item.FechamentoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MovimentoBanco>(entidade =>
        {
            entidade.ToTable("MovimentoBanco");
            entidade.Property(item => item.ValorHora).HasConversion<string>();
            entidade.Property(item => item.Valor).HasConversion<string>();
            entidade.HasOne(item => item.Funcionario).WithMany().HasForeignKey(item => item.FuncionarioId).OnDelete(DeleteBehavior.Restrict);
            entidade.HasOne<Evento>().WithMany().HasForeignKey(item => item.EventoId).OnDelete(DeleteBehavior.Restrict);
            entidade.HasOne<MovimentoBanco>().WithMany().HasForeignKey(item => item.CreditoId).OnDelete(DeleteBehavior.Restrict);
            entidade.HasOne<Fechamento>().WithMany().HasForeignKey(item => item.FechamentoId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public sealed class KoinoniaDbContextFactory : IDesignTimeDbContextFactory<KoinoniaDbContext>
{
    public KoinoniaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<KoinoniaDbContext>().UseSqlite("Data Source=koinonia.db").Options;
        return new KoinoniaDbContext(options);
    }
}
```

In `Program.cs`, register and migrate the new context next to the three existing ones. Add `using Koinonia.Api.Dados;`. Inside `AddDbContext` block:

```csharp
builder.Services.AddDbContext<KoinoniaDbContext>(opcoes => opcoes.UseSqlite(conexao));
```

Inside the migrate scope:

```csharp
escopo.ServiceProvider.GetRequiredService<KoinoniaDbContext>().Database.Migrate();
```

Generate the migration from `koinonia-api`:

```bash
dotnet ef migrations add EsquemaUnico --context KoinoniaDbContext --project src/Koinonia.Api --startup-project src/Koinonia.Api --output-dir Dados/Migrations
```

- [ ] **Step 4: Run test to verify it passes**

Run from `koinonia-api`:

```bash
dotnet test tests/Koinonia.Api.Tests/Koinonia.Api.Tests.csproj --filter EsquemaTestes --nologo
```

Expected: PASS, 4 tests.

- [ ] **Step 5: Commit**

```bash
git add koinonia-api/src/Koinonia.Api/Dados koinonia-api/src/Koinonia.Api/Program.cs koinonia-api/tests/Koinonia.Api.Tests/EsquemaTestes.cs
git commit -m "feat: cria o esquema unico no sqlite"
```

---

### Task 2: Roles semeadas

**Files:**
- Create: `koinonia-api/src/Koinonia.Api/Dados/SemeadorDeRoles.cs`
- Modify: `koinonia-api/src/Koinonia.Api/Program.cs`
- Test: `koinonia-api/tests/Koinonia.Api.Tests/SemeadorDeRolesTestes.cs`

**Interfaces:**
- Consumes: `KoinoniaDbContext`, `CodigosDePermissao.Todas`, `CodigosDePermissao.Telas`
- Produces: `SemeadorDeRoles.Semear(KoinoniaDbContext)`

- [ ] **Step 1: Write the failing test**

Create `koinonia-api/tests/Koinonia.Api.Tests/SemeadorDeRolesTestes.cs`:

```csharp
using Koinonia.Api.Dados;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Koinonia.Api.Tests;

public class SemeadorDeRolesTestes : IDisposable
{
    private readonly string _arquivo = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
    private readonly KoinoniaDbContext _contexto;

    public SemeadorDeRolesTestes()
    {
        var options = new DbContextOptionsBuilder<KoinoniaDbContext>().UseSqlite($"Data Source={_arquivo}").Options;
        _contexto = new KoinoniaDbContext(options);
        _contexto.Database.Migrate();
    }

    [Fact]
    public void Semear_cria_funcionario_e_gestao()
    {
        SemeadorDeRoles.Semear(_contexto);

        var funcionario = _contexto.Roles.Include(item => item.Permissoes).Single(item => item.Nome == "Funcionário");
        var gestao = _contexto.Roles.Include(item => item.Permissoes).Single(item => item.Nome == "Gestão");

        Assert.Equal(["ponto", "registrar-propria-batida", "visao-geral"], funcionario.Permissoes.Select(item => item.Codigo).Order().ToArray());
        Assert.Equal(CodigosDePermissao.Todas.Order().ToArray(), gestao.Permissoes.Select(item => item.Codigo).Order().ToArray());
    }

    [Fact]
    public void Semear_de_novo_nao_duplica()
    {
        SemeadorDeRoles.Semear(_contexto);
        SemeadorDeRoles.Semear(_contexto);
        Assert.Equal(2, _contexto.Roles.Count());
    }

    public void Dispose()
    {
        _contexto.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_arquivo))
            File.Delete(_arquivo);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run from `koinonia-api`:

```bash
dotnet test tests/Koinonia.Api.Tests/Koinonia.Api.Tests.csproj --filter SemeadorDeRolesTestes --nologo
```

Expected: FAIL because `SemeadorDeRoles` does not exist.

- [ ] **Step 3: Write minimal implementation**

Create `koinonia-api/src/Koinonia.Api/Dados/SemeadorDeRoles.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace Koinonia.Api.Dados;

public static class SemeadorDeRoles
{
    public static void Semear(KoinoniaDbContext contexto)
    {
        Garantir("Funcionário", ["visao-geral", "ponto", "registrar-propria-batida"]);
        Garantir("Gestão", CodigosDePermissao.Todas);
        contexto.SaveChanges();

        void Garantir(string nome, IEnumerable<string> codigos)
        {
            var role = contexto.Roles.Include(item => item.Permissoes).SingleOrDefault(item => item.Nome == nome);
            if (role is null)
            {
                role = new Role { Id = Guid.NewGuid(), Nome = nome };
                contexto.Roles.Add(role);
            }

            var presentes = role.Permissoes.Select(item => item.Codigo).ToHashSet();
            foreach (var codigo in codigos.Where(codigo => !presentes.Contains(codigo)))
                role.Permissoes.Add(new PermissaoDaRole { Codigo = codigo });
        }
    }
}
```

In the migrate scope of `Program.cs`, after `KoinoniaDbContext.Database.Migrate()`:

```csharp
SemeadorDeRoles.Semear(escopo.ServiceProvider.GetRequiredService<KoinoniaDbContext>());
```

- [ ] **Step 4: Run test to verify it passes**

Run from `koinonia-api`:

```bash
dotnet test tests/Koinonia.Api.Tests/Koinonia.Api.Tests.csproj --filter SemeadorDeRolesTestes --nologo
```

Expected: PASS, 2 tests.

- [ ] **Step 5: Commit**

```bash
git add koinonia-api/src/Koinonia.Api/Dados/SemeadorDeRoles.cs koinonia-api/src/Koinonia.Api/Program.cs koinonia-api/tests/Koinonia.Api.Tests/SemeadorDeRolesTestes.cs
git commit -m "feat: semeia as roles funcionario e gestao"
```

---

### Task 3: O arquivo em uso recebe o esquema

**Files:**
- Modify: none beyond what Task 1 already migrates on startup
- Test: the two test classes above

**Interfaces:**
- Consumes: `KoinoniaDbContext.Database.Migrate()` and `SemeadorDeRoles.Semear`
- Produces: `koinonia-api/src/Koinonia.Api/koinonia.db` with the new tables when the API starts

- [ ] **Step 1: Run the whole API suite**

From `koinonia-api`:

```bash
dotnet test tests/Koinonia.Api.Tests/Koinonia.Api.Tests.csproj --nologo
```

Expected: PASS, including `EsquemaTestes` and `SemeadorDeRolesTestes`. The old ponto tests still pass.

- [ ] **Step 2: Apply the migration to the local file**

Stop any API already using `koinonia-api/src/Koinonia.Api/koinonia.db`. From `koinonia-api`:

```bash
dotnet run --project src/Koinonia.Api/Koinonia.Api.csproj --urls http://localhost:5188 --no-launch-profile
```

Expected: the process listens. `Program` calls `KoinoniaDbContext.Database.Migrate()` and `SemeadorDeRoles.Semear`. Stop the process after it is listening.

- [ ] **Step 3: Commit**

No new commit. Do not commit `koinonia.db`.

---

## Self-review notes

Spec coverage neste plano: tabelas, chaves, unicidade, linha vazia de `Configuracao`, roles semeadas. Fora deste plano, de propósito: login, sessão HTTP, permissão 403, expediente, evento, batida, fechamento, banco de horas e planilha. Esses blocos dependem deste esquema e entram nos dois planos seguintes.

Placeholder scan: limpo. O gate do arquivo local é a API subindo e migrando `koinonia.db`. O gate dos testes é `dotnet test` em `Koinonia.Api.Tests`.
