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
