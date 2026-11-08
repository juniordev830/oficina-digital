namespace OficinaDigital;

public enum EstadoOrcamento
{
    Rascunho,
    Enviado,
    Aprovado,
    Recusado,
    Expirado
}

public enum TipoItem
{
    Peca,
    MaoDeObra
}

public sealed record ItemOrcamento(
    int Id,
    TipoItem Tipo,
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario)
{
    public decimal Total => decimal.Round(Quantidade * ValorUnitario, 2,
        MidpointRounding.AwayFromZero);

    public static ItemOrcamento Criar(int id, TipoItem tipo, string descricao,
        decimal quantidade, decimal valorUnitario)
    {
        Validation.Positive(id, nameof(id));
        if (!Enum.IsDefined(tipo)) throw new ArgumentOutOfRangeException(nameof(tipo));
        if (quantidade <= 0 || quantidade > 10000)
            throw new ArgumentOutOfRangeException(nameof(quantidade));
        if (decimal.Round(quantidade, 3) != quantidade)
            throw new ArgumentException("Quantidade admite até três casas.", nameof(quantidade));
        return new ItemOrcamento(id, tipo, Validation.Required(descricao, nameof(descricao)),
            quantidade, Validation.Money(valorUnitario, nameof(valorUnitario)));
    }
}

public sealed record TotaisOrcamento(
    decimal Pecas,
    decimal MaoDeObra,
    decimal Subtotal,
    decimal Desconto,
    decimal Impostos,
    decimal Total);

public sealed record EventoOrcamento(
    DateTimeOffset Quando,
    string Acao,
    string? Observacao);

public sealed record Orcamento(
    int Id,
    int OrdemId,
    EstadoOrcamento Estado,
    IReadOnlyList<ItemOrcamento> Itens,
    decimal DescontoPercentual,
    decimal ImpostoPercentual,
    DateOnly ValidoAte,
    IReadOnlyList<EventoOrcamento> Historico)
{
    public TotaisOrcamento Calcular()
    {
        var pecas = Itens.Where(i => i.Tipo == TipoItem.Peca).Sum(i => i.Total);
        var maoDeObra = Itens.Where(i => i.Tipo == TipoItem.MaoDeObra).Sum(i => i.Total);
        var subtotal = pecas + maoDeObra;
        var desconto = decimal.Round(subtotal * DescontoPercentual / 100m, 2,
            MidpointRounding.AwayFromZero);
        var baseImposto = subtotal - desconto;
        var impostos = decimal.Round(baseImposto * ImpostoPercentual / 100m, 2,
            MidpointRounding.AwayFromZero);
        return new TotaisOrcamento(pecas, maoDeObra, subtotal, desconto, impostos,
            baseImposto + impostos);
    }
}

public sealed class OrcamentosService
{
    private readonly object _gate = new();
    private readonly Dictionary<int, Orcamento> _orcamentos = new();
    private int _proximoId = 1;
    private int _proximoItemId = 1;

    public Orcamento Criar(int ordemId, DateOnly validoAte, DateOnly hoje,
        DateTimeOffset? agora = null)
    {
        Validation.Positive(ordemId, nameof(ordemId));
        if (validoAte < hoje)
            throw new ArgumentException("Validade anterior à data atual.", nameof(validoAte));
        lock (_gate)
        {
            if (_orcamentos.Values.Any(o => o.OrdemId == ordemId &&
                o.Estado is not (EstadoOrcamento.Recusado or EstadoOrcamento.Expirado)))
                throw new InvalidOperationException("Já existe orçamento ativo para esta ordem.");
            var registro = new Orcamento(_proximoId++, ordemId, EstadoOrcamento.Rascunho,
                Array.Empty<ItemOrcamento>(), 0m, 0m, validoAte,
                new[] { new EventoOrcamento(agora ?? DateTimeOffset.UtcNow, "Criado", null) });
            _orcamentos.Add(registro.Id, registro);
            return registro;
        }
    }

    public Orcamento Obter(int id)
    {
        lock (_gate) return ObterInterno(id);
    }

    private Orcamento ObterInterno(int id)
    {
        return _orcamentos.TryGetValue(id, out var item)
            ? item : throw new KeyNotFoundException($"Orçamento {id} não encontrado.");
    }

    private static void ExigirRascunho(Orcamento orcamento)
    {
        if (orcamento.Estado != EstadoOrcamento.Rascunho)
            throw new InvalidOperationException("Somente rascunhos podem ser editados.");
    }

    private Orcamento Registrar(Orcamento anterior, Orcamento atualizado,
        string acao, DateTimeOffset? agora, string? observacao = null)
    {
        var historico = anterior.Historico.Append(new EventoOrcamento(
            agora ?? DateTimeOffset.UtcNow, acao, observacao)).ToArray();
        var final = atualizado with { Historico = historico };
        _orcamentos[anterior.Id] = final;
        return final;
    }

    public Orcamento AdicionarItem(int orcamentoId, TipoItem tipo, string descricao,
        decimal quantidade, decimal valorUnitario, DateTimeOffset? agora = null)
    {
        lock (_gate)
        {
            var atual = ObterInterno(orcamentoId);
            ExigirRascunho(atual);
            var item = ItemOrcamento.Criar(_proximoItemId, tipo, descricao, quantidade, valorUnitario);
            _proximoItemId++;
            return Registrar(atual, atual with { Itens = atual.Itens.Append(item).ToArray() },
                "ItemAdicionado", agora, item.Descricao);
        }
    }

    public Orcamento RemoverItem(int orcamentoId, int itemId, DateTimeOffset? agora = null)
    {
        lock (_gate)
        {
            var atual = ObterInterno(orcamentoId);
            ExigirRascunho(atual);
            if (!atual.Itens.Any(i => i.Id == itemId))
                throw new KeyNotFoundException($"Item {itemId} não encontrado.");
            return Registrar(atual, atual with
            {
                Itens = atual.Itens.Where(i => i.Id != itemId).ToArray()
            }, "ItemRemovido", agora, itemId.ToString());
        }
    }

    public Orcamento ConfigurarTributos(int id, decimal descontoPercentual,
        decimal impostoPercentual, DateTimeOffset? agora = null)
    {
        if (descontoPercentual < 0 || descontoPercentual > 100)
            throw new ArgumentOutOfRangeException(nameof(descontoPercentual));
        if (impostoPercentual < 0 || impostoPercentual > 100)
            throw new ArgumentOutOfRangeException(nameof(impostoPercentual));
        lock (_gate)
        {
            var atual = ObterInterno(id);
            ExigirRascunho(atual);
            return Registrar(atual, atual with
            {
                DescontoPercentual = descontoPercentual,
                ImpostoPercentual = impostoPercentual
            }, "TributosConfigurados", agora);
        }
    }

    public Orcamento Enviar(int id, DateOnly hoje, DateTimeOffset? agora = null)
    {
        lock (_gate)
        {
            var atual = ObterInterno(id);
            ExigirRascunho(atual);
            if (atual.Itens.Count == 0)
                throw new InvalidOperationException("Orçamento sem itens.");
            if (atual.ValidoAte < hoje)
                throw new InvalidOperationException("Orçamento vencido.");
            return Registrar(atual, atual with { Estado = EstadoOrcamento.Enviado },
                "Enviado", agora);
        }
    }

    public Orcamento Decidir(int id, bool aprovado, DateOnly hoje,
        string? observacao = null, DateTimeOffset? agora = null)
    {
        lock (_gate)
        {
            var atual = ObterInterno(id);
            if (atual.Estado != EstadoOrcamento.Enviado)
                throw new InvalidOperationException("Orçamento não está enviado.");
            if (hoje > atual.ValidoAte)
                throw new InvalidOperationException("Orçamento vencido.");
            var estado = aprovado ? EstadoOrcamento.Aprovado : EstadoOrcamento.Recusado;
            return Registrar(atual, atual with { Estado = estado },
                aprovado ? "Aprovado" : "Recusado", agora,
                Validation.Optional(observacao, nameof(observacao), 500));
        }
    }

    public Orcamento Expirar(int id, DateOnly hoje, DateTimeOffset? agora = null)
    {
        lock (_gate)
        {
            var atual = ObterInterno(id);
            if (hoje <= atual.ValidoAte)
                throw new InvalidOperationException("Orçamento ainda válido.");
            if (atual.Estado is not (EstadoOrcamento.Enviado or EstadoOrcamento.Rascunho))
                throw new InvalidOperationException("Estado final não pode expirar.");
            return Registrar(atual, atual with { Estado = EstadoOrcamento.Expirado },
                "Expirado", agora);
        }
    }

    public IReadOnlyList<Orcamento> Listar(int? ordemId = null,
        EstadoOrcamento? estado = null)
    {
        lock (_gate)
        {
            var query = _orcamentos.Values.AsEnumerable();
            if (ordemId is not null) query = query.Where(o => o.OrdemId == ordemId.Value);
            if (estado is not null) query = query.Where(o => o.Estado == estado.Value);
            return query.OrderBy(o => o.Id).ToArray();
        }
    }

    public decimal TotalAprovado()
    {
        lock (_gate)
            return _orcamentos.Values.Where(o => o.Estado == EstadoOrcamento.Aprovado)
                .Sum(o => o.Calcular().Total);
    }
}
