using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Koinonia.Ponto.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Koinonia.Api.Tests;

public sealed class RelogioFixo(DateTimeOffset agora) : IRelogio
{
    public DateTimeOffset Agora() => agora;
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public string Arquivo { get; } = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Koinonia", $"Data Source={Arquivo}");
        builder.ConfigureTestServices(servicos =>
        {
            var registrado = servicos.Single(item => item.ServiceType == typeof(IRelogio));
            servicos.Remove(registrado);
            servicos.AddSingleton<IRelogio>(new RelogioFixo(new DateTimeOffset(2026, 10, 6, 20, 0, 0, TimeSpan.FromHours(-3))));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        if (File.Exists(Arquivo))
            File.Delete(Arquivo);
    }
}

public class ApiTestes : IClassFixture<ApiFactory>
{
    private readonly HttpClient _cliente;
    private readonly Guid _gestao = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public ApiTestes(ApiFactory factory) => _cliente = factory.CreateClient();

    [Fact]
    public async Task Fluxo_copia_o_percentual_na_aprovacao()
    {
        var ana = await Cadastrar("Ana");
        var bruno = await Cadastrar("Bruno");

        var desconhecido = await Enviar(Gestao(new HttpRequestMessage(HttpMethod.Get, $"/espelho/{Guid.NewGuid()}/2026-10-06")));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, desconhecido.StatusCode);
        Assert.Equal("Funcionário desconhecido é recusado no host.", await desconhecido.Content.ReadAsStringAsync());

        var alheio = await Enviar(Empregado(ana, new HttpRequestMessage(HttpMethod.Get, $"/espelho/{bruno}/2026-10-06")));
        Assert.Equal("Empregado que consulta ou registra ponto de outra pessoa é recusado.", await alheio.Content.ReadAsStringAsync());

        await Enviar(Gestao(Json(HttpMethod.Put, "/escalas", new { funcionarioId = ana, dia = "2026-10-06", intervalos = new[] { new { inicio = 480, fim = 1020 } } })));
        await Enviar(Gestao(Json(HttpMethod.Post, "/eventos", new { nome = "Formatura", inicio = "2026-10-06T14:00:00-03:00", fim = "2026-10-06T18:00:00-03:00", funcionarioIds = new[] { ana }, forma = "banco de horas", fator = 1 })));
        await Enviar(Gestao(Json(HttpMethod.Put, "/percentual-extra-comum", new { valor = 50 })));

        var entrada = await Enviar(Empregado(ana, Json(HttpMethod.Post, "/batidas/entrada", new { entrada = "2026-10-06T08:00:00-03:00" })));
        var batidaId = (await entrada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Enviar(Empregado(ana, Json(HttpMethod.Post, $"/batidas/{batidaId}/saida", new { saida = "2026-10-06T19:00:00-03:00" })));

        var previa = await LerEspelho(ana);
        Assert.False(previa.GetProperty("aprovado").GetBoolean());
        Assert.Equal(3, previa.GetProperty("trechos").GetArrayLength());

        await Enviar(Gestao(new HttpRequestMessage(HttpMethod.Post, $"/dias/{ana}/2026-10-06/aprovacao")));
        await Enviar(Gestao(Json(HttpMethod.Put, "/percentual-extra-comum", new { valor = 80 })));

        var aprovado = await LerEspelho(ana);
        var extra = aprovado.GetProperty("trechos").EnumerateArray().Single(item => item.GetProperty("percentual").ValueKind != JsonValueKind.Null);
        Assert.Equal(50, extra.GetProperty("percentual").GetDecimal());

        var fechamento = await Enviar(Gestao(new HttpRequestMessage(HttpMethod.Get, "/fechamento?inicio=2026-10-06&fim=2026-10-06&formato=csv")));
        using var zip = new ZipArchive(new MemoryStream(await fechamento.Content.ReadAsByteArrayAsync()));
        using var leitor = new StreamReader(zip.GetEntry("detalhe.csv")!.Open());
        var csv = await leitor.ReadToEndAsync();
        Assert.Contains("Formatura", csv);
        Assert.DoesNotContain("2026-10-07", csv);
    }

    private async Task<Guid> Cadastrar(string nome)
    {
        var resposta = await Enviar(Gestao(Json(HttpMethod.Post, "/funcionarios", new { nome })));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<JsonElement> LerEspelho(Guid pessoa)
    {
        var resposta = await Enviar(Gestao(new HttpRequestMessage(HttpMethod.Get, $"/espelho/{pessoa}/2026-10-06")));
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<HttpResponseMessage> Enviar(HttpRequestMessage mensagem)
    {
        var resposta = await _cliente.SendAsync(mensagem);
        if (resposta.StatusCode == HttpStatusCode.UnprocessableEntity)
            return resposta;
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        return resposta;
    }

    private HttpRequestMessage Gestao(HttpRequestMessage mensagem)
    {
        mensagem.Headers.Add("X-Papel", "Gestao");
        mensagem.Headers.Add("X-Funcionario-Id", _gestao.ToString());
        return mensagem;
    }

    private static HttpRequestMessage Empregado(Guid id, HttpRequestMessage mensagem)
    {
        mensagem.Headers.Add("X-Papel", "Empregado");
        mensagem.Headers.Add("X-Funcionario-Id", id.ToString());
        return mensagem;
    }

    private static HttpRequestMessage Json(HttpMethod metodo, string caminho, object corpo)
        => new(metodo, caminho) { Content = JsonContent.Create(corpo) };
}
