using F1Fantasy.Models;

namespace F1Fantasy.Repository;

public class SeasonRepository
{
    private readonly List<Season> _seasons = new();

    public void AddOrUpdate(Season season)
    {
        var existing = _seasons.FirstOrDefault(s => s.Year == season.Year);
        if (existing != null)
        {
            _seasons.Remove(existing);
        }
        _seasons.Add(season);
    }

    public Season? GetByYear(string year)
    {
        return _seasons.FirstOrDefault(s => s.Year == year);
    }

    public IEnumerable<Season> GetAll()
    {
        return _seasons.OrderBy(s => s.Year);
    }

    public void Clear()
    {
        _seasons.Clear();
    }
}
