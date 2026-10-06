import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { PlanejamentoComponent } from 'planejamento';
import { FechamentoComponent, PontoComponent, papelDoEspelho } from 'ponto';
import { CampoComponent, PeriodoComponent } from 'ui';
import { App } from './app';
import { apiUrl } from './api-url';
import { cabecalhosInterceptor } from './cabecalhos.interceptor';
import { routes } from './app.routes';

const gestaoId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const ana = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

describe('api', () => {
  it('aponta para o host local', () => {
    expect(apiUrl).toBe('http://localhost:5188');
  });
});

describe('papel armazenado', () => {
  it('mapeia Gestao e Empregado para as acoes do espelho', () => {
    expect(papelDoEspelho('Gestao')).toBe('gestao');
    expect(papelDoEspelho('gestao')).toBe('gestao');
    expect(papelDoEspelho('Empregado')).toBe('empregado');
    expect(papelDoEspelho('empregado')).toBe('empregado');
  });
});

describe('shell', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter(routes)],
    });
  });

  afterEach(() => sessionStorage.clear());

  it('guarda papel e funcionario nas chaves da sessao', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    preencher(fixture.nativeElement, 'Papel', 'Gestao');
    preencher(fixture.nativeElement, 'Funcionário da sessão', gestaoId);

    expect(sessionStorage.getItem('koinonia.papel')).toBe('Gestao');
    expect(sessionStorage.getItem('koinonia.funcionarioId')).toBe(gestaoId);
  });

  it('empregado nao ve planejamento nem fechamento', () => {
    sessionStorage.setItem('koinonia.papel', 'Empregado');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const links = hrefs(fixture.nativeElement);

    expect(links).toContain('/ponto');
    expect(links).not.toContain('/planejamento');
    expect(links).not.toContain('/fechamento');
  });

  it('gestao ve planejamento, ponto e fechamento', () => {
    sessionStorage.setItem('koinonia.papel', 'Gestao');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const links = hrefs(fixture.nativeElement);

    expect(links).toEqual(jasmine.arrayContaining(['/planejamento', '/ponto', '/fechamento']));
  });
});

describe('guard de gestao', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(withInterceptors([cabecalhosInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('empregado nao entra em planejamento nem fechamento', async () => {
    sessionStorage.setItem('koinonia.papel', 'Empregado');
    sessionStorage.setItem('koinonia.funcionarioId', ana);
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/planejamento');
    expect(router.url).toBe('/ponto');

    await router.navigateByUrl('/fechamento');
    expect(router.url).toBe('/ponto');
  });

  it('gestao entra em planejamento e fechamento', async () => {
    sessionStorage.setItem('koinonia.papel', 'Gestao');
    sessionStorage.setItem('koinonia.funcionarioId', gestaoId);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/planejamento');
    http.expectOne(`${apiUrl}/funcionarios`).flush('[]');
    expect(TestBed.inject(Router).url).toBe('/planejamento');

    await harness.navigateByUrl('/fechamento');
    expect(TestBed.inject(Router).url).toBe('/fechamento');
  });

  it('trocar para empregado em planejamento ou fechamento vai para ponto', async () => {
    sessionStorage.setItem('koinonia.papel', 'Gestao');
    sessionStorage.setItem('koinonia.funcionarioId', gestaoId);
    const router = TestBed.inject(Router);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    await router.navigateByUrl('/planejamento');
    fixture.detectChanges();
    http.expectOne(`${apiUrl}/funcionarios`).flush('[]');
    expect(router.url).toBe('/planejamento');

    preencher(fixture.nativeElement, 'Papel', 'Empregado');
    await fixture.whenStable();
    expect(router.url).toBe('/ponto');

    preencher(fixture.nativeElement, 'Papel', 'Gestao');
    await router.navigateByUrl('/fechamento');
    expect(router.url).toBe('/fechamento');

    preencher(fixture.nativeElement, 'Papel', 'Empregado');
    await fixture.whenStable();
    expect(router.url).toBe('/ponto');
  });
});

describe('campo e periodo', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('campo emite o texto digitado', () => {
    const fixture = TestBed.createComponent(CampoComponent);
    let valor = '';
    fixture.componentInstance.rotulo = 'Nome';
    fixture.componentInstance.valorChange.subscribe((item) => (valor = item));
    fixture.detectChanges();

    preencher(fixture.nativeElement, 'Nome', 'Ana');
    expect(valor).toBe('Ana');
  });

  it('periodo emite inicio e fim', () => {
    const fixture = TestBed.createComponent(PeriodoComponent);
    let inicio = '';
    let fim = '';
    fixture.componentInstance.inicioChange.subscribe((item) => (inicio = item));
    fixture.componentInstance.fimChange.subscribe((item) => (fim = item));
    fixture.detectChanges();

    preencher(fixture.nativeElement, 'Início', '480');
    preencher(fixture.nativeElement, 'Fim', '1020');
    expect(inicio).toBe('480');
    expect(fim).toBe('1020');
  });
});

describe('planejamento', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.setItem('koinonia.papel', 'Gestao');
    sessionStorage.setItem('koinonia.funcionarioId', gestaoId);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([cabecalhosInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('cadastra funcionario e mostra estado vazio sem lista', () => {
    const fixture = montarPlanejamento(http, []);
    expect(fixture.nativeElement.querySelector('ui-estado-vazio')?.textContent).toContain('Nenhum funcionário');

    const form = formulario(fixture, 'Cadastrar funcionário');
    preencher(form, 'Nome', 'Ana');
    clicar(form, 'Cadastrar');

    const pedido = http.expectOne(`${apiUrl}/funcionarios`);
    expect(pedido.request.method).toBe('POST');
    expect(pedido.request.headers.get('X-Papel')).toBe('Gestao');
    expect(pedido.request.headers.get('X-Funcionario-Id')).toBe(gestaoId);
    expect(pedido.request.body).toEqual({ nome: 'Ana' });
    pedido.flush(JSON.stringify({ id: ana, nome: 'Ana' }));
    http.expectOne(`${apiUrl}/funcionarios`).flush(JSON.stringify([{ id: ana, nome: 'Ana' }]));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Ana');
  });

  it('envia Gestao no cabecalho quando o papel guardado e gestao', () => {
    sessionStorage.setItem('koinonia.papel', 'gestao');
    const fixture = TestBed.createComponent(PlanejamentoComponent);
    fixture.detectChanges();
    const pedido = http.expectOne(`${apiUrl}/funcionarios`);
    expect(pedido.request.headers.get('X-Papel')).toBe('Gestao');
    pedido.flush('[]');
  });

  it('publica e remove a escala sem recalcular trecho', () => {
    const fixture = montarPlanejamento(http, [{ id: ana, nome: 'Ana' }]);
    const form = formulario(fixture, 'Publicar escala');
    preencher(form, 'Funcionário', ana);
    preencher(form, 'Dia', '2026-10-06');
    preencher(form, 'Início', '480');
    preencher(form, 'Fim', '1020');
    clicar(form, 'Publicar');

    const publicar = http.expectOne(`${apiUrl}/escalas`);
    expect(publicar.request.method).toBe('PUT');
    expect(publicar.request.body).toEqual({
      funcionarioId: ana,
      dia: '2026-10-06',
      intervalos: [{ inicio: 480, fim: 1020 }],
    });
    publicar.flush('');

    clicar(form, 'Remover');
    const remover = http.expectOne(`${apiUrl}/escalas/${ana}/2026-10-06`);
    expect(remover.request.method).toBe('DELETE');
    remover.flush('');
    expect(fixture.nativeElement.textContent).not.toContain('minutosTrabalhados');
  });

  it('publica evento com a forma pedida pela api', () => {
    const fixture = montarPlanejamento(http, [{ id: ana, nome: 'Ana' }]);
    const form = formulario(fixture, 'Publicar evento');
    preencher(form, 'Nome', 'Formatura');
    preencher(form, 'Início', '2026-10-06T14:00');
    preencher(form, 'Fim', '2026-10-06T18:00');
    preencher(form, 'Forma', 'banco de horas');
    preencher(form, 'Fator', '1');
    const caixa = form.querySelector('input[type="checkbox"]') as HTMLInputElement;
    caixa.checked = true;
    caixa.dispatchEvent(new Event('change'));
    clicar(form, 'Publicar');

    const pedido = http.expectOne(`${apiUrl}/eventos`);
    expect(pedido.request.method).toBe('POST');
    expect(pedido.request.body).toEqual({
      nome: 'Formatura',
      inicio: '2026-10-06T14:00:00-03:00',
      fim: '2026-10-06T18:00:00-03:00',
      funcionarioIds: [ana],
      forma: 'banco de horas',
      percentual: null,
      fator: 1,
    });
    pedido.flush(JSON.stringify({ id: 'cccccccc-cccc-cccc-cccc-cccccccccccc' }));
  });

  it('grava o percentual institucional', () => {
    const fixture = montarPlanejamento(http, []);
    const form = formulario(fixture, 'Percentual institucional');
    preencher(form, 'Percentual', '50');
    clicar(form, 'Publicar');

    const pedido = http.expectOne(`${apiUrl}/percentual-extra-comum`);
    expect(pedido.request.method).toBe('PUT');
    expect(pedido.request.body).toEqual({ valor: 50 });
    pedido.flush('');
  });

  it('mostra a recusa da api', () => {
    const fixture = montarPlanejamento(http, []);
    const form = formulario(fixture, 'Cadastrar funcionário');
    preencher(form, 'Nome', 'Ana');
    clicar(form, 'Cadastrar');
    http.expectOne(`${apiUrl}/funcionarios`).flush('Funcionário desconhecido é recusado no host.', {
      status: 422,
      statusText: 'Unprocessable Entity',
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Funcionário desconhecido é recusado no host.');
  });
});

describe('ponto e fechamento', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(withInterceptors([cabecalhosInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('mostra a previa devolvida pela api e registra entrada e saida', () => {
    sessionStorage.setItem('koinonia.papel', 'Empregado');
    sessionStorage.setItem('koinonia.funcionarioId', ana);
    const fixture = TestBed.createComponent(PontoComponent);
    fixture.detectChanges();
    preencher(fixture.nativeElement, 'Dia', '2026-10-06');
    clicar(fixture.nativeElement, 'Carregar');

    const espelho = http.expectOne(`${apiUrl}/espelho/${ana}/2026-10-06`);
    expect(espelho.request.method).toBe('GET');
    expect(espelho.request.headers.get('X-Papel')).toBe('Empregado');
    expect(espelho.request.headers.get('X-Funcionario-Id')).toBe(ana);
    espelho.flush(JSON.stringify({ aprovado: false, trechos: [trecho()] }));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Prévia');
    expect(fixture.nativeElement.textContent).toContain('360');
    expect(fixture.nativeElement.textContent).toContain('Regular');
    expect(fixture.nativeElement.querySelectorAll('button').length).toBeGreaterThan(0);

    const batida = formulario(fixture, 'Batida');
    preencher(batida, 'Início', '2026-10-06T08:00');
    preencher(batida, 'Fim', '2026-10-06T19:00');
    clicar(fixture.nativeElement, 'registrar');

    const entrada = http.expectOne(`${apiUrl}/batidas/entrada`);
    expect(entrada.request.method).toBe('POST');
    expect(entrada.request.body).toEqual({ entrada: '2026-10-06T08:00:00-03:00' });
    entrada.flush(JSON.stringify({ id: 'dddddddd-dddd-dddd-dddd-dddddddddddd' }));

    const saida = http.expectOne(`${apiUrl}/batidas/dddddddd-dddd-dddd-dddd-dddddddddddd/saida`);
    expect(saida.request.method).toBe('POST');
    expect(saida.request.body).toEqual({ saida: '2026-10-06T19:00:00-03:00' });
    saida.flush('');
    http.expectOne(`${apiUrl}/espelho/${ana}/2026-10-06`).flush(JSON.stringify({ aprovado: false, trechos: [] }));
  });

  it('mostra estado vazio quando o espelho nao tem trechos', () => {
    sessionStorage.setItem('koinonia.papel', 'Empregado');
    sessionStorage.setItem('koinonia.funcionarioId', ana);
    const fixture = TestBed.createComponent(PontoComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Nenhum trecho');

    preencher(fixture.nativeElement, 'Dia', '2026-10-06');
    clicar(fixture.nativeElement, 'Carregar');
    http.expectOne(`${apiUrl}/espelho/${ana}/2026-10-06`).flush(JSON.stringify({ aprovado: false, trechos: [] }));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('ui-estado-vazio')?.textContent).toContain('Nenhum trecho');
  });

  it('gestao aprova e reabre o dia', () => {
    sessionStorage.setItem('koinonia.papel', 'Gestao');
    sessionStorage.setItem('koinonia.funcionarioId', gestaoId);
    const fixture = TestBed.createComponent(PontoComponent);
    fixture.detectChanges();
    preencher(fixture.nativeElement, 'Funcionário', ana);
    preencher(fixture.nativeElement, 'Dia', '2026-10-06');
    clicar(fixture.nativeElement, 'Carregar');
    http.expectOne(`${apiUrl}/espelho/${ana}/2026-10-06`).flush(JSON.stringify({ aprovado: false, trechos: [trecho()] }));
    fixture.detectChanges();

    clicar(fixture.nativeElement, 'aprovar');
    const aprovar = http.expectOne(`${apiUrl}/dias/${ana}/2026-10-06/aprovacao`);
    expect(aprovar.request.method).toBe('POST');
    expect(aprovar.request.headers.get('X-Papel')).toBe('Gestao');
    aprovar.flush('');
    http.expectOne(`${apiUrl}/espelho/${ana}/2026-10-06`).flush(JSON.stringify({ aprovado: true, trechos: [trecho()] }));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Cópia aprovada');

    clicar(fixture.nativeElement, 'reabrir');
    const reabrir = http.expectOne(`${apiUrl}/dias/${ana}/2026-10-06/aprovacao`);
    expect(reabrir.request.method).toBe('DELETE');
    reabrir.flush('');
    http.expectOne(`${apiUrl}/espelho/${ana}/2026-10-06`).flush(JSON.stringify({ aprovado: false, trechos: [] }));
  });

  it('exportar abre o fechamento', async () => {
    sessionStorage.setItem('koinonia.papel', 'Gestao');
    sessionStorage.setItem('koinonia.funcionarioId', gestaoId);
    const fixture = TestBed.createComponent(PontoComponent);
    fixture.detectChanges();
    preencher(fixture.nativeElement, 'Funcionário', ana);
    preencher(fixture.nativeElement, 'Dia', '2026-10-06');
    clicar(fixture.nativeElement, 'Carregar');
    http.expectOne(`${apiUrl}/espelho/${ana}/2026-10-06`).flush(JSON.stringify({ aprovado: true, trechos: [trecho()] }));
    fixture.detectChanges();

    clicar(fixture.nativeElement, 'exportar');
    await fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/fechamento');
  });

  it('baixa o arquivo do periodo', () => {
    sessionStorage.setItem('koinonia.papel', 'Gestao');
    sessionStorage.setItem('koinonia.funcionarioId', gestaoId);
    const fixture = TestBed.createComponent(FechamentoComponent);
    fixture.detectChanges();
    const form = formulario(fixture, 'Fechamento');
    preencher(form, 'Início', '2026-10-06');
    preencher(form, 'Fim', '2026-10-06');
    preencher(form, 'Formato', 'csv');
    clicar(form, 'Baixar');

    const pedido = http.expectOne(`${apiUrl}/fechamento?inicio=2026-10-06&fim=2026-10-06&formato=csv`);
    expect(pedido.request.method).toBe('GET');
    expect(pedido.request.headers.get('X-Papel')).toBe('Gestao');
    pedido.flush(new Blob(['arquivo']));
  });
});

function trecho() {
  return {
    inicio: '2026-10-06T08:00:00-03:00',
    fim: '2026-10-06T14:00:00-03:00',
    fatia: 'Regular',
    evento: null,
    percentual: null,
    fator: null,
    minutosTrabalhados: 360,
    minutosCreditados: null,
  };
}

function montarPlanejamento(
  http: HttpTestingController,
  lista: { id: string; nome: string }[],
): ComponentFixture<PlanejamentoComponent> {
  const fixture = TestBed.createComponent(PlanejamentoComponent);
  fixture.detectChanges();
  const pedido = http.expectOne(`${apiUrl}/funcionarios`);
  expect(pedido.request.method).toBe('GET');
  pedido.flush(JSON.stringify(lista));
  fixture.detectChanges();
  return fixture;
}

function formulario(fixture: ComponentFixture<unknown> | HTMLElement, nome: string): HTMLElement {
  const raiz = fixture instanceof HTMLElement ? fixture : fixture.nativeElement;
  const form = raiz.querySelector(`form[aria-label="${nome}"]`);
  if (!form) {
    throw new Error(`Formulário não encontrado: ${nome}`);
  }
  return form as HTMLElement;
}

function hrefs(raiz: HTMLElement): string[] {
  return [...raiz.querySelectorAll('a')].map((item) => item.getAttribute('href') ?? '');
}

function preencher(raiz: ParentNode, rotulo: string, valor: string) {
  const label = [...raiz.querySelectorAll('label')].find((item) => rotuloDe(item) === rotulo);
  const campo = label?.querySelector('input, select') as HTMLInputElement | HTMLSelectElement | null;
  if (!campo) {
    throw new Error(`Campo não encontrado: ${rotulo}`);
  }
  campo.value = valor;
  campo.dispatchEvent(new Event(campo.tagName === 'SELECT' ? 'change' : 'input'));
}

function rotuloDe(label: Element): string {
  const texto = [...label.childNodes].find(
    (no) => no.nodeType === Node.TEXT_NODE && no.textContent?.trim(),
  );
  return texto?.textContent?.trim() ?? '';
}

function clicar(raiz: ParentNode, texto: string) {
  const botao = [...raiz.querySelectorAll('button')].find((item) => item.textContent?.trim() === texto);
  if (!botao) {
    throw new Error(`Botão não encontrado: ${texto}`);
  }
  (botao as HTMLButtonElement).click();
}
