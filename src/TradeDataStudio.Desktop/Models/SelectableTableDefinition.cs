using System.ComponentModel;
using System.Runtime.CompilerServices;
using TradeDataStudio.Core.Models;

namespace TradeDataStudio.Desktop.Models
{
    public class SelectableTableDefinition : INotifyPropertyChanged
    {
        private bool _isSelected = false;
        
        public SelectableTableDefinition(TableDefinition table)
        {
            Table = table;
            Name = table.Name;
            DisplayName = table.DisplayName;
            Description = table.Description;
        }
        
        public TableDefinition Table { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public string Description { get; }
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}