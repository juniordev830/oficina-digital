using OficinaDigital;

var executados = 0;
var falhas = new List<string>();

void Testar(string nome, Action teste)
{
    try
    {
        teste();
        executados++;
        Console.WriteLine($"OK {nome}");
    }
    catch (Exception ex)
    {
        falhas.Add($"{nome}: {ex.GetType().Name}: {ex.Message}");
        Console.Error.WriteLine($"FALHOU {nome}: {ex}");
    }
}

void Igual<T>(T esperado, T atual)
{
    if (!EqualityComparer<T>.Default.Equals(esperado, atual))
        throw new Exception($"Esperado {esperado}; recebido {atual}.");
}

void Verdadeiro(bool valor)
{
    if (!valor) throw new Exception("Condição deveria ser verdadeira.");
}

void Contem(string texto, string trecho)
{
    if (!texto.Contains(trecho, StringComparison.Ordinal))
        throw new Exception($"Texto não contém '{trecho}'.");
}

void Lanca<T>(Action acao) where T : Exception
{
    try { acao(); }
    catch (T) { return; }
    throw new Exception($"Esperada exceção {typeof(T).Name}.");
}

var hoje = new DateOnly(2026, 11, 8);
var agora = new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

Testar("fluxo original preservado", () =>
{
    var s = new OficinaService();
    var o = s.AbrirOrdem("Ana", "Notebook", "Trocar tela");
    Igual(1, o.Id);
    Igual(StatusOrdem.Aberta, o.Status);
    Igual(StatusOrdem.EmExecucao, s.AtualizarStatus(o.Id, StatusOrdem.EmExecucao).Status);
    Igual(StatusOrdem.Pronta, s.AtualizarStatus(o.Id, StatusOrdem.Pronta).Status);
    Igual(StatusOrdem.Entregue, s.AtualizarStatus(o.Id, StatusOrdem.Entregue).Status);
    Lanca<InvalidOperationException>(() => s.AtualizarStatus(o.Id, StatusOrdem.Aberta));
});

Testar("ordem original rejeita campos vazios", () =>
{
    Lanca<ArgumentException>(() => OrdemServico.Abrir(1, "", "PC", "Problema"));
    Lanca<ArgumentException>(() => OrdemServico.Abrir(1, "Ana", "", "Problema"));
    Lanca<ArgumentException>(() => OrdemServico.Abrir(1, "Ana", "PC", ""));
    Lanca<ArgumentOutOfRangeException>(() => OrdemServico.Abrir(0, "Ana", "PC", "Erro"));
});

Testar("ordem original permite cancelamento", () =>
{
    var s = new OficinaService();
    var o = s.AbrirOrdem("Ana", "PC", "Erro");
    Igual(StatusOrdem.Cancelada, s.AtualizarStatus(o.Id, StatusOrdem.Cancelada).Status);
    Lanca<InvalidOperationException>(() => s.AtualizarStatus(o.Id, StatusOrdem.EmExecucao));
});

Testar("cliente normaliza campos", () =>
{
    var s = new CadastroOficina();
    var c = s.CadastrarCliente(" Ana ", " ANA@EXEMPLO.COM ", " 999 ", agora);
    Igual(1, c.Id);
    Igual("Ana", c.Nome);
    Igual("ana@exemplo.com", c.Email);
    Igual("999", c.Telefone);
    Igual(agora, c.CriadoEm);
});

Testar("cliente valida nome e email", () =>
{
    var s = new CadastroOficina();
    Lanca<ArgumentException>(() => s.CadastrarCliente("", "a@b.com"));
    Lanca<ArgumentException>(() => s.CadastrarCliente("Ana", "sem-arroba"));
    Lanca<ArgumentException>(() => s.CadastrarCliente("Ana", "@exemplo.com"));
    Lanca<ArgumentException>(() => s.CadastrarCliente("Ana", "a@dominio"));
    Lanca<ArgumentException>(() => s.CadastrarCliente(new string('x', 201), "a@b.com"));
});

Testar("email duplicado não consome identificador", () =>
{
    var s = new CadastroOficina();
    s.CadastrarCliente("Ana", "a@b.com");
    Lanca<InvalidOperationException>(() => s.CadastrarCliente("Bia", "A@B.COM"));
    Igual(2, s.CadastrarCliente("Bia", "b@b.com").Id);
});

Testar("cliente atualiza sem perder criação", () =>
{
    var s = new CadastroOficina();
    var c = s.CadastrarCliente("Ana", "a@b.com", agora: agora);
    var atualizado = s.AtualizarCliente(c.Id, "Beatriz", "b@b.com");
    Igual("Beatriz", atualizado.Nome);
    Igual("b@b.com", s.ObterCliente(c.Id).Email);
    Igual(agora, atualizado.CriadoEm);
});

Testar("atualização rejeita email de outro cliente", () =>
{
    var s = new CadastroOficina();
    var a = s.CadastrarCliente("Ana", "a@b.com");
    var b = s.CadastrarCliente("Bia", "b@b.com");
    Lanca<InvalidOperationException>(() => s.AtualizarCliente(b.Id, "Bia", a.Email));
    Igual("b@b.com", s.ObterCliente(b.Id).Email);
});

Testar("clientes podem ser pesquisados", () =>
{
    var s = new CadastroOficina();
    s.CadastrarCliente("Zelda", "z@b.com");
    s.CadastrarCliente("Ana", "a@b.com");
    Igual("Ana", s.ListarClientes()[0].Nome);
    Igual(1, s.ListarClientes("ZEL").Count);
    Igual(1, s.ListarClientes("@b.com").Count - 1);
});

Testar("cliente inexistente falha", () =>
{
    var s = new CadastroOficina();
    Lanca<KeyNotFoundException>(() => s.ObterCliente(8));
    Lanca<KeyNotFoundException>(() => s.ExcluirCliente(8));
});

Testar("equipamento exige cliente existente", () =>
{
    var s = new CadastroOficina();
    Lanca<KeyNotFoundException>(() => s.CadastrarEquipamento(5, "PC", "Dell", "A"));
});

Testar("equipamento registra garantia", () =>
{
    var s = new CadastroOficina();
    var c = s.CadastrarCliente("Ana", "a@b.com");
    var e = s.CadastrarEquipamento(c.Id, " Notebook ", " Dell ", " XPS ",
        " SER-1 ", hoje.AddDays(30), agora);
    Igual(1, e.Id);
    Igual("Dell", e.Marca);
    Igual("SER-1", e.NumeroSerie);
    Verdadeiro(s.EstaNaGarantia(e.Id, hoje));
    Verdadeiro(s.EstaNaGarantia(e.Id, hoje.AddDays(30)));
    Verdadeiro(!s.EstaNaGarantia(e.Id, hoje.AddDays(31)));
});

Testar("número de série duplicado é rejeitado", () =>
{
    var s = new CadastroOficina();
    var c = s.CadastrarCliente("Ana", "a@b.com");
    s.CadastrarEquipamento(c.Id, "PC", "Dell", "A", "SN-1");
    Lanca<InvalidOperationException>(() =>
        s.CadastrarEquipamento(c.Id, "PC", "Dell", "B", "sn-1"));
    Igual(2, s.CadastrarEquipamento(c.Id, "PC", "Dell", "B", "SN-2").Id);
});

Testar("equipamentos filtram por cliente e modelo", () =>
{
    var s = new CadastroOficina();
    var a = s.CadastrarCliente("Ana", "a@b.com");
    var b = s.CadastrarCliente("Bia", "b@b.com");
    s.CadastrarEquipamento(a.Id, "PC", "Dell", "Latitude");
    s.CadastrarEquipamento(b.Id, "Celular", "Samsung", "Galaxy");
    Igual(1, s.ListarEquipamentos(a.Id).Count);
    Igual(1, s.ListarEquipamentos(busca: "GALAXY").Count);
    Igual(0, s.ListarEquipamentos(a.Id, "Galaxy").Count);
});

Testar("cliente com equipamento não pode ser excluído", () =>
{
    var s = new CadastroOficina();
    var c = s.CadastrarCliente("Ana", "a@b.com");
    var e = s.CadastrarEquipamento(c.Id, "PC", "Dell", "A");
    Lanca<InvalidOperationException>(() => s.ExcluirCliente(c.Id));
    s.ExcluirEquipamento(e.Id);
    s.ExcluirCliente(c.Id);
    Igual(0, s.ListarClientes().Count);
});

Testar("garantias vencendo respeitam intervalo", () =>
{
    var s = new CadastroOficina();
    var c = s.CadastrarCliente("Ana", "a@b.com");
    s.CadastrarEquipamento(c.Id, "PC", "Dell", "A", garantiaAte: hoje.AddDays(5));
    s.CadastrarEquipamento(c.Id, "PC", "Dell", "B", garantiaAte: hoje.AddDays(40));
    s.CadastrarEquipamento(c.Id, "PC", "Dell", "C", garantiaAte: hoje.AddDays(-1));
    Igual(1, s.GarantiasVencendo(hoje, 7).Count);
    Lanca<ArgumentOutOfRangeException>(() => s.GarantiasVencendo(hoje, -1));
});

Testar("item calcula valores decimais", () =>
{
    var item = ItemOrcamento.Criar(1, TipoItem.Peca, "Bateria", 2.5m, 10.01m);
    Igual(25.03m, item.Total);
});

Testar("item valida preço, quantidade e tipo", () =>
{
    Lanca<ArgumentOutOfRangeException>(() => ItemOrcamento.Criar(1, TipoItem.Peca, "X", 0, 1));
    Lanca<ArgumentOutOfRangeException>(() => ItemOrcamento.Criar(1, TipoItem.Peca, "X", 1, -1));
    Lanca<ArgumentException>(() => ItemOrcamento.Criar(1, TipoItem.Peca, "X", 1.1234m, 1));
    Lanca<ArgumentOutOfRangeException>(() => ItemOrcamento.Criar(1, (TipoItem)99, "X", 1, 1));
});

Testar("orçamento começa como rascunho", () =>
{
    var s = new OrcamentosService();
    var o = s.Criar(1, hoje.AddDays(5), hoje, agora);
    Igual(1, o.Id);
    Igual(EstadoOrcamento.Rascunho, o.Estado);
    Igual(1, o.Historico.Count);
    Igual(0m, o.Calcular().Total);
});

Testar("orçamento não aceita validade retroativa", () =>
{
    var s = new OrcamentosService();
    Lanca<ArgumentException>(() => s.Criar(1, hoje.AddDays(-1), hoje));
    Lanca<ArgumentOutOfRangeException>(() => s.Criar(0, hoje, hoje));
});

Testar("orçamento ativo não pode duplicar ordem", () =>
{
    var s = new OrcamentosService();
    s.Criar(1, hoje.AddDays(1), hoje);
    Lanca<InvalidOperationException>(() => s.Criar(1, hoje.AddDays(2), hoje));
});

Testar("orçamento soma peças e mão de obra", () =>
{
    var s = new OrcamentosService();
    var o = s.Criar(1, hoje.AddDays(5), hoje);
    s.AdicionarItem(o.Id, TipoItem.Peca, "Tela", 1, 200m);
    var novo = s.AdicionarItem(o.Id, TipoItem.MaoDeObra, "Troca", 2, 50m);
    Igual(200m, novo.Calcular().Pecas);
    Igual(100m, novo.Calcular().MaoDeObra);
    Igual(300m, novo.Calcular().Total);
});

Testar("orçamento aplica desconto antes do imposto", () =>
{
    var s = new OrcamentosService();
    var o = s.Criar(1, hoje.AddDays(5), hoje);
    s.AdicionarItem(o.Id, TipoItem.Peca, "Tela", 1, 100m);
    var novo = s.ConfigurarTributos(o.Id, 10m, 20m);
    Igual(10m, novo.Calcular().Desconto);
    Igual(18m, novo.Calcular().Impostos);
    Igual(108m, novo.Calcular().Total);
});

Testar("desconto de 100 por cento zera total", () =>
{
    var s = new OrcamentosService();
    var o = s.Criar(1, hoje.AddDays(5), hoje);
    s.AdicionarItem(o.Id, TipoItem.Peca, "Tela", 1, 100m);
    Igual(0m, s.ConfigurarTributos(o.Id, 100m, 30m).Calcular().Total);
});

Testar("tributos fora do intervalo são rejeitados", () =>
{
    var s = new OrcamentosService();
    var o = s.Criar(1, hoje.AddDays(5), hoje);
    Lanca<ArgumentOutOfRangeException>(() => s.ConfigurarTributos(o.Id, -1, 0));
    Lanca<ArgumentOutOfRangeException>(() => s.ConfigurarTributos(o.Id, 0, 101));
});

Testar("item removido altera total e histórico", () =>
{
    var s = new OrcamentosService();
    var o = s.Criar(1, hoje.AddDays(5), hoje);
    var comItem = s.AdicionarItem(o.Id, TipoItem.Peca, "Tela", 1, 100m);
    var semItem = s.RemoverItem(o.Id, comItem.Itens[0].Id);
    Igual(0, semItem.Itens.Count);
    Igual(3, semItem.Historico.Count);
    Lanca<KeyNotFoundException>(() => s.RemoverItem(o.Id, 999));
});

Testar("orçamento sem itens não pode ser enviado", () =>
{
    var s = new OrcamentosService();
    var o = s.Criar(1, hoje.AddDays(5), hoje);
    Lanca<InvalidOperationException>(() => s.Enviar(o.Id, hoje));
});

Testar("orçamento enviado pode ser aprovado", () =>
{
    var s = new OrcamentosService();
    var o = s.Criar(1, hoje.AddDays(5), hoje);
    s.AdicionarItem(o.Id, TipoItem.Peca, "Tela", 1, 120m);
    s.Enviar(o.Id, hoje);
    Igual(EstadoOrcamento.Aprovado, s.Decidir(o.Id, true, hoje).Estado);
    Igual(120m, s.TotalAprovado());
});

Testar("orçamento aprovado fica imutável", () =>
{
    var s = new OrcamentosService();
    var o = s.Criar(1, hoje.AddDays(5), hoje);
    s.AdicionarItem(o.Id, TipoItem.Peca, "Tela", 1, 100m);
    s.Enviar(o.Id, hoje);
    s.Decidir(o.Id, true, hoje);
    Lanca<InvalidOperationException>(() => s.AdicionarItem(o.Id, TipoItem.Peca, "X", 1, 1));
    Lanca<InvalidOperationException>(() => s.ConfigurarTributos(o.Id, 1, 1));
    Lanca<InvalidOperationException>(() => s.Decidir(o.Id, false, hoje));
});

Testar("orçamento recusado permite nova proposta", () =>
{
    var s = new OrcamentosService();
    var o = s.Criar(1, hoje.AddDays(5), hoje);
    s.AdicionarItem(o.Id, TipoItem.Peca, "Tela", 1, 100m);
    s.Enviar(o.Id, hoje);
    s.Decidir(o.Id, false, hoje, "Muito caro");
    Igual(2, s.Criar(1, hoje.AddDays(10), hoje).Id);
    Igual(0m, s.TotalAprovado());
});

Testar("orçamento expirado permite nova proposta", () =>
{
    var s = new OrcamentosService();
    var o = s.Criar(1, hoje.AddDays(1), hoje);
    Lanca<InvalidOperationException>(() => s.Expirar(o.Id, hoje));
    Igual(EstadoOrcamento.Expirado, s.Expirar(o.Id, hoje.AddDays(2)).Estado);
    Igual(2, s.Criar(1, hoje.AddDays(3), hoje).Id);
});

Testar("orçamento vencido não pode ser decidido", () =>
{
    var s = new OrcamentosService();
    var o = s.Criar(1, hoje.AddDays(1), hoje);
    s.AdicionarItem(o.Id, TipoItem.Peca, "Tela", 1, 100m);
    s.Enviar(o.Id, hoje);
    Lanca<InvalidOperationException>(() => s.Decidir(o.Id, true, hoje.AddDays(2)));
});

Testar("orçamentos filtram por ordem e estado", () =>
{
    var s = new OrcamentosService();
    var a = s.Criar(1, hoje.AddDays(5), hoje);
    s.Criar(2, hoje.AddDays(5), hoje);
    s.AdicionarItem(a.Id, TipoItem.Peca, "Tela", 1, 100m);
    s.Enviar(a.Id, hoje);
    Igual(1, s.Listar(ordemId: 1).Count);
    Igual(1, s.Listar(estado: EstadoOrcamento.Enviado).Count);
});

GestaoOficina Preparar()
{
    var g = new GestaoOficina();
    var c = g.Cadastro.CadastrarCliente("Ana", "ana@exemplo.com");
    g.Cadastro.CadastrarEquipamento(c.Id, "Notebook", "Dell", "Latitude");
    return g;
}

Testar("gestão abre ordem vinculada", () =>
{
    var g = Preparar();
    var o = g.Abrir(1, 1, "Trocar teclado", agora: agora);
    Igual(1, o.Id);
    Igual("Ana", o.Cliente);
    Igual(1, g.ObterRegistro(o.Id).ClienteId);
    Igual(1, g.ObterRegistro(o.Id).Historico.Count);
});

Testar("gestão rejeita equipamento de outro cliente", () =>
{
    var g = Preparar();
    var c = g.Cadastro.CadastrarCliente("Bia", "bia@exemplo.com");
    Lanca<InvalidOperationException>(() => g.Abrir(c.Id, 1, "Problema"));
});

Testar("gestão rejeita prazo anterior à abertura", () =>
{
    var g = Preparar();
    Lanca<ArgumentException>(() =>
        g.Abrir(1, 1, "Problema", prazo: agora.AddDays(-1), agora: agora));
});

Testar("gestão atribui técnico e prioridade", () =>
{
    var g = Preparar();
    var o = g.Abrir(1, 1, "Problema", agora: agora);
    Igual("João", g.AtribuirTecnico(o.Id, " João ").Tecnico);
    Igual(PrioridadeOrdem.Urgente, g.AlterarPrioridade(o.Id, PrioridadeOrdem.Urgente).Prioridade);
    Igual(3, g.ObterRegistro(o.Id).Historico.Count);
});

Testar("gestão registra notas com autor", () =>
{
    var g = Preparar();
    var o = g.Abrir(1, 1, "Problema", agora: agora);
    var r = g.Anotar(o.Id, "Técnico", "Peça solicitada", agora);
    Igual(1, r.Notas.Count);
    Igual("Peça solicitada", r.Notas[0].Texto);
    Igual(agora, r.Notas[0].Quando);
});

Testar("gestão valida notas e técnico", () =>
{
    var g = Preparar();
    var o = g.Abrir(1, 1, "Problema");
    Lanca<ArgumentException>(() => g.Anotar(o.Id, "", "Nota"));
    Lanca<ArgumentException>(() => g.Anotar(o.Id, "Técnico", ""));
    Lanca<ArgumentException>(() => g.AtribuirTecnico(o.Id, ""));
});

Testar("gestão exige orçamento aprovado quando existe", () =>
{
    var g = Preparar();
    var o = g.Abrir(1, 1, "Problema");
    var p = g.CriarOrcamento(o.Id, hoje.AddDays(3), hoje);
    g.Orcamentos.AdicionarItem(p.Id, TipoItem.Peca, "Tela", 1, 300m);
    g.Orcamentos.Enviar(p.Id, hoje);
    Lanca<InvalidOperationException>(() => g.Avancar(o.Id, StatusOrdem.EmExecucao));
    g.Orcamentos.Decidir(p.Id, true, hoje);
    Igual(StatusOrdem.EmExecucao, g.Avancar(o.Id, StatusOrdem.EmExecucao).Status);
});

Testar("gestão mantém fluxo sem orçamento", () =>
{
    var g = Preparar();
    var o = g.Abrir(1, 1, "Problema");
    g.Avancar(o.Id, StatusOrdem.EmExecucao);
    g.Avancar(o.Id, StatusOrdem.Pronta);
    Igual(StatusOrdem.Entregue, g.Avancar(o.Id, StatusOrdem.Entregue).Status);
    Lanca<InvalidOperationException>(() => g.AtribuirTecnico(o.Id, "Técnico"));
});

Testar("gestão permite cancelamento", () =>
{
    var g = Preparar();
    var o = g.Abrir(1, 1, "Problema");
    Igual(StatusOrdem.Cancelada, g.Avancar(o.Id, StatusOrdem.Cancelada).Status);
    Lanca<InvalidOperationException>(() => g.CriarOrcamento(o.Id, hoje, hoje));
});

Testar("gestão encontra ordens atrasadas", () =>
{
    var g = Preparar();
    var a = g.Abrir(1, 1, "A", prazo: agora.AddDays(1), agora: agora);
    g.Abrir(1, 1, "B", prazo: agora.AddDays(10), agora: agora);
    Igual(0, g.Atrasadas(agora).Count);
    Igual(a.Id, g.Atrasadas(agora.AddDays(2))[0].Id);
});

Testar("ordem entregue deixa de estar atrasada", () =>
{
    var g = Preparar();
    var o = g.Abrir(1, 1, "A", prazo: agora.AddDays(1), agora: agora);
    g.Avancar(o.Id, StatusOrdem.EmExecucao);
    g.Avancar(o.Id, StatusOrdem.Pronta);
    g.Avancar(o.Id, StatusOrdem.Entregue);
    Igual(0, g.Atrasadas(agora.AddDays(10)).Count);
});

Testar("gestão filtra status, prioridade e técnico", () =>
{
    var g = Preparar();
    var a = g.Abrir(1, 1, "A", PrioridadeOrdem.Alta);
    g.Abrir(1, 1, "B");
    g.AtribuirTecnico(a.Id, "João");
    g.Avancar(a.Id, StatusOrdem.EmExecucao);
    Igual(1, g.Buscar(status: StatusOrdem.EmExecucao).Count);
    Igual(1, g.Buscar(prioridade: PrioridadeOrdem.Alta).Count);
    Igual(1, g.Buscar(tecnico: "JOÃO").Count);
    Igual(2, g.Buscar(clienteId: 1).Count);
});

Testar("gestão resume estados e valores", () =>
{
    var g = Preparar();
    var a = g.Abrir(1, 1, "A", prazo: agora.AddDays(1), agora: agora);
    g.Abrir(1, 1, "B", agora: agora);
    var p = g.CriarOrcamento(a.Id, hoje.AddDays(5), hoje);
    g.Orcamentos.AdicionarItem(p.Id, TipoItem.Peca, "Tela", 1, 100m);
    g.Orcamentos.Enviar(p.Id, hoje);
    g.Orcamentos.Decidir(p.Id, true, hoje);
    g.Avancar(a.Id, StatusOrdem.EmExecucao);
    var resumo = g.Resumo(agora.AddDays(2));
    Igual(2, resumo.Total);
    Igual(1, resumo.Abertas);
    Igual(1, resumo.EmExecucao);
    Igual(1, resumo.Atrasadas);
    Igual(100m, resumo.ValorAprovado);
});

Testar("CSV exporta dados e cabeçalho", () =>
{
    var g = Preparar();
    g.Abrir(1, 1, "A", agora: agora);
    var csv = g.ExportarCsv();
    Contem(csv, "id;cliente;equipamento;status");
    Contem(csv, "\"Ana\"");
    Contem(csv, "\"Dell Latitude\"");
    Contem(csv, "2026-11-08T12:00:00");
});

Testar("CSV protege fórmulas", () =>
{
    var g = new GestaoOficina();
    var c = g.Cadastro.CadastrarCliente("=2+2", "a@b.com");
    var e = g.Cadastro.CadastrarEquipamento(c.Id, "PC", "Dell", "A");
    g.Abrir(c.Id, e.Id, "Problema");
    Contem(g.ExportarCsv(), "\"'=2+2\"");
});

Testar("gestão não retorna estado mutável", () =>
{
    var g = Preparar();
    var o = g.Abrir(1, 1, "A");
    var r = g.ObterRegistro(o.Id);
    var novo = g.Anotar(o.Id, "Técnico", "Nota");
    Igual(0, r.Notas.Count);
    Igual(1, novo.Notas.Count);
});

Testar("gestão falha em ordem inexistente", () =>
{
    var g = Preparar();
    Lanca<KeyNotFoundException>(() => g.ObterOrdem(99));
    Lanca<KeyNotFoundException>(() => g.ObterRegistro(99));
    Lanca<KeyNotFoundException>(() => g.Avancar(99, StatusOrdem.Pronta));
});

Console.WriteLine($"Oficina Digital: {executados} testes aprovados, {falhas.Count} falhas.");
if (falhas.Count > 0)
    throw new Exception(string.Join(Environment.NewLine, falhas));
