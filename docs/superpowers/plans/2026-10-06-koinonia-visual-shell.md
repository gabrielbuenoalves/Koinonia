# Koinonia Visual Shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Colocar entrada, visão geral e menu por papel numa interface escura do Koinonia, e vestir ponto, planejamento e fechamento com a mesma cara, sem mudar a API.

**Architecture:** O Tailwind entra uma vez no aplicativo. Uma lista declara o menu; uma função pura filtra pelo papel. O shell desenha a entrada ou, depois dela, o menu, a faixa e a saída. As bibliotecas `ui`, `planejamento` e `ponto` conservam as telas e só ganham classes nos templates.

**Tech Stack:** Angular 20, Tailwind CSS 4 via `@tailwindcss/postcss`, Karma/Jasmine, ChromeHeadless.

**Spec:** `docs/superpowers/specs/2026-10-06-koinonia-visual-shell-design.md`

## Global Constraints

- Nome visível: Koinonia. Sem Educa Nexus, sem números ilustrativos, sem módulos que ainda não existem.
- Título da visão geral: `O ponto está em uso.`
- Subtítulo: `A gestão organiza a jornada. O empregado registra a sua.`
- Cartão: `Entrada, saída, escala e fechamento da jornada.`
- Valores gravados: `Gestao` ou `Empregado`, nas chaves `koinonia.papel` e `koinonia.funcionarioId`. Na faixa, a gestão aparece como `Gestão`.
- Identificador sem espaços nas pontas, no formato guid `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`. Papel vazio é recusado. A mensagem fica `Informe o papel e um identificador válido.`
- Sem sessão, `/`, `/ponto`, `/planejamento` e `/fechamento` voltam para `/entrar`. Empregado em `/planejamento` ou `/fechamento` cai em `/ponto`. Sair apaga as duas chaves e volta para `/entrar`.
- A entrada não chama a API. Cabeçalhos `X-Funcionario-Id` e `X-Papel` permanecem. A gestão não precisa estar cadastrada.
- Botões do espelho continuam saindo de `acoesDoEspelho`. Texto de 422 continua ao lado da ação. Dia sem trecho continua `Nenhum trecho neste dia.` Lista vazia de funcionários continua `Nenhum funcionário cadastrado.`
- Não envolva o rótulo de uma `label` em outro elemento: `preencher` lê o nó de texto direto. `clicar` compara o texto do `button` sem espaços nas pontas. `formulario` busca `form[aria-label]`.
- Classes só nos templates, com estes nomes e com mais nenhum papel de cor: `bg-fundo`, `bg-superficie`, `text-texto`, `text-secundario`, `bg-acento`, `font-sans`. Cartão: `rounded-xl bg-superficie p-6`. Botão principal: `rounded-md bg-acento px-4 py-2 text-texto`. Botão secundário: `rounded-md bg-fundo px-4 py-2 text-secundario`. Campo e select: `rounded-md bg-fundo px-3 py-2 text-texto`. Título geral: `text-4xl`. Título de trabalho: `mb-4 text-xl`.
- Tokens, nestas cores: fundo `#0c0b10`, superfície `#17151f`, texto `#f4f1ea`, secundário `#a39bb0`, acento `#7c5cbf`. Fonte sans: `"Segoe UI", sans-serif`.
- Não há teste de pixel nem de cor computada. Teste de classe olha `className`, não o CSS aplicado.
- Não edite `koinonia-api/**`, `koinonia-web/src/app/cabecalhos.interceptor.ts`, `koinonia-web/src/app/api-url.ts`, nem os `ui-shim.ts`.
- Comandos são PowerShell, a partir de `C:\Repos\Koinonia\koinonia-web`. Cada `git add` lista só os arquivos da tarefa.

## Estrutura de arquivos

- `koinonia-web/src/app/menu.ts` — lista do menu e o filtro por papel.
- `koinonia-web/src/app/menu.spec.ts` — testes do filtro.
- `koinonia-web/src/app/sessao.ts` — papel HTTP, guid, sessão válida, rótulo e limpeza.
- `koinonia-web/src/app/sessao.spec.ts` — testes dessas funções.
- `koinonia-web/src/app/sessao.guard.ts` — rota interna sem sessão volta para `/entrar`.
- `koinonia-web/src/app/entrada.component.ts` — formulário de entrada.
- `koinonia-web/src/app/visao-geral.component.ts` — a casa e os atalhos do módulo.
- `koinonia-web/src/app/app.ts` e `app.html` — menu, faixa, Sair e um único `router-outlet`.
- `koinonia-web/src/app/app.routes.ts` — `/entrar`, `/` e os guardas.
- `koinonia-web/src/styles.css` e `.postcssrc.json` — Tailwind e os quatro papéis de cor.
- `koinonia-web/projects/ui/src/lib/campo.component.ts`, `periodo.component.ts`, `estado-vazio.component.ts` — classes compartilhadas.
- `koinonia-web/projects/ponto/src/lib/ponto.component.ts` e `espelho.component.ts` — cara de ponto e fechamento.
- `koinonia-web/projects/planejamento/src/lib/planejamento.component.ts` — cara do planejamento.
- `koinonia-web/src/app/telas.spec.ts` e `app.spec.ts` — entrada, menu visível e os fluxos que já existem.

---

### Task 1: Filtro do menu

**Files:**
- Create: `koinonia-web/src/app/menu.ts`
- Test: `koinonia-web/src/app/menu.spec.ts`

**Interfaces:**
- Consumes: nada
- Produces: `ItemDeMenu` com `rotulo: string`, `rota: string`, `papeis: Array<'Gestao' | 'Empregado'>`. `menu`, `itensDoMenu(papel: string | null): ItemDeMenu[]`, `atalhosDoModulo(papel: string | null): ItemDeMenu[]`.

- [ ] **Step 1: Write the failing test**

Crie `koinonia-web/src/app/menu.spec.ts`:

```ts
import { atalhosDoModulo, itensDoMenu } from './menu';

describe('menu', () => {
  it('gestao ve visao geral, ponto, planejamento e fechamento', () => {
    expect(itensDoMenu('Gestao').map((item) => item.rota)).toEqual([
      '/',
      '/ponto',
      '/planejamento',
      '/fechamento',
    ]);
    expect(itensDoMenu('Gestao').map((item) => item.rotulo)).toEqual([
      'Visão geral',
      'Ponto',
      'Planejamento',
      'Fechamento',
    ]);
  });

  it('empregado ve visao geral e ponto', () => {
    expect(itensDoMenu('Empregado').map((item) => item.rota)).toEqual(['/', '/ponto']);
  });

  it('aceita o papel em minusculas e recusa papel desconhecido', () => {
    expect(itensDoMenu('gestao').map((item) => item.rota)).toEqual(
      itensDoMenu('Gestao').map((item) => item.rota),
    );
    expect(itensDoMenu('')).toEqual([]);
    expect(itensDoMenu(null)).toEqual([]);
  });

  it('atalhos do modulo omitem a visao geral', () => {
    expect(atalhosDoModulo('Gestao').map((item) => item.rota)).toEqual([
      '/ponto',
      '/planejamento',
      '/fechamento',
    ]);
    expect(atalhosDoModulo('Empregado').map((item) => item.rota)).toEqual(['/ponto']);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: FAIL. O compilador não encontra `./menu`.

- [ ] **Step 3: Write minimal implementation**

Crie `koinonia-web/src/app/menu.ts`:

```ts
export interface ItemDeMenu {
  rotulo: string;
  rota: string;
  papeis: Array<'Gestao' | 'Empregado'>;
}

export const menu: ItemDeMenu[] = [
  { rotulo: 'Visão geral', rota: '/', papeis: ['Gestao', 'Empregado'] },
  { rotulo: 'Ponto', rota: '/ponto', papeis: ['Gestao', 'Empregado'] },
  { rotulo: 'Planejamento', rota: '/planejamento', papeis: ['Gestao'] },
  { rotulo: 'Fechamento', rota: '/fechamento', papeis: ['Gestao'] },
];

export function itensDoMenu(papel: string | null): ItemDeMenu[] {
  const canonico = papelCanonico(papel);
  if (!canonico) {
    return [];
  }
  return menu.filter((item) => item.papeis.includes(canonico));
}

export function atalhosDoModulo(papel: string | null): ItemDeMenu[] {
  return itensDoMenu(papel).filter((item) => item.rota !== '/');
}

function papelCanonico(papel: string | null): 'Gestao' | 'Empregado' | null {
  const texto = papel?.trim().toLowerCase();
  if (texto === 'gestao') {
    return 'Gestao';
  }
  if (texto === 'empregado') {
    return 'Empregado';
  }
  return null;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: PASS, including the four tests named `menu`.

- [ ] **Step 5: Commit**

```powershell
git add -- koinonia-web/src/app/menu.ts koinonia-web/src/app/menu.spec.ts
git commit -m "feat: filtrar o menu do koinonia pelo papel"
```

---

### Task 2: Entrada, sessão e visão geral

**Files:**
- Modify: `koinonia-web/src/app/sessao.ts`
- Create: `koinonia-web/src/app/sessao.spec.ts`
- Create: `koinonia-web/src/app/sessao.guard.ts`
- Create: `koinonia-web/src/app/entrada.component.ts`
- Create: `koinonia-web/src/app/visao-geral.component.ts`
- Modify: `koinonia-web/src/app/app.routes.ts`
- Test: `koinonia-web/src/app/telas.spec.ts`

**Interfaces:**
- Consumes: `atalhosDoModulo` de `menu.ts`
- Produces: `identificadorValido(valor: string): boolean`, `sessaoValida(papel: string | null, funcionarioId: string | null): boolean`, `rotuloDoPapel(papel: string | null): string`, `limparSessao(): void`, `sessaoGuard`. `papelHttp` permanece. `EntradaComponent` e `VisaoGeralComponent`.

- [ ] **Step 1: Write the failing test**

Crie `koinonia-web/src/app/sessao.spec.ts`:

```ts
import { identificadorValido, rotuloDoPapel, sessaoValida } from './sessao';

const ana = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

describe('sessao', () => {
  it('aceita guid com espacos nas pontas e recusa o resto', () => {
    expect(identificadorValido(`  ${ana}  `)).toBeTrue();
    expect(identificadorValido('abc')).toBeFalse();
    expect(identificadorValido('')).toBeFalse();
  });

  it('sessao valida exige papel conhecido e guid', () => {
    expect(sessaoValida('Gestao', ana)).toBeTrue();
    expect(sessaoValida('Empregado', ana)).toBeTrue();
    expect(sessaoValida('', ana)).toBeFalse();
    expect(sessaoValida('Gestao', 'abc')).toBeFalse();
    expect(sessaoValida(null, null)).toBeFalse();
  });

  it('rotulo da faixa escreve Gestao com acento', () => {
    expect(rotuloDoPapel('Gestao')).toBe('Gestão');
    expect(rotuloDoPapel('Empregado')).toBe('Empregado');
  });
});
```

Em `koinonia-web/src/app/telas.spec.ts`, acrescente estes testes dentro de `describe('guard de gestao')`, depois do teste `gestao entra em planejamento e fechamento`:

```ts
  it('sem sessao uma rota interna volta para entrar', async () => {
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/ponto');
    expect(router.url).toBe('/entrar');

    await router.navigateByUrl('/planejamento');
    expect(router.url).toBe('/entrar');

    await router.navigateByUrl('/fechamento');
    expect(router.url).toBe('/entrar');

    await router.navigateByUrl('/');
    expect(router.url).toBe('/entrar');
  });
```

No final de `telas.spec.ts`, antes das funções auxiliares, acrescente:

```ts
describe('entrada e visao geral', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(withInterceptors([cabecalhosInterceptor])),
        provideHttpClientTesting(),
        { provide: API_URL, useValue: apiUrl },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('identificador invalido nao grava sessao nem navega', async () => {
    const fixture = await abrirEntrada();
    const form = formulario(fixture, 'Entrar');
    preencher(form, 'Papel', 'Gestao');
    preencher(form, 'Identificador', 'abc');
    clicar(form, 'Entrar');
    await fixture.whenStable();

    expect(sessionStorage.getItem('koinonia.papel')).toBeNull();
    expect(sessionStorage.getItem('koinonia.funcionarioId')).toBeNull();
    expect(TestBed.inject(Router).url).toBe('/entrar');
    expect(fixture.nativeElement.textContent).toContain('Informe o papel e um identificador válido.');
  });

  it('papel vazio nao grava sessao', async () => {
    const fixture = await abrirEntrada();
    const form = formulario(fixture, 'Entrar');
    preencher(form, 'Identificador', ana);
    clicar(form, 'Entrar');
    await fixture.whenStable();

    expect(sessionStorage.getItem('koinonia.papel')).toBeNull();
    expect(sessionStorage.getItem('koinonia.funcionarioId')).toBeNull();
    expect(TestBed.inject(Router).url).toBe('/entrar');
  });

  it('sessao valida de cada papel abre a visao geral', async () => {
    for (const papel of ['Gestao', 'Empregado']) {
      sessionStorage.clear();
      const fixture = await abrirEntrada();
      const form = formulario(fixture, 'Entrar');
      preencher(form, 'Papel', papel);
      preencher(form, 'Identificador', `  ${ana}  `);
      clicar(form, 'Entrar');
      await fixture.whenStable();
      fixture.detectChanges();

      expect(sessionStorage.getItem('koinonia.papel')).toBe(papel);
      expect(sessionStorage.getItem('koinonia.funcionarioId')).toBe(ana);
      expect(TestBed.inject(Router).url).toBe('/');
      expect(fixture.nativeElement.textContent).toContain('O ponto está em uso.');
      expect(fixture.nativeElement.textContent).toContain(
        'A gestão organiza a jornada. O empregado registra a sua.',
      );
      expect(fixture.nativeElement.textContent).toContain(
        'Entrada, saída, escala e fechamento da jornada.',
      );
      fixture.destroy();
    }
  });

  it('o cartao da gestao abre ponto, planejamento e fechamento', async () => {
    sessionStorage.setItem('koinonia.papel', 'Gestao');
    sessionStorage.setItem('koinonia.funcionarioId', gestaoId);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/');
    expect(hrefs(harness.routeNativeElement as HTMLElement)).toEqual([
      '/ponto',
      '/planejamento',
      '/fechamento',
    ]);
  });

  it('o cartao do empregado abre so o ponto', async () => {
    sessionStorage.setItem('koinonia.papel', 'Empregado');
    sessionStorage.setItem('koinonia.funcionarioId', ana);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/');
    expect(hrefs(harness.routeNativeElement as HTMLElement)).toEqual(['/ponto']);
  });
});

async function abrirEntrada(): Promise<ComponentFixture<App>> {
  const fixture = TestBed.createComponent(App);
  fixture.detectChanges();
  await TestBed.inject(Router).navigateByUrl('/entrar');
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
  return fixture;
}
```

`RouterTestingHarness` já está importado em `telas.spec.ts`.

- [ ] **Step 2: Run test to verify it fails**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: FAIL. `sessao.ts` não exporta `identificadorValido`.

- [ ] **Step 3: Write minimal implementation**

Substitua `koinonia-web/src/app/sessao.ts` por:

```ts
const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function papelHttp(armazenado: string | null): 'Gestao' | 'Empregado' {
  return armazenado?.toLowerCase() === 'gestao' ? 'Gestao' : 'Empregado';
}

export function identificadorValido(valor: string): boolean {
  return guid.test(valor.trim());
}

export function sessaoValida(papel: string | null, funcionarioId: string | null): boolean {
  const texto = papel?.trim().toLowerCase();
  const conhecido = texto === 'gestao' || texto === 'empregado';
  return conhecido && identificadorValido(funcionarioId ?? '');
}

export function rotuloDoPapel(papel: string | null): string {
  return papel?.toLowerCase() === 'gestao' ? 'Gestão' : 'Empregado';
}

export function limparSessao(): void {
  sessionStorage.removeItem('koinonia.papel');
  sessionStorage.removeItem('koinonia.funcionarioId');
}
```

Crie `koinonia-web/src/app/sessao.guard.ts`:

```ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { sessaoValida } from './sessao';

export const sessaoGuard: CanActivateFn = () => {
  if (sessaoValida(sessionStorage.getItem('koinonia.papel'), sessionStorage.getItem('koinonia.funcionarioId'))) {
    return true;
  }
  return inject(Router).createUrlTree(['/entrar']);
};
```

Crie `koinonia-web/src/app/entrada.component.ts`:

```ts
import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { identificadorValido } from './sessao';

@Component({
  selector: 'app-entrada',
  standalone: true,
  template: `
    <form aria-label="Entrar" (submit)="confirmar($event)">
      <label>
        Papel
        <select [value]="papel" (change)="definirPapel($event)">
          <option value=""></option>
          <option value="Gestao">Gestão</option>
          <option value="Empregado">Empregado</option>
        </select>
      </label>
      <label>
        Identificador
        <input [value]="identificador" (input)="definirIdentificador($event)" />
      </label>
      @if (erro) {
        <p>{{ erro }}</p>
      }
      <button type="submit">Entrar</button>
    </form>
  `,
})
export class EntradaComponent {
  private readonly router = inject(Router);
  papel = '';
  identificador = '';
  erro = '';

  definirPapel(evento: Event) {
    this.papel = (evento.target as HTMLSelectElement).value;
  }

  definirIdentificador(evento: Event) {
    this.identificador = (evento.target as HTMLInputElement).value;
  }

  confirmar(evento: Event) {
    evento.preventDefault();
    const identificador = this.identificador.trim();
    if (!this.papel || !identificadorValido(identificador)) {
      this.erro = 'Informe o papel e um identificador válido.';
      return;
    }
    sessionStorage.setItem('koinonia.papel', this.papel);
    sessionStorage.setItem('koinonia.funcionarioId', identificador);
    void this.router.navigateByUrl('/');
  }
}
```

`confirmar` não injeta `HttpClient` e não faz pedido.

Crie `koinonia-web/src/app/visao-geral.component.ts`:

```ts
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { atalhosDoModulo } from './menu';

@Component({
  selector: 'app-visao-geral',
  standalone: true,
  imports: [RouterLink],
  template: `
    <h1>O ponto está em uso.</h1>
    <p>A gestão organiza a jornada. O empregado registra a sua.</p>
    <article>
      <h2>Ponto</h2>
      <p>Entrada, saída, escala e fechamento da jornada.</p>
      @for (atalho of atalhos; track atalho.rota) {
        <a [routerLink]="atalho.rota">{{ atalho.rotulo }}</a>
      }
    </article>
  `,
})
export class VisaoGeralComponent {
  get atalhos() {
    return atalhosDoModulo(sessionStorage.getItem('koinonia.papel'));
  }
}
```

Substitua `koinonia-web/src/app/app.routes.ts` por:

```ts
import { Routes } from '@angular/router';
import { PlanejamentoComponent } from 'planejamento';
import { FechamentoComponent, PontoComponent } from 'ponto';
import { EntradaComponent } from './entrada.component';
import { gestaoGuard } from './gestao.guard';
import { sessaoGuard } from './sessao.guard';
import { VisaoGeralComponent } from './visao-geral.component';

export const routes: Routes = [
  { path: 'entrar', component: EntradaComponent },
  { path: '', component: VisaoGeralComponent, canActivate: [sessaoGuard] },
  { path: 'planejamento', component: PlanejamentoComponent, canActivate: [sessaoGuard, gestaoGuard] },
  { path: 'ponto', component: PontoComponent, canActivate: [sessaoGuard] },
  { path: 'fechamento', component: FechamentoComponent, canActivate: [sessaoGuard, gestaoGuard] },
];
```

Não altere `app.ts` nem `gestao.guard.ts` nesta tarefa. O select do shell continua até a Task 3, e o teste `trocar para empregado em planejamento ou fechamento vai para ponto` continua passando. Os testes novos da entrada usam `formulario(fixture, 'Entrar')` para não preencher esse select.

- [ ] **Step 4: Run test to verify it passes**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: PASS. Os testes antigos de espelho, rota, guarda e HTTP continuam passando.

- [ ] **Step 5: Commit**

```powershell
git add -- koinonia-web/src/app/sessao.ts koinonia-web/src/app/sessao.spec.ts koinonia-web/src/app/sessao.guard.ts koinonia-web/src/app/entrada.component.ts koinonia-web/src/app/visao-geral.component.ts koinonia-web/src/app/app.routes.ts koinonia-web/src/app/telas.spec.ts
git commit -m "feat: entrar no koinonia e abrir a visao geral"
```

---

### Task 3: Tailwind, menu e faixa

**Files:**
- Create: `koinonia-web/.postcssrc.json`
- Modify: `koinonia-web/package.json`
- Modify: `koinonia-web/package-lock.json`
- Modify: `koinonia-web/src/styles.css`
- Modify: `koinonia-web/src/index.html`
- Modify: `koinonia-web/src/app/app.ts`
- Modify: `koinonia-web/src/app/app.html`
- Modify: `koinonia-web/src/app/gestao.guard.ts`
- Modify: `koinonia-web/src/app/entrada.component.ts`
- Modify: `koinonia-web/src/app/visao-geral.component.ts`
- Modify: `koinonia-web/src/app/app.spec.ts`
- Test: `koinonia-web/src/app/telas.spec.ts`

**Interfaces:**
- Consumes: `itensDoMenu`, `rotuloDoPapel`, `limparSessao`, `EntradaComponent`, `VisaoGeralComponent`
- Produces: shell com um único `router-outlet`. Item ativo usa `routerLinkActive="bg-acento"` e `[routerLinkActiveOptions]="{ exact: true }"`.

- [ ] **Step 1: Write the failing test**

Substitua o `describe('shell')` de `telas.spec.ts` por:

```ts
describe('shell', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter(routes)],
    });
  });

  afterEach(() => sessionStorage.clear());

  it('empregado ve visao geral e ponto', () => {
    sessionStorage.setItem('koinonia.papel', 'Empregado');
    sessionStorage.setItem('koinonia.funcionarioId', ana);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const links = hrefs(fixture.nativeElement);

    expect(fixture.nativeElement.textContent).toContain('Empregado');
    expect(links).toContain('/');
    expect(links).toContain('/ponto');
    expect(links).not.toContain('/planejamento');
    expect(links).not.toContain('/fechamento');
  });

  it('gestao ve visao geral, ponto, planejamento e fechamento', () => {
    sessionStorage.setItem('koinonia.papel', 'Gestao');
    sessionStorage.setItem('koinonia.funcionarioId', gestaoId);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    expect(hrefs(fixture.nativeElement)).toEqual(
      jasmine.arrayContaining(['/', '/ponto', '/planejamento', '/fechamento']),
    );
    expect(fixture.nativeElement.textContent).toContain('Gestão');
    expect(fixture.nativeElement.textContent).toContain(gestaoId);
  });

  it('sair limpa a sessao e volta para entrar', async () => {
    sessionStorage.setItem('koinonia.papel', 'Gestao');
    sessionStorage.setItem('koinonia.funcionarioId', gestaoId);
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);
    fixture.detectChanges();

    clicar(fixture.nativeElement, 'Sair');
    await fixture.whenStable();
    fixture.detectChanges();

    expect(sessionStorage.getItem('koinonia.papel')).toBeNull();
    expect(sessionStorage.getItem('koinonia.funcionarioId')).toBeNull();
    expect(router.url).toBe('/entrar');
    expect(fixture.nativeElement.querySelector('aside')).toBeNull();
  });
});
```

Apague o teste `trocar para empregado em planejamento ou fechamento vai para ponto`. Os outros dois testes de `guard de gestao` ficam.

Substitua o teste `should render title` de `koinonia-web/src/app/app.spec.ts` por:

```ts
  it('mostra o nome Koinonia para quem entrou', () => {
    sessionStorage.setItem('koinonia.papel', 'Gestao');
    sessionStorage.setItem('koinonia.funcionarioId', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Koinonia');
    expect(compiled.textContent).not.toContain('Hello');
  });
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: FAIL. O shell ainda não tem `Sair` nem o link `/`.

- [ ] **Step 3: Write minimal implementation**

Instale o Tailwind:

```powershell
npm install tailwindcss @tailwindcss/postcss --save-dev
```

Crie `koinonia-web/.postcssrc.json`:

```json
{
  "plugins": {
    "@tailwindcss/postcss": {}
  }
}
```

Substitua `koinonia-web/src/styles.css` por:

```css
@import "tailwindcss";
@source "../projects/ui/src";
@source "../projects/planejamento/src";
@source "../projects/ponto/src";

@theme {
  --font-sans: "Segoe UI", sans-serif;
  --color-fundo: #0c0b10;
  --color-superficie: #17151f;
  --color-texto: #f4f1ea;
  --color-secundario: #a39bb0;
  --color-acento: #7c5cbf;
}

body {
  margin: 0;
  background: var(--color-fundo);
  color: var(--color-texto);
  font-family: var(--font-sans);
}
```

O shell em `src/` entra na varredura porque o CSS está em `src/styles.css`. As três bibliotecas entram pelos `@source`.

Em `koinonia-web/src/index.html`, troque `lang="en"` por `lang="pt-BR"` e `<title>KoinoniaWeb</title>` por `<title>Koinonia</title>`.

Substitua `koinonia-web/src/app/app.ts` por:

```ts
import { Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { itensDoMenu } from './menu';
import { limparSessao, rotuloDoPapel } from './sessao';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly router = inject(Router);
  protected readonly papel = signal(sessionStorage.getItem('koinonia.papel') ?? '');
  protected readonly funcionarioId = signal(sessionStorage.getItem('koinonia.funcionarioId') ?? '');
  protected readonly naEntrada = signal(caminhoDe(this.router.url) === '/entrar');

  constructor() {
    this.router.events.subscribe((evento) => {
      if (evento instanceof NavigationEnd) {
        this.naEntrada.set(caminhoDe(evento.urlAfterRedirects) === '/entrar');
      }
    });
  }

  protected itens() {
    return itensDoMenu(this.papel());
  }

  protected rotuloPapel() {
    return rotuloDoPapel(this.papel());
  }

  sair() {
    limparSessao();
    this.papel.set('');
    this.funcionarioId.set('');
    void this.router.navigateByUrl('/entrar');
  }
}

function caminhoDe(url: string): string {
  return url.split(/[?#]/)[0];
}
```

Substitua `koinonia-web/src/app/app.html` por:

```html
<div
  class="min-h-screen bg-fundo font-sans text-texto"
  [class.grid]="naEntrada()"
  [class.place-items-center]="naEntrada()"
  [class.flex]="!naEntrada()"
>
  @if (!naEntrada()) {
    <aside class="flex w-56 flex-col gap-2 bg-superficie p-4">
      <p class="px-3 py-2 text-lg">Koinonia</p>
      @for (item of itens(); track item.rota) {
        <a
          [routerLink]="item.rota"
          routerLinkActive="bg-acento"
          [routerLinkActiveOptions]="{ exact: true }"
          class="rounded-md px-3 py-2"
        >
          {{ item.rotulo }}
        </a>
      }
    </aside>
  }
  <div [class]="naEntrada() ? 'w-full max-w-md' : 'flex min-w-0 flex-1 flex-col'">
    @if (!naEntrada()) {
      <header class="flex items-center justify-end gap-4 px-6 py-4">
        <p>{{ rotuloPapel() }}</p>
        <p>{{ funcionarioId() }}</p>
        <button type="button" class="rounded-md px-3 py-2 text-secundario" (click)="sair()">Sair</button>
      </header>
    }
    <main [class]="naEntrada() ? '' : 'px-6 pb-8'">
      <router-outlet />
    </main>
  </div>
</div>
```

Há um só `router-outlet`. O menu some na entrada porque `naEntrada()` fica verdadeiro; o outlet não é recriado em outro ramo.

Substitua o template de `EntradaComponent` por:

```ts
  template: `
    <form class="flex flex-col gap-4 rounded-xl bg-superficie p-6" aria-label="Entrar" (submit)="confirmar($event)">
      <h1 class="text-xl">Koinonia</h1>
      <label class="flex flex-col gap-1 text-secundario">
        Papel
        <select class="rounded-md bg-fundo px-3 py-2 text-texto" [value]="papel" (change)="definirPapel($event)">
          <option value=""></option>
          <option value="Gestao">Gestão</option>
          <option value="Empregado">Empregado</option>
        </select>
      </label>
      <label class="flex flex-col gap-1 text-secundario">
        Identificador
        <input class="rounded-md bg-fundo px-3 py-2 text-texto" [value]="identificador" (input)="definirIdentificador($event)" />
      </label>
      @if (erro) {
        <p class="text-secundario">{{ erro }}</p>
      }
      <button type="submit" class="rounded-md bg-acento px-4 py-2 text-texto">Entrar</button>
    </form>
  `,
```

Substitua o template de `VisaoGeralComponent` por:

```ts
  template: `
    <h1 class="text-4xl">O ponto está em uso.</h1>
    <p class="mt-2 text-secundario">A gestão organiza a jornada. O empregado registra a sua.</p>
    <article class="mt-8 rounded-xl bg-superficie p-6">
      <h2 class="text-xl">Ponto</h2>
      <p class="mt-2 text-secundario">Entrada, saída, escala e fechamento da jornada.</p>
      <div class="mt-6 flex flex-wrap gap-3">
        @for (atalho of atalhos; track atalho.rota) {
          <a class="rounded-md bg-acento px-4 py-2 text-texto" [routerLink]="atalho.rota">{{ atalho.rotulo }}</a>
        }
      </div>
    </article>
  `,
```

A classe dos componentes não muda.

Substitua `koinonia-web/src/app/gestao.guard.ts` por:

```ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export function papelEhGestao(papel: string | null): boolean {
  return papel?.toLowerCase() === 'gestao';
}

export const gestaoGuard: CanActivateFn = () => {
  if (papelEhGestao(sessionStorage.getItem('koinonia.papel'))) {
    return true;
  }
  return inject(Router).createUrlTree(['/ponto']);
};
```

`rotaDeGestao` e `sairDaGestao` saem porque o shell não troca o papel no lugar. Empregado em `/planejamento` ou `/fechamento` continua caindo em `/ponto` por `gestaoGuard`.

- [ ] **Step 4: Run test to verify it passes**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: PASS.

Depois confirme que o CSS gerado contém o tema e uma classe usada no shell:

```powershell
npx ng build --configuration=development
$css = Get-ChildItem dist/koinonia-web/browser -Filter *.css | Get-Content -Raw
if ($css -notmatch '0c0b10') { throw 'fundo ausente' }
if ($css -notmatch '7c5cbf') { throw 'acento ausente' }
if ($css -notmatch 'bg-fundo') { throw 'classe do fundo ausente' }
```

Expected: o build termina sem lançar essas exceções.

- [ ] **Step 5: Commit**

```powershell
git add -- koinonia-web/.postcssrc.json koinonia-web/package.json koinonia-web/package-lock.json koinonia-web/src/styles.css koinonia-web/src/index.html koinonia-web/src/app/app.ts koinonia-web/src/app/app.html koinonia-web/src/app/entrada.component.ts koinonia-web/src/app/visao-geral.component.ts koinonia-web/src/app/gestao.guard.ts koinonia-web/src/app/app.spec.ts koinonia-web/src/app/telas.spec.ts
git commit -m "feat: vestir a entrada e o menu do koinonia"
```

---

### Task 4: Campo, período e estado vazio

**Files:**
- Modify: `koinonia-web/projects/ui/src/lib/campo.component.ts`
- Modify: `koinonia-web/projects/ui/src/lib/estado-vazio.component.ts`
- Test: `koinonia-web/src/app/telas.spec.ts`

**Interfaces:**
- Consumes: as classes da Global Constraints
- Produces: `ui-campo` e `ui-estado-vazio` com essas classes. `ui-periodo` continua sendo dois campos e herda a classe do input.

- [ ] **Step 1: Write the failing test**

Em `telas.spec.ts`, importe `EstadoVazioComponent` de `ui`. No `describe('campo e periodo')`, acrescente:

```ts
  it('campo e periodo usam a superficie do tema', () => {
    const campo = TestBed.createComponent(CampoComponent);
    campo.componentInstance.rotulo = 'Nome';
    campo.detectChanges();
    expect(campo.nativeElement.querySelector('input')?.className).toContain('bg-fundo');
    expect(campo.nativeElement.querySelector('input')?.className).toContain('text-texto');

    const periodo = TestBed.createComponent(PeriodoComponent);
    periodo.detectChanges();
    expect(periodo.nativeElement.querySelector('input')?.className).toContain('bg-fundo');
  });

  it('estado vazio usa o texto secundario', () => {
    const fixture = TestBed.createComponent(EstadoVazioComponent);
    fixture.componentInstance.mensagem = 'Nada';
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('p')?.className).toContain('text-secundario');
  });
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: FAIL. O input ainda não tem `bg-fundo`.

- [ ] **Step 3: Write minimal implementation**

Substitua o template de `campo.component.ts` por:

```ts
  template: `
    <label class="flex flex-col gap-1 text-secundario">
      {{ rotulo }}
      <input class="rounded-md bg-fundo px-3 py-2 text-texto" [attr.type]="tipo" [value]="valor" (input)="aoDigitar($event)" />
    </label>
  `,
```

O texto `{{ rotulo }}` continua nó direto da `label`. A classe não muda.

Substitua o template de `estado-vazio.component.ts` por:

```ts
  template: `<p class="text-secundario">{{ mensagem }}</p>`,
```

Não altere `periodo.component.ts`: os dois `ui-campo` já carregam a classe.

- [ ] **Step 4: Run test to verify it passes**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: PASS. Os testes de campo, período, planejamento, ponto e fechamento continuam passando.

Run: `npx ng build ui`

Expected: build da biblioteca `ui` termina com sucesso.

- [ ] **Step 5: Commit**

```powershell
git add -- koinonia-web/projects/ui/src/lib/campo.component.ts koinonia-web/projects/ui/src/lib/estado-vazio.component.ts koinonia-web/src/app/telas.spec.ts
git commit -m "feat: aplicar o tema nos campos do koinonia"
```

---

### Task 5: Cara do ponto e do fechamento

**Files:**
- Modify: `koinonia-web/projects/ponto/src/lib/ponto.component.ts`
- Modify: `koinonia-web/projects/ponto/src/lib/espelho.component.ts`
- Test: `koinonia-web/src/app/telas.spec.ts`

**Interfaces:**
- Consumes: `CampoComponent`, `PeriodoComponent`, `EstadoVazioComponent`, `acoesDoEspelho`
- Produces: título `Ponto` ou `Fechamento` com `text-xl`, conteúdo num `section` com `bg-superficie`. Os métodos dos componentes permanecem.

- [ ] **Step 1: Write the failing test**

Dentro de `describe('ponto e fechamento')` em `telas.spec.ts`, acrescente:

```ts
  it('ponto e fechamento usam titulo menor e o cartao', () => {
    const ponto = TestBed.createComponent(PontoComponent);
    ponto.detectChanges();
    expect(ponto.nativeElement.querySelector('h1')?.textContent).toContain('Ponto');
    expect(ponto.nativeElement.querySelector('h1')?.className).toContain('text-xl');
    expect(ponto.nativeElement.querySelector('section')?.className).toContain('bg-superficie');

    const fechamento = TestBed.createComponent(FechamentoComponent);
    fechamento.detectChanges();
    expect(fechamento.nativeElement.querySelector('h1')?.textContent).toContain('Fechamento');
    expect(fechamento.nativeElement.querySelector('section')?.className).toContain('bg-superficie');
  });
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: FAIL. `PontoComponent` ainda não tem `h1`.

- [ ] **Step 3: Write minimal implementation**

No primeiro `@Component` de `ponto.component.ts` (`selector: 'ponto-tela'`), substitua só o `template` por:

```ts
  template: `
    <h1 class="mb-4 text-xl">Ponto</h1>
    <section class="flex flex-col gap-4 rounded-xl bg-superficie p-6">
      @if (erro) {
        <p class="text-secundario">{{ erro }}</p>
      }

      <ui-campo rotulo="Funcionário" [valor]="funcionarioId" (valorChange)="funcionarioId = $event" />
      <ui-campo rotulo="Dia" tipo="date" [valor]="dia" (valorChange)="dia = $event" />
      <button type="button" class="rounded-md bg-acento px-4 py-2 text-texto" (click)="carregar()">Carregar</button>

      <form class="flex flex-col gap-4" aria-label="Batida" (submit)="$event.preventDefault()">
        <ui-periodo
          tipo="datetime-local"
          [inicio]="entrada"
          [fim]="saida"
          (inicioChange)="entrada = $event"
          (fimChange)="saida = $event"
        />
      </form>

      @if (carregado) {
        <ponto-espelho
          [estado]="estado"
          [papel]="papel"
          [trechos]="trechos"
          (acaoEscolhida)="executar($event)"
        />
        @if (trechos.length === 0) {
          <ui-estado-vazio mensagem="Nenhum trecho neste dia." />
        }
        @for (trecho of trechos; track $index) {
          <p class="text-texto">
            {{ trecho.inicio }}
            {{ trecho.fim }}
            {{ trecho.fatia }}
            {{ trecho.evento }}
            {{ trecho.percentual }}
            {{ trecho.fator }}
            {{ trecho.minutosTrabalhados }}
            {{ trecho.minutosCreditados }}
          </p>
        }
      }
    </section>
  `,
```

No segundo `@Component` (`selector: 'ponto-fechamento'`), substitua só o `template` por:

```ts
  template: `
    <h1 class="mb-4 text-xl">Fechamento</h1>
    <section class="rounded-xl bg-superficie p-6">
      @if (erro) {
        <p class="text-secundario">{{ erro }}</p>
      }

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
          <select class="rounded-md bg-fundo px-3 py-2 text-texto" (change)="definirFormato($event)">
            <option value="csv">csv</option>
            <option value="excel">excel</option>
          </select>
        </label>
        <button type="submit" class="rounded-md bg-acento px-4 py-2 text-texto">Baixar</button>
      </form>
    </section>
  `,
```

Em `espelho.component.ts`, substitua só o `template` por:

```ts
  template: `
    <p class="text-texto">{{ estado === 'aprovado' ? 'Cópia aprovada' : 'Prévia' }}</p>
    @for (acao of acoes; track acao) {
      <button type="button" class="rounded-md bg-acento px-4 py-2 text-texto" (click)="acaoEscolhida.emit(acao)">{{ acao }}</button>
    }
  `,
```

Não altere métodos, rotas HTTP, `comFusoDeSaoPaulo`, nem o texto dos botões `registrar`, `aprovar`, `reabrir`, `exportar`, `Carregar` e `Baixar`.

- [ ] **Step 4: Run test to verify it passes**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: PASS, inclusive prévia, registro, saída recusada, estado vazio, aprovar, reabrir, exportar e download.

Run: `npx ng build ponto`

Expected: build da biblioteca `ponto` termina com sucesso.

- [ ] **Step 5: Commit**

```powershell
git add -- koinonia-web/projects/ponto/src/lib/ponto.component.ts koinonia-web/projects/ponto/src/lib/espelho.component.ts koinonia-web/src/app/telas.spec.ts
git commit -m "feat: vestir o ponto e o fechamento"
```

---

### Task 6: Cara do planejamento

**Files:**
- Modify: `koinonia-web/projects/planejamento/src/lib/planejamento.component.ts`
- Test: `koinonia-web/src/app/telas.spec.ts`

**Interfaces:**
- Consumes: as mesmas classes de cartão, botão, campo e erro das tarefas 4 e 5
- Produces: título `Planejamento` com `text-xl` e um `section` com `bg-superficie`. Os métodos permanecem.

- [ ] **Step 1: Write the failing test**

Dentro de `describe('planejamento')` em `telas.spec.ts`, acrescente:

```ts
  it('planejamento usa titulo menor e o cartao', () => {
    const fixture = montarPlanejamento(http, []);
    expect(fixture.nativeElement.querySelector('h1')?.textContent).toContain('Planejamento');
    expect(fixture.nativeElement.querySelector('h1')?.className).toContain('text-xl');
    expect(fixture.nativeElement.querySelector('section')?.className).toContain('bg-superficie');
  });
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: FAIL. Não há `h1` com `Planejamento`.

- [ ] **Step 3: Write minimal implementation**

Substitua só o `template` de `PlanejamentoComponent` por:

```ts
  template: `
    <h1 class="mb-4 text-xl">Planejamento</h1>
    <section class="flex flex-col gap-8 rounded-xl bg-superficie p-6">
      @if (erro) {
        <p class="text-secundario">{{ erro }}</p>
      }

      <form class="flex flex-col gap-4" aria-label="Cadastrar funcionário" (submit)="cadastrar($event)">
        <ui-campo rotulo="Nome" [valor]="nome" (valorChange)="nome = $event" />
        <button type="submit" class="rounded-md bg-acento px-4 py-2 text-texto">Cadastrar</button>
      </form>

      @if (funcionarios.length === 0) {
        <ui-estado-vazio mensagem="Nenhum funcionário cadastrado." />
      }

      <form class="flex flex-col gap-4" aria-label="Publicar escala" (submit)="publicarEscala($event)">
        <label class="flex flex-col gap-1 text-secundario">
          Funcionário
          <select class="rounded-md bg-fundo px-3 py-2 text-texto" [value]="funcionarioEscala" (change)="definirFuncionario($event)">
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
        <button type="submit" class="rounded-md bg-acento px-4 py-2 text-texto">Publicar</button>
        <button type="button" class="rounded-md bg-fundo px-4 py-2 text-secundario" (click)="removerEscala()">Remover</button>
      </form>

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
          <label>
            <input type="checkbox" (change)="alternarFuncionario(funcionario.id, $event)" />
            {{ funcionario.nome }}
          </label>
        }
        <label class="flex flex-col gap-1 text-secundario">
          Forma
          <select class="rounded-md bg-fundo px-3 py-2 text-texto" (change)="definirForma($event)">
            <option value="hora normal">hora normal</option>
            <option value="adicional">adicional</option>
            <option value="banco de horas">banco de horas</option>
          </select>
        </label>
        <ui-campo rotulo="Percentual" [valor]="percentual" (valorChange)="percentual = $event" />
        <ui-campo rotulo="Fator" [valor]="fator" (valorChange)="fator = $event" />
        <button type="submit" class="rounded-md bg-acento px-4 py-2 text-texto">Publicar</button>
      </form>

      <form class="flex flex-col gap-4" aria-label="Percentual institucional" (submit)="definirPercentual($event)">
        <ui-campo
          rotulo="Percentual"
          [valor]="percentualInstitucional"
          (valorChange)="percentualInstitucional = $event"
        />
        <button type="submit" class="rounded-md bg-acento px-4 py-2 text-texto">Publicar</button>
      </form>
    </section>
  `,
```

Não altere os métodos nem os corpos enviados à API.

- [ ] **Step 4: Run test to verify it passes**

Run: `npx ng test koinonia-web --watch=false --browsers=ChromeHeadless`

Expected: PASS, inclusive cadastro, escala, evento, percentual e a recusa 422.

Run:

```powershell
npx ng build ui
npx ng build planejamento
npx ng build ponto
```

Expected: as três bibliotecas compilam sozinhas.

- [ ] **Step 5: Commit**

```powershell
git add -- koinonia-web/projects/planejamento/src/lib/planejamento.component.ts koinonia-web/src/app/telas.spec.ts
git commit -m "feat: vestir o planejamento"
```
