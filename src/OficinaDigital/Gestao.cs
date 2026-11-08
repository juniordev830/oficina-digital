using System.Text;

namespace OficinaDigital;

public enum PrioridadeOrdem
{
    Baixa,
    Normal,
    Alta,
    Urgente
}

public sealed record RegistroOrdem(
    int OrdemId,
    int ClienteId,
    int EquipamentoId,
    PrioridadeOrdem Prioridade,
    string? Tecnico,
    DateTimeOffset AbertaEm,
    DateTimeOffset? Prazo,
    IReadOnlyList<NotaOrdem> Notas,
    IReadOnlyList<MovimentoOrdem> Historico);

public sealed record NotaOrdem(DateTimeOffset Quando, string Autor, string Texto);
public sealed record MovimentoOrdem(DateTimeOffset Quando, string Evento, string? Detalhe);

public sealed record ResumoOficina(
    int Total,
    int Abertas,
    int EmExecucao,
    int Prontas,
    int Entregues,
    int Canceladas,
    int Atrasadas,
    decimal ValorAprovado);

public sealed class GestaoOficina
{
    private readonly object _gate = new();
    private readonly OficinaService _ordens = new();
    private readonly Dictionary<int, RegistroOrdem> _registros = new();

    public CadastroOficina Cadastro { get; } = new();
    public OrcamentosService Orcamentos { get; } = new();

    public OrdemServico Abrir(int clienteId, int equipamentoId, string descricao,
        PrioridadeOrdem prioridade = PrioridadeOrdem.Normal,
        DateTimeOffset? prazo = null, DateTimeOffset? agora = null)
    {
        if (!Enum.IsDefined(prioridade))
            throw new ArgumentOutOfRangeException(nameof(prioridade));
        var cliente = Cadastro.ObterCliente(clienteId);
        var equipamento = Cadastro.ObterEquipamento(equipamentoId);
        if (equipamento.ClienteId != clienteId)
            throw new InvalidOperationException("Equipamento não pertence ao cliente.");
        var instante = agora ?? DateTimeOffset.UtcNow;
        if (prazo is not null && prazo < instante)
            throw new ArgumentException("Prazo anterior à abertura.", nameof(prazo));
        var texto = Validation.Required(descricao, nameof(descricao), 1000);
        lock (_gate)
        {
            var ordem = _ordens.AbrirOrdem(cliente.Nome,
                $"{equipamento.Marca} {equipamento.Modelo}", texto);
            _registros.Add(ordem.Id, new RegistroOrdem(
                ordem.Id, clienteId, equipamentoId, prioridade, null, instante, prazo,
                Array.Empty<NotaOrdem>(),
                new[] { new MovimentoOrdem(instante, "Aberta", null) }));
            return ordem;
        }
    }

    public OrdemServico ObterOrdem(int id)
    {
        lock (_gate)
            return _ordens.Listar().FirstOrDefault(o => o.Id == id)
                ?? throw new KeyNotFoundException($"Ordem {id} não encontrada.");
    }

    public RegistroOrdem ObterRegistro(int id)
    {
        lock (_gate) return RegistroInterno(id);
    }

    private RegistroOrdem RegistroInterno(int id)
    {
        return _registros.TryGetValue(id, out var registro)
            ? registro : throw new KeyNotFoundException($"Ordem {id} não encontrada.");
    }

    private RegistroOrdem AtualizarRegistro(RegistroOrdem anterior,
        string evento, string? detalhe, DateTimeOffset instante,
        Func<RegistroOrdem, RegistroOrdem> atualizar)
    {
        var novo = atualizar(anterior);
        novo = novo with
        {
            Historico = anterior.Historico.Append(
                new MovimentoOrdem(instante, evento, detalhe)).ToArray()
        };
        _registros[anterior.OrdemId] = novo;
        return novo;
    }

    public RegistroOrdem AtribuirTecnico(int ordemId, string tecnico,
        DateTimeOffset? agora = null)
    {
        var nome = Validation.Required(tecnico, nameof(tecnico), 120);
        lock (_gate)
        {
            var registro = RegistroInterno(ordemId);
            if (ObterOrdem(ordemId).Status is StatusOrdem.Entregue or StatusOrdem.Cancelada)
                throw new InvalidOperationException("Ordem encerrada.");
            return AtualizarRegistro(registro, "TecnicoAtribuido", nome,
                agora ?? DateTimeOffset.UtcNow, r => r with { Tecnico = nome });
        }
    }

    public RegistroOrdem DefinirPrazo(int ordemId, DateTimeOffset prazo,
        DateTimeOffset? agora = null)
    {
        lock (_gate)
        {
            var registro = RegistroInterno(ordemId);
            if (prazo < registro.AbertaEm)
                throw new ArgumentException("Prazo anterior à abertura.", nameof(prazo));
            if (ObterOrdem(ordemId).Status is StatusOrdem.Entregue or StatusOrdem.Cancelada)
                throw new InvalidOperationException("Ordem encerrada.");
            return AtualizarRegistro(registro, "PrazoAlterado", prazo.ToString("O"),
                agora ?? DateTimeOffset.UtcNow, r => r with { Prazo = prazo });
        }
    }

    public RegistroOrdem AlterarPrioridade(int ordemId, PrioridadeOrdem prioridade,
        DateTimeOffset? agora = null)
    {
        if (!Enum.IsDefined(prioridade))
            throw new ArgumentOutOfRangeException(nameof(prioridade));
        lock (_gate)
        {
            var registro = RegistroInterno(ordemId);
            if (ObterOrdem(ordemId).Status is StatusOrdem.Entregue or StatusOrdem.Cancelada)
                throw new InvalidOperationException("Ordem encerrada.");
            return AtualizarRegistro(registro, "PrioridadeAlterada", prioridade.ToString(),
                agora ?? DateTimeOffset.UtcNow, r => r with { Prioridade = prioridade });
        }
    }

    public RegistroOrdem Anotar(int ordemId, string autor, string texto,
        DateTimeOffset? agora = null)
    {
        var nome = Validation.Required(autor, nameof(autor), 120);
        var nota = Validation.Required(texto, nameof(texto), 2000);
        var instante = agora ?? DateTimeOffset.UtcNow;
        lock (_gate)
        {
            var registro = RegistroInterno(ordemId);
            return AtualizarRegistro(registro, "NotaAdicionada", nome, instante,
                r => r with
                {
                    Notas = r.Notas.Append(new NotaOrdem(instante, nome, nota)).ToArray()
                });
        }
    }

    public OrdemServico Avancar(int ordemId, StatusOrdem status,
        DateTimeOffset? agora = null)
    {
        lock (_gate)
        {
            var registro = RegistroInterno(ordemId);
            var atual = ObterOrdem(ordemId);
            if (status == StatusOrdem.EmExecucao && atual.Status == StatusOrdem.Aberta)
            {
                var orcamentos = Orcamentos.Listar(ordemId);
                if (orcamentos.Count > 0 && !orcamentos.Any(o =>
                    o.Estado == EstadoOrcamento.Aprovado))
                    throw new InvalidOperationException("Orçamento exige aprovação.");
            }
            var nova = _ordens.AtualizarStatus(ordemId, status);
            if (nova.Status != atual.Status)
                AtualizarRegistro(registro, "StatusAlterado", status.ToString(),
                    agora ?? DateTimeOffset.UtcNow, r => r);
            return nova;
        }
    }

    public Orcamento CriarOrcamento(int ordemId, DateOnly validoAte, DateOnly hoje,
        DateTimeOffset? agora = null)
    {
        lock (_gate)
        {
            RegistroInterno(ordemId);
            if (ObterOrdem(ordemId).Status is StatusOrdem.Entregue or StatusOrdem.Cancelada)
                throw new InvalidOperationException("Ordem encerrada.");
            return Orcamentos.Criar(ordemId, validoAte, hoje, agora);
        }
    }

    public IReadOnlyList<OrdemServico> Buscar(StatusOrdem? status = null,
        int? clienteId = null, int? equipamentoId = null, PrioridadeOrdem? prioridade = null,
        string? tecnico = null)
    {
        lock (_gate)
        {
            var query = _ordens.Listar().AsEnumerable();
            if (status is not null) query = query.Where(o => o.Status == status.Value);
            if (clienteId is not null)
                query = query.Where(o => _registros[o.Id].ClienteId == clienteId);
            if (equipamentoId is not null)
                query = query.Where(o => _registros[o.Id].EquipamentoId == equipamentoId);
            if (prioridade is not null)
                query = query.Where(o => _registros[o.Id].Prioridade == prioridade);
            if (!string.IsNullOrWhiteSpace(tecnico))
                query = query.Where(o => string.Equals(_registros[o.Id].Tecnico,
                    tecnico.Trim(), StringComparison.OrdinalIgnoreCase));
            return query.OrderBy(o => o.Id).ToArray();
        }
    }

    public IReadOnlyList<OrdemServico> Atrasadas(DateTimeOffset agora)
    {
        lock (_gate)
            return _ordens.Listar().Where(o =>
                o.Status is not (StatusOrdem.Entregue or StatusOrdem.Cancelada)
                && _registros[o.Id].Prazo is { } prazo && prazo < agora)
                .OrderBy(o => _registros[o.Id].Prazo).ToArray();
    }

    public ResumoOficina Resumo(DateTimeOffset agora)
    {
        lock (_gate)
        {
            var ordens = _ordens.Listar();
            return new ResumoOficina(
                ordens.Count,
                ordens.Count(o => o.Status == StatusOrdem.Aberta),
                ordens.Count(o => o.Status == StatusOrdem.EmExecucao),
                ordens.Count(o => o.Status == StatusOrdem.Pronta),
                ordens.Count(o => o.Status == StatusOrdem.Entregue),
                ordens.Count(o => o.Status == StatusOrdem.Cancelada),
                Atrasadas(agora).Count,
                Orcamentos.TotalAprovado());
        }
    }

    private static string Csv(string? value)
    {
        var clean = value ?? "";
        if (clean.Length > 0 && "=+-@\t\r".Contains(clean[0]))
            clean = "'" + clean;
        return "\"" + clean.Replace("\"", "\"\"") + "\"";
    }

    public string ExportarCsv()
    {
        var builder = new StringBuilder();
        builder.AppendLine("id;cliente;equipamento;status;prioridade;tecnico;aberta_em;prazo");
        lock (_gate)
        {
            foreach (var ordem in _ordens.Listar().OrderBy(o => o.Id))
            {
                var registro = _registros[ordem.Id];
                var campos = new[]
                {
                    ordem.Id.ToString(),
                    Csv(ordem.Cliente),
                    Csv(ordem.Equipamento),
                    ordem.Status.ToString(),
                    registro.Prioridade.ToString(),
                    Csv(registro.Tecnico),
                    registro.AbertaEm.ToString("O"),
                    registro.Prazo?.ToString("O") ?? ""
                };
                builder.AppendLine(string.Join(';', campos));
            }
        }
        return builder.ToString();
    }
}
