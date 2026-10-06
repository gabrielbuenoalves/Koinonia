using Koinonia.Api;
using Koinonia.Api.Cadastro;
using Koinonia.Planejamento.Application;
using Koinonia.Planejamento.Domain;
using Koinonia.Planejamento.Infrastructure;
using Koinonia.Ponto.Application;
using Koinonia.Ponto.Infrastructure;
using Koinonia.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var conexao = builder.Configuration.GetConnectionString("Koinonia") ?? "Data Source=koinonia.db";
builder.Services.AddDbContext<CadastroDbContext>(opcoes => opcoes.UseSqlite(conexao));
builder.Services.AddDbContext<PlanejamentoDbContext>(opcoes => opcoes.UseSqlite(conexao));
builder.Services.AddDbContext<PontoDbContext>(opcoes => opcoes.UseSqlite(conexao));
builder.Services.AddScoped<RepositorioDePlanejamento>();
builder.Services.AddScoped<IRepositorioDePlanejamento>(provedor => provedor.GetRequiredService<RepositorioDePlanejamento>());
builder.Services.AddScoped<ServicoDePlanejamento>();
builder.Services.AddScoped<ILeituraDePlanejamento>(provedor => provedor.GetRequiredService<ServicoDePlanejamento>());
builder.Services.AddScoped<RepositorioDePonto>();
builder.Services.AddScoped<IRepositorioDePonto>(provedor => provedor.GetRequiredService<RepositorioDePonto>());
builder.Services.AddScoped<IConsultaDePlanejamento, ConsultaDePlanejamentoAdapter>();
builder.Services.AddScoped<ServicoDePonto>();
builder.Services.AddScoped<CadastroDeFuncionarios>();
builder.Services.AddSingleton<IRelogio, RelogioDoSistema>();
builder.Services.AddCors(opcoes => opcoes.AddDefaultPolicy(politica =>
    politica.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
using (var escopo = app.Services.CreateScope())
{
    escopo.ServiceProvider.GetRequiredService<CadastroDbContext>().Database.Migrate();
    escopo.ServiceProvider.GetRequiredService<PlanejamentoDbContext>().Database.Migrate();
    escopo.ServiceProvider.GetRequiredService<PontoDbContext>().Database.Migrate();
}

app.UseCors();
app.Use(async (contexto, proximo) =>
{
    try
    {
        await proximo();
    }
    catch (ViolacaoDeRegra erro)
    {
        contexto.Response.StatusCode = 422;
        await contexto.Response.WriteAsync(erro.Message);
    }
});

app.MapPost("/funcionarios", (NomeBody body, HttpRequest request, CadastroDeFuncionarios cadastro) =>
{
    ExigirGestao(request, cadastro);
    var criado = cadastro.Cadastrar(body.Nome);
    return Results.Ok(new { id = criado.Id.Valor, nome = criado.Nome });
});

app.MapGet("/funcionarios", (HttpRequest request, CadastroDeFuncionarios cadastro) =>
{
    ExigirGestao(request, cadastro);
    return Results.Ok(cadastro.Listar().Select(item => new { id = item.Id.Valor, nome = item.Nome }));
});

app.MapPut("/escalas", (EscalaBody body, HttpRequest request, CadastroDeFuncionarios cadastro, ServicoDePlanejamento servico) =>
{
    ExigirGestao(request, cadastro);
    var pessoa = ExigirExistente(body.FuncionarioId, cadastro);
    servico.PublicarEscala(pessoa, DateOnly.Parse(body.Dia), body.Intervalos.Select(item => new IntervaloDeEscala(item.Inicio, item.Fim)).ToList());
    return Results.NoContent();
});

app.MapDelete("/escalas/{funcionarioId:guid}/{dia}", (Guid funcionarioId, DateOnly dia, HttpRequest request, CadastroDeFuncionarios cadastro, ServicoDePlanejamento servico) =>
{
    ExigirGestao(request, cadastro);
    servico.RemoverEscala(ExigirExistente(funcionarioId, cadastro), dia);
    return Results.NoContent();
});

app.MapPost("/eventos", (EventoBody body, HttpRequest request, CadastroDeFuncionarios cadastro, ServicoDePlanejamento servico) =>
{
    ExigirGestao(request, cadastro);
    var pessoas = body.FuncionarioIds.Select(id => ExigirExistente(id, cadastro)).ToList();
    var id = servico.PublicarEvento(body.Nome, body.Inicio, body.Fim, pessoas, Tratamento(body.Forma, body.Percentual, body.Fator));
    return Results.Ok(new { id });
});

app.MapPut("/eventos/{id:guid}", (Guid id, EventoBody body, HttpRequest request, CadastroDeFuncionarios cadastro, ServicoDePlanejamento servico) =>
{
    ExigirGestao(request, cadastro);
    var pessoas = body.FuncionarioIds.Select(item => ExigirExistente(item, cadastro)).ToList();
    servico.AlterarEvento(id, body.Nome, body.Inicio, body.Fim, pessoas, Tratamento(body.Forma, body.Percentual, body.Fator));
    return Results.NoContent();
});

app.MapPut("/percentual-extra-comum", (PercentualBody body, HttpRequest request, CadastroDeFuncionarios cadastro, ServicoDePlanejamento servico) =>
{
    ExigirGestao(request, cadastro);
    servico.DefinirPercentual(body.Valor);
    return Results.NoContent();
});

app.MapPost("/batidas/entrada", (EntradaBody body, HttpRequest request, CadastroDeFuncionarios cadastro, ServicoDePonto ponto) =>
{
    var ator = ExigirEmpregado(request, cadastro);
    var id = ponto.RegistrarEntrada(ator, body.Entrada);
    return Results.Ok(new { id });
});

app.MapPost("/batidas/{id:guid}/saida", (Guid id, SaidaBody body, HttpRequest request, CadastroDeFuncionarios cadastro, ServicoDePonto ponto) =>
{
    var ator = ExigirEmpregado(request, cadastro);
    ponto.RegistrarSaida(id, ator, body.Saida);
    return Results.NoContent();
});

app.MapDelete("/batidas/{id:guid}", (Guid id, HttpRequest request, CadastroDeFuncionarios cadastro, ServicoDePonto ponto) =>
{
    var ator = ExigirEmpregado(request, cadastro);
    ponto.RemoverBatida(id, ator);
    return Results.NoContent();
});

app.MapGet("/espelho/{funcionarioId:guid}/{dia}", (Guid funcionarioId, DateOnly dia, HttpRequest request, CadastroDeFuncionarios cadastro, ServicoDePonto ponto) =>
{
    var pessoa = ExigirProprioOuGestao(funcionarioId, request, cadastro);
    var espelho = ponto.ObterEspelho(pessoa, dia);
    return Results.Ok(new
    {
        aprovado = espelho.Aprovado,
        trechos = espelho.Trechos.Select(item => new
        {
            inicio = item.Inicio,
            fim = item.Fim,
            fatia = item.Fatia.ToString(),
            evento = item.NomeEvento,
            percentual = item.Percentual,
            fator = item.Fator,
            minutosTrabalhados = item.MinutosTrabalhados,
            minutosCreditados = item.MinutosCreditados
        })
    });
});

app.MapPost("/dias/{funcionarioId:guid}/{dia}/aprovacao", (Guid funcionarioId, DateOnly dia, HttpRequest request, CadastroDeFuncionarios cadastro, ServicoDePonto ponto) =>
{
    ExigirGestao(request, cadastro);
    ponto.Aprovar(ExigirExistente(funcionarioId, cadastro), dia);
    return Results.NoContent();
});

app.MapDelete("/dias/{funcionarioId:guid}/{dia}/aprovacao", (Guid funcionarioId, DateOnly dia, HttpRequest request, CadastroDeFuncionarios cadastro, ServicoDePonto ponto) =>
{
    ExigirGestao(request, cadastro);
    ponto.Reabrir(ExigirExistente(funcionarioId, cadastro), dia);
    return Results.NoContent();
});

app.MapGet("/fechamento", (DateOnly inicio, DateOnly fim, string formato, HttpRequest request, CadastroDeFuncionarios cadastro, IRepositorioDePonto repositorio) =>
{
    ExigirGestao(request, cadastro);
    var arquivo = ExportadorDeFechamento.Gerar(
        repositorio.ListarAprovados(inicio, fim),
        cadastro.ObterNome,
        formato == "excel" ? FormatoDeFechamento.Excel : FormatoDeFechamento.Csv);
    return Results.File(arquivo.Conteudo, arquivo.TipoConteudo, arquivo.NomeDoArquivo);
});

app.Run();

static void ExigirGestao(HttpRequest request, CadastroDeFuncionarios cadastro)
{
    var (_, gestao) = Identidade(request, cadastro);
    if (!gestao)
        throw new ViolacaoDeRegra("Funcionário desconhecido é recusado no host.");
}

static FuncionarioId ExigirEmpregado(HttpRequest request, CadastroDeFuncionarios cadastro)
{
    var (id, gestao) = Identidade(request, cadastro);
    if (gestao || !cadastro.Existe(id))
        throw new ViolacaoDeRegra("Funcionário desconhecido é recusado no host.");
    return id;
}

static FuncionarioId ExigirProprioOuGestao(Guid funcionarioId, HttpRequest request, CadastroDeFuncionarios cadastro)
{
    var pessoa = ExigirExistente(funcionarioId, cadastro);
    var (ator, gestao) = Identidade(request, cadastro);
    if (!gestao && ator != pessoa)
        throw new ViolacaoDeRegra("Empregado que consulta ou registra ponto de outra pessoa é recusado.");
    return pessoa;
}

static FuncionarioId ExigirExistente(Guid funcionarioId, CadastroDeFuncionarios cadastro)
{
    var id = FuncionarioId.De(funcionarioId);
    if (!cadastro.Existe(id))
        throw new ViolacaoDeRegra("Funcionário desconhecido é recusado no host.");
    return id;
}

static (FuncionarioId Id, bool Gestao) Identidade(HttpRequest request, CadastroDeFuncionarios cadastro)
{
    var papel = request.Headers["X-Papel"].ToString();
    if (!Guid.TryParse(request.Headers["X-Funcionario-Id"], out var guid) || papel is not ("Gestao" or "Empregado"))
        throw new ViolacaoDeRegra("Funcionário desconhecido é recusado no host.");
    return (FuncionarioId.De(guid), papel == "Gestao");
}

static TratamentoDeEvento Tratamento(string forma, decimal? percentual, decimal? fator) => forma switch
{
    "hora normal" => TratamentoDeEvento.HoraNormal(),
    "adicional" => TratamentoDeEvento.Adicional(percentual ?? throw new ViolacaoDeRegra("Evento sem percentual, quando o tratamento é adicional, é recusado.")),
    "banco de horas" => TratamentoDeEvento.Banco(fator ?? throw new ViolacaoDeRegra("Evento sem fator, quando o tratamento é banco de horas, é recusado.")),
    _ => throw new ViolacaoDeRegra("Evento sem percentual, quando o tratamento é adicional, é recusado.")
};

public sealed class RelogioDoSistema : IRelogio
{
    public DateTimeOffset Agora() => DateTimeOffset.Now;
}

public sealed record NomeBody(string Nome);
public sealed record EscalaBody(Guid FuncionarioId, string Dia, List<IntervaloBody> Intervalos);
public sealed record IntervaloBody(int Inicio, int Fim);
public sealed record EventoBody(string Nome, DateTimeOffset Inicio, DateTimeOffset Fim, List<Guid> FuncionarioIds, string Forma, decimal? Percentual, decimal? Fator);
public sealed record PercentualBody(decimal Valor);
public sealed record EntradaBody(DateTimeOffset Entrada);
public sealed record SaidaBody(DateTimeOffset Saida);

public partial class Program { }
