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

/// <summary>Snapshot of the service orders currently held in memory.</summary>
public sealed record ResumoOrdens(int Abertas, int EmExecucao, int Prontas, int Entregues, int Canceladas);

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

    /// <summary>Counts orders by lifecycle state without mutating any order.</summary>
    public ResumoOrdens Resumo() => new(
        _ordens.Count(o => o.Status == StatusOrdem.Aberta),
        _ordens.Count(o => o.Status == StatusOrdem.EmExecucao),
        _ordens.Count(o => o.Status == StatusOrdem.Pronta),
        _ordens.Count(o => o.Status == StatusOrdem.Entregue),
        _ordens.Count(o => o.Status == StatusOrdem.Cancelada));

    /// <summary>
    /// Lists actionable orders: first ready for pickup, then in progress, then new.
    /// Delivered and cancelled orders do not appear.
    /// </summary>
    public IReadOnlyList<OrdemServico> ListarPendentes()
    {
        var results = _ordens
            .Where(o => o.Status is StatusOrdem.Aberta or StatusOrdem.EmExecucao or StatusOrdem.Pronta)
            .OrderBy(o => o.Status switch
            {
                StatusOrdem.Pronta => 0,
                StatusOrdem.EmExecucao => 1,
                _ => 2
            })
            .ThenBy(o => o.Id)
            .ToArray();
        return Array.AsReadOnly(results);
    }

    /// <summary>Searches by equipment name, case-insensitively; returns an immutable snapshot.</summary>
    public IReadOnlyList<OrdemServico> BuscarPorEquipamento(string termo)
    {
        if (string.IsNullOrWhiteSpace(termo))
            throw new ArgumentException("Termo de busca obrigatório.", nameof(termo));
        var results = _ordens
            .Where(o => o.Equipamento.Contains(termo.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(o => o.Id)
            .ToArray();
        return Array.AsReadOnly(results);
    }

    public IReadOnlyList<OrdemServico> Listar() => _ordens.AsReadOnly();
}
