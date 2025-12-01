using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace TradeDataStudio.Desktop.Services;

/// <summary>
/// Service for displaying user dialogs and prompts.
/// </summary>
public class DialogService
{
    /// <summary>
    /// Shows a dialog asking user to choose between "Generate Report" or "Skip" for zero-record tables.
    /// </summary>
    /// <param name="tableName">Name of the table with zero records</param>
    /// <returns>True if user wants to generate report, False if user wants to skip</returns>
    public async Task<bool> ShowZeroRecordPromptAsync(string tableName)
    {
        var window = GetMainWindow();
        if (window == null)
            return false; // Default to skip if window not available

        var dialog = new Window
        {
            Title = "Zero Records Detected",
            Width = 450,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#052633"))
        };

        var result = false;
        var stackPanel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 15,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#052633"))
        };

        stackPanel.Children.Add(new TextBlock
        {
            Text = $"Selected table '{tableName}' has zero records.",
            FontSize = 14,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            Margin = new Thickness(0, 0, 0, 10),
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E2E8F0"))
        });

        stackPanel.Children.Add(new TextBlock
        {
            Text = "How would you like to proceed?",
            FontSize = 13,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#94A3B8"))
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0)
        };

        var generateButton = new Button
        {
            Content = "Generate Report",
            Width = 140,
            Height = 35,
            Padding = new Thickness(15, 8),
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2010B981")),
            BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981")),
            BorderThickness = new Thickness(1),
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E2E8F0"))
        };
        generateButton.Click += (s, e) =>
        {
            result = true;
            dialog.Close();
        };

        var skipButton = new Button
        {
            Content = "Skip",
            Width = 140,
            Height = 35,
            Padding = new Thickness(15, 8),
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#20FBBF24")),
            BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FBBF24")),
            BorderThickness = new Thickness(1),
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"))
        };
        skipButton.Click += (s, e) =>
        {
            result = false;
            dialog.Close();
        };

        buttonPanel.Children.Add(generateButton);
        buttonPanel.Children.Add(skipButton);
        stackPanel.Children.Add(buttonPanel);

        dialog.Content = stackPanel;

        await dialog.ShowDialog(window);
        return result;
    }

    private Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }
}
