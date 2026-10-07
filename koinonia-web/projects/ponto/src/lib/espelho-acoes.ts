export type EstadoEspelho = 'previa' | 'aprovado';
export type Papel = 'gestao' | 'empregado';

export function papelDoEspelho(armazenado: string | null): Papel {
  return armazenado?.toLowerCase() === 'gestao' ? 'gestao' : 'empregado';
}

export function acoesDoEspelho(estado: EstadoEspelho, papel: Papel): string[] {
  if (papel === 'empregado') {
    return estado === 'previa' ? ['registrar'] : [];
  }
  return estado === 'previa' ? ['aprovar'] : ['reabrir', 'exportar'];
}

const rotulos: Record<string, string> = {
  registrar: 'Registrar',
  aprovar: 'Aprovar',
  reabrir: 'Reabrir',
  exportar: 'Exportar',
};

export function rotuloDaAcao(acao: string): string {
  return rotulos[acao] ?? acao;
}
