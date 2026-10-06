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
