using System.Globalization;

namespace OficinaDigital;

internal static class Validation
{
    public static string Required(string? value, string name, int maximum = 200)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} obrigatório.", name);
        var clean = value.Trim();
        if (clean.Length > maximum)
            throw new ArgumentException($"{name} excede {maximum} caracteres.", name);
        return clean;
    }

    public static string? Optional(string? value, string name, int maximum = 200)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Required(value, name, maximum);
    }

    public static decimal Money(decimal value, string name)
    {
        if (value < 0 || value > 1_000_000m)
            throw new ArgumentOutOfRangeException(name);
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    public static void Positive(int value, string name)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(name);
    }

    public static string Email(string? value)
    {
        var email = Required(value, "email", 254);
        var at = email.IndexOf('@');
        if (at <= 0 || at == email.Length - 1 || email.Contains(' '))
            throw new ArgumentException("Email inválido.", nameof(value));
        var domain = email[(at + 1)..];
        if (!domain.Contains('.') || domain.StartsWith('.') || domain.EndsWith('.'))
            throw new ArgumentException("Email inválido.", nameof(value));
        return email.ToLowerInvariant();
    }
}

public sealed record Cliente(
    int Id,
    string Nome,
    string Email,
    string? Telefone,
    DateTimeOffset CriadoEm);

public sealed record Equipamento(
    int Id,
    int ClienteId,
    string Tipo,
    string Marca,
    string Modelo,
    string? NumeroSerie,
    DateOnly? GarantiaAte,
    DateTimeOffset CriadoEm);

public sealed class CadastroOficina
{
    private readonly object _gate = new();
    private readonly Dictionary<int, Cliente> _clientes = new();
    private readonly Dictionary<int, Equipamento> _equipamentos = new();
    private int _proximoCliente = 1;
    private int _proximoEquipamento = 1;

    public Cliente CadastrarCliente(string nome, string email, string? telefone = null,
        DateTimeOffset? agora = null)
    {
        var nomeValido = Validation.Required(nome, nameof(nome));
        var emailValido = Validation.Email(email);
        var telefoneValido = Validation.Optional(telefone, nameof(telefone), 40);
        lock (_gate)
        {
            if (_clientes.Values.Any(c => c.Email == emailValido))
                throw new InvalidOperationException("Email já cadastrado.");
            var cliente = new Cliente(_proximoCliente++, nomeValido, emailValido,
                telefoneValido, agora ?? DateTimeOffset.UtcNow);
            _clientes.Add(cliente.Id, cliente);
            return cliente;
        }
    }

    public Cliente AtualizarCliente(int id, string nome, string email, string? telefone = null)
    {
        Validation.Positive(id, nameof(id));
        var nomeValido = Validation.Required(nome, nameof(nome));
        var emailValido = Validation.Email(email);
        var telefoneValido = Validation.Optional(telefone, nameof(telefone), 40);
        lock (_gate)
        {
            var atual = ObterClienteInterno(id);
            if (_clientes.Values.Any(c => c.Id != id && c.Email == emailValido))
                throw new InvalidOperationException("Email já cadastrado.");
            var atualizado = atual with
            {
                Nome = nomeValido,
                Email = emailValido,
                Telefone = telefoneValido
            };
            _clientes[id] = atualizado;
            return atualizado;
        }
    }

    public Cliente ObterCliente(int id)
    {
        lock (_gate) return ObterClienteInterno(id);
    }

    private Cliente ObterClienteInterno(int id)
    {
        return _clientes.TryGetValue(id, out var cliente)
            ? cliente : throw new KeyNotFoundException($"Cliente {id} não encontrado.");
    }

    public IReadOnlyList<Cliente> ListarClientes(string? busca = null)
    {
        lock (_gate)
        {
            var query = _clientes.Values.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim();
                query = query.Where(c => c.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase)
                    || c.Email.Contains(termo, StringComparison.OrdinalIgnoreCase));
            }
            return query.OrderBy(c => c.Nome, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Id).ToArray();
        }
    }

    public void ExcluirCliente(int id)
    {
        lock (_gate)
        {
            ObterClienteInterno(id);
            if (_equipamentos.Values.Any(e => e.ClienteId == id))
                throw new InvalidOperationException("Cliente possui equipamentos.");
            _clientes.Remove(id);
        }
    }

    public Equipamento CadastrarEquipamento(int clienteId, string tipo, string marca,
        string modelo, string? numeroSerie = null, DateOnly? garantiaAte = null,
        DateTimeOffset? agora = null)
    {
        var tipoValido = Validation.Required(tipo, nameof(tipo), 80);
        var marcaValida = Validation.Required(marca, nameof(marca), 80);
        var modeloValido = Validation.Required(modelo, nameof(modelo), 120);
        var serieValida = Validation.Optional(numeroSerie, nameof(numeroSerie), 100);
        lock (_gate)
        {
            ObterClienteInterno(clienteId);
            if (serieValida is not null && _equipamentos.Values.Any(e =>
                string.Equals(e.NumeroSerie, serieValida, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Número de série duplicado.");
            var equipamento = new Equipamento(_proximoEquipamento++, clienteId, tipoValido,
                marcaValida, modeloValido, serieValida, garantiaAte,
                agora ?? DateTimeOffset.UtcNow);
            _equipamentos.Add(equipamento.Id, equipamento);
            return equipamento;
        }
    }

    public Equipamento ObterEquipamento(int id)
    {
        lock (_gate)
        {
            return _equipamentos.TryGetValue(id, out var equipamento)
                ? equipamento : throw new KeyNotFoundException($"Equipamento {id} não encontrado.");
        }
    }

    public IReadOnlyList<Equipamento> ListarEquipamentos(int? clienteId = null,
        string? busca = null)
    {
        lock (_gate)
        {
            var query = _equipamentos.Values.AsEnumerable();
            if (clienteId is not null)
                query = query.Where(e => e.ClienteId == clienteId.Value);
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim();
                query = query.Where(e => e.Marca.Contains(termo, StringComparison.OrdinalIgnoreCase)
                    || e.Modelo.Contains(termo, StringComparison.OrdinalIgnoreCase)
                    || (e.NumeroSerie?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false));
            }
            return query.OrderBy(e => e.Id).ToArray();
        }
    }

    public void ExcluirEquipamento(int id)
    {
        lock (_gate)
        {
            if (!_equipamentos.Remove(id))
                throw new KeyNotFoundException($"Equipamento {id} não encontrado.");
        }
    }

    public bool EstaNaGarantia(int equipamentoId, DateOnly data)
    {
        var equipamento = ObterEquipamento(equipamentoId);
        return equipamento.GarantiaAte is { } fim && data <= fim;
    }

    public IReadOnlyList<Equipamento> GarantiasVencendo(DateOnly hoje, int dias)
    {
        if (dias < 0 || dias > 3650) throw new ArgumentOutOfRangeException(nameof(dias));
        var limite = hoje.AddDays(dias);
        lock (_gate)
            return _equipamentos.Values.Where(e => e.GarantiaAte is { } fim &&
                fim >= hoje && fim <= limite).OrderBy(e => e.GarantiaAte).ToArray();
    }
}
