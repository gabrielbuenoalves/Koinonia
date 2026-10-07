# Controle de Ponto e Escalas — Design

Data: 2026-10-06
Status: aprovado

## Objetivo

Classificar cada minuto trabalhado em hora regular, hora extra comum ou hora de evento, com a política de pagamento copiada no momento da aprovação, e exportar essas horas separadas e somadas.

Planejamento publica a escala e o evento. Ponto registra a batida, calcula a prévia, aprova e exporta. A planilha lê somente a cópia gravada na aprovação.

## Ciclos seguintes

Ficam para um ciclo posterior:

- saldo acumulado de banco de horas, compensação e vencimento
- valor em moeda
- histórico da cópia removida ao reabrir
- mais de um fuso horário

## Regras de classificação

O dia civil usa o fuso `America/Sao_Paulo`.

Só entra na conta o tempo entre uma entrada e uma saída. O vão entre duas batidas fica de fora. Uma entrada ainda sem saída fica de fora da prévia e impede a aprovação daquele dia.

A apuração corta o tempo trabalhado em três fatias. O mesmo minuto aparece em uma só.

1. **Evento.** Minutos trabalhados dentro de um evento em que a pessoa está escalada. O evento fica com o trecho inteiro, inclusive a parte que cai fora da escala. O tratamento vem do evento: hora normal, hora extra com adicional (um percentual) ou banco de horas (um fator). No banco de horas, o crédito daquele trecho é `minutos trabalhados × fator`, arredondado ao minuto mais próximo, com metade para cima. Exemplo: 1 minuto trabalhado com fator 1,5 vira 2 minutos creditados; 4 horas com fator 1 permanecem 4 horas.
2. **Regular.** O que sobrou dentro da escala publicada daquela pessoa naquele dia.
3. **Extra comum.** O que sobrou fora da escala e fora de evento. Leva o percentual único da instituição. Percentual zero é adicional zero.

Sem escala publicada, tudo o que não for evento sai como extra comum.

Dois eventos com interseção de duração maior que zero, para a mesma pessoa, são recusados. O mesmo vale para dois intervalos de escala no mesmo dia. Um intervalo que termina às 12h e outro que começa às 12h são válidos. Minutos contíguos da mesma fatia e dos mesmos parâmetros formam um único trecho.

Um par de batidas que cruza a meia-noite é dividido no dia civil. Cada lado usa a escala e os eventos daquele dia. O evento também é um intervalo de tempo: cada dia civil fica com o pedaço que cai nele.

A planilha soma horas e repete percentual e fator. Com o dia aberto, a prévia usa escala, evento e percentual vigentes. Na aprovação, cada trecho guarda a cópia desses números. A planilha lê só essa cópia.

Exemplo combinado: escala 8h–17h, evento 14h–18h em banco de horas com fator 1, batidas 8h–19h, adicional institucional de 50%.

| Trecho | Fatia | Parâmetro |
| --- | --- | --- |
| 8h–14h | Regular | — |
| 14h–18h | Evento | banco, fator 1, crédito 4h |
| 18h–19h | Extra comum | adicional de 50% |

## O que cada módulo guarda

Planejamento publica o plano e a política. Ponto guarda o fato e a cópia aprovada. Os dois módulos compartilham um banco relacional. Cada módulo tem as próprias tabelas. Não há chave estrangeira de um módulo para o outro. Identificadores de funcionário e de evento, quando copiados, são valores.

### Planejamento

- **Escala do dia:** funcionário, dia civil e um ou mais intervalos esperados. Publicar de novo substitui a escala daquele dia. Remover a escala restaura o dia sem escala.
- **Evento:** início, fim, funcionários escalados e um tratamento. Hora normal não carrega número. Adicional carrega percentual maior ou igual a zero. Banco de horas carrega fator maior que zero.
- **Percentual da extra comum:** um valor vigente da instituição, maior ou igual a zero. Alterá-lo muda as prévias. O dia já aprovado conserva o número copiado.

### Ponto

- **Batida:** funcionário, entrada e saída. Só o intervalo fechado entra na apuração. Entrada e saída ficam no passado ou no instante atual.
- **Dia:** funcionário mais dia civil, aberto ou aprovado.
- Dia **aberto:** nenhum trecho gravado. A prévia é calculada na hora.
- Dia **aprovado:** cada trecho guarda início, fim, fatia e a cópia dos números. Extra comum guarda o percentual. Evento guarda o tratamento, o percentual ou o fator, os minutos trabalhados e, no banco, os minutos creditados, além do identificador e do nome do evento.
- **Reabrir** volta o dia a aberto e remove a cópia vigente. A planilha deixa de ver esse dia até a próxima aprovação.

### Funcionário

Nome informado pela gestão e identificador gerado pelo host. Planejamento e Ponto guardam o identificador. O host confere se a pessoa existe e só então chama o caso de uso.

### Planilha

Consulta dos trechos aprovados num período. Soma por funcionário, por fatia e pelo parâmetro copiado. Duas extras comuns com percentuais diferentes permanecem separadas. Horas de evento permanecem separadas por evento e tratamento.

O Excel tem duas abas, `Detalhe` e `Totais`. O CSV sai num zip com `detalhe.csv` e `totais.csv`, em UTF-8, com separador `;` e vírgula decimal.

Colunas de `Detalhe`: funcionário, dia, início, fim, fatia, evento, tratamento, percentual, fator, minutos trabalhados, minutos creditados.

Colunas de `Totais`: funcionário, fatia, evento, tratamento, percentual, fator, minutos trabalhados, minutos creditados.

A coluna de minutos creditados vem preenchida no banco de horas. Nas outras fatias ela vai vazia. Evento, tratamento, percentual e fator vão vazios quando não se aplicam. Período sem dia aprovado gera arquivo só com o cabeçalho.

## Projetos, porta e telas

### API

O host compõe os módulos e publica o HTTP. Nenhum módulo referencia o outro.

- `Koinonia.Api` — host, cadastro de funcionário, rotas, papel do usuário e o adaptador da porta
- `Koinonia.Shared` — identificador do funcionário
- `Koinonia.Planejamento.Domain`
- `Koinonia.Planejamento.Application`
- `Koinonia.Planejamento.Infrastructure`
- `Koinonia.Ponto.Domain`
- `Koinonia.Ponto.Application`
- `Koinonia.Ponto.Infrastructure`

`Domain` referencia só `Shared`. `Application` referencia `Domain`. `Infrastructure` referencia `Domain` e `Application`. O host referencia as aplicações, as infraestruturas e `Shared`. `Domain` fica sem referência a banco ou HTTP.

A apuração é a função pura `ClassificarHoras` em `Ponto.Domain`. Ela recebe batidas fechadas já cortadas no dia, intervalos da escala, eventos do dia e o percentual institucional, e devolve os trechos.

A porta `IConsultaDePlanejamento` mora em `Ponto.Application`. Para um funcionário e um dia civil, devolve a escala vigente, os eventos em que a pessoa está escalada (pedaço daquele dia, nome e tratamento) e o percentual institucional. O host liga essa porta à consulta de Planejamento.

A prévia chama a porta e `ClassificarHoras`, e não grava trecho. A aprovação grava a cópia nas tabelas de Ponto. A planilha lê somente essa cópia.

### Angular

Três bibliotecas e um shell, em `koinonia-web`:

- `ui` — tabela, formulário, período e estado vazio, sem regra de jornada
- `planejamento` — publicar escala, evento e percentual
- `ponto` — bater ponto, espelho, aprovar, reabrir e exportar
- shell — rotas e permissão

Gestão publica, aprova, reabre, exporta e consulta o espelho dos funcionários. Empregado registra o próprio ponto e vê o próprio espelho. O componente de espelho é um só; a ação muda com o papel. A prévia chega pronta da API.

O host identifica o usuário e o papel. O empregado autenticado corresponde a um `FuncionarioId`.

## Fluxos

**Publicar escala.** A gestão grava os intervalos de uma pessoa num dia civil. Publicar de novo substitui. Remover restaura o dia sem escala, e a prévia passa a tratar o tempo fora de evento como extra comum.

**Publicar evento.** A gestão grava início, fim, pessoas e um tratamento. Publicar e alterar repetem as mesmas recusas: interseção para a mesma pessoa, e tratamento sem o número exigido.

**Ajustar percentual.** A gestão grava o percentual institucional. As prévias seguintes usam o valor novo.

**Cadastrar funcionário.** A gestão informa o nome. O host gera o identificador.

**Bater ponto.** O empregado registra a própria entrada e a própria saída. Com o dia aberto, pode remover um intervalo fechado ou descartar uma entrada em aberto. A prévia usa só intervalos já fechados.

**Espelho.** Dia aberto: a API calcula a prévia na hora. Dia aprovado: a API devolve a cópia gravada.

**Aprovar.** A gestão aprova o dia de uma pessoa. A API lê o plano vigente, calcula e grava os trechos. Sem intervalo fechado, ou com entrada ainda sem saída, a aprovação é recusada e o dia segue aberto. Com a cópia gravada, o dia não aceita batida nova.

**Reabrir.** A gestão reabre aquele dia. A cópia sai, o dia volta a aberto e as batidas voltam a ser aceitas.

**Exportar.** A gestão escolhe o período e o formato, CSV ou Excel. Dia aberto fica de fora.

## Falhas

- Evento sem percentual, quando o tratamento é adicional, é recusado.
- Evento sem fator, quando o tratamento é banco de horas, é recusado.
- Percentual menor que zero é recusado. Fator menor ou igual a zero é recusado.
- Dois eventos com interseção de duração maior que zero, para a mesma pessoa, são recusados. O mesmo vale para intervalos de escala no mesmo dia. Um intervalo que termina quando o outro começa é válido.
- Saída anterior à entrada é recusada.
- Intervalo que cruza outro intervalo já fechado da mesma pessoa, com duração de interseção maior que zero, é recusado. Extremos que apenas se encostam são válidos.
- Entrada ou saída no futuro é recusada.
- Batida em dia aprovado é recusada até a gestão reabrir.
- Aprovar um dia já aprovado é recusado.
- Reabrir um dia aberto é recusado.
- Aprovar sem intervalo fechado é recusado.
- Aprovar com entrada sem saída é recusado.
- Empregado que consulta ou registra ponto de outra pessoa é recusado.
- Funcionário desconhecido é recusado no host.
- Duas aprovações simultâneas do mesmo dia: a primeira grava a cópia; a segunda encontra o dia aprovado e é recusada.

Alterar escala, evento ou percentual com um dia já aprovado continua permitido. A cópia desse dia permanece até uma reabertura.

## Testes

Cada comportamento nasce de um teste que falha antes da implementação.

`ClassificarHoras` trava o exemplo combinado: regular 8h–14h, evento 14h–18h com 4h creditadas, extra comum 18h–19h a 50%. A mesma função cobre dia sem escala, vão entre batidas, corte na meia-noite, minutos contíguos fundidos num trecho, e fator 1,5 com arredondamento de metade para cima.

`Planejamento.Domain` recusa sobreposição de evento e de escala, e recusa tratamento sem o número exigido.

Um teste de aplicação, com a porta falsa, mostra que a prévia não grava trecho, que a aprovação copia os números, que mudar o evento depois não altera essa cópia, que entrada sem saída impede a aprovação, e que a planilha lê só a cópia vigente.

Um teste de arquitetura impede `Ponto` de referenciar `Planejamento`, impede o caminho inverso, e impede `Domain` de referenciar `Infrastructure`.

No Angular, o espelho cobre três estados: prévia, cópia aprovada e ações de gestão.
