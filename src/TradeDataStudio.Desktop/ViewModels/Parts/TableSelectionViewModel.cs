using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;
using TradeDataStudio.Desktop.Models;

namespace TradeDataStudio.Desktop.ViewModels.Parts;

/// <summary>
/// Manages table selection state and available output tables for export operations.
/// </summary>
public partial class TableSelectionViewModel : ObservableObject
{
    private readonly IConfigurationService _configurationService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private TableDefinition? _selectedOutputTable;

    [ObservableProperty]
    private bool _isTableSelectionPopupOpen = false;

    public ObservableCollection<SelectableTableDefinition> AvailableOutputTables { get; } = new();

    public event EventHandler? SelectionChanged;

    public TableSelectionViewModel(
        IConfigurationService configurationService,
        ILoggingService loggingService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));

        AvailableOutputTables.CollectionChanged += AvailableOutputTablesOnCollectionChanged;
    }

    /// <summary>
    /// Loads output tables for the specified mode, excluding main tables.
    /// </summary>
    public async Task LoadOutputTablesAsync(OperationMode mode)
    {
        try
        {
            await _loggingService.LogMainAsync($"Loading output tables for mode: {mode}");
            AvailableOutputTables.Clear();
            var tables = await _configurationService.GetTablesAsync(mode);
            
            // Filter out main table - only show Table1 and Table2 (or similar output tables)
            var outputTables = tables.Where(t => !t.Name.Contains("MAIN", StringComparison.OrdinalIgnoreCase)).ToList();
            
            foreach (var table in outputTables)
            {
                AvailableOutputTables.Add(new SelectableTableDefinition(table));
            }
            
            await _loggingService.LogMainAsync($"Loaded {outputTables.Count} output tables (excluded main table)");
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to load output tables", ex);
        }
    }

    /// <summary>
    /// Filters output tables based on the selected stored procedure's output tables.
    /// </summary>
    public async Task FilterOutputTablesForSelectedProcedureAsync(StoredProcedureDefinition? selectedProcedure, OperationMode currentMode)
    {
        try
        {
            Console.WriteLine($"\n=== FilterOutputTablesForSelectedProcedureAsync STARTED ===");
            Console.WriteLine($"  Selected Procedure: {selectedProcedure?.DisplayName ?? "NULL"}");
            Console.WriteLine($"  Current Mode: {currentMode}");
            Console.WriteLine($"  Current AvailableOutputTables.Count BEFORE clear: {AvailableOutputTables.Count}");
            
            if (selectedProcedure == null)
            {
                Console.WriteLine("  ❌ Selected procedure is null - clearing tables");
                AvailableOutputTables.Clear();
                await _loggingService.LogMainAsync("FilterOutputTables: Cleared tables (no procedure selected)");
                return;
            }

            // Clear current tables first
            AvailableOutputTables.Clear();
            Console.WriteLine($"  ✅ Cleared AvailableOutputTables");

            // Load all available tables from configuration
            Console.WriteLine($"  📥 Loading all tables for mode: {currentMode}...");
            var allTables = await _configurationService.GetTablesAsync(currentMode);
            Console.WriteLine($"  📥 Loaded {allTables.Count} total tables from configuration");
            
            // Log all available tables
            foreach (var t in allTables)
            {
                Console.WriteLine($"    - Available table: {t.Name}");
            }
            
            // Check if procedure has output tables defined
            Console.WriteLine($"  🔍 Checking procedure.OutputTables...");
            Console.WriteLine($"    OutputTables is null: {selectedProcedure.OutputTables == null}");
            Console.WriteLine($"    OutputTables count: {selectedProcedure.OutputTables?.Count ?? 0}");
            
            // Only add tables that are associated with the selected procedure
            if (selectedProcedure.OutputTables?.Any() == true)
            {
                Console.WriteLine($"  ✅ Procedure has {selectedProcedure.OutputTables.Count} output tables defined");
                
                // Use a HashSet to track which tables we've already added (defensive programming)
                var addedTableNames = new HashSet<string>();
                
                foreach (var tableName in selectedProcedure.OutputTables)
                {
                    Console.WriteLine($"    🔍 Looking for table: '{tableName}'");
                    
                    // Skip if already added (prevent duplicates)
                    if (addedTableNames.Contains(tableName))
                    {
                        Console.WriteLine($"      ⚠ SKIPPED - Table '{tableName}' already added");
                        continue;
                    }
                    
                    var table = allTables.FirstOrDefault(t => t.Name == tableName);
                    if (table != null)
                    {
                        Console.WriteLine($"      ✅ FOUND - Adding '{table.Name}' to AvailableOutputTables");
                        AvailableOutputTables.Add(new SelectableTableDefinition(table));
                        addedTableNames.Add(tableName);
                        await _loggingService.LogMainAsync($"Added output table: {table.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"      ❌ NOT FOUND - Table '{tableName}' not in configuration");
                        await _loggingService.LogMainAsync($"Warning: Output table '{tableName}' not found in configuration");
                    }
                }
                
                Console.WriteLine($"  📊 Final AvailableOutputTables.Count: {AvailableOutputTables.Count}");
                await _loggingService.LogMainAsync($"FilterOutputTables: Added {AvailableOutputTables.Count} tables for procedure {selectedProcedure.DisplayName}");
            }
            else
            {
                Console.WriteLine($"  ⚠ No output tables defined for this procedure");
                await _loggingService.LogMainAsync($"FilterOutputTables: No output tables defined for {selectedProcedure.DisplayName}");
            }
            
            Console.WriteLine($"=== FilterOutputTablesForSelectedProcedureAsync COMPLETED ===\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR in FilterOutputTablesForSelectedProcedureAsync: {ex.Message}");
            Console.WriteLine($"   StackTrace: {ex.StackTrace}");
            await _loggingService.LogErrorAsync("Failed to filter output tables", ex);
        }
    }

    /// <summary>
    /// Gets all currently selected tables.
    /// </summary>
    public List<TableDefinition> GetSelectedTables()
    {
        return AvailableOutputTables.Where(t => t.IsSelected).Select(t => t.Table).ToList();
    }

    /// <summary>
    /// Deselects all tables.
    /// </summary>
    public void ClearSelection()
    {
        foreach (var table in AvailableOutputTables)
        {
            table.IsSelected = false;
        }
    }

    /// <summary>
    /// Auto-selects all available output tables.
    /// </summary>
    public void SelectAllTables()
    {
        foreach (var table in AvailableOutputTables)
        {
            table.IsSelected = true;
        }
    }

    private void AvailableOutputTablesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Console.WriteLine($"\n*** AvailableOutputTables Collection Changed ***");
        Console.WriteLine($"    Action: {e.Action}");
        Console.WriteLine($"    Current Count: {AvailableOutputTables.Count}");
        
        if (e.OldItems != null)
        {
            Console.WriteLine($"    Removed {e.OldItems.Count} items:");
            foreach (SelectableTableDefinition table in e.OldItems)
            {
                Console.WriteLine($"      - {table.DisplayName}");
                table.PropertyChanged -= TableOnPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            Console.WriteLine($"    Added {e.NewItems.Count} items:");
            foreach (SelectableTableDefinition table in e.NewItems)
            {
                Console.WriteLine($"      + {table.DisplayName}");
                table.PropertyChanged += TableOnPropertyChanged;
            }
        }

        Console.WriteLine($"    Invoking SelectionChanged event...");
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"*** Collection Changed Event Complete ***\n");
    }

    private void TableOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableTableDefinition.IsSelected))
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
