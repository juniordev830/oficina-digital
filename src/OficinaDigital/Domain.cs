namespace OficinaDigital;

public enum StatusOrdem
{
    Aberta,
    EmExecucao,
    Pronta,
    Entregue,
    Cancelada
}

public sealed record OrdemServico(
    int Id,
    string Cliente,
    string Equipamento,
    string Descricao,
    StatusOrdem Status)
{
    public static OrdemServico Abrir(int id, string cliente, string equipamento, string descricao)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (string.IsNullOrWhiteSpace(cliente)) throw new ArgumentException("Cliente obrigatório.", nameof(cliente));
        if (string.IsNullOrWhiteSpace(equipamento)) throw new ArgumentException("Equipamento obrigatório.", nameof(equipamento));
        if (string.IsNullOrWhiteSpace(descricao)) throw new ArgumentException("Descrição obrigatória.", nameof(descricao));
        return new OrdemServico(id, cliente.Trim(), equipamento.Trim(), descricao.Trim(), StatusOrdem.Aberta);
    }

    public OrdemServico Avancar(StatusOrdem novoStatus)
    {
        if (novoStatus == Status) return this;
        var permitido = (Status, novoStatus) switch
        {
            (StatusOrdem.Aberta, StatusOrdem.EmExecucao) => true,
            (StatusOrdem.Aberta, StatusOrdem.Cancelada) => true,
            (StatusOrdem.EmExecucao, StatusOrdem.Pronta) => true,
            (StatusOrdem.EmExecucao, StatusOrdem.Cancelada) => true,
            (StatusOrdem.Pronta, StatusOrdem.Entregue) => true,
            _ => false
        };
        if (!permitido) throw new InvalidOperationException($"Transição inválida: {Status} -> {novoStatus}.");
        return this with { Status = novoStatus };
    }
}

public sealed class OficinaService
{
    private readonly List<OrdemServico> _ordens = [];
    private int _nextId = 1;

    public OrdemServico AbrirOrdem(string cliente, string equipamento, string descricao)
    {
        var ordem = OrdemServico.Abrir(_nextId++, cliente, equipamento, descricao);
        _ordens.Add(ordem);
        return ordem;
    }

    public OrdemServico AtualizarStatus(int id, StatusOrdem status)
    {
        var index = _ordens.FindIndex(o => o.Id == id);
        if (index < 0) throw new KeyNotFoundException($"Ordem {id} não encontrada.");
        var atualizada = _ordens[index].Avancar(status);
        _ordens[index] = atualizada;
        return atualizada;
    }

    public IReadOnlyList<OrdemServico> Listar() => _ordens.AsReadOnly();
}
