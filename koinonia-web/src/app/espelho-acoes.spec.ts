import { acoesDoEspelho, rotuloDaAcao } from 'ponto';

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

  it('mostra o rotulo da acao sem mudar a chave', () => {
    expect(rotuloDaAcao('registrar')).toBe('Registrar');
    expect(rotuloDaAcao('aprovar')).toBe('Aprovar');
    expect(rotuloDaAcao('reabrir')).toBe('Reabrir');
    expect(rotuloDaAcao('exportar')).toBe('Exportar');
  });
});
