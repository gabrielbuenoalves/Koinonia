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
