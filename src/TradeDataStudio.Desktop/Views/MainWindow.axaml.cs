using Avalonia.Controls;
using System;
using TradeDataStudio.Desktop.ViewModels;

namespace TradeDataStudio.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        if (viewModel == null)
            throw new ArgumentNullException(nameof(viewModel));
            
        DataContext = viewModel;
    }
}