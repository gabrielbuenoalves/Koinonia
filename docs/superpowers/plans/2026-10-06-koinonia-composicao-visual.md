# Koinonia Composição Visual Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Trocar a cara escura e empilhada por papel creme, título em Fraunces e cartões separados, sem mudar rotas, sessão nem chamadas HTTP.

**Architecture:** Os tokens e as duas fontes entram uma vez em `styles.css`. Os primitivos `ui-campo` e `ui-periodo` ganham as classes novas. O shell, a entrada e a visão geral mudam de composição. Ponto, planejamento e fechamento repartem o que já existe em cartões; o espelho passa a mostrar rótulo e a emitir a mesma chave.

**Tech Stack:** Angular 20, Tailwind CSS 4 via `@tailwindcss/postcss`, `@fontsource/source-sans-3`, `@fontsource/fraunces`, Karma/Jasmine, ChromeHeadless.

**Spec:** `docs/superpowers/specs/2026-10-06-koinonia-composicao-visual-design.md`

## Global Constraints

- Fundo `#f4efe6`, cartão `#fffdf9`, trilho `#efe6d8`, texto `#2a241e`, secundário `#74685c`, acento `#6b2d3c`, realce `#f6e6e8`, campo `#ffffff`, texto sobre o botão `#fff8f4`.
- Tokens: `fundo`, `superficie`, `texto`, `secundario`, `acento`, `trilho`, `realce`, `campo`, `sobre`. `color-scheme` é `light`.
- Interface: Source Sans 3, pacote `@fontsource/source-sans-3`. Título e o nome Koinonia: Fraunces, pacote `@fontsource/fraunces`. Import no CSS, sem CDN.
- Cartão: `rounded-2xl border border-trilho bg-superficie p-6`. Campo e select: `rounded-xl border border-trilho bg-campo px-3 py-2 text-texto`. Botão principal: `rounded-full bg-acento px-4 py-2 text-sobre`. Atalho secundário: `rounded-full border border-acento px-4 py-2 text-acento`. Remover e Sair: `rounded-full px-3 py-2 text-secundario`, sem `bg-acento`. Item de menu: `rounded-full px-3 py-2 text-secundario`, ativo com `bg-realce text-acento`.
- Abaixo de `md`, o menu é uma faixa que quebra linha. De `md` para cima, o trilho fica à esquerda com `md:w-56 md:flex-col`.
- Frases que permanecem: `O ponto está em uso.`, `A gestão organiza a jornada. O empregado registra a sua.`, `Entrada, saída, escala e fechamento da jornada.`, `Informe o papel e um identificador válido.`, `Nenhum trecho neste dia.`, `Nenhum funcionário cadastrado.`, `Prévia`, `Cópia aprovada`, `Casa` no cartão de entrada.
- Ponto é o botão vinho da visão geral. Planejamento e Fechamento, quando visíveis, são contorno. A lista continua saindo de `atalhosDoModulo`.
- `acoesDoEspelho` devolve `registrar`, `aprovar`, `reabrir`, `exportar`. O botão emite essa chave. O texto visível é `Registrar`, `Aprovar`, `Reabrir`, `Exportar`. Registrar fica no cartão do espelho, só depois do carregamento.
- Tabela do espelho, nesta ordem: Início, Fim, Fatia, Evento, Percentual, Fator, Minutos trabalhados, Minutos creditados. Célula nula mostra `—`.
- Formulários conservam o `aria-label`: `Entrar`, `Batida`, `Fechamento`, `Cadastrar funcionário`, `Publicar escala`, `Publicar evento`, `Percentual institucional`.
- Não envolva o rótulo de uma `label` em outro elemento: `preencher` lê o nó de texto direto. `clicar` compara o texto do `button` sem espaços nas pontas. `formulario` busca `form[aria-label]`.
- O 422 continua num `p` com `text-secundario` acima dos cartões da tela. Sair apaga a sessão. Sem sessão, volta para `/entrar`.
- Não há teste de pixel nem de cor computada. Teste de classe olha `className`.
- Não edite `koinonia-api/**`, `koinonia-web/src/app/cabecalhos.interceptor.ts`, `koinonia-web/src/app/api-url.ts`, `koinonia-web/src/app/menu.ts`, `koinonia-web/src/app/sessao.ts`, nem os `ui-shim.ts`. Não mude métodos de HTTP, guarda ou navegação.
- Comandos são PowerShell, a partir de `C:\Repos\Koinonia\koinonia-web`. Cada `git add` lista só os arquivos da tarefa.

## Estrutura de arquivos

- `koinonia-web/src/styles.css` — tokens, fontes e `color-scheme`.
- `koinonia-web/package.json` e `package-lock.json` — os dois pacotes de fonte.
- `koinonia-web/projects/ui/src/lib/campo.component.ts` — input com `bg-campo`.
- `koinonia-web/projects/ui/src/lib/periodo.component.ts` — início e fim lado a lado a partir de `sm`.
- `koinonia-web/projects/ponto/src/lib/espelho-acoes.ts` — `rotuloDaAcao`.
- `koinonia-web/projects/ponto/src/lib/espelho.component.ts` — pílula e texto do botão.
- `koinonia-web/projects/ponto/src/lib/ponto.component.ts` — cartões de ponto e de fechamento, e a tabela.
- `koinonia-web/projects/planejamento/src/lib/planejamento.component.ts` — quatro cartões.
- `koinonia-web/src/app/app.html` — trilho e faixa.
- `koinonia-web/src/app/entrada.component.ts` — cartão de entrada.
- `koinonia-web/src/app/visao-geral.component.ts` — título e atalhos.
- `koinonia-web/src/app/telas.spec.ts` e `koinonia-web/src/app/espelho-acoes.spec.ts` — as asserções de cara.

`estado-vazio.component.ts` já usa `text-secundario`. Não mexa nele.

---

### Task 1: Tokens, fontes e campo

**Files:**
- Modify: `koinonia-web/src/styles.css`
- Modify: `koinonia-web/package.json`
- Modify: `koinonia-web/package-lock.json`
- Modify: `koinonia-web/projects/ui/src/lib/campo.component.ts`
- Modify: `koinonia-web/projects/ui/src/lib/periodo.component.ts`
- Test: `koinonia-web/src/app/telas.spec.ts`

**Interfaces:**
- Consumes: nada
- Produces: utilitários `bg-campo`, `bg-trilho`, `bg-realce`, `text-sobre`, `font-serif`, `border-trilho`. `ui-periodo` envolve os dois campos num `div` com `sm:grid-cols-2`.

- [ ] **Step 1: Write the failing test**

Em `koinonia-web/src/app/telas.spec.ts`, substitua o teste `campo e periodo usam a superficie do tema` por:

```ts
  it('campo e periodo usam a superficie do tema', () => {
    const campo = TestBed.createComponent(CampoComponent);
    campo.componentInstance.rotulo = 'Nome';
    campo.detectChanges();
    expect(campo.nativeElement.querySelector('input')?.className).toContain('bg-campo');
    expect(campo.nativeElement.querySelector('input')?.className).toContain('text-texto');

    const periodo = TestBed.createComponent(PeriodoComponent);
    periodo.detectChanges();
    expect(periodo.nativeElement.querySelector('input')?.className).toContain('bg-campo');
    expect(periodo.nativeElement.querySelector('div')?.className).toContain('sm:grid-cols-2');
  });
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: FAIL em `campo e periodo usam a superficie do tema`, porque o input ainda tem `bg-fundo` e o período não tem `sm:grid-cols-2`.

- [ ] **Step 3: Write minimal implementation**

Na pasta `koinonia-web`:

```powershell
npm install @fontsource/source-sans-3 @fontsource/fraunces
```

Substitua `koinonia-web/src/styles.css` por:

```css
@import "@fontsource/source-sans-3/400.css";
@import "@fontsource/source-sans-3/600.css";
@import "@fontsource/source-sans-3/700.css";
@import "@fontsource/fraunces/400.css";
@import "@fontsource/fraunces/600.css";
@import "tailwindcss";
@source "../projects/ui/src";
@source "../projects/planejamento/src";
@source "../projects/ponto/src";

@theme {
  --font-sans: "Source Sans 3", "Segoe UI", sans-serif;
  --font-serif: "Fraunces", Georgia, serif;
  --color-fundo: #f4efe6;
  --color-superficie: #fffdf9;
  --color-trilho: #efe6d8;
  --color-texto: #2a241e;
  --color-secundario: #74685c;
  --color-acento: #6b2d3c;
  --color-realce: #f6e6e8;
  --color-campo: #ffffff;
  --color-sobre: #fff8f4;
}

body {
  margin: 0;
  background: var(--color-fundo);
  color: var(--color-texto);
  color-scheme: light;
  font-family: var(--font-sans);
}
```

Substitua o template de `koinonia-web/projects/ui/src/lib/campo.component.ts` por:

```ts
  template: `
    <label class="flex flex-col gap-1 text-secundario">
      {{ rotulo }}
      <input class="rounded-xl border border-trilho bg-campo px-3 py-2 text-texto" [attr.type]="tipo" [value]="valor" (input)="aoDigitar($event)" />
    </label>
  `,
```

Substitua o template de `koinonia-web/projects/ui/src/lib/periodo.component.ts` por:

```ts
  template: `
    <div class="grid gap-4 sm:grid-cols-2">
      <ui-campo
        rotulo="Início"
        [tipo]="tipo"
        [valor]="inicio"
        (valorChange)="inicioChange.emit($event)"
      />
      <ui-campo rotulo="Fim" [tipo]="tipo" [valor]="fim" (valorChange)="fimChange.emit($event)" />
    </div>
  `,
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: PASS

- [ ] **Step 5: Commit**

```powershell
git add koinonia-web/src/styles.css koinonia-web/package.json koinonia-web/package-lock.json koinonia-web/projects/ui/src/lib/campo.component.ts koinonia-web/projects/ui/src/lib/periodo.component.ts koinonia-web/src/app/telas.spec.ts
git commit -m "feat: troca o tema do campo para papel e vinho"
```

---

### Task 2: Rótulo visível do espelho

**Files:**
- Modify: `koinonia-web/projects/ponto/src/lib/espelho-acoes.ts`
- Modify: `koinonia-web/projects/ponto/src/lib/espelho.component.ts`
- Test: `koinonia-web/src/app/espelho-acoes.spec.ts`
- Test: `koinonia-web/src/app/telas.spec.ts`

**Interfaces:**
- Consumes: `acoesDoEspelho(estado, papel): string[]`, chaves `registrar`, `aprovar`, `reabrir`, `exportar`
- Produces: `rotuloDaAcao(acao: string): string`

- [ ] **Step 1: Write the failing test**

Em `koinonia-web/src/app/espelho-acoes.spec.ts`, importe `rotuloDaAcao` junto de `acoesDoEspelho` e acrescente:

```ts
  it('mostra o rotulo da acao sem mudar a chave', () => {
    expect(rotuloDaAcao('registrar')).toBe('Registrar');
    expect(rotuloDaAcao('aprovar')).toBe('Aprovar');
    expect(rotuloDaAcao('reabrir')).toBe('Reabrir');
    expect(rotuloDaAcao('exportar')).toBe('Exportar');
  });
```

Em `koinonia-web/src/app/telas.spec.ts`, troque os textos procurados por `clicar`:

- `'registrar'` vira `'Registrar'` nas duas ocorrências
- `'aprovar'` vira `'Aprovar'`
- `'reabrir'` vira `'Reabrir'`
- `'exportar'` vira `'Exportar'`

- [ ] **Step 2: Run test to verify it fails**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: FAIL. `rotuloDaAcao` não existe, e os cliques dizem `Botão não encontrado: Registrar`.

- [ ] **Step 3: Write minimal implementation**

No fim de `koinonia-web/projects/ponto/src/lib/espelho-acoes.ts`:

```ts
const rotulos: Record<string, string> = {
  registrar: 'Registrar',
  aprovar: 'Aprovar',
  reabrir: 'Reabrir',
  exportar: 'Exportar',
};

export function rotuloDaAcao(acao: string): string {
  return rotulos[acao] ?? acao;
}
```

Substitua `koinonia-web/projects/ponto/src/lib/espelho.component.ts` por:

```ts
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { acoesDoEspelho, EstadoEspelho, Papel, rotuloDaAcao } from './espelho-acoes';

@Component({
  selector: 'ponto-espelho',
  standalone: true,
  template: `
    <p class="inline-flex rounded-full bg-realce px-3 py-1 text-xs font-semibold uppercase tracking-wide text-acento">{{ estado === 'aprovado' ? 'Cópia aprovada' : 'Prévia' }}</p>
    <div class="mt-4 flex flex-wrap gap-2">
      @for (acao of acoes; track acao) {
        <button type="button" class="rounded-full bg-acento px-4 py-2 text-sobre" (click)="acaoEscolhida.emit(acao)">{{ rotuloDaAcao(acao) }}</button>
      }
    </div>
  `
})
export class EspelhoComponent {
  @Input() estado: EstadoEspelho = 'previa';
  @Input() papel: Papel = 'empregado';
  @Input() trechos: unknown[] = [];
  @Output() acaoEscolhida = new EventEmitter<string>();
  protected readonly rotuloDaAcao = rotuloDaAcao;
  get acoes() { return acoesDoEspelho(this.estado, this.papel); }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: PASS. Os testes de `acoesDoEspelho` continuam esperando as chaves, não o texto.

- [ ] **Step 5: Commit**

```powershell
git add koinonia-web/projects/ponto/src/lib/espelho-acoes.ts koinonia-web/projects/ponto/src/lib/espelho.component.ts koinonia-web/src/app/espelho-acoes.spec.ts koinonia-web/src/app/telas.spec.ts
git commit -m "feat: mostra o rotulo do espelho e emite a mesma acao"
```

---

### Task 3: Shell, entrada e visão geral

**Files:**
- Modify: `koinonia-web/src/app/app.html`
- Modify: `koinonia-web/src/app/entrada.component.ts`
- Modify: `koinonia-web/src/app/visao-geral.component.ts`
- Test: `koinonia-web/src/app/telas.spec.ts`

**Interfaces:**
- Consumes: `itensDoMenu`, `atalhosDoModulo`, `rotuloDoPapel`, sinais `naEntrada`, `rotuloPapel`, `funcionarioId`
- Produces: aside com `bg-trilho` e `md:flex-col`; título da entrada e da visão geral com `font-serif`; link de `/ponto` com `bg-acento`; os outros atalhos sem `bg-acento`

- [ ] **Step 1: Write the failing test**

Em `koinonia-web/src/app/telas.spec.ts`, no fim do teste `depois da entrada o menu mostra gestao e o identificador`, antes do fechamento do `it`, acrescente:

```ts
    const aside = fixture.nativeElement.querySelector('aside');
    expect(aside.className).toContain('bg-trilho');
    expect(aside.className).toContain('md:flex-col');
    expect(aside.querySelector('p').className).toContain('font-serif');
    const sair = [...fixture.nativeElement.querySelectorAll('button')].find(
      (item) => (item as HTMLButtonElement).textContent?.trim() === 'Sair',
    ) as HTMLButtonElement;
    expect(sair.className).toContain('text-secundario');
    expect(sair.className).not.toContain('bg-acento');
```

Acrescente este teste no describe `entrada e visao geral`:

```ts
  it('entrada usa titulo serif e o rotulo Casa', async () => {
    const fixture = await abrirEntrada();
    const titulo = fixture.nativeElement.querySelector('h1');
    expect(titulo.textContent).toContain('Koinonia');
    expect(titulo.className).toContain('font-serif');
    expect(fixture.nativeElement.textContent).toContain('Casa');
  });
```

No teste `o cartao da gestao abre ponto, planejamento e fechamento`, depois do `expect(hrefs...)`, acrescente:

```ts
    const el = harness.routeNativeElement as HTMLElement;
    const links = [...el.querySelectorAll('a')];
    expect(el.querySelector('h1')?.className).toContain('font-serif');
    expect(links[0].className).toContain('bg-acento');
    expect(links[1].className).not.toContain('bg-acento');
    expect(links[2].className).not.toContain('bg-acento');
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: FAIL. O aside não tem `bg-trilho`, a entrada não tem `Casa`, e Planejamento ainda tem `bg-acento`.

- [ ] **Step 3: Write minimal implementation**

Substitua `koinonia-web/src/app/app.html` por:

```html
<div
  [class]="naEntrada()
    ? 'grid min-h-screen place-items-center bg-fundo font-sans text-texto'
    : 'flex min-h-screen flex-col bg-fundo font-sans text-texto md:flex-row'"
>
  @if (!naEntrada()) {
    <aside class="flex flex-wrap gap-1 bg-trilho p-4 md:w-56 md:flex-col md:gap-2">
      <p class="w-full px-3 py-2 font-serif text-2xl">Koinonia</p>
      @for (item of itens(); track item.rota) {
        <a
          [routerLink]="item.rota"
          routerLinkActive="bg-realce text-acento"
          [routerLinkActiveOptions]="{ exact: true }"
          class="rounded-full px-3 py-2 text-secundario"
        >
          {{ item.rotulo }}
        </a>
      }
    </aside>
  }
  <div [class]="naEntrada() ? 'w-full max-w-md' : 'flex min-w-0 flex-1 flex-col'">
    @if (!naEntrada()) {
      <header class="flex items-center justify-end gap-4 px-6 py-4 text-sm">
        <p class="font-semibold">{{ rotuloPapel() }}</p>
        <p class="text-secundario">{{ funcionarioId() }}</p>
        <button type="button" class="rounded-full px-3 py-2 text-secundario" (click)="sair()">Sair</button>
      </header>
    }
    <main [class]="naEntrada() ? '' : 'px-6 pb-8'">
      <router-outlet />
    </main>
  </div>
</div>
```

Substitua o template de `koinonia-web/src/app/entrada.component.ts` por:

```ts
  template: `
    <form class="flex flex-col gap-4 rounded-2xl border border-trilho bg-superficie p-6" aria-label="Entrar" (submit)="confirmar($event)">
      <p class="text-xs font-semibold uppercase tracking-widest text-acento">Casa</p>
      <h1 class="font-serif text-4xl">Koinonia</h1>
      <label class="flex flex-col gap-1 text-secundario">
        Papel
        <select class="rounded-xl border border-trilho bg-campo px-3 py-2 text-texto" [value]="papel" (change)="definirPapel($event)">
          <option value=""></option>
          <option value="Gestao">Gestão</option>
          <option value="Empregado">Empregado</option>
        </select>
      </label>
      <label class="flex flex-col gap-1 text-secundario">
        Identificador
        <input class="rounded-xl border border-trilho bg-campo px-3 py-2 text-texto" [value]="identificador" (input)="definirIdentificador($event)" />
      </label>
      @if (erro) {
        <p class="text-secundario">{{ erro }}</p>
      }
      <button type="submit" class="rounded-full bg-acento px-4 py-2 text-sobre">Entrar</button>
    </form>
  `,
```

Substitua o template de `koinonia-web/src/app/visao-geral.component.ts` por:

```ts
  template: `
    <h1 class="font-serif text-4xl">O ponto está em uso.</h1>
    <p class="mt-2 text-secundario">A gestão organiza a jornada. O empregado registra a sua.</p>
    <article class="mt-8 rounded-2xl border border-trilho bg-superficie p-6">
      <p class="text-xs font-semibold uppercase tracking-widest text-acento">Módulo</p>
      <h2 class="mt-2 font-serif text-2xl">Ponto</h2>
      <p class="mt-2 text-secundario">Entrada, saída, escala e fechamento da jornada.</p>
      <div class="mt-6 flex flex-wrap gap-3">
        @for (atalho of atalhos; track atalho.rota) {
          <a
            [class]="atalho.rota === '/ponto'
              ? 'rounded-full bg-acento px-4 py-2 text-sobre'
              : 'rounded-full border border-acento px-4 py-2 text-acento'"
            [routerLink]="atalho.rota"
          >{{ atalho.rotulo }}</a>
        }
      </div>
    </article>
  `,
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: PASS

- [ ] **Step 5: Commit**

```powershell
git add koinonia-web/src/app/app.html koinonia-web/src/app/entrada.component.ts koinonia-web/src/app/visao-geral.component.ts koinonia-web/src/app/telas.spec.ts
git commit -m "feat: recompoe entrada, menu e visao geral"
```

---

### Task 4: Cartões e tabela do ponto

**Files:**
- Modify: `koinonia-web/projects/ponto/src/lib/ponto.component.ts`
- Test: `koinonia-web/src/app/telas.spec.ts`

**Interfaces:**
- Consumes: `CampoComponent`, `PeriodoComponent`, `EstadoVazioComponent`, `EspelhoComponent`, `executar(acao: string)`
- Produces: `h1` de Ponto com `font-serif`; primeira `section` com `bg-superficie`; `table` quando há trecho; `ui-estado-vazio` quando não há

Não altere o template de `FechamentoComponent` nesta tarefa. Não mova Registrar para o cartão da batida.

- [ ] **Step 1: Write the failing test**

No teste `ponto e fechamento usam titulo menor e o cartao`, troque só a asserção do ponto:

```ts
    expect(ponto.nativeElement.querySelector('h1')?.className).toContain('font-serif');
```

A asserção de `bg-superficie` do ponto e a do fechamento ficam. Não troque ainda o `text-xl` do planejamento.

No teste que registra a batida do empregado, depois do `fixture.detectChanges()` que segue o flush do espelho com `trecho()`, e antes de `const batida`, acrescente:

```ts
    expect(fixture.nativeElement.querySelector('table')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('—');
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: FAIL. O `h1` do ponto não tem `font-serif`, e não existe `table`.

- [ ] **Step 3: Write minimal implementation**

Substitua somente o template de `PontoComponent` em `koinonia-web/projects/ponto/src/lib/ponto.component.ts` por:

```ts
  template: `
    <h1 class="mb-4 font-serif text-3xl">Ponto</h1>
    @if (erro) {
      <p class="mb-4 text-secundario">{{ erro }}</p>
    }
    <div class="grid gap-4 md:grid-cols-2">
      <section class="flex flex-col gap-4 rounded-2xl border border-trilho bg-superficie p-6">
        <p class="text-xs font-semibold uppercase tracking-widest text-acento">Consulta</p>
        <ui-campo rotulo="Funcionário" [valor]="funcionarioId" (valorChange)="funcionarioId = $event" />
        <ui-campo rotulo="Dia" tipo="date" [valor]="dia" (valorChange)="dia = $event" />
        <button type="button" class="w-fit rounded-full bg-acento px-4 py-2 text-sobre" (click)="carregar()">Carregar</button>
      </section>
      <section class="rounded-2xl border border-trilho bg-superficie p-6">
        <p class="text-xs font-semibold uppercase tracking-widest text-acento">Batida</p>
        <form class="mt-4" aria-label="Batida" (submit)="$event.preventDefault()">
          <ui-periodo
            tipo="datetime-local"
            [inicio]="entrada"
            [fim]="saida"
            (inicioChange)="entrada = $event"
            (fimChange)="saida = $event"
          />
        </form>
      </section>
    </div>
    @if (carregado) {
      <section class="mt-4 rounded-2xl border border-trilho bg-superficie p-6">
        <ponto-espelho
          [estado]="estado"
          [papel]="papel"
          [trechos]="trechos"
          (acaoEscolhida)="executar($event)"
        />
        @if (trechos.length === 0) {
          <ui-estado-vazio mensagem="Nenhum trecho neste dia." />
        } @else {
          <table class="mt-4 w-full border-collapse text-left text-sm">
            <thead>
              <tr class="text-xs uppercase tracking-wide text-secundario">
                <th class="px-2 py-2 font-semibold">Início</th>
                <th class="px-2 py-2 font-semibold">Fim</th>
                <th class="px-2 py-2 font-semibold">Fatia</th>
                <th class="px-2 py-2 font-semibold">Evento</th>
                <th class="px-2 py-2 font-semibold">Percentual</th>
                <th class="px-2 py-2 font-semibold">Fator</th>
                <th class="px-2 py-2 font-semibold">Minutos trabalhados</th>
                <th class="px-2 py-2 font-semibold">Minutos creditados</th>
              </tr>
            </thead>
            <tbody>
              @for (trecho of trechos; track $index) {
                <tr class="border-t border-trilho">
                  <td class="px-2 py-2">{{ trecho.inicio }}</td>
                  <td class="px-2 py-2">{{ trecho.fim }}</td>
                  <td class="px-2 py-2">{{ trecho.fatia }}</td>
                  <td class="px-2 py-2">{{ trecho.evento ?? '—' }}</td>
                  <td class="px-2 py-2">{{ trecho.percentual ?? '—' }}</td>
                  <td class="px-2 py-2">{{ trecho.fator ?? '—' }}</td>
                  <td class="px-2 py-2">{{ trecho.minutosTrabalhados }}</td>
                  <td class="px-2 py-2">{{ trecho.minutosCreditados ?? '—' }}</td>
                </tr>
              }
            </tbody>
          </table>
        }
      </section>
    }
  `,
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: PASS

- [ ] **Step 5: Commit**

```powershell
git add koinonia-web/projects/ponto/src/lib/ponto.component.ts koinonia-web/src/app/telas.spec.ts
git commit -m "feat: separa a consulta, a batida e a tabela do ponto"
```

---

### Task 5: Quatro cartões do planejamento

**Files:**
- Modify: `koinonia-web/projects/planejamento/src/lib/planejamento.component.ts`
- Test: `koinonia-web/src/app/telas.spec.ts`

**Interfaces:**
- Consumes: `CampoComponent`, `PeriodoComponent`, `EstadoVazioComponent`, os métodos já existentes do componente
- Produces: `h1` com `font-serif`; quatro `section` com `bg-superficie`; os `aria-label` atuais

- [ ] **Step 1: Write the failing test**

No teste `planejamento usa titulo menor e o cartao`, troque a asserção do título:

```ts
    expect(fixture.nativeElement.querySelector('h1')?.className).toContain('font-serif');
```

Mantenha `bg-superficie` na primeira `section`. Acrescente:

```ts
    expect(fixture.nativeElement.querySelectorAll('section').length).toBe(4);
    expect(fixture.nativeElement.textContent).toContain('Pessoas');
    expect(fixture.nativeElement.textContent).toContain('Jornada');
    expect(fixture.nativeElement.textContent).toContain('Exceção');
    expect(fixture.nativeElement.textContent).toContain('Extra comum');
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: FAIL. O título ainda exige a classe antiga ou não há quatro seções.

- [ ] **Step 3: Write minimal implementation**

Substitua somente o template de `PlanejamentoComponent` por:

```ts
  template: `
    <h1 class="mb-4 font-serif text-3xl">Planejamento</h1>
    @if (erro) {
      <p class="mb-4 text-secundario">{{ erro }}</p>
    }
    <div class="grid gap-4 md:grid-cols-2">
      <section class="flex flex-col gap-4 rounded-2xl border border-trilho bg-superficie p-6">
        <p class="text-xs font-semibold uppercase tracking-widest text-acento">Pessoas</p>
        <h2 class="font-serif text-2xl">Funcionário</h2>
        <form class="flex flex-col gap-4" aria-label="Cadastrar funcionário" (submit)="cadastrar($event)">
          <ui-campo rotulo="Nome" [valor]="nome" (valorChange)="nome = $event" />
          <button type="submit" class="w-fit rounded-full bg-acento px-4 py-2 text-sobre">Cadastrar</button>
        </form>
        @if (funcionarios.length === 0) {
          <ui-estado-vazio mensagem="Nenhum funcionário cadastrado." />
        }
      </section>

      <section class="flex flex-col gap-4 rounded-2xl border border-trilho bg-superficie p-6">
        <p class="text-xs font-semibold uppercase tracking-widest text-acento">Jornada</p>
        <h2 class="font-serif text-2xl">Escala</h2>
        <form class="flex flex-col gap-4" aria-label="Publicar escala" (submit)="publicarEscala($event)">
          <label class="flex flex-col gap-1 text-secundario">
            Funcionário
            <select class="rounded-xl border border-trilho bg-campo px-3 py-2 text-texto" [value]="funcionarioEscala" (change)="definirFuncionario($event)">
              @for (funcionario of funcionarios; track funcionario.id) {
                <option [value]="funcionario.id">{{ funcionario.nome }}</option>
              }
            </select>
          </label>
          <ui-campo rotulo="Dia" tipo="date" [valor]="diaEscala" (valorChange)="diaEscala = $event" />
          <ui-periodo
            tipo="number"
            [inicio]="inicioEscala"
            [fim]="fimEscala"
            (inicioChange)="inicioEscala = $event"
            (fimChange)="fimEscala = $event"
          />
          <div class="flex flex-wrap gap-2">
            <button type="submit" class="rounded-full bg-acento px-4 py-2 text-sobre">Publicar</button>
            <button type="button" class="rounded-full px-3 py-2 text-secundario" (click)="removerEscala()">Remover</button>
          </div>
        </form>
      </section>

      <section class="flex flex-col gap-4 rounded-2xl border border-trilho bg-superficie p-6">
        <p class="text-xs font-semibold uppercase tracking-widest text-acento">Exceção</p>
        <h2 class="font-serif text-2xl">Evento</h2>
        <form class="flex flex-col gap-4" aria-label="Publicar evento" (submit)="publicarEvento($event)">
          <ui-campo rotulo="Nome" [valor]="nomeEvento" (valorChange)="nomeEvento = $event" />
          <ui-periodo
            tipo="datetime-local"
            [inicio]="inicioEvento"
            [fim]="fimEvento"
            (inicioChange)="inicioEvento = $event"
            (fimChange)="fimEvento = $event"
          />
          <p class="text-secundario">Funcionários</p>
          @for (funcionario of funcionarios; track funcionario.id) {
            <label class="flex items-center gap-2 text-texto">
              <input type="checkbox" (change)="alternarFuncionario(funcionario.id, $event)" />
              {{ funcionario.nome }}
            </label>
          }
          <label class="flex flex-col gap-1 text-secundario">
            Forma
            <select class="rounded-xl border border-trilho bg-campo px-3 py-2 text-texto" (change)="definirForma($event)">
              <option value="hora normal">hora normal</option>
              <option value="adicional">adicional</option>
              <option value="banco de horas">banco de horas</option>
            </select>
          </label>
          <ui-campo rotulo="Percentual" [valor]="percentual" (valorChange)="percentual = $event" />
          <ui-campo rotulo="Fator" [valor]="fator" (valorChange)="fator = $event" />
          <button type="submit" class="w-fit rounded-full bg-acento px-4 py-2 text-sobre">Publicar</button>
        </form>
      </section>

      <section class="flex flex-col gap-4 rounded-2xl border border-trilho bg-superficie p-6">
        <p class="text-xs font-semibold uppercase tracking-widest text-acento">Casa</p>
        <h2 class="font-serif text-2xl">Extra comum</h2>
        <form class="flex flex-col gap-4" aria-label="Percentual institucional" (submit)="definirPercentual($event)">
          <ui-campo
            rotulo="Percentual"
            [valor]="percentualInstitucional"
            (valorChange)="percentualInstitucional = $event"
          />
          <button type="submit" class="w-fit rounded-full bg-acento px-4 py-2 text-sobre">Publicar</button>
        </form>
      </section>
    </div>
  `,
```

O teste procura o texto `Extra comum`. O `h2` traz essas duas palavras. O rótulo pequeno desse cartão continua `Casa`, como no spec.

- [ ] **Step 4: Run test to verify it passes**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: PASS

- [ ] **Step 5: Commit**

```powershell
git add koinonia-web/projects/planejamento/src/lib/planejamento.component.ts koinonia-web/src/app/telas.spec.ts
git commit -m "feat: separa o planejamento em quatro cartoes"
```

---

### Task 6: Cartão do fechamento

**Files:**
- Modify: `koinonia-web/projects/ponto/src/lib/ponto.component.ts`
- Test: `koinonia-web/src/app/telas.spec.ts`

**Interfaces:**
- Consumes: `PeriodoComponent`, `baixar`, `definirFormato`
- Produces: `h1` de Fechamento com `font-serif`; uma `section` com `bg-superficie`; select com `bg-campo`

Não altere o template de `PontoComponent`.

- [ ] **Step 1: Write the failing test**

No teste `ponto e fechamento usam titulo menor e o cartao`, depois de criar o fechamento, acrescente:

```ts
    expect(fechamento.nativeElement.querySelector('h1')?.className).toContain('font-serif');
    expect(fechamento.nativeElement.querySelector('select')?.className).toContain('bg-campo');
```

A asserção existente de `bg-superficie` permanece.

- [ ] **Step 2: Run test to verify it fails**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: FAIL. O `h1` do fechamento não tem `font-serif`, e o select ainda não tem `bg-campo`.

- [ ] **Step 3: Write minimal implementation**

Substitua somente o template de `FechamentoComponent` por:

```ts
  template: `
    <h1 class="mb-4 font-serif text-3xl">Fechamento</h1>
    @if (erro) {
      <p class="mb-4 text-secundario">{{ erro }}</p>
    }
    <section class="flex flex-col gap-4 rounded-2xl border border-trilho bg-superficie p-6">
      <form class="flex flex-col gap-4" aria-label="Fechamento" (submit)="baixar($event)">
        <ui-periodo
          tipo="date"
          [inicio]="inicio"
          [fim]="fim"
          (inicioChange)="inicio = $event"
          (fimChange)="fim = $event"
        />
        <label class="flex flex-col gap-1 text-secundario">
          Formato
          <select class="rounded-xl border border-trilho bg-campo px-3 py-2 text-texto" (change)="definirFormato($event)">
            <option value="csv">csv</option>
            <option value="excel">excel</option>
          </select>
        </label>
        <button type="submit" class="w-fit rounded-full bg-acento px-4 py-2 text-sobre">Baixar</button>
      </form>
    </section>
  `,
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: PASS

- [ ] **Step 5: Commit**

```powershell
git add koinonia-web/projects/ponto/src/lib/ponto.component.ts koinonia-web/src/app/telas.spec.ts
git commit -m "feat: veste o fechamento no mesmo cartao"
```
