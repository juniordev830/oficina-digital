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

Console.WriteLine("Oficina Digital: smoke tests OK");
