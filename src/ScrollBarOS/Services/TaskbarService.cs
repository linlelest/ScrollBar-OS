using ScrollBarOS.Helpers;

namespace ScrollBarOS.Services;

/// <summary>
/// Service for hiding and restoring the Windows taskbar
/// </summary>
public class TaskbarService
{
    public bool IsHidden { get; private set; }

    /// <summary>
    /// Hides the system taskbar
    /// </summary>
    public void Hide()
    {
        if (IsHidden) return;

        Win32Helper.HideTaskbar();
        IsHidden = true;
    }

    /// <summary>
    /// Restores the system taskbar
    /// </summary>
    public void Restore()
    {
        if (!IsHidden) return;

        Win32Helper.ShowTaskbar();
        IsHidden = false;
    }

    /// <summary>
    /// Toggles taskbar visibility
    /// </summary>
    public void Toggle()
    {
        if (IsHidden)
            Restore();
        else
            Hide();
    }
}
