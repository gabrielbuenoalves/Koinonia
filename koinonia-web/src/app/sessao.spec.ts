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
