# Oficina Digital

Núcleo de gestão de assistência técnica em **.NET 8 / C#**, sem dependências externas e com testes executáveis no CI. Preserva as classes originais `OrdemServico` e `OficinaService`.

## Funcionalidades implementadas

- **Clientes:** cadastro, edição, exclusão condicionada, validação de email, pesquisa e unicidade.
- **Equipamentos:** vínculo ao cliente, número de série único, filtros e alertas de garantia.
- **Ordens de serviço:** abertura, transições válidas, cancelamento, entrega e histórico de eventos.
- **Operação:** técnico responsável, prioridade, notas de acompanhamento, prazos e atrasos.
- **Orçamentos:** peças, mão de obra, quantidades, descontos, impostos, envio, aprovação, recusa, expiração e histórico.
- **Indicadores:** totais por estado, atrasos e valor de orçamentos aprovados.
- **Exportação CSV:** relatório de ordens com escape de campos e mitigação de injeção de fórmulas.

## Executar

É necessário instalar o SDK .NET 8. Na raiz do repositório:

```bash
dotnet build src/OficinaDigital/OficinaDigital.csproj --configuration Release
dotnet run --project tests/OficinaDigital.Tests/OficinaDigital.Tests.csproj --configuration Release
```

O teste executável usa apenas a biblioteca padrão do .NET; o processo termina com erro caso alguma verificação falhe. O GitHub Actions executa esses comandos em push e pull request.

## Exemplo de uso

```csharp
using OficinaDigital;

var gestao = new GestaoOficina();
var cliente = gestao.Cadastro.CadastrarCliente("Cliente Exemplo", "exemplo@dominio.com");
var equipamento = gestao.Cadastro.CadastrarEquipamento(
    cliente.Id, "Notebook", "Marca Exemplo", "Modelo X", "SN-0001");
var ordem = gestao.Abrir(cliente.Id, equipamento.Id, "Substituir bateria");

var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
var orcamento = gestao.CriarOrcamento(ordem.Id, hoje.AddDays(7), hoje);
gestao.Orcamentos.AdicionarItem(orcamento.Id, TipoItem.Peca, "Bateria", 1, 180m);
gestao.Orcamentos.AdicionarItem(orcamento.Id, TipoItem.MaoDeObra, "Instalação", 1, 70m);
gestao.Orcamentos.Enviar(orcamento.Id, hoje);
gestao.Orcamentos.Decidir(orcamento.Id, true, hoje);
gestao.Avancar(ordem.Id, StatusOrdem.EmExecucao);
gestao.Avancar(ordem.Id, StatusOrdem.Pronta);
gestao.Avancar(ordem.Id, StatusOrdem.Entregue);
Console.WriteLine(gestao.ExportarCsv());
```

## Regras de negócio

Apenas rascunhos podem ter itens ou tributos alterados. Uma ordem com orçamento ativo exige uma proposta aprovada antes do início do serviço. Ordens entregues e canceladas não podem ser reabertas; o histórico é preservado. A validade do orçamento é inclusiva. O imposto incide sobre o subtotal já descontado; valores monetários são arredondados para duas casas.

## Limitações conhecidas

Esta implementação é **em memória**: ao encerrar o processo, os dados são perdidos. Não possui API HTTP, autenticação, banco de dados, interface gráfica ou emissão de documentos fiscais. O CSV é uma exportação de relatório, **não** um backup restaurável. O validador de email cobre formato básico, não verifica a existência do domínio. O serviço não é um substituto para um ERP de produção.

## Privacidade e segurança

Use dados fictícios durante desenvolvimento e testes. Não envie informações pessoais reais ou credenciais ao repositório. Os campos de CSV são escapados e entradas que poderiam ser interpretadas como fórmulas são prefixadas. Não armazene senhas no domínio.

## Organização

- `src/OficinaDigital/Domain.cs` — domínio e serviço originais, preservados.
- `src/OficinaDigital/Cadastro.cs` — clientes, equipamentos, validações e garantias.
- `src/OficinaDigital/Orcamentos.cs` — itens, cálculo, aprovação e auditoria.
- `src/OficinaDigital/Gestao.cs` — fluxo integrado, prazos, filtros e relatórios.
- `tests/OficinaDigital.Tests/Program.cs` — testes de regressão e casos de borda.

## Licença

Ainda não definida.
