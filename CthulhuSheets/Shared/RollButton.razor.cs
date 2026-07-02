using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace CthulhuSheets.Shared;

public partial class RollButton
{
    [Parameter, EditorRequired]
    public EventCallback<int> OnRoll { get; set; }

    [Parameter]
    public Size Size { get; set; } = Size.Small;

    [Parameter]
    public Color Color { get; set; } = Color.Primary;

    /// <summary>
    /// Whether the right-click bonus/penalty-dice popup is available. Disable
    /// for rolls the rules never modify with bonus/penalty dice (e.g. Sanity
    /// rolls, ch_8 — the sole exception, Self-Help with the key connection,
    /// is handled narratively by the Keeper).
    /// </summary>
    [Parameter]
    public bool AllowModifier { get; set; } = true;

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private bool _open;
    private int _modifier;
    private double _popupX;
    private double _popupY;
    private bool _flipBelow;

    private const double PopupWidth = 180;
    private const double PopupHeight = 165;

    private string ModifierLabel => _modifier switch
    {
        2  => "+2 Bonus",
        1  => "+1 Bonus",
        0  => "Normal",
        -1 => "-1 Penalty",
        _  => "-2 Penalty"
    };

    private string ModifierClass => _modifier switch
    {
        > 0 => "roll-popup-mod--bonus",
        < 0 => "roll-popup-mod--penalty",
        _   => "roll-popup-mod--normal"
    };

    private async Task OpenPopover(MouseEventArgs e)
    {
        if (!AllowModifier) return;

        _modifier = 0;

        var vw = await JSRuntime.InvokeAsync<double>("eval", "window.innerWidth");
        var vh = await JSRuntime.InvokeAsync<double>("eval", "window.innerHeight");

        // Flip horizontally if popup would overflow right edge
        _popupX = e.ClientX + 6 + PopupWidth > vw
            ? e.ClientX - 6 - PopupWidth
            : e.ClientX + 6;

        // Flip vertically if popup would overflow top edge
        _flipBelow = e.ClientY - 8 < PopupHeight;
        _popupY = e.ClientY - 8;

        _open = true;
    }

    private void Close() => _open = false;

    private void Decrement() => _modifier = Math.Max(-2, _modifier - 1);
    private void Increment() => _modifier = Math.Min(2, _modifier + 1);

    private async Task RollImmediate() => await OnRoll.InvokeAsync(0);

    private async Task ConfirmRoll()
    {
        _open = false;
        await OnRoll.InvokeAsync(_modifier);
    }
}
