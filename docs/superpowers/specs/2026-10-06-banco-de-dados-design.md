# Banco de dados — Design

Data: 2026-10-06
Status: aprovado nas seções, aguardando revisão do arquivo

## Objetivo

Um esquema só, com chave estrangeira, em que a role só bloqueia tela e API. Jornada, hora extra, evento e pagamento leem a pessoa, o expediente e o evento. O fechamento do mês grava a conta e os movimentos do banco de horas. A planilha da contabilidade lê essa gravação.

Este desenho substitui a persistência, a identidade, a escala por pessoa, a aprovação diária e o pagamento descritos em `2026-10-06-controle-ponto-escalas-design.md`. Onde os dois divergirem, vale este.

## Ordem

A implementação segue esta ordem:

1. Conta, role e sessão.
2. Funcionário, expediente, evento e batida, com chave estrangeira.
3. Fechamento, banco de horas e planilha.

## Fuso e tempo trabalhado

O dia civil e o mês usam o fuso `America/Sao_Paulo`.

Só entra na conta o tempo entre uma entrada e uma saída. O vão entre duas batidas fica de fora. Uma entrada ainda sem saída fica de fora da prévia e impede o fechamento daquele mês.

Um par de batidas que cruza a meia-noite é dividido no dia civil. Cada lado usa o expediente e os eventos daquele dia. O evento também é cortado no dia civil.

## Tabelas

### Conta

`Usuario` guarda nome, e-mail e o hash da senha. O e-mail é único. Cada usuário tem uma role.

`Sessao` guarda o usuário e o hash de um token opaco. A entrada confere e-mail e senha e abre uma sessão. As chamadas seguintes apresentam o token. A API resolve o usuário e as permissões por essa sessão. Sair apaga a sessão. Os cabeçalhos `X-Papel` e `X-Funcionario-Id` saem.

`Role` é tabela própria. O nome é único. Uma role serve uma pessoa ou um grupo.

`PermissaoDaRole` liga a role a um código do catálogo. A chave é role e código. Código fora do catálogo é recusado.

A role não entra no cálculo.

### Quadro

`Funcionario` é a pessoa que tem jornada. Aponta para um usuário, e esse usuário aparece no máximo uma vez. Guarda as horas contratadas do mês e o salário em reais. O nome da planilha é o nome do usuário.

Usuário sem essa linha entra na plataforma e não recebe batida nem pagamento.

O valor da hora é o salário dividido pelas horas contratadas. As horas contratadas entram com precisão de minuto e são guardadas em minutos. Na conta, o valor da hora é `salário / (minutos contratados / 60)`. O mês fechado copia salário, minutos contratados e valor da hora.

Criar o usuário e atribuir a role é a ação de gerir contas. Cadastrar funcionário liga um usuário já existente à linha de funcionário, com horas e salário.

### Calendário

`Expediente` é o horário comum, com início e fim de vigência, inclusive. Quem define é o gestor. Duas vigências que compartilham um dia são recusadas. A vigência seguinte pode começar no dia seguinte ao fim da anterior.

`IntervaloExpediente` guarda os trechos daquele expediente, em minutos a partir da meia-noite, dentro do mesmo dia civil. O início é menor que o fim. Dois trechos do mesmo expediente que compartilham um minuto são recusados. Trechos que se encostam, um terminando no minuto em que o outro começa, são válidos. Minuto dentro de algum trecho está dentro do expediente. O almoço, ficando entre os trechos, está fora.

`Configuracao` é uma linha só. Guarda o percentual da hora extra e o prazo padrão do banco de horas, em dias. A migração cria a linha vazia. Fechar é recusado enquanto o gestor não gravar os dois valores.

`Evento` guarda nome, início, fim e uma regra: hora normal, percentual a mais, ou banco de horas. No banco de horas, o evento pode guardar o próprio prazo, em dias. Sem esse prazo, vale o da instituição. Um minuto trabalhado vira um minuto de crédito. Não há fator.

`ParticipanteEvento` é o grupo daquele evento: pares de evento e funcionário. Não existe grupo reutilizável.

### Ponto

`Batida` guarda entrada e saída de um funcionário. A saída pode estar vazia enquanto a batida estiver aberta.

### Mês gravado

`Fechamento` é um por funcionário por mês. Guarda a escolha da falta e os números copiados daquele mês.

`LinhaEventoFechamento` guarda a cópia de cada evento que entrou na conta: nome, regra, percentual, minutos, valor e minutos creditados.

`MovimentoBanco` é crédito, cobertura, pagamento ou vencimento. O crédito guarda o valor da hora, o vencimento, o evento e o dia. Cobertura, pagamento e vencimento apontam para o crédito que consomem e para o fechamento que os criou.

## Catálogo de permissões

O sistema olha o código da permissão, nunca o nome da role.

Telas: `visao-geral`, `ponto`, `planejamento`, `fechamento`, `contas`.

Ações: `registrar-propria-batida`, `ver-ponto-de-outro`, `cadastrar-funcionario`, `definir-expediente`, `publicar-evento`, `configurar-pagamento`, `fechar-mes`, `reabrir-mes`, `baixar-planilha`, `gerir-contas`.

A tela abre o lugar. A ação libera o que se faz lá dentro. Dá para abrir planejamento e ficar sem publicar evento.

Registrar batida exige a linha de funcionário. Sem a ação `ver-ponto-de-outro`, o espelho abre só o funcionário ligado a quem entrou.

Duas roles já nascem cadastradas, como dado. **Funcionário** tem `visao-geral`, `ponto` e `registrar-propria-batida`. **Gestão** tem as cinco telas e todas as ações. As duas podem ser editadas depois.

A aprovação diária e a escala por pessoa saem do catálogo.

## Classificação do minuto

Cada minuto trabalhado cai numa classe só.

1. **Evento.** A pessoa está no grupo e o minuto está dentro do evento. Vale a regra do evento, também quando o minuto está fora do expediente.
2. **Hora normal.** O minuto está dentro do expediente e fora de evento.
3. **Hora extra.** O minuto está fora do expediente e fora de evento.

Dois eventos da mesma pessoa que compartilham um minuto são recusados. Eventos que apenas se encostam são válidos.

Entram na carga do salário a hora normal e o evento de hora normal ou de percentual. Banco de horas e hora extra ficam de fora da carga.

## Conta do mês

O fechamento é de um funcionário em um mês civil. O valor da hora daquele fechamento fica copiado.

Cumpriu a carga, o salário entra inteiro. Hora normal além da carga não paga a mais. Hora a menos é o que faltou para a carga. Sem hora a menos, a escolha da falta é `Nenhuma`.

Por cima do salário:

- **Evento com percentual.** A hora entra na carga. O valor a mais é `minutos × valor da hora × (percentual / 100)`. Cinquenta por cento paga meia hora a mais.
- **Evento de hora normal.** A hora entra na carga. O valor a mais é zero.
- **Hora extra.** O valor é `minutos × valor da hora × (1 + percentual / 100)`. Cinquenta por cento paga uma hora e meia. O percentual copiado é o da configuração no momento do fechamento. Na planilha, cinquenta por cento aparece como 50.
- **Banco de horas.** A hora não entra na carga e não paga agora.

Cada componente em dinheiro é arredondado para centavos, com metade para cima. O total é a soma dos componentes já arredondados: salário, menos o desconto, mais a hora extra, mais os eventos, mais o valor pago do banco.

Se houver falta, o gestor escolhe `Banco` ou `Desconto`. Na escolha `Desconto`, tudo o que falta desconta o valor da hora. Na escolha `Banco`, a cobertura consome o saldo até a falta. O que o saldo não alcançar desconta. A escolha e os minutos cobertos e descontados ficam gravados.

Salário, horas contratadas, percentual e prazo podem mudar. O mês ainda aberto usa o valor novo. O mês fechado conserva a cópia.

Dia sem expediente trata o minuto fora de evento como hora extra.

Batida que cai num mês fechado não muda até a reabertura. Expediente que cobre um dia de mês fechado não muda até a reabertura. Evento que cruza um mês fechado não muda e não é apagado até a reabertura. Expediente novo, em dias que nenhum mês fechado cubra, pode ser criado. Evento que não cruza mês fechado pode mudar.

## Banco de horas

O saldo de um crédito só existe depois que o mês que o criou está fechado. Ao fechar um mês, entram os créditos novos dele e os saldos de meses já fechados.

Cada crédito nasce no fechamento: uma linha por funcionário, por evento e por dia trabalhado. O crédito guarda o valor da hora daquele fechamento. O vencimento é o dia do crédito mais o prazo do evento, se houver, senão o prazo da instituição.

No fechamento, a ordem é esta:

1. Nascem os créditos novos.
2. Se houve falta e a escolha foi `Banco`, a cobertura consome do crédito mais antigo para o mais novo. Essa cobertura é a folga. Não há lançamento de folga no calendário.
3. Crédito com vencimento até o último dia daquele mês, ainda com saldo, é pago pelo valor guardado no crédito.
4. Se o gestor manda pagar o saldo em aberto, o restante que ainda não venceu é pago, também do mais antigo para o mais novo. A escolha paga o saldo inteiro que sobrou.

Reabrir apaga o fechamento, as linhas de evento e os movimentos criados por aquele fechamento. O mês seguinte daquele funcionário, se já estiver fechado, impede a reabertura.

Fechar março com fevereiro ainda aberto é permitido. Março só enxerga créditos de meses já fechados, mais os créditos novos de março.

## Planilha

O arquivo é Excel. Só mês fechado entra. Mês aberto não aparece. Um período sem fechamento gera o arquivo só com o cabeçalho. Horas saem em decimal, com duas casas, a partir dos minutos divididos por sessenta. Valores saem em reais.

**Pagamento.** Uma linha por funcionário no mês: Mês, Funcionário, Horas contratadas, Salário, Valor da hora, Horas normais, Horas a menos, Escolha da falta, Horas cobertas pelo banco, Horas descontadas, Desconto, Horas extra, Percentual da extra, Valor da extra, Valor dos eventos, Valor pago do banco, Total.

Horas normais são só a classe hora normal. Os minutos de evento ficam na aba Eventos. A carga é a soma das horas normais com as horas de evento de hora normal e de percentual.

**Eventos.** Uma linha por evento do fechamento: Mês, Funcionário, Evento, Regra, Percentual, Horas, Valor, Horas creditadas no banco. Evento de hora normal leva valor zero e horas creditadas vazias. Evento de percentual leva o valor a mais e horas creditadas vazias. Evento de banco leva valor vazio e as horas creditadas.

**Banco.** Uma linha por movimento nascido naquele fechamento: Mês, Funcionário, Tipo, Evento, Horas, Vencimento, Valor. Os tipos são crédito, cobertura, pagamento e vencimento. Valor vem preenchido em pagamento e vencimento. Nos outros tipos, fica vazio. O valor pago do banco na aba Pagamento é a soma de pagamento e vencimento.

## Recusas

E-mail desconhecido e senha errada respondem 401, os dois com o texto `E-mail ou senha inválidos.` Sem sessão, a API responde 401 e a tela interna volta para a entrada. Sem a permissão, a API responde 403. Regra de negócio responde 422.

São recusados:

- e-mail repetido
- senha vazia
- horas contratadas ou salário que não sejam maiores que zero
- percentual de hora extra ou de evento abaixo de zero
- prazo do banco abaixo de um dia
- evento com fim antes do início
- dois eventos da mesma pessoa no mesmo minuto
- duas vigências de expediente que compartilhem um dia
- dois trechos do mesmo expediente que compartilhem um minuto
- saída antes da entrada
- segunda entrada com outra batida ainda aberta
- participante que não seja funcionário
- batida de usuário sem linha de funcionário
- apagar role que ainda tenha usuário
- tirar `gerir-contas` da última conta que ainda a possui, seja mudando a role, a permissão ou o usuário
- fechar mês com batida aberta
- fechar mês antes de a configuração ter percentual e prazo gravados
- fechar mês já fechado
- reabrir quando o mês seguinte daquele funcionário já está fechado
- alterar batida que cai num mês fechado, expediente que cobre dia de mês fechado, ou evento que cruza mês fechado, enquanto esse mês estiver fechado
- apagar evento que cruza mês fechado ou que seja citado por movimento de banco ainda gravado

## O que o esquema substitui

Continua valendo: o domínio não referencia banco, e a aplicação não referencia o Entity Framework. Um contexto só sobe a migração.

Saem os contextos separados, a escala por funcionário e por dia, o percentual extra em tabela solta, o fator do banco de horas, a aprovação do dia e a cópia diária de trechos. A batida permanece. O que trava o passado é o fechamento do mês.

## Testes

Cada regra nasce primeiro como teste que falha.

**Acesso.** E-mail errado e senha errada respondem 401 com o mesmo texto. Sem sessão, a rota interna volta para a entrada. Sem a permissão, a API responde 403. Duas roles com as mesmas permissões passam nas mesmas ações. O nome da role não entra na decisão.

**Jornada.** Usuário sem funcionário não registra batida. Participante que não é funcionário é recusado. Dois eventos da mesma pessoa no mesmo minuto são recusados. Batida aberta impede outra entrada e impede o fechamento.

**Conta do mês.** Minuto de evento usa a regra do evento, inclusive fora do expediente. Minuto no expediente, sem evento, é hora normal. Minuto fora, sem evento, é hora extra. Dia sem expediente manda o minuto fora de evento para hora extra. Hora normal e evento de hora normal ou percentual entram na carga. Banco e hora extra ficam de fora. Carga cumprida paga o salário inteiro. Hora normal além da carga não paga a mais. Hora a menos desconta o valor da hora. Evento com cinquenta por cento paga meia hora a mais. Hora extra com cinquenta por cento paga uma hora e meia. Fechar sem percentual e prazo gravados é recusado.

**Banco.** O crédito nasce no fechamento, um minuto por minuto, com o valor da hora e o vencimento daquele momento. A falta coberta consome o crédito mais antigo. Saldo curto desconta o resto. Crédito vencido é pago pelo valor guardado. Pagar o saldo em aberto também sai do mais antigo e paga o restante inteiro. Reabrir apaga o fechamento e os movimentos daquele mês. O mês seguinte fechado impede a reabertura. Mudar salário não altera o mês fechado. Evento que cruza mês fechado e expediente que cobre dia fechado são recusados.

**Planilha.** Só mês fechado entra. As três abas saem com as colunas deste desenho. Período sem fechamento traz só o cabeçalho.
