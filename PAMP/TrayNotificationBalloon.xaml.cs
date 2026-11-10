using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PAMP;

public partial class TrayNotificationBalloon : UserControl
{
    public event Action? DismissRequested;
    public event Action? DontRemindRequested;
    public event Action? OpenAppRequested;

    public TrayNotificationBalloon()
    {
        InitializeComponent();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        DismissRequested?.Invoke();
    }

    private void BtnDontRemind_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        DontRemindRequested?.Invoke();
    }

    private void BtnGotIt_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        DismissRequested?.Invoke();
    }

    private void RootBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject depObj && FindVisualParent<Button>(depObj) != null)
        {
            return;
        }

        OpenAppRequested?.Invoke();
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject? parent = VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is T typedParent) return typedParent;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}
