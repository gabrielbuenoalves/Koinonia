using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Koinonia.Planejamento.Infrastructure;

public sealed class PlanejamentoDbContext : DbContext
{
    public PlanejamentoDbContext(DbContextOptions<PlanejamentoDbContext> options) : base(options) { }

    public DbSet<EscalaRow> Escalas => Set<EscalaRow>();
    public DbSet<IntervaloEscalaRow> Intervalos => Set<IntervaloEscalaRow>();
    public DbSet<EventoRow> Eventos => Set<EventoRow>();
    public DbSet<EventoEscaladoRow> Escalados => Set<EventoEscaladoRow>();
    public DbSet<PercentualRow> Percentual => Set<PercentualRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EscalaRow>(entidade =>
        {
            entidade.ToTable("Escalas");
            entidade.HasKey(item => item.Id);
            entidade.HasIndex(item => new { item.FuncionarioId, item.Dia }).IsUnique();
            entidade.HasMany(item => item.Intervalos).WithOne().HasForeignKey(item => item.EscalaId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<IntervaloEscalaRow>(entidade =>
        {
            entidade.ToTable("IntervalosEscala");
            entidade.HasKey(item => item.Id);
        });
        modelBuilder.Entity<EventoRow>(entidade =>
        {
            entidade.ToTable("Eventos");
            entidade.HasKey(item => item.Id);
            entidade.HasMany(item => item.Escalados).WithOne().HasForeignKey(item => item.EventoId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<EventoEscaladoRow>(entidade =>
        {
            entidade.ToTable("EventosEscalados");
            entidade.HasKey(item => new { item.EventoId, item.FuncionarioId });
        });
        modelBuilder.Entity<PercentualRow>(entidade =>
        {
            entidade.ToTable("PercentualExtraComum");
            entidade.HasKey(item => item.Id);
        });
    }
}

public sealed class EscalaRow
{
    public Guid Id { get; set; }
    public Guid FuncionarioId { get; set; }
    public DateOnly Dia { get; set; }
    public List<IntervaloEscalaRow> Intervalos { get; set; } = [];
}

public sealed class IntervaloEscalaRow
{
    public Guid Id { get; set; }
    public Guid EscalaId { get; set; }
    public int Inicio { get; set; }
    public int Fim { get; set; }
}

public sealed class EventoRow
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public DateTimeOffset Inicio { get; set; }
    public DateTimeOffset Fim { get; set; }
    public int Forma { get; set; }
    public decimal? Percentual { get; set; }
    public decimal? Fator { get; set; }
    public List<EventoEscaladoRow> Escalados { get; set; } = [];
}

public sealed class EventoEscaladoRow
{
    public Guid EventoId { get; set; }
    public Guid FuncionarioId { get; set; }
}

public sealed class PercentualRow
{
    public int Id { get; set; }
    public decimal Valor { get; set; }
}

public sealed class PlanejamentoDbContextFactory : IDesignTimeDbContextFactory<PlanejamentoDbContext>
{
    public PlanejamentoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlanejamentoDbContext>()
            .UseSqlite("Data Source=koinonia.db")
            .Options;
        return new PlanejamentoDbContext(options);
    }
}
