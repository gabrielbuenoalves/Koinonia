using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Koinonia.Ponto.Infrastructure;

public sealed class PontoDbContext : DbContext
{
    public PontoDbContext(DbContextOptions<PontoDbContext> options) : base(options) { }

    public DbSet<BatidaRow> Batidas => Set<BatidaRow>();
    public DbSet<DiaRow> Dias => Set<DiaRow>();
    public DbSet<TrechoRow> Trechos => Set<TrechoRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BatidaRow>(entidade =>
        {
            entidade.ToTable("Batidas");
            entidade.HasKey(item => item.Id);
        });
        modelBuilder.Entity<DiaRow>(entidade =>
        {
            entidade.ToTable("DiasDePonto");
            entidade.HasKey(item => item.Id);
            entidade.HasIndex(item => new { item.FuncionarioId, item.Dia }).IsUnique();
            entidade.HasMany(item => item.Trechos).WithOne().HasForeignKey(item => item.DiaId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<TrechoRow>(entidade =>
        {
            entidade.ToTable("Trechos");
            entidade.HasKey(item => item.Id);
        });
    }
}

public sealed class BatidaRow
{
    public Guid Id { get; set; }
    public Guid FuncionarioId { get; set; }
    public DateTimeOffset Entrada { get; set; }
    public DateTimeOffset? Saida { get; set; }
}

public sealed class DiaRow
{
    public Guid Id { get; set; }
    public Guid FuncionarioId { get; set; }
    public DateOnly Dia { get; set; }
    public bool Aprovado { get; set; }
    public List<TrechoRow> Trechos { get; set; } = [];
}

public sealed class TrechoRow
{
    public Guid Id { get; set; }
    public Guid DiaId { get; set; }
    public int Inicio { get; set; }
    public int Fim { get; set; }
    public int Fatia { get; set; }
    public Guid? EventoId { get; set; }
    public string? NomeEvento { get; set; }
    public int? Forma { get; set; }
    public decimal? Percentual { get; set; }
    public decimal? Fator { get; set; }
    public int MinutosTrabalhados { get; set; }
    public int? MinutosCreditados { get; set; }
}

public sealed class PontoDbContextFactory : IDesignTimeDbContextFactory<PontoDbContext>
{
    public PontoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PontoDbContext>().UseSqlite("Data Source=koinonia.db").Options;
        return new PontoDbContext(options);
    }
}
