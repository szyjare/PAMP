using System.Windows;

namespace PAMP;

public static class WindowManager
{
    public static bool ActivateExisting<T>() where T : Window
    {
        var existing = Application.Current.Windows.OfType<T>().FirstOrDefault();
        if (existing != null)
        {
            if (!existing.IsVisible)
            {
                existing.Show();
            }
            if (existing.WindowState == WindowState.Minimized)
            {
                existing.WindowState = WindowState.Normal;
            }
            existing.Activate();
            existing.Focus();
            return true;
        }
        return false;
    }

    public static T ShowSingle<T>(Func<T> factory, Window? owner = null) where T : Window
    {
        var existing = Application.Current.Windows.OfType<T>().FirstOrDefault();
        if (existing != null)
        {
            if (!existing.IsVisible)
            {
                existing.Show();
            }
            if (existing.WindowState == WindowState.Minimized)
            {
                existing.WindowState = WindowState.Normal;
            }
            existing.Activate();
            existing.Focus();
            return existing;
        }

        var window = factory();
        if (owner != null && owner.IsVisible)
        {
            try { window.Owner = owner; } catch { }
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        window.Show();
        window.Activate();
        window.Focus();
        return window;
    }
}
