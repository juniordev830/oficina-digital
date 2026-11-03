using OficinaDigital;

var oficina = new OficinaService();
var ordem = oficina.AbrirOrdem("Cliente Demo", "Notebook", "Trocar teclado");

if (ordem.Status != StatusOrdem.Aberta || ordem.Id != 1)
    throw new Exception("A abertura deve criar uma ordem numerada e aberta.");

oficina.AtualizarStatus(ordem.Id, StatusOrdem.EmExecucao);
oficina.AtualizarStatus(ordem.Id, StatusOrdem.Pronta);
var entregue = oficina.AtualizarStatus(ordem.Id, StatusOrdem.Entregue);

if (entregue.Status != StatusOrdem.Entregue || oficina.Listar().Count != 1)
    throw new Exception("O fluxo da ordem não foi concluído corretamente.");

try
{
    oficina.AtualizarStatus(ordem.Id, StatusOrdem.Aberta);
    throw new Exception("Uma ordem entregue não pode voltar para aberta.");
}
catch (InvalidOperationException)
{
    // comportamento esperado
}

// Operational dashboard and immutable query snapshots.
var aberta = oficina.AbrirOrdem("Cliente A", "Impressora", "Trocar cartucho");
var emExecucao = oficina.AbrirOrdem("Cliente B", "Monitor", "Reparar alimentação");
var pronta = oficina.AbrirOrdem("Cliente C", "Notebook Dell", "Trocar bateria");
var cancelada = oficina.AbrirOrdem("Cliente D", "Roteador", "Falha intermitente");
oficina.AtualizarStatus(emExecucao.Id, StatusOrdem.EmExecucao);
oficina.AtualizarStatus(pronta.Id, StatusOrdem.EmExecucao);
oficina.AtualizarStatus(pronta.Id, StatusOrdem.Pronta);
oficina.AtualizarStatus(cancelada.Id, StatusOrdem.Cancelada);

var resumo = oficina.Resumo();
if (resumo != new ResumoOrdens(1, 1, 1, 1, 1))
    throw new Exception("O painel deve contar corretamente cada status.");

var pendentes = oficina.ListarPendentes();
if (!pendentes.Select(o => o.Id).SequenceEqual(new[] { pronta.Id, emExecucao.Id, aberta.Id }))
    throw new Exception("A fila deve priorizar as ordens prontas e excluir finalizadas.");
if (!oficina.BuscarPorEquipamento("  NOTEBOOK  ").Select(o => o.Id)
        .SequenceEqual(new[] { ordem.Id, pronta.Id }))
    throw new Exception("A busca por equipamento deve ser insensível a caixa.");

try
{
    oficina.BuscarPorEquipamento(" ");
    throw new Exception("Uma busca vazia deveria ser rejeitada.");
}
catch (ArgumentException)
{
    // comportamento esperado
}

try
{
    ((IList<OrdemServico>)pendentes).Clear();
    throw new Exception("O snapshot da fila deveria ser imutável.");
}
catch (NotSupportedException)
{
    // comportamento esperado
}

oficina.AtualizarStatus(pronta.Id, StatusOrdem.Entregue);
if (oficina.Resumo() != new ResumoOrdens(1, 1, 0, 2, 1) || pendentes.Count != 3)
    throw new Exception("O painel deve refletir alterações enquanto o snapshot permanece estável.");

Console.WriteLine("Oficina Digital: smoke tests OK");
