namespace CthulhuSheets.Pages.CharacterCreation.Components;

public partial class CreationEquipmentStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    private void AddGearItem()
    {
        Investigator.GearAndPossessions.Add(string.Empty);
    }

    private void RemoveGearItem(int index)
    {
        Investigator.GearAndPossessions.RemoveAt(index);
    }

    private void AddWeapon()
    {
        Investigator.Weapons.Add(new Weapon());
    }

    private void RemoveWeapon(Weapon weapon)
    {
        Investigator.Weapons.Remove(weapon);
    }
}
