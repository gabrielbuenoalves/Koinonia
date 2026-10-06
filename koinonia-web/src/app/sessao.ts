export function papelHttp(armazenado: string | null): 'Gestao' | 'Empregado' {
  return armazenado?.toLowerCase() === 'gestao' ? 'Gestao' : 'Empregado';
}
