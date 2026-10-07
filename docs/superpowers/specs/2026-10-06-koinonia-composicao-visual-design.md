# Koinonia — Composição visual

Data: 2026-10-06
Status: aprovado

## Objetivo

Trocar a cara do Koinonia. O painel escuro, empilhado e de um violeta só sai. Entra papel creme, título com presença e acento vinho, com as tarefas em cartões separados.

Rotas, sessão, guardas, cabeçalhos, chamadas HTTP e regras de quem vê cada tela permanecem as do shell já aprovado. Este spec substitui a linguagem visual daquele documento. O comportamento descrito lá continua valendo.

## Linguagem

O fundo da página é papel `#f4efe6`. O cartão é marfim `#fffdf9`. O trilho do menu é `#efe6d8`. O texto é `#2a241e`. O secundário é `#74685c`. O acento é vinho `#6b2d3c`. O item ativo do menu e a pílula de estado usam um rosa `#f6e6e8`. O campo é branco `#ffffff`. O texto sobre o botão principal é `#fff8f4`.

Os nomes dos tokens permanecem os que o tema já expõe, mais os novos: `fundo`, `superficie`, `texto`, `secundario`, `acento`, `trilho`, `realce`, `campo`, `sobre`. O `color-scheme` passa a `light`.

A interface usa Source Sans 3. O título da página e o nome Koinonia usam Fraunces. As famílias entram pelos pacotes `@fontsource/source-sans-3` e `@fontsource/fraunces`, importadas no CSS, sem depender de uma rede na hora de abrir a tela.

Cartão usa `rounded-2xl` e borda fina. Campo e select usam `rounded-xl` e `bg-campo`. Botão e item de menu usam `rounded-full`. O botão principal é vinho. O atalho secundário tem contorno vinho e fundo transparente. Remover e Sair ficam só no texto secundário, sem preenchimento. Todo input e todo select da aplicação usa `bg-campo` e `text-texto`.

Abaixo de `md`, o menu vira uma faixa que quebra linha acima do conteúdo. De `md` para cima, o trilho fica fixo à esquerda.

## Telas

A entrada continua um cartão no centro, sem menu. O cartão leva o rótulo “Casa”, o título “Koinonia”, papel, identificador e Entrar. A validação não muda.

Depois da entrada, o shell é trilho, faixa e conteúdo. A faixa mostra o papel como “Gestão” ou “Empregado”, o identificador completo gravado na sessão e Sair. O item ativo do menu usa o rosa e o vinho.

A visão geral mantém as frases “O ponto está em uso.” e “A gestão organiza a jornada. O empregado registra a sua.” O módulo Ponto mantém “Entrada, saída, escala e fechamento da jornada.” Ponto é o botão vinho. Planejamento e Fechamento, quando o papel os vê, são contorno. A lista continua saindo de `atalhosDoModulo`.

O ponto tem o título “Ponto”, o cartão da consulta e o cartão da batida. A consulta pede funcionário, dia e Carregar. A batida pede início e fim. O espelho só aparece depois do carregamento, num cartão próprio: a pílula diz “Prévia” ou “Cópia aprovada”, e os botões saem de `acoesDoEspelho`. Registrar não fica no cartão da batida e não aparece antes do dia carregar, nem para o papel que não tem essa ação.

Os trechos deixam de ser uma linha de texto. Viram tabela com início, fim, fatia, evento, percentual, fator, minutos trabalhados e minutos creditados. Célula vazia mostra “—”. Sem trecho, permanece “Nenhum trecho neste dia.”

O planejamento tem o título “Planejamento” e quatro cartões, em duas colunas a partir de `md`:

| Cartão | Rótulo pequeno | Formulário |
|---|---|---|
| Funcionário | Pessoas | `Cadastrar funcionário` |
| Escala | Jornada | `Publicar escala` |
| Evento | Exceção | `Publicar evento` |
| Extra comum | Casa | `Percentual institucional` |

Os `aria-label` desses formulários permanecem esses. Remover continua no cartão da escala, como texto. A lista vazia continua “Nenhum funcionário cadastrado.”

O fechamento permanece na rota `/fechamento`, num cartão só, com início, fim, formato e Baixar. Não entra dentro do planejamento.

`ui-periodo` coloca início e fim lado a lado a partir de `sm`. `ui-campo` e o estado vazio mudam de classe e não mudam de contrato.

## Botões do espelho

`acoesDoEspelho` continua devolvendo `registrar`, `aprovar`, `reabrir` e `exportar`. O botão emite esse valor. O texto visível é Registrar, Aprovar, Reabrir e Exportar.

## Falhas

Papel vazio ou identificador inválido continua na entrada, com a mesma frase. A resposta 422 continua num parágrafo acima dos cartões da tela que falhou. Sair apaga a sessão. Sem sessão, o shell volta para `/entrar`.

## Testes

Os testes de menu, sessão, guarda, espelho, rotas e HTTP continuam passando, com três ajustes de cara:

- O título de Ponto e de Planejamento deixa de exigir `text-xl` e passa a exigir `font-serif`.
- O campo e o período deixam de exigir `bg-fundo` no input e passam a exigir `bg-campo`. `text-texto` permanece.
- O clique que hoje procura o botão `registrar` passa a procurar `Registrar`.

`acoesDoEspelho` continua sendo testado pelas chaves, não pelo texto do botão. Cada cartão de trabalho é uma `section` com `bg-superficie`, então a asserção que procura essa classe no primeiro cartão permanece. O estado vazio continua com `text-secundario`. Não há teste de pixel nem de cor.
