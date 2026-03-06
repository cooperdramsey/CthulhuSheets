using CthulhuSheets.Models;

namespace CthulhuSheets.Pages.CharacterCreation.Components;

public partial class CreationCharacteristicsStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    private record CharacteristicDef(Characteristic Stat, bool Is3d6, string Formula);

    private CharacteristicDef[] _characteristicDefs = [];

    protected override void OnParametersSet()
    {
        _characteristicDefs =
        [
            new(Investigator.Strength,     Is3d6: true,  Formula: "3D6 × 5"),
            new(Investigator.Constitution, Is3d6: true,  Formula: "3D6 × 5"),
            new(Investigator.Dexterity,    Is3d6: true,  Formula: "3D6 × 5"),
            new(Investigator.Appearance,   Is3d6: true,  Formula: "3D6 × 5"),
            new(Investigator.Power,        Is3d6: true,  Formula: "3D6 × 5"),
            new(Investigator.Size,         Is3d6: false, Formula: "(2D6+6) × 5"),
            new(Investigator.Intelligence, Is3d6: false, Formula: "(2D6+6) × 5"),
            new(Investigator.Education,    Is3d6: false, Formula: "(2D6+6) × 5"),
        ];
    }

    private void RollAll()
    {
        foreach (var def in _characteristicDefs)
            Roll(def);
    }

    private void RollSingle(CharacteristicDef def) => Roll(def);

    private void Roll(CharacteristicDef def)
    {
        int raw = def.Is3d6
            ? Random.Shared.Next(1, 7) + Random.Shared.Next(1, 7) + Random.Shared.Next(1, 7)
            : Random.Shared.Next(1, 7) + Random.Shared.Next(1, 7) + 6;
        def.Stat.Regular = raw * 5;
    }
}
