# Engenharia reversa e estudo — Oficina Digital

## Objetivo e fluxo

Ordens de serviço, cadastro e orçamentos de assistência técnica. Domain.cs mantém a ordem e seu estado. Cadastro.cs conecta cliente e equipamento, Gestao.cs acompanha registros e Orcamentos.cs encapsula a aprovação de propostas.

**Tecnologias identificadas** · C#, .NET 8.

**Revisão de referência** · `6441f76ca0fc`.

## Mapa do código

| Caminho | Papel de leitura | Símbolos selecionados |
| --- | --- | --- |
| [src/OficinaDigital/Cadastro.cs](../src/OficinaDigital/Cadastro.cs) | Implementação | `OficinaDigital`, `Validation`, `Positive`, `Cliente`, `Equipamento`, `CadastroOficina`, `ExcluirCliente`, `ExcluirEquipamento`, `Required` |
| [src/OficinaDigital/Domain.cs](../src/OficinaDigital/Domain.cs) | Implementação | `OficinaDigital`, `StatusOrdem`, `OrdemServico`, `OficinaService`, `Abrir`, `Avancar`, `AbrirOrdem`, `AtualizarStatus`, `Listar` |
| [src/OficinaDigital/Gestao.cs](../src/OficinaDigital/Gestao.cs) | Implementação | `OficinaDigital`, `PrioridadeOrdem`, `RegistroOrdem`, `NotaOrdem`, `MovimentoOrdem`, `ResumoOficina`, `GestaoOficina`, `Abrir`, `ObterOrdem` |
| [src/OficinaDigital/Orcamentos.cs](../src/OficinaDigital/Orcamentos.cs) | Implementação | `OficinaDigital`, `EstadoOrcamento`, `TipoItem`, `ItemOrcamento`, `TotaisOrcamento`, `EventoOrcamento`, `Orcamento`, `OrcamentosService`, `ExigirRascunho` |

## Configuração e referências

Ambiente e dependências: [src/OficinaDigital/OficinaDigital.csproj](../src/OficinaDigital/OficinaDigital.csproj), [tests/OficinaDigital.Tests/OficinaDigital.Tests.csproj](../tests/OficinaDigital.Tests/OficinaDigital.Tests.csproj).

Referências existentes: [docs/GUIA_DE_VALIDACAO.md](GUIA_DE_VALIDACAO.md).

## Roteiro de estudo e desenvolvimento

1. Comece pelo fluxo descrito acima e localize seus pontos de entrada no mapa. Acompanhe uma entrada válida até a transformação ou a resposta.
2. Abra um teste relacionado ao fluxo e descreva qual regra ele protege. Compare o tratamento esperado para entradas válidas, inválidas e nos limites.
3. Ao adicionar uma regra, determine qual módulo controla o dado e cubra o caso nesse nível. Em seguida, confira o efeito no comando, na API ou na interface de entrada.

**Pergunta de entrevista**

Em qual camada você impediria o avanço de uma ordem com orçamento pendente?

## Testes versionados

Arquivos para começar a leitura dos cenários de teste: [tests/OficinaDigital.Tests/Program.cs](../tests/OficinaDigital.Tests/Program.cs).

Os comandos e versões de runtime usados no CI ficam em [.github/workflows/ci.yml](../.github/workflows/ci.yml).

[Voltar ao README](../README.md).
