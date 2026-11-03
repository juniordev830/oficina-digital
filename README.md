# Oficina Digital

Ordens de serviço, orçamentos e acompanhamento de consertos.

## Estado atual

MVP executável em branch de desenvolvimento, com testes automatizados. Núcleo em memória de ordens de serviço com transições de status e teste executável. Ainda não há orçamento, interface ou banco de dados.

## Tecnologias implementadas

.NET 8 e C#.

## Escopo da primeira versão

- [ ] Cadastrar clientes e equipamentos
- [ ] Abrir ordem de serviço e orçamento
- [ ] Acompanhar etapas do conserto

## Critérios para concluir a primeira versão

- [ ] Implementar o fluxo principal com validação de entradas e tratamento de erros.
- [ ] Documentar instalação, configuração e execução com comandos reproduzíveis.
- [ ] Adicionar exemplos de uso e dados fictícios.
- [ ] Verificar os casos principais e limites e registrar os resultados.

## Desenvolvimento

Execute na raiz do repositório:

```bash
dotnet build src/OficinaDigital/OficinaDigital.csproj
dotnet run --project tests/OficinaDigital.Tests/OficinaDigital.Tests.csproj
```

O GitHub Actions executa a validação em pushes e pull requests. Este é um núcleo demonstrativo; as funcionalidades ainda marcadas como pendentes na lista acima continuam planejadas.

## Privacidade e configuração

Usar dados fictícios nos exemplos e manter senhas, tokens, arquivos pessoais e credenciais fora do controle de versão.

## Licença

Ainda não definida.
