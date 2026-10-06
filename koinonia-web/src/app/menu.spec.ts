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
