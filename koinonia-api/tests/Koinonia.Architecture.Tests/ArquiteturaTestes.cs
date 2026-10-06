using System.Reflection;
using Koinonia.Planejamento.Domain;
using Koinonia.Ponto.Domain;
using NetArchTest.Rules;

namespace Koinonia.Architecture.Tests;

public class ArquiteturaTestes
{
    [Fact]
    public void Ponto_nao_referencia_Planejamento()
    {
        foreach (var assembly in new[] { typeof(ClassificarHoras).Assembly, Assembly.Load("Koinonia.Ponto.Application"), Assembly.Load("Koinonia.Ponto.Infrastructure") })
        {
            var resultado = Types.InAssembly(assembly).ShouldNot().HaveDependencyOn("Koinonia.Planejamento").GetResult();
            Assert.True(resultado.IsSuccessful, assembly.GetName().Name);
        }
    }

    [Fact]
    public void Planejamento_nao_referencia_Ponto()
    {
        foreach (var assembly in new[] { typeof(EscalaDoDia).Assembly, Assembly.Load("Koinonia.Planejamento.Application"), Assembly.Load("Koinonia.Planejamento.Infrastructure") })
        {
            var resultado = Types.InAssembly(assembly).ShouldNot().HaveDependencyOn("Koinonia.Ponto").GetResult();
            Assert.True(resultado.IsSuccessful, assembly.GetName().Name);
        }
    }

    [Fact]
    public void Domain_nao_referencia_Entity_Framework()
    {
        foreach (var assembly in new[] { typeof(ClassificarHoras).Assembly, typeof(EscalaDoDia).Assembly })
        {
            var resultado = Types.InAssembly(assembly).ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore").GetResult();
            Assert.True(resultado.IsSuccessful, assembly.GetName().Name);
        }
    }

    [Fact]
    public void Csproj_nao_cruza_modulos()
    {
        var raiz = AcharRaiz();
        Assert.DoesNotContain("Koinonia.Planejamento", File.ReadAllText(Path.Combine(raiz, "src", "Koinonia.Ponto.Domain", "Koinonia.Ponto.Domain.csproj")));
        Assert.DoesNotContain("Koinonia.Ponto", File.ReadAllText(Path.Combine(raiz, "src", "Koinonia.Planejamento.Domain", "Koinonia.Planejamento.Domain.csproj")));
        Assert.DoesNotContain("Infrastructure", File.ReadAllText(Path.Combine(raiz, "src", "Koinonia.Ponto.Domain", "Koinonia.Ponto.Domain.csproj")));
    }

    private static string AcharRaiz()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio is not null && !File.Exists(Path.Combine(diretorio.FullName, "Koinonia.sln")))
            diretorio = diretorio.Parent;
        return diretorio?.FullName ?? throw new InvalidOperationException("Solução não encontrada.");
    }
}
