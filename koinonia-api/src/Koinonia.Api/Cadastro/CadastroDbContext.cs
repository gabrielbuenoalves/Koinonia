using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Koinonia.Api.Cadastro;

public sealed class CadastroDbContext : DbContext
{
    public CadastroDbContext(DbContextOptions<CadastroDbContext> options) : base(options) { }

    public DbSet<Funcionario> Funcionarios => Set<Funcionario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Funcionario>(entidade =>
        {
            entidade.ToTable("Funcionarios");
            entidade.HasKey(item => item.Id);
        });
    }
}

public sealed class CadastroDbContextFactory : IDesignTimeDbContextFactory<CadastroDbContext>
{
    public CadastroDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CadastroDbContext>().UseSqlite("Data Source=koinonia.db").Options;
        return new CadastroDbContext(options);
    }
}
