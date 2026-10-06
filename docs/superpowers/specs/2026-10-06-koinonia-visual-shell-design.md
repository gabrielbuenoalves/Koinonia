# Koinonia — Shell visual

Data: 2026-10-06
Status: aprovado

## Objetivo

Dar ao Koinonia uma interface escura, com menu por papel, inspirada no clima de um painel de ecossistema escolar. O nome visível é Koinonia. A página de referência não é copiada: sem a marca Educa Nexus, sem os números ilustrativos e sem módulos que ainda não existem.

Nesta entrega entram a tela de entrada, a visão geral e o visual das telas de ponto, planejamento e fechamento. A API, os cabeçalhos e as regras de negócio permanecem os que já estão no ar.

## Ciclos seguintes

Ficam para um ciclo posterior:

- módulos além do ponto
- nome da pessoa no lugar do identificador
- troca de papel sem voltar à entrada
- teste de pixel ou de cor

Quando um módulo novo existir, ele entra como mais um item na lista do menu, com os papéis que podem vê-lo. A visão geral e o shell não são refeitos.

## Entrada, casa e menu

A rota `/entrar` pede o papel (`Gestao` ou `Empregado`) e o identificador. Ao confirmar, grava `koinonia.papel` e `koinonia.funcionarioId` no `sessionStorage` e leva os dois papéis para `/`, a visão geral.

Sem sessão, qualquer outra rota volta para `/entrar`.

O menu é uma lista. Cada item tem rótulo, rota e os papéis que o veem.

| Atalho | Rota | Quem vê |
|---|---|---|
| Visão geral | `/` | gestão e empregado |
| Ponto | `/ponto` | gestão e empregado |
| Planejamento | `/planejamento` | gestão |
| Fechamento | `/fechamento` | gestão |

Uma função pura filtra essa lista pelo papel. O empregado que abre `/planejamento` ou `/fechamento` pelo endereço cai em `/ponto`. Sair limpa a sessão e volta para `/entrar`.

A visão geral tem um único módulo, o Ponto. Para a gestão, esse módulo oferece abrir o ponto, o planejamento e o fechamento. Para o empregado, oferece abrir o ponto. O shell mostra o papel e o identificador de quem entrou. O valor gravado continua `Gestao` ou `Empregado`. Na faixa, a gestão aparece como “Gestão” e o empregado como “Empregado”. A gestão não precisa estar cadastrada. Um empregado desconhecido continua recusado pela API quando usa o ponto.

## Linguagem visual

O clima é fundo escuro, menu à esquerda, um cartão em destaque e um acento só. A paleta e os textos são do Koinonia.

O tema tem quatro papéis: fundo quase preto, superfície do cartão um tom acima, texto claro com um cinza para o secundário, e um violeta para o botão principal e para o item ativo do menu. Uma fonte sans. A visão geral usa um título grande. As telas de trabalho usam título menor e o mesmo cartão.

A entrada é um cartão no centro, sem menu. Depois do login, o shell é menu fixo, uma faixa no topo com papel, identificador e Sair, e o conteúdo à direita.

O título da visão geral é “O ponto está em uso.” A linha de baixo é “A gestão organiza a jornada. O empregado registra a sua.” O cartão do Ponto diz “Entrada, saída, escala e fechamento da jornada.”

Campo, período e estado vazio passam a usar essas mesmas classes. Formulário, botão, mensagem de erro e lista ficam com a mesma cara nas três telas.

## Telas e fronteiras

O Tailwind entra uma vez no aplicativo. Os caminhos de conteúdo incluem o shell e as bibliotecas `ui`, `planejamento` e `ponto`. As classes ficam nos templates. As bibliotecas continuam compilando sozinhas.

O shell fica com a entrada, a visão geral, a faixa de quem entrou e o menu. As telas de trabalho mudam de cara e mantêm o comportamento.

- Ponto carrega o espelho do dia, mostra a prévia ou o dia aprovado, e os botões continuam vindo de `acoesDoEspelho`. Registrar, aprovar e reabrir chamam as mesmas rotas.
- Planejamento cadastra funcionário, publica e remove escala, publica evento e define o percentual da extra comum.
- Fechamento pede início, fim e formato, e baixa o arquivo.

A API, os cabeçalhos `X-Funcionario-Id` e `X-Papel`, as chaves do `sessionStorage` e as regras de quem pode abrir cada rota permanecem as atuais.

## Falhas

A entrada recusa papel vazio ou identificador que, sem espaços nas pontas, não seja um guid, e permanece na tela. Com os dois válidos, grava a sessão e abre a visão geral.

Quando a API responde 422, o texto da resposta continua ao lado da ação que falhou. Dia sem trecho continua no estado vazio. Sair apaga a sessão. Sem sessão, o shell volta para `/entrar`.

## Testes

Os testes novos cobrem a função do menu e a entrada:

- Gestão vê visão geral, ponto, planejamento e fechamento.
- Empregado vê visão geral e ponto.
- Identificador inválido não grava sessão e não navega.
- Sessão válida de cada papel navega para `/`.
- Sem sessão, uma rota interna volta para `/entrar`.

Os testes que já existem do espelho, das rotas, do guarda e das chamadas HTTP continuam passando. Não há teste de pixel nem de cor.
