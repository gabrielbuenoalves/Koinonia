# Controle de Ponto e Escalas — Plano de implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Classificar cada minuto trabalhado em hora regular, hora extra comum ou hora de evento, copiar a política na aprovação e exportar essas horas separadas e somadas.

**Architecture:** Dois módulos, Planejamento e Ponto, cada um com Domain, Application e Infrastructure. O host compõe os dois, cadastra funcionário (id gerado, nome informado) e adapta a leitura de Planejamento para a porta de Ponto. A prévia calcula na hora. A aprovação grava a cópia. A planilha lê só essa cópia. O Angular espelha os módulos em três bibliotecas mais um shell.

**Tech Stack:** .NET 10, xUnit, EF Core com SQLite (um arquivo relacional, um DbContext por módulo), ClosedXML, NetArchTest.Rules, Angular 20.

**Spec:** `docs/superpowers/specs/2026-10-06-controle-ponto-escalas-design.md`

## Global Constraints

- Dia civil no fuso `America/Sao_Paulo`. Instantes são truncados ao minuto anterior antes do corte.
- O mesmo minuto entra em uma fatia só. Evento recorta a jornada, inclusive fora da escala. Sem escala, o que não for evento é extra comum.
- Banco de horas: `minutos trabalhados × fator`, arredondado ao minuto mais próximo, metade para cima (`MidpointRounding.AwayFromZero`).
- Percentual maior ou igual a zero. Fator maior que zero. Percentual inicial da instituição é 0 até a gestão definir outro.
- Interseção com duração maior que zero é recusada. Intervalo que termina quando o outro começa é válido.
- CSV em zip UTF-8 com BOM, separador `;`, vírgula decimal, arquivos `detalhe.csv` e `totais.csv`. Excel com abas `Detalhe` e `Totais`.
- Colunas, nesta ordem: funcionário, dia, início, fim, fatia, evento, tratamento, percentual, fator, minutos trabalhados, minutos creditados.
- Minutos creditados só no banco de horas. Demais células sem valor ficam vazias.
- Período sem dia aprovado gera arquivo só com o cabeçalho.
- Projetos: `Koinonia.Api`, `Koinonia.Shared`, `Koinonia.Planejamento.Domain`, `Koinonia.Planejamento.Application`, `Koinonia.Planejamento.Infrastructure`, `Koinonia.Ponto.Domain`, `Koinonia.Ponto.Application`, `Koinonia.Ponto.Infrastructure`.
- `ClassificarHoras` fica em `Koinonia.Ponto.Domain`. `IConsultaDePlanejamento` fica em `Koinonia.Ponto.Application`.
- Nenhum módulo referencia o outro. Não há chave estrangeira entre módulos.
- Identidade desta entrega: cabeçalhos `X-Funcionario-Id` e `X-Papel` (`Gestao` ou `Empregado`).
- Cada comportamento nasce de um teste que falha antes da implementação.
- Comandos abaixo são PowerShell, a partir da raiz do repositório `C:\Repos\Koinonia`.

## Estrutura de arquivos

- `koinonia-api/src/Koinonia.Shared/FuncionarioId.cs` — identificador e `ViolacaoDeRegra`.
- `koinonia-api/src/Koinonia.Ponto.Domain/ClassificarHoras.cs` — corte civil e classificação pura.
- `koinonia-api/src/Koinonia.Ponto.Domain/Batida.cs` — entrada e saída.
- `koinonia-api/src/Koinonia.Ponto.Domain/DiaDePonto.cs` — dia aberto ou aprovado e a cópia dos trechos.
- `koinonia-api/src/Koinonia.Ponto.Application/ServicoDePonto.cs` — prévia, aprovação, reabertura, batida.
- `koinonia-api/src/Koinonia.Ponto.Application/IConsultaDePlanejamento.cs` — porta de leitura.
- `koinonia-api/src/Koinonia.Ponto.Application/ExportadorDeFechamento.cs` — CSV e Excel.
- `koinonia-api/src/Koinonia.Ponto.Infrastructure/PontoDbContext.cs` — tabelas de Ponto.
- `koinonia-api/src/Koinonia.Planejamento.Domain/EscalaDoDia.cs` — escala do dia.
- `koinonia-api/src/Koinonia.Planejamento.Domain/Evento.cs` — evento, tratamento e conflito.
- `koinonia-api/src/Koinonia.Planejamento.Domain/PercentualExtraComum.cs` — percentual vigente.
- `koinonia-api/src/Koinonia.Planejamento.Application/ServicoDePlanejamento.cs` — publicar, alterar, remover, ler.
- `koinonia-api/src/Koinonia.Planejamento.Infrastructure/PlanejamentoDbContext.cs` — tabelas de Planejamento.
- `koinonia-api/src/Koinonia.Api/Program.cs` — rotas, cadastro, adaptador da porta.
- `koinonia-web/projects/ui` — tabela, formulário, período, estado vazio.
- `koinonia-web/projects/planejamento` — telas de escala, evento e percentual.
- `koinonia-web/projects/ponto` — batida, espelho, aprovação e exportação.
- `koinonia-web/src/app` — shell, rotas e papel.

---

### Task 1: Solução e identificador do funcionário

**Files:**
- Create: `koinonia-api/Directory.Build.props`
- Create: `koinonia-api/src/Koinonia.Shared/FuncionarioId.cs`
- Test: `koinonia-api/tests/Koinonia.Shared.Tests/FuncionarioIdTestes.cs`

**Interfaces:**
- Consumes: nada
- Produces: `Koinonia.Shared.FuncionarioId` com `Novo()` e `De(Guid)`. `Koinonia.Shared.ViolacaoDeRegra`.

- [ ] **Step 1: Criar a solução e o projeto de teste**

```powershell
dotnet new sln -n Koinonia -o koinonia-api --force
dotnet new classlib -n Koinonia.Shared -o koinonia-api/src/Koinonia.Shared -f net10.0 --force
dotnet new xunit -n Koinonia.Shared.Tests -o koinonia-api/tests/Koinonia.Shared.Tests -f net10.0 --force
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/src/Koinonia.Shared/Koinonia.Shared.csproj
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/tests/Koinonia.Shared.Tests/Koinonia.Shared.Tests.csproj
dotnet add koinonia-api/tests/Koinonia.Shared.Tests/Koinonia.Shared.Tests.csproj reference koinonia-api/src/Koinonia.Shared/Koinonia.Shared.csproj
Remove-Item koinonia-api/src/Koinonia.Shared/Class1.cs -ErrorAction SilentlyContinue
Remove-Item koinonia-api/tests/Koinonia.Shared.Tests/UnitTest1.cs -ErrorAction SilentlyContinue
```

Criar `koinonia-api/Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Escrever o teste que falha**

Criar `koinonia-api/tests/Koinonia.Shared.Tests/FuncionarioIdTestes.cs`:

```csharp
using Koinonia.Shared;

namespace Koinonia.Shared.Tests;

public class FuncionarioIdTestes
{
    [Fact]
    public void Novo_gera_guid_preenchido()
    {
        var id = FuncionarioId.Novo();
        Assert.NotEqual(Guid.Empty, id.Valor);
    }

    [Fact]
    public void De_recusa_guid_vazio()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => FuncionarioId.De(Guid.Empty));
        Assert.Equal("Identificador vazio.", erro.Message);
    }
}
```

- [ ] **Step 3: Rodar o teste e ver a falha de compilação**

Run: `dotnet test koinonia-api/tests/Koinonia.Shared.Tests/Koinonia.Shared.Tests.csproj --filter FuncionarioIdTestes`
Expected: FAIL. O compilador não encontra `FuncionarioId` nem `ViolacaoDeRegra`.

- [ ] **Step 4: Implementar o mínimo**

Criar `koinonia-api/src/Koinonia.Shared/FuncionarioId.cs`:

```csharp
namespace Koinonia.Shared;

public readonly record struct FuncionarioId(Guid Valor)
{
    public static FuncionarioId Novo() => new(Guid.NewGuid());

    public static FuncionarioId De(Guid valor)
    {
        if (valor == Guid.Empty)
            throw new ViolacaoDeRegra("Identificador vazio.");
        return new FuncionarioId(valor);
    }
}

public sealed class ViolacaoDeRegra : Exception
{
    public ViolacaoDeRegra(string mensagem) : base(mensagem) { }
}
```

- [ ] **Step 5: Rodar o teste e ver passar**

Run: `dotnet test koinonia-api/tests/Koinonia.Shared.Tests/Koinonia.Shared.Tests.csproj --filter FuncionarioIdTestes`
Expected: PASS, exit code 0.

- [ ] **Step 6: Commit**

```powershell
git add koinonia-api
git commit -m "feat: identificador do funcionario no nucleo compartilhado"
```

---

### Task 2: Classificar o exemplo combinado

**Files:**
- Create: `koinonia-api/src/Koinonia.Ponto.Domain/ClassificarHoras.cs`
- Test: `koinonia-api/tests/Koinonia.Ponto.Domain.Tests/ClassificarHorasTestes.cs`

**Interfaces:**
- Consumes: `ViolacaoDeRegra`
- Produces: `ClassificarHoras.Executar(IReadOnlyList<IntervaloMinutos> batidas, IReadOnlyList<IntervaloMinutos> escala, IReadOnlyList<EventoNoDia> eventos, decimal percentualExtraComum)` devolvendo `IReadOnlyList<TrechoClassificado>`. `IntervaloMinutos` usa minutos inclusivos no início e exclusivos no fim, de 0 a 1440. `TratamentoCopiado.Banco(decimal fator)`, `Adicional(decimal percentual)`, `HoraNormal()`. `Fatia` tem `Regular`, `ExtraComum`, `Evento`.

- [ ] **Step 1: Criar o projeto de domínio e o teste**

```powershell
dotnet new classlib -n Koinonia.Ponto.Domain -o koinonia-api/src/Koinonia.Ponto.Domain -f net10.0 --force
dotnet new xunit -n Koinonia.Ponto.Domain.Tests -o koinonia-api/tests/Koinonia.Ponto.Domain.Tests -f net10.0 --force
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/src/Koinonia.Ponto.Domain/Koinonia.Ponto.Domain.csproj
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/tests/Koinonia.Ponto.Domain.Tests/Koinonia.Ponto.Domain.Tests.csproj
dotnet add koinonia-api/src/Koinonia.Ponto.Domain/Koinonia.Ponto.Domain.csproj reference koinonia-api/src/Koinonia.Shared/Koinonia.Shared.csproj
dotnet add koinonia-api/tests/Koinonia.Ponto.Domain.Tests/Koinonia.Ponto.Domain.Tests.csproj reference koinonia-api/src/Koinonia.Ponto.Domain/Koinonia.Ponto.Domain.csproj
Remove-Item koinonia-api/src/Koinonia.Ponto.Domain/Class1.cs -ErrorAction SilentlyContinue
Remove-Item koinonia-api/tests/Koinonia.Ponto.Domain.Tests/UnitTest1.cs -ErrorAction SilentlyContinue
```

- [ ] **Step 2: Escrever o teste do exemplo combinado**

Criar `koinonia-api/tests/Koinonia.Ponto.Domain.Tests/ClassificarHorasTestes.cs`:

```csharp
using Koinonia.Ponto.Domain;

namespace Koinonia.Ponto.Domain.Tests;

public class ClassificarHorasTestes
{
    [Fact]
    public void Exemplo_combinado_recorta_evento_e_deixa_extra_depois_dele()
    {
        var eventoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var trechos = ClassificarHoras.Executar(
            batidas: [new IntervaloMinutos(8 * 60, 19 * 60)],
            escala: [new IntervaloMinutos(8 * 60, 17 * 60)],
            eventos:
            [
                new EventoNoDia(eventoId, "Formatura", new IntervaloMinutos(14 * 60, 18 * 60), TratamentoCopiado.Banco(1m))
            ],
            percentualExtraComum: 50m);

        Assert.Equal(3, trechos.Count);

        Assert.Equal(Fatia.Regular, trechos[0].Fatia);
        Assert.Equal(8 * 60, trechos[0].Intervalo.Inicio);
        Assert.Equal(14 * 60, trechos[0].Intervalo.Fim);
        Assert.Null(trechos[0].MinutosCreditados);

        Assert.Equal(Fatia.Evento, trechos[1].Fatia);
        Assert.Equal(14 * 60, trechos[1].Intervalo.Inicio);
        Assert.Equal(18 * 60, trechos[1].Intervalo.Fim);
        Assert.Equal(eventoId, trechos[1].EventoId);
        Assert.Equal("Formatura", trechos[1].NomeEvento);
        Assert.Equal(240, trechos[1].MinutosTrabalhados);
        Assert.Equal(240, trechos[1].MinutosCreditados);
        Assert.Equal(1m, trechos[1].Tratamento!.Fator);

        Assert.Equal(Fatia.ExtraComum, trechos[2].Fatia);
        Assert.Equal(18 * 60, trechos[2].Intervalo.Inicio);
        Assert.Equal(19 * 60, trechos[2].Intervalo.Fim);
        Assert.Equal(50m, trechos[2].PercentualExtraComum);
        Assert.Null(trechos[2].MinutosCreditados);
    }
}
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `dotnet test koinonia-api/tests/Koinonia.Ponto.Domain.Tests/Koinonia.Ponto.Domain.Tests.csproj --filter Exemplo_combinado`
Expected: FAIL de compilação. `ClassificarHoras` ainda não existe.

- [ ] **Step 4: Implementar a classificação por minuto**

Criar `koinonia-api/src/Koinonia.Ponto.Domain/ClassificarHoras.cs`:

```csharp
using Koinonia.Shared;

namespace Koinonia.Ponto.Domain;

public enum Fatia { Regular, ExtraComum, Evento }

public enum FormaDePagamento { HoraNormal, Adicional, BancoDeHoras }

public sealed record TratamentoCopiado(FormaDePagamento Forma, decimal? Percentual, decimal? Fator)
{
    public static TratamentoCopiado HoraNormal() => new(FormaDePagamento.HoraNormal, null, null);

    public static TratamentoCopiado Adicional(decimal percentual)
    {
        if (percentual < 0)
            throw new ViolacaoDeRegra("Percentual menor que zero é recusado.");
        return new TratamentoCopiado(FormaDePagamento.Adicional, percentual, null);
    }

    public static TratamentoCopiado Banco(decimal fator)
    {
        if (fator <= 0)
            throw new ViolacaoDeRegra("Fator menor ou igual a zero é recusado.");
        return new TratamentoCopiado(FormaDePagamento.BancoDeHoras, null, fator);
    }

    public int? MinutosCreditados(int minutosTrabalhados)
    {
        if (Forma != FormaDePagamento.BancoDeHoras || Fator is null)
            return null;
        return (int)Math.Round(minutosTrabalhados * Fator.Value, MidpointRounding.AwayFromZero);
    }
}

public readonly record struct IntervaloMinutos(int Inicio, int Fim)
{
    public IntervaloMinutos
    {
        if (Fim <= Inicio || Inicio < 0 || Fim > 1440)
            throw new ViolacaoDeRegra("Intervalo inválido.");
    }

    public bool Cobre(int minuto) => minuto >= Inicio && minuto < Fim;

    public bool Intersecta(IntervaloMinutos outro) => Inicio < outro.Fim && outro.Inicio < Fim;
}

public sealed record EventoNoDia(Guid Id, string Nome, IntervaloMinutos Intervalo, TratamentoCopiado Tratamento);

public sealed record TrechoClassificado(
    IntervaloMinutos Intervalo,
    Fatia Fatia,
    Guid? EventoId,
    string? NomeEvento,
    TratamentoCopiado? Tratamento,
    decimal? PercentualExtraComum)
{
    public int MinutosTrabalhados => Intervalo.Fim - Intervalo.Inicio;
    public int? MinutosCreditados => Tratamento?.MinutosCreditados(MinutosTrabalhados);
}

public static class ClassificarHoras
{
    public static IReadOnlyList<TrechoClassificado> Executar(
        IReadOnlyList<IntervaloMinutos> batidas,
        IReadOnlyList<IntervaloMinutos> escala,
        IReadOnlyList<EventoNoDia> eventos,
        decimal percentualExtraComum)
    {
        if (percentualExtraComum < 0)
            throw new ViolacaoDeRegra("Percentual menor que zero é recusado.");

        var fatiaPorMinuto = new Fatia?[1440];
        var eventoPorMinuto = new int?[1440];

        for (var minuto = 0; minuto < 1440; minuto++)
        {
            if (!batidas.Any(batida => batida.Cobre(minuto)))
                continue;

            var indiceEvento = IndiceDoEvento(eventos, minuto);
            if (indiceEvento >= 0)
            {
                fatiaPorMinuto[minuto] = Fatia.Evento;
                eventoPorMinuto[minuto] = indiceEvento;
                continue;
            }

            fatiaPorMinuto[minuto] = escala.Count > 0 && escala.Any(item => item.Cobre(minuto))
                ? Fatia.Regular
                : Fatia.ExtraComum;
        }

        var trechos = new List<TrechoClassificado>();
        var cursor = 0;
        while (cursor < 1440)
        {
            if (fatiaPorMinuto[cursor] is null)
            {
                cursor++;
                continue;
            }

            var inicio = cursor;
            var fatia = fatiaPorMinuto[cursor]!.Value;
            var indice = eventoPorMinuto[cursor];
            while (cursor < 1440 && fatiaPorMinuto[cursor] == fatia && eventoPorMinuto[cursor] == indice)
                cursor++;

            EventoNoDia? evento = indice is int encontrado ? eventos[encontrado] : null;
            trechos.Add(new TrechoClassificado(
                new IntervaloMinutos(inicio, cursor),
                fatia,
                evento?.Id,
                evento?.Nome,
                fatia == Fatia.Evento ? evento!.Tratamento : null,
                fatia == Fatia.ExtraComum ? percentualExtraComum : null));
        }

        return trechos;
    }

    private static int IndiceDoEvento(IReadOnlyList<EventoNoDia> eventos, int minuto)
    {
        for (var i = 0; i < eventos.Count; i++)
        {
            if (eventos[i].Intervalo.Cobre(minuto))
                return i;
        }

        return -1;
    }
}
```

- [ ] **Step 5: Rodar e ver passar**

Run: `dotnet test koinonia-api/tests/Koinonia.Ponto.Domain.Tests/Koinonia.Ponto.Domain.Tests.csproj --filter Exemplo_combinado`
Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add koinonia-api
git commit -m "feat: classificar hora regular, evento e extra no exemplo combinado"
```

---

### Task 3: Bordas da classificação e corte na meia-noite

**Files:**
- Modify: `koinonia-api/src/Koinonia.Ponto.Domain/ClassificarHoras.cs`
- Modify: `koinonia-api/tests/Koinonia.Ponto.Domain.Tests/ClassificarHorasTestes.cs`

**Interfaces:**
- Consumes: `ClassificarHoras.Executar` da Task 2
- Produces: `ClassificarHoras.Cortar(DateTimeOffset entrada, DateTimeOffset saida)` devolvendo `IReadOnlyList<PedacoDeBatida>`. `ClassificarHoras.DiaCivil(DateTimeOffset instante)` devolvendo `DateOnly`. `PedacoDeBatida(DateOnly Dia, IntervaloMinutos Intervalo)`.

- [ ] **Step 1: Acrescentar os testes que faltam**

No final da classe `ClassificarHorasTestes`, antes do fechamento da classe, adicionar:

```csharp
    [Fact]
    public void Sem_escala_o_tempo_fora_de_evento_e_extra_comum()
    {
        var trechos = ClassificarHoras.Executar(
            [new IntervaloMinutos(8 * 60, 10 * 60)],
            [],
            [],
            50m);

        var trecho = Assert.Single(trechos);
        Assert.Equal(Fatia.ExtraComum, trecho.Fatia);
        Assert.Equal(8 * 60, trecho.Intervalo.Inicio);
        Assert.Equal(10 * 60, trecho.Intervalo.Fim);
        Assert.Equal(50m, trecho.PercentualExtraComum);
    }

    [Fact]
    public void Vao_entre_batidas_nao_entra_na_conta()
    {
        var trechos = ClassificarHoras.Executar(
            [new IntervaloMinutos(8 * 60, 12 * 60), new IntervaloMinutos(13 * 60, 17 * 60)],
            [new IntervaloMinutos(8 * 60, 17 * 60)],
            [],
            50m);

        Assert.Equal(2, trechos.Count);
        Assert.Equal(12 * 60, trechos[0].Intervalo.Fim);
        Assert.Equal(13 * 60, trechos[1].Intervalo.Inicio);
        Assert.All(trechos, trecho => Assert.Equal(Fatia.Regular, trecho.Fatia));
    }

    [Fact]
    public void Minutos_contiguos_da_mesma_fatia_formam_um_trecho()
    {
        var trechos = ClassificarHoras.Executar(
            [new IntervaloMinutos(8 * 60, 12 * 60), new IntervaloMinutos(12 * 60, 17 * 60)],
            [new IntervaloMinutos(8 * 60, 17 * 60)],
            [],
            50m);

        var trecho = Assert.Single(trechos);
        Assert.Equal(8 * 60, trecho.Intervalo.Inicio);
        Assert.Equal(17 * 60, trecho.Intervalo.Fim);
    }

    [Fact]
    public void Fator_um_e_meio_arredonda_um_minuto_para_dois()
    {
        var trechos = ClassificarHoras.Executar(
            [new IntervaloMinutos(8 * 60, 8 * 60 + 1)],
            [],
            [new EventoNoDia(Guid.NewGuid(), "Plantão", new IntervaloMinutos(8 * 60, 9 * 60), TratamentoCopiado.Banco(1.5m))],
            0m);

        var trecho = Assert.Single(trechos);
        Assert.Equal(1, trecho.MinutosTrabalhados);
        Assert.Equal(2, trecho.MinutosCreditados);
    }

    [Fact]
    public void Hora_normal_de_evento_nao_credita_banco()
    {
        var trechos = ClassificarHoras.Executar(
            [new IntervaloMinutos(8 * 60, 9 * 60)],
            [new IntervaloMinutos(8 * 60, 17 * 60)],
            [new EventoNoDia(Guid.NewGuid(), "Conselho", new IntervaloMinutos(8 * 60, 9 * 60), TratamentoCopiado.HoraNormal())],
            50m);

        var trecho = Assert.Single(trechos);
        Assert.Equal(Fatia.Evento, trecho.Fatia);
        Assert.Equal(FormaDePagamento.HoraNormal, trecho.Tratamento!.Forma);
        Assert.Null(trecho.MinutosCreditados);
        Assert.Null(trecho.PercentualExtraComum);
    }

    [Fact]
    public void Cortar_divide_a_batida_na_meia_noite_de_Sao_Paulo()
    {
        var entrada = new DateTimeOffset(2026, 10, 6, 22, 0, 0, TimeSpan.FromHours(-3));
        var saida = new DateTimeOffset(2026, 10, 7, 2, 0, 0, TimeSpan.FromHours(-3));

        var pedacos = ClassificarHoras.Cortar(entrada, saida);

        Assert.Equal(2, pedacos.Count);
        Assert.Equal(new DateOnly(2026, 10, 6), pedacos[0].Dia);
        Assert.Equal(22 * 60, pedacos[0].Intervalo.Inicio);
        Assert.Equal(1440, pedacos[0].Intervalo.Fim);
        Assert.Equal(new DateOnly(2026, 10, 7), pedacos[1].Dia);
        Assert.Equal(0, pedacos[1].Intervalo.Inicio);
        Assert.Equal(2 * 60, pedacos[1].Intervalo.Fim);

        var noite = ClassificarHoras.Executar([pedacos[0].Intervalo], [], [], 50m);
        var madrugada = ClassificarHoras.Executar(
            [pedacos[1].Intervalo],
            [new IntervaloMinutos(0, 8 * 60)],
            [],
            50m);

        Assert.Equal(Fatia.ExtraComum, Assert.Single(noite).Fatia);
        Assert.Equal(Fatia.Regular, Assert.Single(madrugada).Fatia);
    }
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test koinonia-api/tests/Koinonia.Ponto.Domain.Tests/Koinonia.Ponto.Domain.Tests.csproj --filter ClassificarHorasTestes`
Expected: FAIL. `Cortar` ainda não existe. Os outros testes novos passam com a implementação da Task 2; a falha obrigatória é a de compilação de `Cortar`.

- [ ] **Step 3: Implementar o corte**

Em `ClassificarHoras.cs`, acrescentar o record e os métodos. O record fica junto dos outros records. Os métodos entram na classe `ClassificarHoras`.

```csharp
public sealed record PedacoDeBatida(DateOnly Dia, IntervaloMinutos Intervalo);
```

```csharp
    public static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateOnly DiaCivil(DateTimeOffset instante)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instante, Zona).Date);

    public static IReadOnlyList<PedacoDeBatida> Cortar(DateTimeOffset entrada, DateTimeOffset saida)
    {
        if (saida <= entrada)
            throw new ViolacaoDeRegra("Saída anterior à entrada.");

        var inicio = TimeZoneInfo.ConvertTime(TruncarMinuto(entrada), Zona);
        var fim = TimeZoneInfo.ConvertTime(TruncarMinuto(saida), Zona);
        if (fim <= inicio)
            throw new ViolacaoDeRegra("Saída anterior à entrada.");

        var pedacos = new List<PedacoDeBatida>();
        var cursor = inicio;
        while (cursor < fim)
        {
            var dia = DateOnly.FromDateTime(cursor.Date);
            var meiaNoiteSeguinte = new DateTimeOffset(cursor.Date.AddDays(1), cursor.Offset);
            var limite = meiaNoiteSeguinte < fim ? meiaNoiteSeguinte : fim;
            var inicioMinuto = cursor.Hour * 60 + cursor.Minute;
            var fimMinuto = limite == meiaNoiteSeguinte ? 1440 : limite.Hour * 60 + limite.Minute;
            pedacos.Add(new PedacoDeBatida(dia, new IntervaloMinutos(inicioMinuto, fimMinuto)));
            cursor = limite;
        }

        return pedacos;
    }

    private static DateTimeOffset TruncarMinuto(DateTimeOffset instante)
        => new(instante.Year, instante.Month, instante.Day, instante.Hour, instante.Minute, 0, instante.Offset);
```

Adicionar `using Koinonia.Shared;` se o arquivo ainda não tiver. O arquivo da Task 2 já tem esse using.

- [ ] **Step 4: Rodar e ver passar**

Run: `dotnet test koinonia-api/tests/Koinonia.Ponto.Domain.Tests/Koinonia.Ponto.Domain.Tests.csproj --filter ClassificarHorasTestes`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add koinonia-api
git commit -m "feat: cortar batida na meia-noite e fechar as bordas da apuracao"
```

---

### Task 4: Escala do dia

**Files:**
- Create: `koinonia-api/src/Koinonia.Planejamento.Domain/EscalaDoDia.cs`
- Test: `koinonia-api/tests/Koinonia.Planejamento.Domain.Tests/EscalaDoDiaTestes.cs`

**Interfaces:**
- Consumes: `FuncionarioId`, `ViolacaoDeRegra`
- Produces: `EscalaDoDia.Publicar(FuncionarioId funcionarioId, DateOnly dia, IReadOnlyList<IntervaloDeEscala> intervalos)`. `IntervaloDeEscala(int Inicio, int Fim)` com `Intersecta`.

- [ ] **Step 1: Criar o projeto**

```powershell
dotnet new classlib -n Koinonia.Planejamento.Domain -o koinonia-api/src/Koinonia.Planejamento.Domain -f net10.0 --force
dotnet new xunit -n Koinonia.Planejamento.Domain.Tests -o koinonia-api/tests/Koinonia.Planejamento.Domain.Tests -f net10.0 --force
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/src/Koinonia.Planejamento.Domain/Koinonia.Planejamento.Domain.csproj
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/tests/Koinonia.Planejamento.Domain.Tests/Koinonia.Planejamento.Domain.Tests.csproj
dotnet add koinonia-api/src/Koinonia.Planejamento.Domain/Koinonia.Planejamento.Domain.csproj reference koinonia-api/src/Koinonia.Shared/Koinonia.Shared.csproj
dotnet add koinonia-api/tests/Koinonia.Planejamento.Domain.Tests/Koinonia.Planejamento.Domain.Tests.csproj reference koinonia-api/src/Koinonia.Planejamento.Domain/Koinonia.Planejamento.Domain.csproj
Remove-Item koinonia-api/src/Koinonia.Planejamento.Domain/Class1.cs -ErrorAction SilentlyContinue
Remove-Item koinonia-api/tests/Koinonia.Planejamento.Domain.Tests/UnitTest1.cs -ErrorAction SilentlyContinue
```

- [ ] **Step 2: Escrever os testes**

Criar `koinonia-api/tests/Koinonia.Planejamento.Domain.Tests/EscalaDoDiaTestes.cs`:

```csharp
using Koinonia.Planejamento.Domain;
using Koinonia.Shared;

namespace Koinonia.Planejamento.Domain.Tests;

public class EscalaDoDiaTestes
{
    [Fact]
    public void Publicar_aceita_intervalos_que_apenas_se_encostam()
    {
        var escala = EscalaDoDia.Publicar(
            FuncionarioId.Novo(),
            new DateOnly(2026, 10, 6),
            [new IntervaloDeEscala(8 * 60, 12 * 60), new IntervaloDeEscala(12 * 60, 18 * 60)]);

        Assert.Equal(2, escala.Intervalos.Count);
    }

    [Fact]
    public void Publicar_recusa_intersecao()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => EscalaDoDia.Publicar(
            FuncionarioId.Novo(),
            new DateOnly(2026, 10, 6),
            [new IntervaloDeEscala(8 * 60, 13 * 60), new IntervaloDeEscala(12 * 60, 18 * 60)]));

        Assert.Equal("Intervalos de escala com interseção de duração maior que zero, no mesmo dia, são recusados.", erro.Message);
    }

    [Fact]
    public void Publicar_recusa_lista_vazia()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => EscalaDoDia.Publicar(
            FuncionarioId.Novo(),
            new DateOnly(2026, 10, 6),
            []));

        Assert.Equal("Intervalo inválido.", erro.Message);
    }
}
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `dotnet test koinonia-api/tests/Koinonia.Planejamento.Domain.Tests/Koinonia.Planejamento.Domain.Tests.csproj --filter EscalaDoDiaTestes`
Expected: FAIL de compilação. `EscalaDoDia` não existe.

- [ ] **Step 4: Implementar**

Criar `koinonia-api/src/Koinonia.Planejamento.Domain/EscalaDoDia.cs`:

```csharp
using Koinonia.Shared;

namespace Koinonia.Planejamento.Domain;

public readonly record struct IntervaloDeEscala(int Inicio, int Fim)
{
    public IntervaloDeEscala
    {
        if (Fim <= Inicio || Inicio < 0 || Fim > 1440)
            throw new ViolacaoDeRegra("Intervalo inválido.");
    }

    public bool Intersecta(IntervaloDeEscala outro) => Inicio < outro.Fim && outro.Inicio < Fim;
}

public sealed class EscalaDoDia
{
    private EscalaDoDia(FuncionarioId funcionarioId, DateOnly dia, IReadOnlyList<IntervaloDeEscala> intervalos)
    {
        FuncionarioId = funcionarioId;
        Dia = dia;
        Intervalos = intervalos;
    }

    public FuncionarioId FuncionarioId { get; }
    public DateOnly Dia { get; }
    public IReadOnlyList<IntervaloDeEscala> Intervalos { get; }

    public static EscalaDoDia Publicar(FuncionarioId funcionarioId, DateOnly dia, IReadOnlyList<IntervaloDeEscala> intervalos)
    {
        if (intervalos.Count == 0)
            throw new ViolacaoDeRegra("Intervalo inválido.");

        for (var i = 0; i < intervalos.Count; i++)
        {
            for (var j = i + 1; j < intervalos.Count; j++)
            {
                if (intervalos[i].Intersecta(intervalos[j]))
                    throw new ViolacaoDeRegra("Intervalos de escala com interseção de duração maior que zero, no mesmo dia, são recusados.");
            }
        }

        return new EscalaDoDia(funcionarioId, dia, intervalos.OrderBy(item => item.Inicio).ToList());
    }
}
```

- [ ] **Step 5: Rodar e ver passar**

Run: `dotnet test koinonia-api/tests/Koinonia.Planejamento.Domain.Tests/Koinonia.Planejamento.Domain.Tests.csproj --filter EscalaDoDiaTestes`
Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add koinonia-api
git commit -m "feat: publicar escala do dia sem intervalos sobrepostos"
```

---

### Task 5: Evento e tratamento de pagamento

**Files:**
- Create: `koinonia-api/src/Koinonia.Planejamento.Domain/Evento.cs`
- Test: `koinonia-api/tests/Koinonia.Planejamento.Domain.Tests/EventoTestes.cs`

**Interfaces:**
- Consumes: `FuncionarioId`, `ViolacaoDeRegra`
- Produces: `TratamentoDeEvento.HoraNormal()`, `Adicional(decimal)`, `Banco(decimal)`. `FormaDePagamentoDoEvento` com `HoraNormal`, `Adicional`, `BancoDeHoras`. `Evento.Criar(...)` e `Evento.Alterar(...)`. `GradeDeEventos.GarantirSemConflito(Evento candidato, IEnumerable<Evento> existentes)`.

- [ ] **Step 1: Escrever os testes**

Criar `koinonia-api/tests/Koinonia.Planejamento.Domain.Tests/EventoTestes.cs`:

```csharp
using Koinonia.Planejamento.Domain;
using Koinonia.Shared;

namespace Koinonia.Planejamento.Domain.Tests;

public class EventoTestes
{
    private static readonly DateTimeOffset Inicio = new(2026, 10, 6, 14, 0, 0, TimeSpan.FromHours(-3));
    private static readonly DateTimeOffset Fim = new(2026, 10, 6, 18, 0, 0, TimeSpan.FromHours(-3));

    [Fact]
    public void Adicional_sem_percentual_e_recusado()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => TratamentoDeEvento.Adicional(-1m));
        Assert.Equal("Percentual menor que zero é recusado.", erro.Message);
    }

    [Fact]
    public void Banco_sem_fator_valido_e_recusado()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => TratamentoDeEvento.Banco(0m));
        Assert.Equal("Fator menor ou igual a zero é recusado.", erro.Message);
    }

    [Fact]
    public void Hora_normal_nao_carrega_numero()
    {
        var tratamento = TratamentoDeEvento.HoraNormal();
        Assert.Equal(FormaDePagamentoDoEvento.HoraNormal, tratamento.Forma);
        Assert.Null(tratamento.Percentual);
        Assert.Null(tratamento.Fator);
    }

    [Fact]
    public void Eventos_que_apenas_se_encostam_para_a_mesma_pessoa_sao_validos()
    {
        var pessoa = FuncionarioId.Novo();
        var primeiro = Evento.Criar("A", Inicio, Fim, [pessoa], TratamentoDeEvento.HoraNormal());
        var segundo = Evento.Criar("B", Fim, Fim.AddHours(1), [pessoa], TratamentoDeEvento.HoraNormal());

        GradeDeEventos.GarantirSemConflito(segundo, [primeiro]);
    }

    [Fact]
    public void Intersecao_para_a_mesma_pessoa_e_recusada()
    {
        var pessoa = FuncionarioId.Novo();
        var primeiro = Evento.Criar("A", Inicio, Fim, [pessoa], TratamentoDeEvento.Adicional(50m));
        var segundo = Evento.Criar("B", Inicio.AddHours(1), Fim.AddHours(1), [pessoa], TratamentoDeEvento.Banco(1m));

        var erro = Assert.Throws<ViolacaoDeRegra>(() => GradeDeEventos.GarantirSemConflito(segundo, [primeiro]));
        Assert.Equal("Dois eventos com interseção de duração maior que zero, para a mesma pessoa, são recusados.", erro.Message);
    }

    [Fact]
    public void Alterar_repete_a_recusa_de_intersecao()
    {
        var pessoa = FuncionarioId.Novo();
        var primeiro = Evento.Criar("A", Inicio, Fim, [pessoa], TratamentoDeEvento.HoraNormal());
        var segundo = Evento.Criar("B", Fim, Fim.AddHours(2), [pessoa], TratamentoDeEvento.HoraNormal());

        var erro = Assert.Throws<ViolacaoDeRegra>(() =>
        {
            segundo.Alterar("B", Inicio.AddHours(1), Fim.AddHours(1), [pessoa], TratamentoDeEvento.HoraNormal());
            GradeDeEventos.GarantirSemConflito(segundo, [primeiro]);
        });

        Assert.Equal("Dois eventos com interseção de duração maior que zero, para a mesma pessoa, são recusados.", erro.Message);
    }
}
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test koinonia-api/tests/Koinonia.Planejamento.Domain.Tests/Koinonia.Planejamento.Domain.Tests.csproj --filter EventoTestes`
Expected: FAIL de compilação.

- [ ] **Step 3: Implementar**

Criar `koinonia-api/src/Koinonia.Planejamento.Domain/Evento.cs`:

```csharp
using Koinonia.Shared;

namespace Koinonia.Planejamento.Domain;

public enum FormaDePagamentoDoEvento { HoraNormal, Adicional, BancoDeHoras }

public sealed record TratamentoDeEvento(FormaDePagamentoDoEvento Forma, decimal? Percentual, decimal? Fator)
{
    public static TratamentoDeEvento HoraNormal() => new(FormaDePagamentoDoEvento.HoraNormal, null, null);

    public static TratamentoDeEvento Adicional(decimal percentual)
    {
        if (percentual < 0)
            throw new ViolacaoDeRegra("Percentual menor que zero é recusado.");
        return new TratamentoDeEvento(FormaDePagamentoDoEvento.Adicional, percentual, null);
    }

    public static TratamentoDeEvento Banco(decimal fator)
    {
        if (fator <= 0)
            throw new ViolacaoDeRegra("Fator menor ou igual a zero é recusado.");
        return new TratamentoDeEvento(FormaDePagamentoDoEvento.BancoDeHoras, null, fator);
    }
}

public sealed class Evento
{
    private readonly HashSet<FuncionarioId> _escalados;

    private Evento(Guid id, string nome, DateTimeOffset inicio, DateTimeOffset fim, IEnumerable<FuncionarioId> escalados, TratamentoDeEvento tratamento)
    {
        Id = id;
        Nome = nome;
        Inicio = inicio;
        Fim = fim;
        Tratamento = tratamento;
        _escalados = escalados.ToHashSet();
    }

    public Guid Id { get; }
    public string Nome { get; private set; }
    public DateTimeOffset Inicio { get; private set; }
    public DateTimeOffset Fim { get; private set; }
    public TratamentoDeEvento Tratamento { get; private set; }
    public IReadOnlySet<FuncionarioId> Escalados => _escalados;

    public static Evento Criar(string nome, DateTimeOffset inicio, DateTimeOffset fim, IReadOnlyCollection<FuncionarioId> escalados, TratamentoDeEvento tratamento)
    {
        ValidarFim(inicio, fim);
        return new Evento(Guid.NewGuid(), ValidarNome(nome), inicio, fim, ValidarEscalados(escalados), tratamento);
    }

    public static Evento Reidratar(Guid id, string nome, DateTimeOffset inicio, DateTimeOffset fim, IEnumerable<FuncionarioId> escalados, TratamentoDeEvento tratamento)
        => new(id, nome, inicio, fim, escalados, tratamento);

    public void Alterar(string nome, DateTimeOffset inicio, DateTimeOffset fim, IReadOnlyCollection<FuncionarioId> escalados, TratamentoDeEvento tratamento)
    {
        Nome = ValidarNome(nome);
        Inicio = inicio;
        Fim = ValidarFim(inicio, fim);
        Tratamento = tratamento;
        _escalados.Clear();
        foreach (var escalado in ValidarEscalados(escalados))
            _escalados.Add(escalado);
    }

    public bool ConflitaCom(Evento outro)
    {
        if (Id == outro.Id)
            return false;
        if (Inicio >= outro.Fim || outro.Inicio >= Fim)
            return false;
        return _escalados.Overlaps(outro._escalados);
    }

    private static string ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ViolacaoDeRegra("Intervalo inválido.");
        return nome.Trim();
    }

    private static DateTimeOffset ValidarFim(DateTimeOffset inicio, DateTimeOffset fim)
    {
        if (fim <= inicio)
            throw new ViolacaoDeRegra("Saída anterior à entrada.");
        return fim;
    }

    private static IReadOnlyCollection<FuncionarioId> ValidarEscalados(IReadOnlyCollection<FuncionarioId> escalados)
    {
        if (escalados.Count == 0)
            throw new ViolacaoDeRegra("Intervalo inválido.");
        return escalados;
    }
}

public static class GradeDeEventos
{
    public static void GarantirSemConflito(Evento candidato, IEnumerable<Evento> existentes)
    {
        foreach (var existente in existentes)
        {
            if (candidato.ConflitaCom(existente))
                throw new ViolacaoDeRegra("Dois eventos com interseção de duração maior que zero, para a mesma pessoa, são recusados.");
        }
    }
}
```

- [ ] **Step 4: Rodar e ver passar**

Run: `dotnet test koinonia-api/tests/Koinonia.Planejamento.Domain.Tests/Koinonia.Planejamento.Domain.Tests.csproj --filter EventoTestes`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add koinonia-api
git commit -m "feat: recusar evento sobreposto e tratamento sem numero"
```

---

### Task 6: Percentual institucional da extra comum

**Files:**
- Create: `koinonia-api/src/Koinonia.Planejamento.Domain/PercentualExtraComum.cs`
- Test: `koinonia-api/tests/Koinonia.Planejamento.Domain.Tests/PercentualExtraComumTestes.cs`

**Interfaces:**
- Consumes: `ViolacaoDeRegra`
- Produces: `PercentualExtraComum.Definir(decimal valor)` com a propriedade `Valor`.

- [ ] **Step 1: Escrever o teste**

Criar `koinonia-api/tests/Koinonia.Planejamento.Domain.Tests/PercentualExtraComumTestes.cs`:

```csharp
using Koinonia.Planejamento.Domain;
using Koinonia.Shared;

namespace Koinonia.Planejamento.Domain.Tests;

public class PercentualExtraComumTestes
{
    [Fact]
    public void Zero_e_valido()
    {
        Assert.Equal(0m, PercentualExtraComum.Definir(0m).Valor);
    }

    [Fact]
    public void Negativo_e_recusado()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => PercentualExtraComum.Definir(-1m));
        Assert.Equal("Percentual menor que zero é recusado.", erro.Message);
    }
}
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test koinonia-api/tests/Koinonia.Planejamento.Domain.Tests/Koinonia.Planejamento.Domain.Tests.csproj --filter PercentualExtraComumTestes`
Expected: FAIL de compilação.

- [ ] **Step 3: Implementar**

Criar `koinonia-api/src/Koinonia.Planejamento.Domain/PercentualExtraComum.cs`:

```csharp
using Koinonia.Shared;

namespace Koinonia.Planejamento.Domain;

public sealed record PercentualExtraComum(decimal Valor)
{
    public static PercentualExtraComum Definir(decimal valor)
    {
        if (valor < 0)
            throw new ViolacaoDeRegra("Percentual menor que zero é recusado.");
        return new PercentualExtraComum(valor);
    }
}
```

- [ ] **Step 4: Rodar e ver passar**

Run: `dotnet test koinonia-api/tests/Koinonia.Planejamento.Domain.Tests/Koinonia.Planejamento.Domain.Tests.csproj --filter PercentualExtraComumTestes`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add koinonia-api
git commit -m "feat: guardar percentual institucional da extra comum"
```

---

### Task 7: Persistência e casos de uso de Planejamento

**Files:**
- Create: `koinonia-api/src/Koinonia.Planejamento.Application/ServicoDePlanejamento.cs`
- Create: `koinonia-api/src/Koinonia.Planejamento.Infrastructure/PlanejamentoDbContext.cs`
- Create: `koinonia-api/src/Koinonia.Planejamento.Infrastructure/RepositorioDePlanejamento.cs`
- Test: `koinonia-api/tests/Koinonia.Planejamento.Application.Tests/ServicoDePlanejamentoTestes.cs`

**Interfaces:**
- Consumes: `EscalaDoDia`, `Evento`, `GradeDeEventos`, `PercentualExtraComum`, `TratamentoDeEvento`, `FuncionarioId`
- Produces: `ServicoDePlanejamento` com `PublicarEscala`, `RemoverEscala`, `PublicarEvento`, `AlterarEvento`, `DefinirPercentual`. `ILeituraDePlanejamento.Ler(FuncionarioId, DateOnly)` devolve `LeituraDoDia`. `LeituraDoDia` contém `IReadOnlyList<IntervaloDeEscala> Escala`, `IReadOnlyList<EventoLido> Eventos`, `decimal PercentualExtraComum`. `EventoLido(Guid Id, string Nome, DateTimeOffset Inicio, DateTimeOffset Fim, TratamentoDeEvento Tratamento)`.

- [ ] **Step 1: Criar projetos e pacotes**

```powershell
dotnet new classlib -n Koinonia.Planejamento.Application -o koinonia-api/src/Koinonia.Planejamento.Application -f net10.0 --force
dotnet new classlib -n Koinonia.Planejamento.Infrastructure -o koinonia-api/src/Koinonia.Planejamento.Infrastructure -f net10.0 --force
dotnet new xunit -n Koinonia.Planejamento.Application.Tests -o koinonia-api/tests/Koinonia.Planejamento.Application.Tests -f net10.0 --force
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/src/Koinonia.Planejamento.Application/Koinonia.Planejamento.Application.csproj
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/src/Koinonia.Planejamento.Infrastructure/Koinonia.Planejamento.Infrastructure.csproj
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/tests/Koinonia.Planejamento.Application.Tests/Koinonia.Planejamento.Application.Tests.csproj
dotnet add koinonia-api/src/Koinonia.Planejamento.Application/Koinonia.Planejamento.Application.csproj reference koinonia-api/src/Koinonia.Planejamento.Domain/Koinonia.Planejamento.Domain.csproj
dotnet add koinonia-api/src/Koinonia.Planejamento.Infrastructure/Koinonia.Planejamento.Infrastructure.csproj reference koinonia-api/src/Koinonia.Planejamento.Domain/Koinonia.Planejamento.Domain.csproj
dotnet add koinonia-api/src/Koinonia.Planejamento.Infrastructure/Koinonia.Planejamento.Infrastructure.csproj reference koinonia-api/src/Koinonia.Planejamento.Application/Koinonia.Planejamento.Application.csproj
dotnet add koinonia-api/tests/Koinonia.Planejamento.Application.Tests/Koinonia.Planejamento.Application.Tests.csproj reference koinonia-api/src/Koinonia.Planejamento.Infrastructure/Koinonia.Planejamento.Infrastructure.csproj
dotnet add koinonia-api/src/Koinonia.Planejamento.Infrastructure/Koinonia.Planejamento.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Sqlite
dotnet add koinonia-api/src/Koinonia.Planejamento.Infrastructure/Koinonia.Planejamento.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design
Remove-Item koinonia-api/src/Koinonia.Planejamento.Application/Class1.cs -ErrorAction SilentlyContinue
Remove-Item koinonia-api/src/Koinonia.Planejamento.Infrastructure/Class1.cs -ErrorAction SilentlyContinue
Remove-Item koinonia-api/tests/Koinonia.Planejamento.Application.Tests/UnitTest1.cs -ErrorAction SilentlyContinue
```

- [ ] **Step 2: Escrever o teste de aplicação**

Criar `koinonia-api/tests/Koinonia.Planejamento.Application.Tests/ServicoDePlanejamentoTestes.cs`:

```csharp
using Koinonia.Planejamento.Application;
using Koinonia.Planejamento.Domain;
using Koinonia.Planejamento.Infrastructure;
using Koinonia.Shared;
using Microsoft.EntityFrameworkCore;

namespace Koinonia.Planejamento.Application.Tests;

public class ServicoDePlanejamentoTestes : IDisposable
{
    private readonly string _arquivo = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
    private readonly PlanejamentoDbContext _contexto;
    private readonly ServicoDePlanejamento _servico;

    public ServicoDePlanejamentoTestes()
    {
        var options = new DbContextOptionsBuilder<PlanejamentoDbContext>()
            .UseSqlite($"Data Source={_arquivo}")
            .Options;
        _contexto = new PlanejamentoDbContext(options);
        _contexto.Database.Migrate();
        _servico = new ServicoDePlanejamento(new RepositorioDePlanejamento(_contexto));
    }

    [Fact]
    public void Remover_escala_faz_a_leitura_voltar_sem_intervalos()
    {
        var pessoa = FuncionarioId.Novo();
        var dia = new DateOnly(2026, 10, 6);
        _servico.PublicarEscala(pessoa, dia, [new IntervaloDeEscala(8 * 60, 17 * 60)]);
        _servico.RemoverEscala(pessoa, dia);

        var leitura = _servico.Ler(pessoa, dia);

        Assert.Empty(leitura.Escala);
        Assert.Equal(0m, leitura.PercentualExtraComum);
    }

    [Fact]
    public void Publicar_de_novo_substitui_os_intervalos()
    {
        var pessoa = FuncionarioId.Novo();
        var dia = new DateOnly(2026, 10, 6);
        _servico.PublicarEscala(pessoa, dia, [new IntervaloDeEscala(8 * 60, 12 * 60)]);
        _servico.PublicarEscala(pessoa, dia, [new IntervaloDeEscala(13 * 60, 17 * 60)]);

        var leitura = _servico.Ler(pessoa, dia);

        var intervalo = Assert.Single(leitura.Escala);
        Assert.Equal(13 * 60, intervalo.Inicio);
    }

    [Fact]
    public void Evento_publicado_aparece_na_leitura_de_quem_esta_escalado()
    {
        var pessoa = FuncionarioId.Novo();
        var outro = FuncionarioId.Novo();
        var inicio = new DateTimeOffset(2026, 10, 6, 14, 0, 0, TimeSpan.FromHours(-3));
        var fim = new DateTimeOffset(2026, 10, 6, 18, 0, 0, TimeSpan.FromHours(-3));
        _servico.PublicarEvento("Formatura", inicio, fim, [pessoa], TratamentoDeEvento.Banco(1m));

        var daPessoa = _servico.Ler(pessoa, new DateOnly(2026, 10, 6));
        var doOutro = _servico.Ler(outro, new DateOnly(2026, 10, 6));

        Assert.Equal("Formatura", Assert.Single(daPessoa.Eventos).Nome);
        Assert.Empty(doOutro.Eventos);
    }

    [Fact]
    public void Definir_percentual_muda_a_leitura()
    {
        _servico.DefinirPercentual(50m);
        var leitura = _servico.Ler(FuncionarioId.Novo(), new DateOnly(2026, 10, 6));
        Assert.Equal(50m, leitura.PercentualExtraComum);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        if (File.Exists(_arquivo))
            File.Delete(_arquivo);
    }
}
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `dotnet test koinonia-api/tests/Koinonia.Planejamento.Application.Tests/Koinonia.Planejamento.Application.Tests.csproj --filter ServicoDePlanejamentoTestes`
Expected: FAIL de compilação.

- [ ] **Step 4: Implementar aplicação, contexto e repositório**

Criar `koinonia-api/src/Koinonia.Planejamento.Application/ServicoDePlanejamento.cs`:

```csharp
using Koinonia.Planejamento.Domain;
using Koinonia.Shared;

namespace Koinonia.Planejamento.Application;

public sealed record EventoLido(Guid Id, string Nome, DateTimeOffset Inicio, DateTimeOffset Fim, TratamentoDeEvento Tratamento);

public sealed record LeituraDoDia(
    IReadOnlyList<IntervaloDeEscala> Escala,
    IReadOnlyList<EventoLido> Eventos,
    decimal PercentualExtraComum);

public interface IRepositorioDePlanejamento
{
    void SalvarEscala(EscalaDoDia escala);
    void RemoverEscala(FuncionarioId funcionarioId, DateOnly dia);
    void SalvarEvento(Evento evento);
    Evento? ObterEvento(Guid id);
    IReadOnlyList<Evento> ListarEventos();
    void SalvarPercentual(PercentualExtraComum percentual);
    LeituraDoDia Ler(FuncionarioId funcionarioId, DateOnly dia);
}

public interface ILeituraDePlanejamento
{
    LeituraDoDia Ler(FuncionarioId funcionarioId, DateOnly dia);
}

public sealed class ServicoDePlanejamento : ILeituraDePlanejamento
{
    private readonly IRepositorioDePlanejamento _repositorio;

    public ServicoDePlanejamento(IRepositorioDePlanejamento repositorio) => _repositorio = repositorio;

    public void PublicarEscala(FuncionarioId funcionarioId, DateOnly dia, IReadOnlyList<IntervaloDeEscala> intervalos)
        => _repositorio.SalvarEscala(EscalaDoDia.Publicar(funcionarioId, dia, intervalos));

    public void RemoverEscala(FuncionarioId funcionarioId, DateOnly dia)
        => _repositorio.RemoverEscala(funcionarioId, dia);

    public Guid PublicarEvento(string nome, DateTimeOffset inicio, DateTimeOffset fim, IReadOnlyCollection<FuncionarioId> escalados, TratamentoDeEvento tratamento)
    {
        var evento = Evento.Criar(nome, inicio, fim, escalados, tratamento);
        GradeDeEventos.GarantirSemConflito(evento, _repositorio.ListarEventos());
        _repositorio.SalvarEvento(evento);
        return evento.Id;
    }

    public void AlterarEvento(Guid id, string nome, DateTimeOffset inicio, DateTimeOffset fim, IReadOnlyCollection<FuncionarioId> escalados, TratamentoDeEvento tratamento)
    {
        var evento = _repositorio.ObterEvento(id) ?? throw new ViolacaoDeRegra("Intervalo inválido.");
        evento.Alterar(nome, inicio, fim, escalados, tratamento);
        GradeDeEventos.GarantirSemConflito(evento, _repositorio.ListarEventos());
        _repositorio.SalvarEvento(evento);
    }

    public void DefinirPercentual(decimal valor)
        => _repositorio.SalvarPercentual(PercentualExtraComum.Definir(valor));

    public LeituraDoDia Ler(FuncionarioId funcionarioId, DateOnly dia) => _repositorio.Ler(funcionarioId, dia);
}
```

Criar `koinonia-api/src/Koinonia.Planejamento.Infrastructure/PlanejamentoDbContext.cs`:

```csharp
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
```

Criar `koinonia-api/src/Koinonia.Planejamento.Infrastructure/RepositorioDePlanejamento.cs`:

```csharp
using Koinonia.Planejamento.Application;
using Koinonia.Planejamento.Domain;
using Koinonia.Shared;

namespace Koinonia.Planejamento.Infrastructure;

public sealed class RepositorioDePlanejamento : IRepositorioDePlanejamento
{
    private readonly PlanejamentoDbContext _contexto;

    public RepositorioDePlanejamento(PlanejamentoDbContext contexto) => _contexto = contexto;

    public void SalvarEscala(EscalaDoDia escala)
    {
        RemoverEscala(escala.FuncionarioId, escala.Dia);
        var row = new EscalaRow
        {
            Id = Guid.NewGuid(),
            FuncionarioId = escala.FuncionarioId.Valor,
            Dia = escala.Dia,
            Intervalos = escala.Intervalos.Select(item => new IntervaloEscalaRow
            {
                Id = Guid.NewGuid(),
                Inicio = item.Inicio,
                Fim = item.Fim
            }).ToList()
        };
        _contexto.Escalas.Add(row);
        _contexto.SaveChanges();
    }

    public void RemoverEscala(FuncionarioId funcionarioId, DateOnly dia)
    {
        var existente = _contexto.Escalas.FirstOrDefault(item => item.FuncionarioId == funcionarioId.Valor && item.Dia == dia);
        if (existente is null)
            return;
        _contexto.Escalas.Remove(existente);
        _contexto.SaveChanges();
    }

    public void SalvarEvento(Evento evento)
    {
        var row = _contexto.Eventos.FirstOrDefault(item => item.Id == evento.Id);
        if (row is null)
        {
            row = new EventoRow { Id = evento.Id };
            _contexto.Eventos.Add(row);
        }

        row.Nome = evento.Nome;
        row.Inicio = evento.Inicio;
        row.Fim = evento.Fim;
        row.Forma = (int)evento.Tratamento.Forma;
        row.Percentual = evento.Tratamento.Percentual;
        row.Fator = evento.Tratamento.Fator;
        _contexto.Escalados.RemoveRange(_contexto.Escalados.Where(item => item.EventoId == evento.Id));
        _contexto.Escalados.AddRange(evento.Escalados.Select(item => new EventoEscaladoRow
        {
            EventoId = evento.Id,
            FuncionarioId = item.Valor
        }));
        _contexto.SaveChanges();
    }

    public Evento? ObterEvento(Guid id)
    {
        var row = _contexto.Eventos.FirstOrDefault(item => item.Id == id);
        return row is null ? null : Mapear(row);
    }

    public IReadOnlyList<Evento> ListarEventos()
        => _contexto.Eventos.ToList().Select(Mapear).ToList();

    public void SalvarPercentual(PercentualExtraComum percentual)
    {
        var row = _contexto.Percentual.FirstOrDefault(item => item.Id == 1);
        if (row is null)
        {
            row = new PercentualRow { Id = 1 };
            _contexto.Percentual.Add(row);
        }

        row.Valor = percentual.Valor;
        _contexto.SaveChanges();
    }

    public LeituraDoDia Ler(FuncionarioId funcionarioId, DateOnly dia)
    {
        var escala = _contexto.Escalas.FirstOrDefault(item => item.FuncionarioId == funcionarioId.Valor && item.Dia == dia);
        var intervalos = escala is null
            ? []
            : _contexto.Intervalos.Where(item => item.EscalaId == escala.Id)
                .OrderBy(item => item.Inicio)
                .Select(item => new IntervaloDeEscala(item.Inicio, item.Fim))
                .ToList();

        var eventos = _contexto.Eventos
            .Where(item => _contexto.Escalados.Any(escalado => escalado.EventoId == item.Id && escalado.FuncionarioId == funcionarioId.Valor))
            .Select(item => new EventoLido(
                item.Id,
                item.Nome,
                item.Inicio,
                item.Fim,
                new TratamentoDeEvento((FormaDePagamentoDoEvento)item.Forma, item.Percentual, item.Fator)))
            .ToList();

        var percentual = _contexto.Percentual.FirstOrDefault(item => item.Id == 1)?.Valor ?? 0m;
        return new LeituraDoDia(intervalos, eventos, percentual);
    }

    private Evento Mapear(EventoRow row)
    {
        var escalados = _contexto.Escalados.Where(item => item.EventoId == row.Id).Select(item => FuncionarioId.De(item.FuncionarioId)).ToList();
        var tratamento = new TratamentoDeEvento((FormaDePagamentoDoEvento)row.Forma, row.Percentual, row.Fator);
        return Evento.Reidratar(row.Id, row.Nome, row.Inicio, row.Fim, escalados, tratamento);
    }
}
```

- [ ] **Step 5: Gerar a migration e ver o teste passar**

```powershell
dotnet ef migrations add Inicial --project koinonia-api/src/Koinonia.Planejamento.Infrastructure --startup-project koinonia-api/src/Koinonia.Planejamento.Infrastructure --context PlanejamentoDbContext --output-dir Persistencia/Migrations
dotnet test koinonia-api/tests/Koinonia.Planejamento.Application.Tests/Koinonia.Planejamento.Application.Tests.csproj --filter ServicoDePlanejamentoTestes
```

Expected: PASS. Se `dotnet ef` não existir, rode antes `dotnet tool install dotnet-ef --global`.

- [ ] **Step 6: Commit**

```powershell
git add koinonia-api
git commit -m "feat: persistir escala, evento e percentual do planejamento"
```

---

### Task 8: Cadastro de funcionário no host

**Files:**
- Create: `koinonia-api/src/Koinonia.Api/Cadastro/Funcionario.cs`
- Create: `koinonia-api/src/Koinonia.Api/Cadastro/CadastroDbContext.cs`
- Create: `koinonia-api/src/Koinonia.Api/Cadastro/CadastroDeFuncionarios.cs`
- Test: `koinonia-api/tests/Koinonia.Api.Tests/CadastroDeFuncionariosTestes.cs`

**Interfaces:**
- Consumes: `FuncionarioId`, `ViolacaoDeRegra`
- Produces: `CadastroDeFuncionarios.Cadastrar(string nome)` devolve `FuncionarioCadastrado(FuncionarioId Id, string Nome)`. `Existe(FuncionarioId)`. `Listar()`. `ObterNome(FuncionarioId)`.

- [ ] **Step 1: Criar o host e o teste**

```powershell
dotnet new web -n Koinonia.Api -o koinonia-api/src/Koinonia.Api -f net10.0 --force
dotnet new xunit -n Koinonia.Api.Tests -o koinonia-api/tests/Koinonia.Api.Tests -f net10.0 --force
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/src/Koinonia.Api/Koinonia.Api.csproj
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/tests/Koinonia.Api.Tests/Koinonia.Api.Tests.csproj
dotnet add koinonia-api/src/Koinonia.Api/Koinonia.Api.csproj reference koinonia-api/src/Koinonia.Shared/Koinonia.Shared.csproj
dotnet add koinonia-api/tests/Koinonia.Api.Tests/Koinonia.Api.Tests.csproj reference koinonia-api/src/Koinonia.Api/Koinonia.Api.csproj
dotnet add koinonia-api/src/Koinonia.Api/Koinonia.Api.csproj package Microsoft.EntityFrameworkCore.Sqlite
dotnet add koinonia-api/src/Koinonia.Api/Koinonia.Api.csproj package Microsoft.EntityFrameworkCore.Design
Remove-Item koinonia-api/tests/Koinonia.Api.Tests/UnitTest1.cs -ErrorAction SilentlyContinue
```

- [ ] **Step 2: Escrever o teste**

Criar `koinonia-api/tests/Koinonia.Api.Tests/CadastroDeFuncionariosTestes.cs`:

```csharp
using Koinonia.Api.Cadastro;
using Koinonia.Shared;
using Microsoft.EntityFrameworkCore;

namespace Koinonia.Api.Tests;

public class CadastroDeFuncionariosTestes : IDisposable
{
    private readonly string _arquivo = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
    private readonly CadastroDbContext _contexto;
    private readonly CadastroDeFuncionarios _cadastro;

    public CadastroDeFuncionariosTestes()
    {
        var options = new DbContextOptionsBuilder<CadastroDbContext>().UseSqlite($"Data Source={_arquivo}").Options;
        _contexto = new CadastroDbContext(options);
        _contexto.Database.Migrate();
        _cadastro = new CadastroDeFuncionarios(_contexto);
    }

    [Fact]
    public void Cadastrar_gera_id_e_guarda_o_nome()
    {
        var criado = _cadastro.Cadastrar("Ana");
        Assert.NotEqual(Guid.Empty, criado.Id.Valor);
        Assert.Equal("Ana", _cadastro.ObterNome(criado.Id));
        Assert.True(_cadastro.Existe(criado.Id));
    }

    [Fact]
    public void Nome_em_branco_e_recusado()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => _cadastro.Cadastrar("  "));
        Assert.Equal("Nome inválido.", erro.Message);
    }

    [Fact]
    public void Desconhecido_nao_existe()
    {
        Assert.False(_cadastro.Existe(FuncionarioId.Novo()));
    }

    public void Dispose()
    {
        _contexto.Dispose();
        if (File.Exists(_arquivo))
            File.Delete(_arquivo);
    }
}
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `dotnet test koinonia-api/tests/Koinonia.Api.Tests/Koinonia.Api.Tests.csproj --filter CadastroDeFuncionariosTestes`
Expected: FAIL de compilação.

- [ ] **Step 4: Implementar o cadastro**

Criar `koinonia-api/src/Koinonia.Api/Cadastro/Funcionario.cs`:

```csharp
namespace Koinonia.Api.Cadastro;

public sealed class Funcionario
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
}
```

Criar `koinonia-api/src/Koinonia.Api/Cadastro/CadastroDbContext.cs`:

```csharp
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
```

Criar `koinonia-api/src/Koinonia.Api/Cadastro/CadastroDeFuncionarios.cs`:

```csharp
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
```

- [ ] **Step 5: Gerar a migration e ver passar**

```powershell
dotnet ef migrations add Inicial --project koinonia-api/src/Koinonia.Api --startup-project koinonia-api/src/Koinonia.Api --context CadastroDbContext --output-dir Cadastro/Migrations
dotnet test koinonia-api/tests/Koinonia.Api.Tests/Koinonia.Api.Tests.csproj --filter CadastroDeFuncionariosTestes
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add koinonia-api
git commit -m "feat: cadastrar funcionario com id gerado pelo host"
```

---

### Task 9: Batida e dia de ponto

**Files:**
- Create: `koinonia-api/src/Koinonia.Ponto.Domain/Batida.cs`
- Create: `koinonia-api/src/Koinonia.Ponto.Domain/DiaDePonto.cs`
- Test: `koinonia-api/tests/Koinonia.Ponto.Domain.Tests/BatidaEDiaTestes.cs`

**Interfaces:**
- Consumes: `FuncionarioId`, `ViolacaoDeRegra`, `TrechoClassificado`, `Fatia`, `FormaDePagamento`
- Produces: `Batida.Abrir`, `Batida.Fechar`, `Batida.Reidratar`. `DiaDePonto.Abrir`, `Aprovar`, `Reabrir`, `Reidratar`. `TrechoGravado.De(TrechoClassificado)`.

- [ ] **Step 1: Escrever os testes**

Criar `koinonia-api/tests/Koinonia.Ponto.Domain.Tests/BatidaEDiaTestes.cs`:

```csharp
using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Ponto.Domain.Tests;

public class BatidaEDiaTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 6, 20, 0, 0, TimeSpan.FromHours(-3));
    private static readonly FuncionarioId Pessoa = FuncionarioId.Novo();

    [Fact]
    public void Entrada_no_futuro_e_recusada()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => Batida.Abrir(Pessoa, Agora.AddMinutes(1), Agora));
        Assert.Equal("Entrada ou saída no futuro é recusada.", erro.Message);
    }

    [Fact]
    public void Saida_anterior_a_entrada_e_recusada()
    {
        var batida = Batida.Abrir(Pessoa, Agora.AddHours(-2), Agora);
        var erro = Assert.Throws<ViolacaoDeRegra>(() => batida.Fechar(Agora.AddHours(-3), Agora));
        Assert.Equal("Saída anterior à entrada.", erro.Message);
    }

    [Fact]
    public void Aprovar_duas_vezes_e_recusado()
    {
        var dia = DiaDePonto.Abrir(Pessoa, new DateOnly(2026, 10, 6));
        dia.Aprovar([]);
        var erro = Assert.Throws<ViolacaoDeRegra>(() => dia.Aprovar([]));
        Assert.Equal("Aprovar um dia já aprovado é recusado.", erro.Message);
    }

    [Fact]
    public void Reabrir_dia_aberto_e_recusado()
    {
        var dia = DiaDePonto.Abrir(Pessoa, new DateOnly(2026, 10, 6));
        var erro = Assert.Throws<ViolacaoDeRegra>(dia.Reabrir);
        Assert.Equal("Reabrir um dia aberto é recusado.", erro.Message);
    }

    [Fact]
    public void Aprovar_copia_o_trecho_e_reabrir_remove_a_copia()
    {
        var dia = DiaDePonto.Abrir(Pessoa, new DateOnly(2026, 10, 6));
        var trecho = new TrechoClassificado(
            new IntervaloMinutos(8 * 60, 9 * 60),
            Fatia.ExtraComum,
            null,
            null,
            null,
            50m);

        dia.Aprovar([trecho]);
        Assert.Equal(50m, Assert.Single(dia.Trechos).Percentual);

        dia.Reabrir();
        Assert.False(dia.Aprovado);
        Assert.Empty(dia.Trechos);
    }
}
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test koinonia-api/tests/Koinonia.Ponto.Domain.Tests/Koinonia.Ponto.Domain.Tests.csproj --filter BatidaEDiaTestes`
Expected: FAIL de compilação.

- [ ] **Step 3: Implementar**

Criar `koinonia-api/src/Koinonia.Ponto.Domain/Batida.cs`:

```csharp
using Koinonia.Shared;

namespace Koinonia.Ponto.Domain;

public sealed class Batida
{
    private Batida() { }

    public Guid Id { get; private set; }
    public FuncionarioId FuncionarioId { get; private set; }
    public DateTimeOffset Entrada { get; private set; }
    public DateTimeOffset? Saida { get; private set; }
    public bool Aberta => Saida is null;

    public static Batida Abrir(FuncionarioId funcionarioId, DateTimeOffset entrada, DateTimeOffset agora)
    {
        if (entrada > agora)
            throw new ViolacaoDeRegra("Entrada ou saída no futuro é recusada.");
        return new Batida { Id = Guid.NewGuid(), FuncionarioId = funcionarioId, Entrada = entrada };
    }

    public static Batida Reidratar(Guid id, FuncionarioId funcionarioId, DateTimeOffset entrada, DateTimeOffset? saida)
        => new() { Id = id, FuncionarioId = funcionarioId, Entrada = entrada, Saida = saida };

    public void Fechar(DateTimeOffset saida, DateTimeOffset agora)
    {
        if (saida > agora)
            throw new ViolacaoDeRegra("Entrada ou saída no futuro é recusada.");
        if (saida <= Entrada)
            throw new ViolacaoDeRegra("Saída anterior à entrada.");
        Saida = saida;
    }
}
```

Criar `koinonia-api/src/Koinonia.Ponto.Domain/DiaDePonto.cs`:

```csharp
using Koinonia.Shared;

namespace Koinonia.Ponto.Domain;

public sealed record TrechoGravado(
    int Inicio,
    int Fim,
    Fatia Fatia,
    Guid? EventoId,
    string? NomeEvento,
    FormaDePagamento? Forma,
    decimal? Percentual,
    decimal? Fator,
    int MinutosTrabalhados,
    int? MinutosCreditados)
{
    public static TrechoGravado De(TrechoClassificado trecho)
    {
        var percentual = trecho.Fatia == Fatia.ExtraComum
            ? trecho.PercentualExtraComum
            : trecho.Tratamento?.Percentual;
        return new TrechoGravado(
            trecho.Intervalo.Inicio,
            trecho.Intervalo.Fim,
            trecho.Fatia,
            trecho.EventoId,
            trecho.NomeEvento,
            trecho.Tratamento?.Forma,
            percentual,
            trecho.Tratamento?.Fator,
            trecho.MinutosTrabalhados,
            trecho.MinutosCreditados);
    }
}

public sealed class DiaDePonto
{
    private readonly List<TrechoGravado> _trechos = [];

    private DiaDePonto(FuncionarioId funcionarioId, DateOnly dia)
    {
        FuncionarioId = funcionarioId;
        Dia = dia;
    }

    public FuncionarioId FuncionarioId { get; }
    public DateOnly Dia { get; }
    public bool Aprovado { get; private set; }
    public IReadOnlyList<TrechoGravado> Trechos => _trechos;

    public static DiaDePonto Abrir(FuncionarioId funcionarioId, DateOnly dia) => new(funcionarioId, dia);

    public static DiaDePonto Reidratar(FuncionarioId funcionarioId, DateOnly dia, bool aprovado, IEnumerable<TrechoGravado> trechos)
    {
        var diaDePonto = new DiaDePonto(funcionarioId, dia);
        if (!aprovado)
            return diaDePonto;
        diaDePonto._trechos.AddRange(trechos);
        diaDePonto.Aprovado = true;
        return diaDePonto;
    }

    public void Aprovar(IReadOnlyList<TrechoClassificado> trechos)
    {
        if (Aprovado)
            throw new ViolacaoDeRegra("Aprovar um dia já aprovado é recusado.");
        _trechos.Clear();
        _trechos.AddRange(trechos.Select(TrechoGravado.De));
        Aprovado = true;
    }

    public void Reabrir()
    {
        if (!Aprovado)
            throw new ViolacaoDeRegra("Reabrir um dia aberto é recusado.");
        _trechos.Clear();
        Aprovado = false;
    }
}
```

- [ ] **Step 4: Rodar e ver passar**

Run: `dotnet test koinonia-api/tests/Koinonia.Ponto.Domain.Tests/Koinonia.Ponto.Domain.Tests.csproj --filter BatidaEDiaTestes`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add koinonia-api
git commit -m "feat: travar batida futura e copia do dia aprovado"
```

---

### Task 10: Prévia, aprovação e reabertura

**Files:**
- Create: `koinonia-api/src/Koinonia.Ponto.Application/IConsultaDePlanejamento.cs`
- Create: `koinonia-api/src/Koinonia.Ponto.Application/ServicoDePonto.cs`
- Test: `koinonia-api/tests/Koinonia.Ponto.Application.Tests/ServicoDePontoTestes.cs`

**Interfaces:**
- Consumes: `ClassificarHoras`, `Batida`, `DiaDePonto`, `TrechoGravado`, `EventoNoDia`, `IntervaloMinutos`, `TratamentoCopiado`
- Produces: `IConsultaDePlanejamento.Obter(FuncionarioId, DateOnly)` devolve `ConsultaDoDia(IReadOnlyList<IntervaloMinutos> Escala, IReadOnlyList<EventoNoDia> Eventos, decimal PercentualExtraComum)`. `IRelogio.Agora()`. `IRepositorioDePonto` com `ListarBatidas`, `Salvar(Batida)`, `Remover(Guid)`, `ObterOuAbrir`, `Salvar(DiaDePonto)`, `ListarAprovados(DateOnly inicio, DateOnly fim)`. `ServicoDePonto.ObterEspelho`, `Aprovar`, `Reabrir`, `RegistrarEntrada`, `RegistrarSaida`, `RemoverBatida`. `Espelho(bool Aprovado, IReadOnlyList<TrechoGravado> Trechos)`.

- [ ] **Step 1: Criar o projeto de aplicação e o teste**

```powershell
dotnet new classlib -n Koinonia.Ponto.Application -o koinonia-api/src/Koinonia.Ponto.Application -f net10.0 --force
dotnet new xunit -n Koinonia.Ponto.Application.Tests -o koinonia-api/tests/Koinonia.Ponto.Application.Tests -f net10.0 --force
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/src/Koinonia.Ponto.Application/Koinonia.Ponto.Application.csproj
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/tests/Koinonia.Ponto.Application.Tests/Koinonia.Ponto.Application.Tests.csproj
dotnet add koinonia-api/src/Koinonia.Ponto.Application/Koinonia.Ponto.Application.csproj reference koinonia-api/src/Koinonia.Ponto.Domain/Koinonia.Ponto.Domain.csproj
dotnet add koinonia-api/tests/Koinonia.Ponto.Application.Tests/Koinonia.Ponto.Application.Tests.csproj reference koinonia-api/src/Koinonia.Ponto.Application/Koinonia.Ponto.Application.csproj
Remove-Item koinonia-api/src/Koinonia.Ponto.Application/Class1.cs -ErrorAction SilentlyContinue
Remove-Item koinonia-api/tests/Koinonia.Ponto.Application.Tests/UnitTest1.cs -ErrorAction SilentlyContinue
```

- [ ] **Step 2: Escrever o teste com porta falsa**

Criar `koinonia-api/tests/Koinonia.Ponto.Application.Tests/ServicoDePontoTestes.cs`:

```csharp
using Koinonia.Ponto.Application;
using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Ponto.Application.Tests;

public class ServicoDePontoTestes
{
    private readonly DateTimeOffset _agora = new(2026, 10, 6, 20, 0, 0, TimeSpan.FromHours(-3));
    private readonly FuncionarioId _pessoa = FuncionarioId.Novo();
    private readonly DateOnly _dia = new(2026, 10, 6);
    private readonly ConsultaFalsa _consulta = new();
    private readonly RepositorioFalso _repositorio = new();
    private readonly ServicoDePonto _servico;

    public ServicoDePontoTestes()
    {
        _servico = new ServicoDePonto(_consulta, _repositorio, new RelogioFixo(_agora));
    }

    [Fact]
    public void Previa_nao_grava_trecho()
    {
        FecharJornada();
        var espelho = _servico.ObterEspelho(_pessoa, _dia);

        Assert.False(espelho.Aprovado);
        Assert.Equal(3, espelho.Trechos.Count);
        Assert.Equal(0, _repositorio.SalvamentosDeDia);
    }

    [Fact]
    public void Aprovacao_copia_os_numeros_e_ignora_mudanca_posterior_do_plano()
    {
        FecharJornada();
        _servico.Aprovar(_pessoa, _dia);
        _consulta.Percentual = 80m;

        var espelho = _servico.ObterEspelho(_pessoa, _dia);
        var extra = espelho.Trechos.Single(item => item.Fatia == Fatia.ExtraComum);

        Assert.True(espelho.Aprovado);
        Assert.Equal(50m, extra.Percentual);
        Assert.Equal(1, _repositorio.SalvamentosDeDia);
    }

    [Fact]
    public void Entrada_sem_saida_impede_a_aprovacao()
    {
        _repositorio.Batidas.Add(Batida.Abrir(_pessoa, _agora.AddHours(-1), _agora));
        var erro = Assert.Throws<ViolacaoDeRegra>(() => _servico.Aprovar(_pessoa, _dia));
        Assert.Equal("Aprovar com entrada sem saída é recusado.", erro.Message);
        Assert.Equal(0, _repositorio.SalvamentosDeDia);
    }

    [Fact]
    public void Sem_intervalo_fechado_a_aprovacao_e_recusada()
    {
        var erro = Assert.Throws<ViolacaoDeRegra>(() => _servico.Aprovar(_pessoa, _dia));
        Assert.Equal("Aprovar sem intervalo fechado é recusado.", erro.Message);
    }

    [Fact]
    public void Batida_em_dia_aprovado_e_recusada()
    {
        FecharJornada();
        _servico.Aprovar(_pessoa, _dia);
        var erro = Assert.Throws<ViolacaoDeRegra>(() => _servico.RegistrarEntrada(_pessoa, _agora.AddMinutes(-30)));
        Assert.Equal("Batida em dia aprovado é recusada até a gestão reabrir.", erro.Message);
    }

    [Fact]
    public void Intervalo_que_cruza_outro_fechado_e_recusado()
    {
        _servico.RegistrarEntrada(_pessoa, _agora.AddHours(-4));
        var id = _repositorio.Batidas.Single().Id;
        _servico.RegistrarSaida(id, _pessoa, _agora.AddHours(-1));

        _servico.RegistrarEntrada(_pessoa, _agora.AddHours(-3));
        var segunda = _repositorio.Batidas.Single(item => item.Aberta).Id;
        var erro = Assert.Throws<ViolacaoDeRegra>(() => _servico.RegistrarSaida(segunda, _pessoa, _agora.AddMinutes(-30)));
        Assert.Equal("Intervalo que cruza outro intervalo já fechado da mesma pessoa é recusado.", erro.Message);
    }

    private void FecharJornada()
    {
        var entrada = new DateTimeOffset(2026, 10, 6, 8, 0, 0, TimeSpan.FromHours(-3));
        var saida = new DateTimeOffset(2026, 10, 6, 19, 0, 0, TimeSpan.FromHours(-3));
        _servico.RegistrarEntrada(_pessoa, entrada);
        _servico.RegistrarSaida(_repositorio.Batidas.Single().Id, _pessoa, saida);
    }

    private sealed class RelogioFixo(DateTimeOffset agora) : IRelogio
    {
        public DateTimeOffset Agora() => agora;
    }

    private sealed class ConsultaFalsa : IConsultaDePlanejamento
    {
        public decimal Percentual { get; set; } = 50m;

        public ConsultaDoDia Obter(FuncionarioId funcionarioId, DateOnly dia) => new(
            [new IntervaloMinutos(8 * 60, 17 * 60)],
            [new EventoNoDia(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Formatura", new IntervaloMinutos(14 * 60, 18 * 60), TratamentoCopiado.Banco(1m))],
            Percentual);
    }

    private sealed class RepositorioFalso : IRepositorioDePonto
    {
        public List<Batida> Batidas { get; } = [];
        public DiaDePonto? Dia { get; private set; }
        public int SalvamentosDeDia { get; private set; }

        public IReadOnlyList<Batida> ListarBatidas(FuncionarioId funcionarioId)
            => Batidas.Where(item => item.FuncionarioId == funcionarioId).ToList();

        public void Salvar(Batida batida)
        {
            Batidas.RemoveAll(item => item.Id == batida.Id);
            Batidas.Add(batida);
        }

        public void Remover(Guid batidaId) => Batidas.RemoveAll(item => item.Id == batidaId);

        public DiaDePonto ObterOuAbrir(FuncionarioId funcionarioId, DateOnly dia)
            => Dia ?? DiaDePonto.Abrir(funcionarioId, dia);

        public void Salvar(DiaDePonto dia)
        {
            SalvamentosDeDia++;
            Dia = dia;
        }

        public IReadOnlyList<DiaDePonto> ListarAprovados(DateOnly inicio, DateOnly fim)
            => Dia is { Aprovado: true } && Dia.Dia >= inicio && Dia.Dia <= fim ? [Dia] : [];
    }
}
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `dotnet test koinonia-api/tests/Koinonia.Ponto.Application.Tests/Koinonia.Ponto.Application.Tests.csproj --filter ServicoDePontoTestes`
Expected: FAIL de compilação.

- [ ] **Step 4: Implementar a porta e o serviço**

Criar `koinonia-api/src/Koinonia.Ponto.Application/IConsultaDePlanejamento.cs`:

```csharp
using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Ponto.Application;

public sealed record ConsultaDoDia(
    IReadOnlyList<IntervaloMinutos> Escala,
    IReadOnlyList<EventoNoDia> Eventos,
    decimal PercentualExtraComum);

public interface IConsultaDePlanejamento
{
    ConsultaDoDia Obter(FuncionarioId funcionarioId, DateOnly dia);
}

public interface IRelogio
{
    DateTimeOffset Agora();
}

public interface IRepositorioDePonto
{
    IReadOnlyList<Batida> ListarBatidas(FuncionarioId funcionarioId);
    void Salvar(Batida batida);
    void Remover(Guid batidaId);
    DiaDePonto ObterOuAbrir(FuncionarioId funcionarioId, DateOnly dia);
    void Salvar(DiaDePonto dia);
    IReadOnlyList<DiaDePonto> ListarAprovados(DateOnly inicio, DateOnly fim);
}

public sealed record Espelho(bool Aprovado, IReadOnlyList<TrechoGravado> Trechos);
```

Criar `koinonia-api/src/Koinonia.Ponto.Application/ServicoDePonto.cs`:

```csharp
using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Ponto.Application;

public sealed class ServicoDePonto
{
    private readonly IConsultaDePlanejamento _consulta;
    private readonly IRepositorioDePonto _repositorio;
    private readonly IRelogio _relogio;

    public ServicoDePonto(IConsultaDePlanejamento consulta, IRepositorioDePonto repositorio, IRelogio relogio)
    {
        _consulta = consulta;
        _repositorio = repositorio;
        _relogio = relogio;
    }

    public Espelho ObterEspelho(FuncionarioId funcionarioId, DateOnly dia)
    {
        var diaDePonto = _repositorio.ObterOuAbrir(funcionarioId, dia);
        if (diaDePonto.Aprovado)
            return new Espelho(true, diaDePonto.Trechos);

        var consulta = _consulta.Obter(funcionarioId, dia);
        var trechos = ClassificarHoras.Executar(
            IntervalosFechados(funcionarioId, dia),
            consulta.Escala,
            consulta.Eventos,
            consulta.PercentualExtraComum);
        return new Espelho(false, trechos.Select(TrechoGravado.De).ToList());
    }

    public void Aprovar(FuncionarioId funcionarioId, DateOnly dia)
    {
        var batidas = BatidasDoDia(funcionarioId, dia);
        if (batidas.Any(item => item.Aberta))
            throw new ViolacaoDeRegra("Aprovar com entrada sem saída é recusado.");
        if (!batidas.Any(item => !item.Aberta))
            throw new ViolacaoDeRegra("Aprovar sem intervalo fechado é recusado.");

        var diaDePonto = _repositorio.ObterOuAbrir(funcionarioId, dia);
        var consulta = _consulta.Obter(funcionarioId, dia);
        var trechos = ClassificarHoras.Executar(
            IntervalosFechados(funcionarioId, dia),
            consulta.Escala,
            consulta.Eventos,
            consulta.PercentualExtraComum);
        diaDePonto.Aprovar(trechos);
        _repositorio.Salvar(diaDePonto);
    }

    public void Reabrir(FuncionarioId funcionarioId, DateOnly dia)
    {
        var diaDePonto = _repositorio.ObterOuAbrir(funcionarioId, dia);
        diaDePonto.Reabrir();
        _repositorio.Salvar(diaDePonto);
    }

    public Guid RegistrarEntrada(FuncionarioId funcionarioId, DateTimeOffset entrada)
    {
        var agora = _relogio.Agora();
        var dia = ClassificarHoras.DiaCivil(entrada);
        GarantirDiaAberto(funcionarioId, dia);
        if (_repositorio.ListarBatidas(funcionarioId).Any(item => item.Aberta))
            throw new ViolacaoDeRegra("Já existe entrada sem saída.");
        var batida = Batida.Abrir(funcionarioId, entrada, agora);
        _repositorio.Salvar(batida);
        return batida.Id;
    }

    public void RegistrarSaida(Guid batidaId, FuncionarioId funcionarioId, DateTimeOffset saida)
    {
        var batida = Obter(batidaId, funcionarioId);
        var agora = _relogio.Agora();
        if (saida > agora)
            throw new ViolacaoDeRegra("Entrada ou saída no futuro é recusada.");
        if (saida <= batida.Entrada)
            throw new ViolacaoDeRegra("Saída anterior à entrada.");
        foreach (var pedaco in ClassificarHoras.Cortar(batida.Entrada, saida))
            GarantirDiaAberto(funcionarioId, pedaco.Dia);
        GarantirSemIntersecao(funcionarioId, batida.Entrada, saida, batida.Id);
        batida.Fechar(saida, agora);
        _repositorio.Salvar(batida);
    }

    public void RemoverBatida(Guid batidaId, FuncionarioId funcionarioId)
    {
        var batida = Obter(batidaId, funcionarioId);
        if (batida.Saida is null)
        {
            GarantirDiaAberto(funcionarioId, ClassificarHoras.DiaCivil(batida.Entrada));
        }
        else
        {
            foreach (var pedaco in ClassificarHoras.Cortar(batida.Entrada, batida.Saida.Value))
                GarantirDiaAberto(funcionarioId, pedaco.Dia);
        }

        _repositorio.Remover(batidaId);
    }

    private Batida Obter(Guid batidaId, FuncionarioId funcionarioId)
        => _repositorio.ListarBatidas(funcionarioId).FirstOrDefault(item => item.Id == batidaId)
           ?? throw new ViolacaoDeRegra("Funcionário desconhecido é recusado no host.");

    private void GarantirDiaAberto(FuncionarioId funcionarioId, DateOnly dia)
    {
        if (_repositorio.ObterOuAbrir(funcionarioId, dia).Aprovado)
            throw new ViolacaoDeRegra("Batida em dia aprovado é recusada até a gestão reabrir.");
    }

    private void GarantirSemIntersecao(FuncionarioId funcionarioId, DateTimeOffset entrada, DateTimeOffset saida, Guid ignorar)
    {
        foreach (var outra in _repositorio.ListarBatidas(funcionarioId))
        {
            if (outra.Id == ignorar || outra.Saida is null)
                continue;
            if (entrada < outra.Saida && outra.Entrada < saida)
                throw new ViolacaoDeRegra("Intervalo que cruza outro intervalo já fechado da mesma pessoa é recusado.");
        }
    }

    private List<Batida> BatidasDoDia(FuncionarioId funcionarioId, DateOnly dia)
        => _repositorio.ListarBatidas(funcionarioId).Where(batida =>
            ClassificarHoras.DiaCivil(batida.Entrada) == dia
            || (batida.Saida is not null && ClassificarHoras.Cortar(batida.Entrada, batida.Saida.Value).Any(pedaco => pedaco.Dia == dia)))
            .ToList();

    private List<IntervaloMinutos> IntervalosFechados(FuncionarioId funcionarioId, DateOnly dia)
        => BatidasDoDia(funcionarioId, dia)
            .Where(item => item.Saida is not null)
            .SelectMany(item => ClassificarHoras.Cortar(item.Entrada, item.Saida!.Value))
            .Where(item => item.Dia == dia)
            .Select(item => item.Intervalo)
            .ToList();
}
```

- [ ] **Step 5: Rodar e ver passar**

Run: `dotnet test koinonia-api/tests/Koinonia.Ponto.Application.Tests/Koinonia.Ponto.Application.Tests.csproj --filter ServicoDePontoTestes`
Expected: PASS. O espelho da prévia tem três trechos: regular 8h–14h, evento 14h–18h com 240 minutos creditados, extra 18h–19h a 50.

- [ ] **Step 6: Commit**

```powershell
git add koinonia-api
git commit -m "feat: calcular previa ao vivo e congelar a copia na aprovacao"
```

---

### Task 11: Planilha de fechamento

**Files:**
- Create: `koinonia-api/src/Koinonia.Ponto.Application/ExportadorDeFechamento.cs`
- Test: `koinonia-api/tests/Koinonia.Ponto.Application.Tests/ExportadorDeFechamentoTestes.cs`

**Interfaces:**
- Consumes: `DiaDePonto`, `TrechoGravado`, `FuncionarioId`, `Fatia`, `FormaDePagamento`
- Produces: `ExportadorDeFechamento.Gerar(IReadOnlyList<DiaDePonto> dias, Func<FuncionarioId, string> nome, FormatoDeFechamento formato)` devolve `ArquivoDeFechamento(string NomeDoArquivo, string TipoConteudo, byte[] Conteudo)`. `FormatoDeFechamento.Csv` e `FormatoDeFechamento.Excel`.

- [ ] **Step 1: Adicionar o pacote e o teste**

```powershell
dotnet add koinonia-api/src/Koinonia.Ponto.Application/Koinonia.Ponto.Application.csproj package ClosedXML
```

Criar `koinonia-api/tests/Koinonia.Ponto.Application.Tests/ExportadorDeFechamentoTestes.cs`:

```csharp
using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using Koinonia.Ponto.Application;
using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Ponto.Application.Tests;

public class ExportadorDeFechamentoTestes
{
    private readonly FuncionarioId _ana = FuncionarioId.Novo();

    [Fact]
    public void Periodo_sem_dia_aprovado_gera_so_o_cabecalho()
    {
        var arquivo = ExportadorDeFechamento.Gerar([], id => "Ana", FormatoDeFechamento.Csv);
        var (detalhe, totais) = LerZip(arquivo.Conteudo);
        Assert.Equal(ExportadorDeFechamento.Cabecalho, detalhe.Trim());
        Assert.Equal(ExportadorDeFechamento.Cabecalho, totais.Trim());
    }

    [Fact]
    public void Csv_separa_extra_por_percentual_e_preenche_credito_so_no_banco()
    {
        var dia = DiaAprovado();
        var arquivo = ExportadorDeFechamento.Gerar([dia], id => "Ana", FormatoDeFechamento.Csv);
        var (detalhe, totais) = LerZip(arquivo.Conteudo);

        Assert.Contains("Ana;2026-10-06;08:00;14:00;regular;;;;;360;", detalhe);
        Assert.Contains("Ana;2026-10-06;14:00;18:00;evento;Formatura;banco de horas;;1;240;240", detalhe);
        Assert.Contains("Ana;2026-10-06;18:00;19:00;extra comum;;;50;;60;", detalhe);
        Assert.Contains("extra comum;;;50;;60;", totais);
        Assert.DoesNotContain("2026-10-07", detalhe);
        Assert.Equal("fechamento.zip", arquivo.NomeDoArquivo);
    }

    [Fact]
    public void Excel_tem_abas_Detalhe_e_Totais()
    {
        var arquivo = ExportadorDeFechamento.Gerar([DiaAprovado()], id => "Ana", FormatoDeFechamento.Excel);
        using var pasta = new XLWorkbook(new MemoryStream(arquivo.Conteudo));
        Assert.NotNull(pasta.Worksheet("Detalhe"));
        Assert.NotNull(pasta.Worksheet("Totais"));
        Assert.Equal("funcionário", pasta.Worksheet("Detalhe").Cell(1, 1).GetString());
        Assert.Equal("fechamento.xlsx", arquivo.NomeDoArquivo);
    }

    private DiaDePonto DiaAprovado()
    {
        var dia = DiaDePonto.Abrir(_ana, new DateOnly(2026, 10, 6));
        dia.Aprovar(ClassificarHoras.Executar(
            [new IntervaloMinutos(8 * 60, 19 * 60)],
            [new IntervaloMinutos(8 * 60, 17 * 60)],
            [new EventoNoDia(Guid.NewGuid(), "Formatura", new IntervaloMinutos(14 * 60, 18 * 60), TratamentoCopiado.Banco(1m))],
            50m));
        return dia;
    }

    private static (string Detalhe, string Totais) LerZip(byte[] conteudo)
    {
        using var zip = new ZipArchive(new MemoryStream(conteudo), ZipArchiveMode.Read);
        return (Ler(zip, "detalhe.csv"), Ler(zip, "totais.csv"));
    }

    private static string Ler(ZipArchive zip, string nome)
    {
        var entrada = zip.GetEntry(nome) ?? throw new InvalidOperationException(nome);
        using var leitor = new StreamReader(entrada.Open(), Encoding.UTF8);
        return leitor.ReadToEnd();
    }
}
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test koinonia-api/tests/Koinonia.Ponto.Application.Tests/Koinonia.Ponto.Application.Tests.csproj --filter ExportadorDeFechamentoTestes`
Expected: FAIL de compilação. `ExportadorDeFechamento` não existe.

- [ ] **Step 3: Implementar o exportador**

Criar `koinonia-api/src/Koinonia.Ponto.Application/ExportadorDeFechamento.cs`:

```csharp
using System.Globalization;
using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Ponto.Application;

public enum FormatoDeFechamento { Csv, Excel }

public sealed record ArquivoDeFechamento(string NomeDoArquivo, string TipoConteudo, byte[] Conteudo);

public static class ExportadorDeFechamento
{
    public const string Cabecalho = "funcionário;dia;início;fim;fatia;evento;tratamento;percentual;fator;minutos trabalhados;minutos creditados";
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: true);

    public static ArquivoDeFechamento Gerar(IReadOnlyList<DiaDePonto> dias, Func<FuncionarioId, string> nome, FormatoDeFechamento formato)
    {
        var detalhe = dias
            .SelectMany(dia => dia.Trechos.Select(trecho => Linha(nome(dia.FuncionarioId), dia.Dia, trecho)))
            .OrderBy(item => item[0]).ThenBy(item => item[1]).ThenBy(item => item[2])
            .ToList();
        var totais = detalhe
            .GroupBy(item => (item[0], item[4], item[5], item[6], item[7], item[8]))
            .Select(grupo => new[]
            {
                grupo.Key.Item1, "", "", "", grupo.Key.Item2, grupo.Key.Item3, grupo.Key.Item4, grupo.Key.Item5, grupo.Key.Item6,
                grupo.Sum(item => int.Parse(item[9], Cultura)).ToString(Cultura),
                SomarCredito(grupo)
            })
            .ToList();

        return formato == FormatoDeFechamento.Csv ? Csv(detalhe, totais) : Excel(detalhe, totais);
    }

    private static string[] Linha(string nome, DateOnly dia, TrechoGravado trecho) =>
    [
        nome,
        dia.ToString("yyyy-MM-dd"),
        Formatar(trecho.Inicio),
        Formatar(trecho.Fim),
        FatiaTexto(trecho.Fatia),
        trecho.NomeEvento ?? "",
        FormaTexto(trecho.Forma),
        trecho.Percentual?.ToString(Cultura) ?? "",
        trecho.Fator?.ToString(Cultura) ?? "",
        trecho.MinutosTrabalhados.ToString(Cultura),
        trecho.MinutosCreditados?.ToString(Cultura) ?? ""
    ];

    private static string SomarCredito(IEnumerable<string[]> grupo)
    {
        var valores = grupo.Select(item => item[10]).Where(item => item.Length > 0).Select(item => int.Parse(item, Cultura)).ToList();
        return valores.Count == 0 ? "" : valores.Sum().ToString(Cultura);
    }

    private static string Formatar(int minuto)
        => minuto == 1440 ? "24:00" : TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minuto)).ToString("HH:mm");

    private static string FatiaTexto(Fatia fatia) => fatia switch
    {
        Fatia.Regular => "regular",
        Fatia.ExtraComum => "extra comum",
        Fatia.Evento => "evento",
        _ => ""
    };

    private static string FormaTexto(FormaDePagamento? forma) => forma switch
    {
        FormaDePagamento.HoraNormal => "hora normal",
        FormaDePagamento.Adicional => "adicional",
        FormaDePagamento.BancoDeHoras => "banco de horas",
        _ => ""
    };

    private static ArquivoDeFechamento Csv(IReadOnlyList<string[]> detalhe, IReadOnlyList<string[]> totais)
    {
        using var memoria = new MemoryStream();
        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            Escrever(zip, "detalhe.csv", detalhe);
            Escrever(zip, "totais.csv", totais);
        }

        return new ArquivoDeFechamento("fechamento.zip", "application/zip", memoria.ToArray());
    }

    private static void Escrever(ZipArchive zip, string nome, IReadOnlyList<string[]> linhas)
    {
        var entrada = zip.CreateEntry(nome);
        using var escritor = new StreamWriter(entrada.Open(), Utf8);
        escritor.Write(Cabecalho);
        escritor.Write('\n');
        foreach (var linha in linhas)
        {
            escritor.Write(string.Join(';', linha));
            escritor.Write('\n');
        }
    }

    private static ArquivoDeFechamento Excel(IReadOnlyList<string[]> detalhe, IReadOnlyList<string[]> totais)
    {
        using var pasta = new XLWorkbook();
        Preencher(pasta.Worksheets.Add("Detalhe"), detalhe);
        Preencher(pasta.Worksheets.Add("Totais"), totais);
        using var memoria = new MemoryStream();
        pasta.SaveAs(memoria);
        return new ArquivoDeFechamento(
            "fechamento.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            memoria.ToArray());
    }

    private static void Preencher(IXLWorksheet aba, IReadOnlyList<string[]> linhas)
    {
        var colunas = Cabecalho.Split(';');
        for (var coluna = 0; coluna < colunas.Length; coluna++)
            aba.Cell(1, coluna + 1).Value = colunas[coluna];
        for (var linha = 0; linha < linhas.Count; linha++)
        {
            for (var coluna = 0; coluna < linhas[linha].Length; coluna++)
                aba.Cell(linha + 2, coluna + 1).Value = linhas[linha][coluna];
        }
    }
}
```

- [ ] **Step 4: Rodar e ver passar**

Run: `dotnet test koinonia-api/tests/Koinonia.Ponto.Application.Tests/Koinonia.Ponto.Application.Tests.csproj --filter ExportadorDeFechamentoTestes`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add koinonia-api
git commit -m "feat: exportar fechamento em csv e excel com horas separadas"
```

---

### Task 12: Persistência de Ponto

**Files:**
- Create: `koinonia-api/src/Koinonia.Ponto.Infrastructure/PontoDbContext.cs`
- Create: `koinonia-api/src/Koinonia.Ponto.Infrastructure/RepositorioDePonto.cs`
- Test: `koinonia-api/tests/Koinonia.Ponto.Infrastructure.Tests/RepositorioDePontoTestes.cs`

**Interfaces:**
- Consumes: `IRepositorioDePonto`, `Batida`, `DiaDePonto`, `TrechoGravado`
- Produces: `RepositorioDePonto` e `PontoDbContext`. Tabelas `Batidas`, `DiasDePonto`, `Trechos`. `FuncionarioId` e `EventoId` são colunas `Guid`, sem chave estrangeira para outro módulo.

- [ ] **Step 1: Criar projetos**

```powershell
dotnet new classlib -n Koinonia.Ponto.Infrastructure -o koinonia-api/src/Koinonia.Ponto.Infrastructure -f net10.0 --force
dotnet new xunit -n Koinonia.Ponto.Infrastructure.Tests -o koinonia-api/tests/Koinonia.Ponto.Infrastructure.Tests -f net10.0 --force
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/src/Koinonia.Ponto.Infrastructure/Koinonia.Ponto.Infrastructure.csproj
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/tests/Koinonia.Ponto.Infrastructure.Tests/Koinonia.Ponto.Infrastructure.Tests.csproj
dotnet add koinonia-api/src/Koinonia.Ponto.Infrastructure/Koinonia.Ponto.Infrastructure.csproj reference koinonia-api/src/Koinonia.Ponto.Domain/Koinonia.Ponto.Domain.csproj
dotnet add koinonia-api/src/Koinonia.Ponto.Infrastructure/Koinonia.Ponto.Infrastructure.csproj reference koinonia-api/src/Koinonia.Ponto.Application/Koinonia.Ponto.Application.csproj
dotnet add koinonia-api/src/Koinonia.Ponto.Infrastructure/Koinonia.Ponto.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Sqlite
dotnet add koinonia-api/src/Koinonia.Ponto.Infrastructure/Koinonia.Ponto.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add koinonia-api/tests/Koinonia.Ponto.Infrastructure.Tests/Koinonia.Ponto.Infrastructure.Tests.csproj reference koinonia-api/src/Koinonia.Ponto.Infrastructure/Koinonia.Ponto.Infrastructure.csproj
Remove-Item koinonia-api/src/Koinonia.Ponto.Infrastructure/Class1.cs -ErrorAction SilentlyContinue
Remove-Item koinonia-api/tests/Koinonia.Ponto.Infrastructure.Tests/UnitTest1.cs -ErrorAction SilentlyContinue
```

- [ ] **Step 2: Escrever o teste de ida e volta**

Criar `koinonia-api/tests/Koinonia.Ponto.Infrastructure.Tests/RepositorioDePontoTestes.cs`:

```csharp
using Koinonia.Ponto.Domain;
using Koinonia.Ponto.Infrastructure;
using Koinonia.Shared;
using Microsoft.EntityFrameworkCore;

namespace Koinonia.Ponto.Infrastructure.Tests;

public class RepositorioDePontoTestes : IDisposable
{
    private readonly string _arquivo = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
    private readonly PontoDbContext _contexto;
    private readonly RepositorioDePonto _repositorio;
    private readonly FuncionarioId _pessoa = FuncionarioId.Novo();

    public RepositorioDePontoTestes()
    {
        var options = new DbContextOptionsBuilder<PontoDbContext>().UseSqlite($"Data Source={_arquivo}").Options;
        _contexto = new PontoDbContext(options);
        _contexto.Database.Migrate();
        _repositorio = new RepositorioDePonto(_contexto);
    }

    [Fact]
    public void Dia_aprovado_volta_com_os_numeros_copiados()
    {
        var agora = new DateTimeOffset(2026, 10, 6, 20, 0, 0, TimeSpan.FromHours(-3));
        var batida = Batida.Abrir(_pessoa, agora.AddHours(-2), agora);
        batida.Fechar(agora.AddHours(-1), agora);
        _repositorio.Salvar(batida);

        var dia = DiaDePonto.Abrir(_pessoa, new DateOnly(2026, 10, 6));
        dia.Aprovar(
        [
            new TrechoClassificado(new IntervaloMinutos(18 * 60, 19 * 60), Fatia.ExtraComum, null, null, null, 50m)
        ]);
        _repositorio.Salvar(dia);

        using var outro = new PontoDbContext(new DbContextOptionsBuilder<PontoDbContext>().UseSqlite($"Data Source={_arquivo}").Options);
        var lido = new RepositorioDePonto(outro).ObterOuAbrir(_pessoa, new DateOnly(2026, 10, 6));

        Assert.True(lido.Aprovado);
        Assert.Equal(50m, Assert.Single(lido.Trechos).Percentual);
        Assert.Single(new RepositorioDePonto(outro).ListarAprovados(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)));
    }

    [Fact]
    public void Nao_ha_chave_estrangeira_para_planejamento()
    {
        var modelo = _contexto.Model.GetEntityTypes().SelectMany(item => item.GetForeignKeys());
        Assert.DoesNotContain(modelo, chave => chave.PrincipalEntityType.Name.Contains("Planejamento") || chave.PrincipalEntityType.Name.Contains("Evento"));
    }

    public void Dispose()
    {
        _contexto.Dispose();
        if (File.Exists(_arquivo))
            File.Delete(_arquivo);
    }
}
```

- [ ] **Step 3: Rodar e ver falhar**

Run: `dotnet test koinonia-api/tests/Koinonia.Ponto.Infrastructure.Tests/Koinonia.Ponto.Infrastructure.Tests.csproj --filter RepositorioDePontoTestes`
Expected: FAIL de compilação.

- [ ] **Step 4: Implementar contexto e repositório**

Criar `koinonia-api/src/Koinonia.Ponto.Infrastructure/PontoDbContext.cs`:

```csharp
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
```

Criar `koinonia-api/src/Koinonia.Ponto.Infrastructure/RepositorioDePonto.cs`:

```csharp
using Koinonia.Ponto.Application;
using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Ponto.Infrastructure;

public sealed class RepositorioDePonto : IRepositorioDePonto
{
    private readonly PontoDbContext _contexto;

    public RepositorioDePonto(PontoDbContext contexto) => _contexto = contexto;

    public IReadOnlyList<Batida> ListarBatidas(FuncionarioId funcionarioId)
        => _contexto.Batidas.Where(item => item.FuncionarioId == funcionarioId.Valor).ToList()
            .Select(item => Batida.Reidratar(item.Id, funcionarioId, item.Entrada, item.Saida)).ToList();

    public void Salvar(Batida batida)
    {
        var row = _contexto.Batidas.FirstOrDefault(item => item.Id == batida.Id);
        if (row is null)
        {
            row = new BatidaRow { Id = batida.Id };
            _contexto.Batidas.Add(row);
        }

        row.FuncionarioId = batida.FuncionarioId.Valor;
        row.Entrada = batida.Entrada;
        row.Saida = batida.Saida;
        _contexto.SaveChanges();
    }

    public void Remover(Guid batidaId)
    {
        var row = _contexto.Batidas.FirstOrDefault(item => item.Id == batidaId);
        if (row is null)
            return;
        _contexto.Batidas.Remove(row);
        _contexto.SaveChanges();
    }

    public DiaDePonto ObterOuAbrir(FuncionarioId funcionarioId, DateOnly dia)
    {
        var row = _contexto.Dias.FirstOrDefault(item => item.FuncionarioId == funcionarioId.Valor && item.Dia == dia);
        if (row is null)
            return DiaDePonto.Abrir(funcionarioId, dia);
        var trechos = _contexto.Trechos.Where(item => item.DiaId == row.Id).ToList().Select(Mapear);
        return DiaDePonto.Reidratar(funcionarioId, dia, row.Aprovado, trechos);
    }

    public void Salvar(DiaDePonto dia)
    {
        var row = _contexto.Dias.FirstOrDefault(item => item.FuncionarioId == dia.FuncionarioId.Valor && item.Dia == dia.Dia);
        if (row is null)
        {
            row = new DiaRow { Id = Guid.NewGuid(), FuncionarioId = dia.FuncionarioId.Valor, Dia = dia.Dia };
            _contexto.Dias.Add(row);
        }

        row.Aprovado = dia.Aprovado;
        _contexto.Trechos.RemoveRange(_contexto.Trechos.Where(item => item.DiaId == row.Id));
        _contexto.Trechos.AddRange(dia.Trechos.Select(item => new TrechoRow
        {
            Id = Guid.NewGuid(),
            DiaId = row.Id,
            Inicio = item.Inicio,
            Fim = item.Fim,
            Fatia = (int)item.Fatia,
            EventoId = item.EventoId,
            NomeEvento = item.NomeEvento,
            Forma = item.Forma is null ? null : (int)item.Forma,
            Percentual = item.Percentual,
            Fator = item.Fator,
            MinutosTrabalhados = item.MinutosTrabalhados,
            MinutosCreditados = item.MinutosCreditados
        }));
        _contexto.SaveChanges();
    }

    public IReadOnlyList<DiaDePonto> ListarAprovados(DateOnly inicio, DateOnly fim)
        => _contexto.Dias.Where(item => item.Aprovado && item.Dia >= inicio && item.Dia <= fim).ToList()
            .Select(item => ObterOuAbrir(FuncionarioId.De(item.FuncionarioId), item.Dia)).ToList();

    private static TrechoGravado Mapear(TrechoRow item) => new(
        item.Inicio,
        item.Fim,
        (Fatia)item.Fatia,
        item.EventoId,
        item.NomeEvento,
        item.Forma is null ? null : (FormaDePagamento)item.Forma,
        item.Percentual,
        item.Fator,
        item.MinutosTrabalhados,
        item.MinutosCreditados);
}
```

- [ ] **Step 5: Gerar a migration e ver passar**

```powershell
dotnet ef migrations add Inicial --project koinonia-api/src/Koinonia.Ponto.Infrastructure --startup-project koinonia-api/src/Koinonia.Ponto.Infrastructure --context PontoDbContext --output-dir Persistencia/Migrations
dotnet test koinonia-api/tests/Koinonia.Ponto.Infrastructure.Tests/Koinonia.Ponto.Infrastructure.Tests.csproj --filter RepositorioDePontoTestes
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add koinonia-api
git commit -m "feat: persistir batida e copia aprovada sem chave entre modulos"
```

---

### Task 13: Teste de arquitetura

**Files:**
- Create: `koinonia-api/tests/Koinonia.Architecture.Tests/ArquiteturaTestes.cs`

**Interfaces:**
- Consumes: os assemblies de Domain, Application e Infrastructure dos dois módulos
- Produces: a garantia de que `Ponto` não referencia `Planejamento`, o caminho inverso não existe, e `Domain` não referencia `Microsoft.EntityFrameworkCore`

- [ ] **Step 1: Criar o projeto de teste**

```powershell
dotnet new xunit -n Koinonia.Architecture.Tests -o koinonia-api/tests/Koinonia.Architecture.Tests -f net10.0 --force
dotnet sln koinonia-api/Koinonia.sln add koinonia-api/tests/Koinonia.Architecture.Tests/Koinonia.Architecture.Tests.csproj
dotnet add koinonia-api/tests/Koinonia.Architecture.Tests/Koinonia.Architecture.Tests.csproj package NetArchTest.Rules
dotnet add koinonia-api/tests/Koinonia.Architecture.Tests/Koinonia.Architecture.Tests.csproj reference koinonia-api/src/Koinonia.Ponto.Domain/Koinonia.Ponto.Domain.csproj
dotnet add koinonia-api/tests/Koinonia.Architecture.Tests/Koinonia.Architecture.Tests.csproj reference koinonia-api/src/Koinonia.Ponto.Application/Koinonia.Ponto.Application.csproj
dotnet add koinonia-api/tests/Koinonia.Architecture.Tests/Koinonia.Architecture.Tests.csproj reference koinonia-api/src/Koinonia.Ponto.Infrastructure/Koinonia.Ponto.Infrastructure.csproj
dotnet add koinonia-api/tests/Koinonia.Architecture.Tests/Koinonia.Architecture.Tests.csproj reference koinonia-api/src/Koinonia.Planejamento.Domain/Koinonia.Planejamento.Domain.csproj
dotnet add koinonia-api/tests/Koinonia.Architecture.Tests/Koinonia.Architecture.Tests.csproj reference koinonia-api/src/Koinonia.Planejamento.Application/Koinonia.Planejamento.Application.csproj
dotnet add koinonia-api/tests/Koinonia.Architecture.Tests/Koinonia.Architecture.Tests.csproj reference koinonia-api/src/Koinonia.Planejamento.Infrastructure/Koinonia.Planejamento.Infrastructure.csproj
Remove-Item koinonia-api/tests/Koinonia.Architecture.Tests/UnitTest1.cs -ErrorAction SilentlyContinue
```

- [ ] **Step 2: Escrever o teste**

Criar `koinonia-api/tests/Koinonia.Architecture.Tests/ArquiteturaTestes.cs`:

```csharp
using System.Reflection;
using System.Xml.Linq;
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
```

Remova o `using System.Xml.Linq` se o arquivo não usar XML. O teste de csproj lê texto.

- [ ] **Step 3: Rodar e ver passar**

Run: `dotnet test koinonia-api/tests/Koinonia.Architecture.Tests/Koinonia.Architecture.Tests.csproj`
Expected: PASS. Se falhar porque um csproj ganhou referência cruzada, remova essa referência. O host é o único projeto que pode referenciar os dois módulos, e isso entra na Task 14.

- [ ] **Step 4: Commit**

```powershell
git add koinonia-api
git commit -m "test: impedir que ponto e planejamento se referenciem"
```

---

### Task 14: Host, adaptador e rotas

**Files:**
- Create: `koinonia-api/src/Koinonia.Api/Program.cs`
- Create: `koinonia-api/src/Koinonia.Api/ConsultaDePlanejamentoAdapter.cs`
- Create: `koinonia-api/src/Koinonia.Api/appsettings.json`
- Test: `koinonia-api/tests/Koinonia.Api.Tests/RecorteDeEventoTestes.cs`
- Test: `koinonia-api/tests/Koinonia.Api.Tests/ApiTestes.cs`

**Interfaces:**
- Consumes: `ILeituraDePlanejamento`, `IConsultaDePlanejamento`, `ServicoDePonto`, `ServicoDePlanejamento`, `CadastroDeFuncionarios`, `ExportadorDeFechamento`
- Produces: `ConsultaDePlanejamentoAdapter.Obter`. Rotas HTTP listadas no passo 4. Cabeçalhos `X-Funcionario-Id` e `X-Papel`.

- [ ] **Step 1: Ligar os projetos e escrever o teste do recorte**

```powershell
dotnet add koinonia-api/src/Koinonia.Api/Koinonia.Api.csproj reference koinonia-api/src/Koinonia.Planejamento.Application/Koinonia.Planejamento.Application.csproj
dotnet add koinonia-api/src/Koinonia.Api/Koinonia.Api.csproj reference koinonia-api/src/Koinonia.Planejamento.Infrastructure/Koinonia.Planejamento.Infrastructure.csproj
dotnet add koinonia-api/src/Koinonia.Api/Koinonia.Api.csproj reference koinonia-api/src/Koinonia.Ponto.Application/Koinonia.Ponto.Application.csproj
dotnet add koinonia-api/src/Koinonia.Api/Koinonia.Api.csproj reference koinonia-api/src/Koinonia.Ponto.Infrastructure/Koinonia.Ponto.Infrastructure.csproj
dotnet add koinonia-api/tests/Koinonia.Api.Tests/Koinonia.Api.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing
```

Criar `koinonia-api/tests/Koinonia.Api.Tests/RecorteDeEventoTestes.cs`:

```csharp
using Koinonia.Api;
using Koinonia.Ponto.Domain;

namespace Koinonia.Api.Tests;

public class RecorteDeEventoTestes
{
    [Fact]
    public void Evento_que_cruza_a_meia_noite_vira_um_pedaco_em_cada_dia()
    {
        var inicio = new DateTimeOffset(2026, 10, 6, 22, 0, 0, TimeSpan.FromHours(-3));
        var fim = new DateTimeOffset(2026, 10, 7, 2, 0, 0, TimeSpan.FromHours(-3));

        var noite = ConsultaDePlanejamentoAdapter.Recortar(inicio, fim, new DateOnly(2026, 10, 6));
        var madrugada = ConsultaDePlanejamentoAdapter.Recortar(inicio, fim, new DateOnly(2026, 10, 7));

        Assert.Equal(new IntervaloMinutos(22 * 60, 1440), noite);
        Assert.Equal(new IntervaloMinutos(0, 2 * 60), madrugada);
    }

    [Fact]
    public void Dia_sem_intersecao_devolve_nulo()
    {
        var inicio = new DateTimeOffset(2026, 10, 6, 14, 0, 0, TimeSpan.FromHours(-3));
        var fim = new DateTimeOffset(2026, 10, 6, 18, 0, 0, TimeSpan.FromHours(-3));
        Assert.Null(ConsultaDePlanejamentoAdapter.Recortar(inicio, fim, new DateOnly(2026, 10, 7)));
    }
}
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test koinonia-api/tests/Koinonia.Api.Tests/Koinonia.Api.Tests.csproj --filter RecorteDeEventoTestes`
Expected: FAIL de compilação.

- [ ] **Step 3: Implementar o adaptador**

Criar `koinonia-api/src/Koinonia.Api/ConsultaDePlanejamentoAdapter.cs`:

```csharp
using Koinonia.Planejamento.Application;
using Koinonia.Planejamento.Domain;
using Koinonia.Ponto.Application;
using Koinonia.Ponto.Domain;
using Koinonia.Shared;

namespace Koinonia.Api;

public sealed class ConsultaDePlanejamentoAdapter : IConsultaDePlanejamento
{
    private readonly ILeituraDePlanejamento _leitura;

    public ConsultaDePlanejamentoAdapter(ILeituraDePlanejamento leitura) => _leitura = leitura;

    public ConsultaDoDia Obter(FuncionarioId funcionarioId, DateOnly dia)
    {
        var leitura = _leitura.Ler(funcionarioId, dia);
        var eventos = new List<EventoNoDia>();
        foreach (var evento in leitura.Eventos)
        {
            var recorte = Recortar(evento.Inicio, evento.Fim, dia);
            if (recorte is null)
                continue;
            eventos.Add(new EventoNoDia(evento.Id, evento.Nome, recorte.Value, Mapear(evento.Tratamento)));
        }

        return new ConsultaDoDia(
            leitura.Escala.Select(item => new IntervaloMinutos(item.Inicio, item.Fim)).ToList(),
            eventos,
            leitura.PercentualExtraComum);
    }

    public static IntervaloMinutos? Recortar(DateTimeOffset inicio, DateTimeOffset fim, DateOnly dia)
    {
        var zona = ClassificarHoras.Zona;
        var inicioDia = new DateTimeOffset(dia.ToDateTime(TimeOnly.MinValue), zona.GetUtcOffset(dia.ToDateTime(TimeOnly.MinValue)));
        var fimDia = inicioDia.AddDays(1);
        var a = inicio > inicioDia ? inicio : inicioDia;
        var b = fim < fimDia ? fim : fimDia;
        if (b <= a)
            return null;

        var aLocal = TimeZoneInfo.ConvertTime(a, zona);
        var bLocal = TimeZoneInfo.ConvertTime(b, zona);
        var inicioMinuto = aLocal.Hour * 60 + aLocal.Minute;
        var fimMinuto = b >= fimDia ? 1440 : bLocal.Hour * 60 + bLocal.Minute;
        return fimMinuto <= inicioMinuto ? null : new IntervaloMinutos(inicioMinuto, fimMinuto);
    }

    private static TratamentoCopiado Mapear(TratamentoDeEvento tratamento) => tratamento.Forma switch
    {
        FormaDePagamentoDoEvento.HoraNormal => TratamentoCopiado.HoraNormal(),
        FormaDePagamentoDoEvento.Adicional => TratamentoCopiado.Adicional(tratamento.Percentual!.Value),
        FormaDePagamentoDoEvento.BancoDeHoras => TratamentoCopiado.Banco(tratamento.Fator!.Value),
        _ => throw new ViolacaoDeRegra("Evento sem percentual, quando o tratamento é adicional, é recusado.")
    };
}
```

- [ ] **Step 4: Subir as rotas e o teste de ponta a ponta**

Grave `koinonia-api/src/Koinonia.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Koinonia": "Data Source=koinonia.db"
  }
}
```

Em `koinonia-api/src/Koinonia.Api/Properties/launchSettings.json`, `applicationUrl` é `http://localhost:5188`.

Substitua `Program.cs` por este host. Gestão não precisa existir no cadastro. Empregado precisa. O relógio de produção é `DateTimeOffset.Now`; o teste troca `IRelogio` por 20:00 de 6 de outubro de 2026, senão a batida das 19:00 cai no futuro.

```csharp
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
```

Crie `koinonia-api/tests/Koinonia.Api.Tests/ApiTestes.cs` com o fluxo inteiro num único fato, para a ordem ficar garantida. A factory aponta o sqlite temporário e troca o relógio para 20:00 de 6 de outubro de 2026.

```csharp
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Koinonia.Ponto.Application;
using Microsoft.AspNetCore.Hosting;
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
```

- [ ] **Step 5: Rodar os testes da API**

Run: `dotnet test koinonia-api/tests/Koinonia.Api.Tests/Koinonia.Api.Tests.csproj`
Expected: PASS, inclusive `RecorteDeEventoTestes` e `ApiTestes`.

- [ ] **Step 6: Commit**

```powershell
git add koinonia-api
git commit -m "feat: expor planejamento e ponto no host com a porta de consulta"
```

---

### Task 15: Angular, bibliotecas e os três estados do espelho

**Files:**
- Create: `koinonia-web/projects/ponto/src/lib/espelho-acoes.ts`
- Create: `koinonia-web/src/app/espelho-acoes.spec.ts`
- Create: `koinonia-web/projects/ui/src/lib/estado-vazio.component.ts`
- Create: `koinonia-web/projects/ponto/src/lib/espelho.component.ts`

**Interfaces:**
- Consumes: a API em `http://localhost:5188`
- Produces: `acoesDoEspelho(estado: 'previa' | 'aprovado', papel: 'gestao' | 'empregado'): string[]`. Prévia de empregado devolve `['registrar']`. Prévia de gestão devolve `['aprovar']`. Aprovado de gestão devolve `['reabrir', 'exportar']`. Aprovado de empregado devolve `[]`.

- [ ] **Step 1: Criar o workspace**

```powershell
npx -y @angular/cli@20 new koinonia-web --directory koinonia-web --routing --style=css --skip-git --ssr=false --defaults
Set-Location koinonia-web
npx ng generate library ui
npx ng generate library planejamento
npx ng generate library ponto
Set-Location ..
```

- [ ] **Step 2: Escrever o teste dos três estados**

Criar `koinonia-web/projects/ponto/src/lib/espelho-acoes.ts`:

```typescript
export type EstadoEspelho = 'previa' | 'aprovado';
export type Papel = 'gestao' | 'empregado';

export function acoesDoEspelho(estado: EstadoEspelho, papel: Papel): string[] {
  if (papel === 'empregado') {
    return estado === 'previa' ? ['registrar'] : [];
  }
  return estado === 'previa' ? ['aprovar'] : ['reabrir', 'exportar'];
}
```

Exporte a função em `projects/ponto/src/public-api.ts`.

Criar `koinonia-web/src/app/espelho-acoes.spec.ts`:

```typescript
import { acoesDoEspelho } from 'ponto';

describe('espelho', () => {
  it('previa do empregado oferece registrar', () => {
    expect(acoesDoEspelho('previa', 'empregado')).toEqual(['registrar']);
  });

  it('copia aprovada do empregado nao oferece registrar', () => {
    expect(acoesDoEspelho('aprovado', 'empregado')).toEqual([]);
  });

  it('gestao aprova na previa e reabre na copia', () => {
    expect(acoesDoEspelho('previa', 'gestao')).toEqual(['aprovar']);
    expect(acoesDoEspelho('aprovado', 'gestao')).toEqual(['reabrir', 'exportar']);
  });
});
```

O `ng generate library ponto` grava em `tsconfig.json` o caminho `"ponto": ["./projects/ponto/src/public-api.ts"]`. O spec importa desse nome.

- [ ] **Step 3: Rodar e ver falhar**

```powershell
Set-Location koinonia-web
npx ng test --watch=false --browsers=ChromeHeadless
Set-Location ..
```

Expected: FAIL de compilação do spec enquanto `acoesDoEspelho` não estiver exportada em `public-api.ts`. O passo 4 faz esse export.

- [ ] **Step 4: Componentes sem regra de jornada**

Crie `koinonia-web/projects/ui/src/lib/estado-vazio.component.ts` e exporte em `public-api.ts`. O componente só recebe `mensagem` e a projeta. Não importa `planejamento` nem `ponto`.

```typescript
import { Component, Input } from '@angular/core';

@Component({
  selector: 'ui-estado-vazio',
  standalone: true,
  template: `<p>{{ mensagem }}</p>`
})
export class EstadoVazioComponent {
  @Input() mensagem = '';
}
```

Crie `koinonia-web/projects/ponto/src/lib/espelho.component.ts`. Ele recebe `estado`, `papel` e `trechos` e desenha um botão por ação. Também exporte em `public-api.ts`.

```typescript
import { Component, Input } from '@angular/core';
import { acoesDoEspelho, EstadoEspelho, Papel } from './espelho-acoes';

@Component({
  selector: 'ponto-espelho',
  standalone: true,
  template: `
    <p>{{ estado === 'aprovado' ? 'Cópia aprovada' : 'Prévia' }}</p>
    @for (acao of acoes; track acao) {
      <button type="button">{{ acao }}</button>
    }
  `
})
export class EspelhoComponent {
  @Input() estado: EstadoEspelho = 'previa';
  @Input() papel: Papel = 'empregado';
  @Input() trechos: unknown[] = [];
  get acoes() { return acoesDoEspelho(this.estado, this.papel); }
}
```

- [ ] **Step 5: Rodar e ver passar**

```powershell
Set-Location koinonia-web
npx ng test --watch=false --browsers=ChromeHeadless
Set-Location ..
```

Expected: PASS nos três testes do espelho.

- [ ] **Step 6: Commit**

```powershell
git add koinonia-web
git commit -m "feat: espelho unico com acoes conforme o papel"
```

---

### Task 16: Telas de planejamento, ponto e fechamento

**Files:**
- Create: `koinonia-web/src/app/api-url.ts`
- Create: `koinonia-web/projects/planejamento/src/lib/planejamento.component.ts`
- Create: `koinonia-web/projects/ponto/src/lib/ponto.component.ts`
- Modify: `koinonia-web/src/app/app.routes.ts`
- Modify: `koinonia-web/src/app/app.config.ts`

**Interfaces:**
- Consumes: `acoesDoEspelho`, `http://localhost:5188`, cabeçalhos `X-Funcionario-Id` e `X-Papel`
- Produces: rotas `/planejamento`, `/ponto` e `/fechamento`. O shell guarda papel e id em `sessionStorage` nas chaves `koinonia.papel` e `koinonia.funcionarioId`.

- [ ] **Step 1: Cliente HTTP**

Criar `koinonia-web/src/app/api-url.ts` com `export const apiUrl = 'http://localhost:5188';`.

Registrar `provideHttpClient()` em `app.config.ts`.

- [ ] **Step 2: Tela de planejamento**

`planejamento.component.ts` é visível para gestão. Formulários separados:
- cadastrar funcionário: campo nome, `POST /funcionarios`
- publicar escala: funcionário, dia, início e fim em minutos, `PUT /escalas`; botão remover chama `DELETE`
- publicar evento: nome, início, fim, funcionários, forma (`hora normal`, `adicional`, `banco de horas`), percentual e fator
- percentual institucional: `PUT /percentual-extra-comum`

Use os componentes de `ui` para campo, período e estado vazio. A tela não recalcula trecho.

- [ ] **Step 3: Tela de ponto e fechamento**

`ponto.component.ts` carrega `GET /espelho/{id}/{dia}`. Se `aprovado` é falso, o estado é `previa`. O botão `registrar` envia entrada e saída para `/batidas`. O botão `aprovar` chama `POST /dias/{id}/{dia}/aprovacao`. O botão `reabrir` chama o `DELETE` correspondente. `/fechamento` pede início, fim e formato e baixa o arquivo de `GET /fechamento`.

Empregado não vê `/planejamento` nem `/fechamento`. O guard lê `sessionStorage`.

- [ ] **Step 4: Conferir o build**

```powershell
Set-Location koinonia-web
npx ng build
npx ng test --watch=false --browsers=ChromeHeadless
Set-Location ..
dotnet test koinonia-api/Koinonia.sln
```

Expected: os dois comandos do Angular terminam com exit code 0 e `dotnet test` continua PASS.

- [ ] **Step 5: Commit**

```powershell
git add koinonia-web
git commit -m "feat: telas de escala, espelho e fechamento sobre a mesma api"
```

---
