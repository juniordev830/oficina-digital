# Roteiro de manutenção — Oficina Digital

## O que o código faz

Núcleo C#/.NET 8 em memória para ordens de serviço e estados; ainda não oferece orçamento nem interface web.

## Arquivos de referência

`src/OficinaDigital/Domain.cs`, `src/OficinaDigital/OficinaDigital.csproj` e `tests/OficinaDigital.Tests/Program.cs`.

## Executar verificações

`dotnet build src/OficinaDigital/OficinaDigital.csproj`; `dotnet run --project tests/OficinaDigital.Tests/OficinaDigital.Tests.csproj`.

Use ambiente local e dados fictícios. O CI complementa, mas não substitui a inspeção do comportamento e das entradas de fronteira.

## Testes que não podem regredir

Testar passagem de status inválida, descrição vazia, ordem inexistente e preservação de registros diante de erro.

## Próxima melhoria útil

Concluir cadastro de clientes/equipamentos e orçamento com testes de domínio antes de criar UI e banco de dados.

### Antes de abrir PR

- [ ] Teste ou exemplo reproduzível para a alteração.
- [ ] README atualizado se o comando ou escopo mudou.
- [ ] Dados privados e segredos fora do controle de versão.
- [ ] Revisão em branch, sem merge automático.
