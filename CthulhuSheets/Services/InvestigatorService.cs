using CthulhuSheets.Models;

namespace CthulhuSheets.Services;

public class InvestigatorService
{
    public Investigator? Current { get; private set; }

    public event Action? OnChanged;

    public void Load(Investigator investigator)
    {
        Current = investigator;
        OnChanged?.Invoke();
    }

    public void Clear()
    {
        Current = null;
        OnChanged?.Invoke();
    }
}
