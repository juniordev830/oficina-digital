# Oficina Digital

Núcleo de ordens de serviço em **C# e .NET 8** para acompanhar consertos, consultar situações e organizar uma fila de atendimento.

## Funcionalidades

- `AbrirOrdem(cliente, equipamento, descricao)`: cria uma ordem com ID e status `Aberta`.
- `AtualizarStatus(id, status)`: respeita o fluxo de transições permitido: `Aberta` → `EmExecucao` → `Pronta` → `Entregue`, com cancelamento nas etapas iniciais.
- `Resumo()`: devolve contadores por status (`Abertas`, `EmExecucao`, `Prontas`, `Entregues`, `Canceladas`).
- `ListarPendentes()`: lista ordenada e imutável das ordens **prontas para retirada**, depois **em execução** e depois **abertas**. Ordens entregues ou canceladas não aparecem.
- `BuscarPorEquipamento(termo)`: pesquisa sem diferenciar maiúsculas/minúsculas, preservando a lista original e ordenando por ID.

## Exemplo de uso

```csharp
using OficinaDigital;

var oficina = new OficinaService();
var ordem = oficina.AbrirOrdem("Cliente fictício", "Notebook", "Trocar teclado");
oficina.AtualizarStatus(ordem.Id, StatusOrdem.EmExecucao);
Console.WriteLine(oficina.Resumo());
Console.WriteLine(oficina.ListarPendentes().Count);
Console.WriteLine(oficina.BuscarPorEquipamento("NOTEBOOK").Count);
```

## Compilar e validar

```bash
dotnet build src/OficinaDigital/OficinaDigital.csproj
dotnet run --project tests/OficinaDigital.Tests/OficinaDigital.Tests.csproj
```

O GitHub Actions executa compilação e testes em cada push e pull request. A suíte cobre o fluxo de status, painel, ordenação por urgência operacional, busca e preservação de snapshots.

## Limitações

MVP **em memória**, sem API, banco de dados, orçamento, autenticação ou notificações. As consultas são snapshots da instância atual e não oferecem coordenação entre usuários ou threads concorrentes. Exemplo com dados fictícios.

Licença: ainda não definida.
