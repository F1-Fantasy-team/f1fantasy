using F1Fantasy.Models;

namespace F1Fantasy.Repository;

public class CircuitRepository
{
    private readonly List<Circuit> _circuits = new();

    public void AddOrUpdate(Circuit circuit)
    {
        var existing = _circuits.FirstOrDefault(c => c.CircuitId == circuit.CircuitId);
        if (existing != null)
        {
            _circuits.Remove(existing);
        }
        _circuits.Add(circuit);
    }

    public Circuit? GetByCircuitId(string circuitId)
    {
        return _circuits.FirstOrDefault(c => c.CircuitId == circuitId);
    }

    public IEnumerable<Circuit> GetAll()
    {
        return _circuits.OrderBy(c => c.CircuitName);
    }

    public void Clear()
    {
        _circuits.Clear();
    }
}
