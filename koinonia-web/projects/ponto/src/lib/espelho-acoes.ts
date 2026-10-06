export type EstadoEspelho = 'previa' | 'aprovado';
export type Papel = 'gestao' | 'empregado';

export function acoesDoEspelho(estado: EstadoEspelho, papel: Papel): string[] {
  if (papel === 'empregado') {
    return estado === 'previa' ? ['registrar'] : [];
  }
  return estado === 'previa' ? ['aprovar'] : ['reabrir', 'exportar'];
}
