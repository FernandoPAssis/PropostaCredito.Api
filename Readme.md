# Cartão de Crédito

Microsserviço que consome o evento **PropostaGerada** e emite os cartões de crédito
conforme a faixa de score aprovada na proposta.

Faz parte de uma solução com três microsserviços:

```
[Cadastro de Clientes] → ClienteCadastrado → [Proposta de Crédito] → PropostaGerada → [Cartão de Crédito]
```

## Tecnologias

- .NET 10 (ASP.NET Core Web API com controllers)
- Entity Framework Core + SQLite
- RabbitMQ (biblioteca oficial RabbitMQ.Client)
- xUnit (testes de unidade)

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/products/docker-desktop/) (para o RabbitMQ)

## Como executar

**1. Suba o RabbitMQ:**

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:4-management
```

Painel de administração: http://localhost:15672 (usuário `guest`, senha `guest`).

**2. Execute a API:**

- **Visual Studio:** abra a solução e pressione F5
- **Terminal** (na pasta raiz do repositório):

```bash
dotnet run --project Cartao.Api
```

A API sobe em `http://localhost:XXXX`. Ao subir, o serviço cria automaticamente o banco
SQLite (`cartao.db`) e as filas no RabbitMQ.

> **Importante:** suba o Cartão (e a Proposta) **antes** de cadastrar clientes. A fila é criada
> pelo próprio serviço na primeira execução; depois disso, as mensagens ficam guardadas mesmo
> que o serviço esteja desligado.

**3. Execute os testes:**

- **Visual Studio:** menu *Teste > Gerenciador de Testes > Executar Todos*
- **Terminal** (na pasta raiz do repositório):

```bash
dotnet test
```

## Configuração

| Chave (appsettings.json) | Variável de ambiente | Padrão |
|---|---|---|
| `ConnectionStrings:CartaoDb` | `ConnectionStrings__CartaoDb` | `Data Source=cartao.db` |
| `RabbitMQ:HostName` | `RabbitMQ__HostName` | `localhost` |

## Endpoint

### GET /api/v1/cartoes/{clienteId}

Retorna a lista de cartões do cliente (200 OK). Se o cliente não tiver cartões
(proposta negada ou ainda não processada), retorna uma lista vazia.

```json
[
  {
    "id": "9d2e1a4b-0c7f-4e2a-8b3d-5f6a7b8c9d0e",
    "clienteId": "ff064ca3-2428-482d-b7bf-a3e6b06aacc1",
    "propostaId": "5bde5d7c-4357-4fd8-97cb-99f25949a58f",
    "nomeTitular": "Sonia Silva",
    "numero": "4829103756281940",
    "limite": 5000.0,
    "dataEmissao": "2026-10-27T01:32:12.1234567"
  }
]
```

## Regras de emissão

A quantidade de cartões e o limite vêm da proposta (calculados pelo serviço de Proposta):

| Score | Status | Limite por cartão | Qtd. cartões |
|---|---|---|---|
| 0 – 100 | Negado | — | 0 |
| 101 – 500 | Aprovado | R$ 1.000,00 | 1 |
| 501 – 1000 | Aprovado | R$ 5.000,00 | 2 |

O número do cartão é fictício (16 dígitos aleatórios), apenas para simulação.

## Mensageria

| | Nome |
|---|---|
| Consome do exchange | `proposta-gerada` (fanout) |
| Fila | `cartao.proposta-gerada` |
| Dead-letter exchange | `cartao.dead-letter` |
| Dead-letter queue | `cartao.proposta-gerada.dlq` |

## Decisões técnicas

- **Consumidor em segundo plano (`BackgroundService`):** fica escutando a fila enquanto a API
  está no ar.
- **Ack manual:** a mensagem só é removida da fila depois de processada com sucesso.
- **Retry:** se o processamento falhar, o serviço tenta de novo até 3 vezes, esperando um pouco
  mais a cada tentativa (2s, 4s).
- **Dead-letter queue:** se falhar nas 3 tentativas, a mensagem é rejeitada (nack) e o RabbitMQ
  a envia para a fila `.dlq`, onde fica guardada para análise, sem travar a fila principal.
- **Idempotência:** se a mesma proposta chegar duas vezes, os cartões não são emitidos de novo.
- **Regra de crédito centralizada na Proposta:** o Cartão apenas emite a quantidade e o limite
  aprovados, evitando duplicar a tabela de score em dois serviços.
- **Regra de emissão isolada no domínio** (`EmissorCartao`), em uma classe simples testada com xUnit.
- **SQLite:** permite rodar o projeto sem instalar banco de dados.