using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WpfLottery.ViewModels
{
    /// <summary>
    /// ViewModel for lottery control
    /// </summary>
    public partial class LotteryViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial string CurrentResult { get; set; } = "";

        [ObservableProperty]
        public partial bool IsRolling { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<string> Items { get; set; } = new();

        private CancellationTokenSource? _cancellationTokenSource;
        private readonly Random _random = new();

        /// <summary>
        /// Add item to lottery
        /// </summary>
        [RelayCommand]
        private void AddItem(string? item)
        {
            if (string.IsNullOrWhiteSpace(item))
                return;

            Items.Add(item.Trim());
        }

        /// <summary>
        /// Remove item from lottery
        /// </summary>
        [RelayCommand]
        private void RemoveItem(string? item)
        {
            if (item == null || !Items.Contains(item))
                return;

            Items.Remove(item);
        }

        /// <summary>
        /// Clear all items
        /// </summary>
        [RelayCommand]
        private void ClearItems()
        {
            Items.Clear();
            CurrentResult = "";
        }

        /// <summary>
        /// Start lottery rolling
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanStartRolling))]
        private async Task StartRollingAsync()
        {
            if (Items.Count == 0)
                return;

            IsRolling = true;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Roll for 2-3 seconds
                var rollDuration = TimeSpan.FromSeconds(2 + _random.NextDouble());
                var startTime = DateTime.Now;

                while (DateTime.Now - startTime < rollDuration && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    // Randomly select an item
                    var randomIndex = _random.Next(Items.Count);
                    CurrentResult = Items[randomIndex];

                    // Wait a bit before next update (faster at start, slower at end)
                    var elapsed = (DateTime.Now - startTime).TotalSeconds;
                    var delay = Math.Max(10, 100 - (int)(elapsed * 30)); // Slow down gradually
                    await Task.Delay(delay, _cancellationTokenSource.Token);
                }

                // Final result
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var finalIndex = _random.Next(Items.Count);
                    CurrentResult = Items[finalIndex];
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelled, do nothing
            }
            finally
            {
                IsRolling = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Stop lottery rolling
        /// </summary>
        [RelayCommand]
        private void StopRolling()
        {
            _cancellationTokenSource?.Cancel();
        }

        private bool CanStartRolling()
        {
            return !IsRolling && Items.Count > 0;
        }
    }
}

