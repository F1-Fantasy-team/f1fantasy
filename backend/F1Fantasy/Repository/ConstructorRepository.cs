using F1Fantasy.Models;

namespace F1Fantasy.Repository;

public class ConstructorRepository
{
    private readonly List<Constructor> _constructors = new();

    public void AddOrUpdate(Constructor constructor)
    {
        var existing = _constructors.FirstOrDefault(c => c.ConstructorId == constructor.ConstructorId);
        if (existing != null)
        {
            _constructors.Remove(existing);
        }
        _constructors.Add(constructor);
    }

    public Constructor? GetByConstructorId(string constructorId)
    {
        return _constructors.FirstOrDefault(c => c.ConstructorId == constructorId);
    }

    public IEnumerable<Constructor> GetAll()
    {
        return _constructors.OrderBy(c => c.Name);
    }

    public void Clear()
    {
        _constructors.Clear();
    }
}
