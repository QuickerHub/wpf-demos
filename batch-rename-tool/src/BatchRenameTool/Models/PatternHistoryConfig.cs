using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BatchRenameTool.Models
{
    /// <summary>
    /// Configuration for pattern history storage
    /// </summary>
    public partial class PatternHistoryConfig : ObservableObject
    {
        private ObservableCollection<PatternHistoryItem> _patterns = new();

        public PatternHistoryConfig()
        {
            // Subscribe to collection changes to trigger PropertyChanged for auto-save
            _patterns.CollectionChanged += Patterns_CollectionChanged;
        }

        public ObservableCollection<PatternHistoryItem> Patterns
        {
            get => _patterns;
            set
            {
                if (_patterns != null)
                {
                    _patterns.CollectionChanged -= Patterns_CollectionChanged;
                }
                
                SetProperty(ref _patterns, value);
                
                if (_patterns != null)
                {
                    _patterns.CollectionChanged += Patterns_CollectionChanged;
                }
            }
        }

        private void Patterns_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Trigger PropertyChanged to notify ConfigService to save
            OnPropertyChanged(nameof(Patterns));
        }

        /// <summary>
        /// Add a pattern to history (avoid duplicates, keep most recent)
        /// </summary>
        public void AddPattern(string pattern, string? title = null)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return;

            // Remove existing pattern if it exists (to move it to the top)
            var existing = Patterns.FirstOrDefault(p => p.Pattern == pattern);
            if (existing != null)
            {
                Patterns.Remove(existing);
            }

            // Add new pattern at the beginning
            Patterns.Insert(0, new PatternHistoryItem
            {
                Pattern = pattern,
                Title = title ?? string.Empty,
                UsedAt = DateTime.Now
            });

            // Limit history size (keep last 50 patterns)
            while (Patterns.Count > 50)
            {
                Patterns.RemoveAt(Patterns.Count - 1);
            }
        }
    }
}
