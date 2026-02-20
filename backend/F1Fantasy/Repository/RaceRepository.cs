using F1Fantasy.Models;

namespace F1Fantasy.Repository;

public class RaceRepository
{
    private readonly List<Race> _races = new();

    public void AddOrUpdate(Race race)
    {
        var existing = _races.FirstOrDefault(r => r.Season == race.Season && r.Round == race.Round);
        if (existing != null)
        {
            _races.Remove(existing);
        }
        _races.Add(race);
    }

    public Race? GetByRound(string season, string round)
    {
        return _races.FirstOrDefault(r => r.Season == season && r.Round == round);
    }

    public IEnumerable<Race> GetAll()
    {
        return _races;
    }

    public IEnumerable<Race> GetBySeason(string season)
    {
        return _races.Where(r => r.Season == season);
    }

    public void Clear()
    {
        _races.Clear();
    }
}