using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using QuickerActionManage.State;
using QuickerActionManage.Utils;
using log4net;
using Newtonsoft.Json;
using Quicker.Domain.Actions.X;
using Quicker.Public.Extensions;
using Quicker.Utilities._3rd;

namespace QuickerActionManage.ViewModel
{
    public class GlobalSubprogramListModel : ListModel, IDisposable
    {
        protected override CollectionView GetView() => _view;

        public QuickerActionManage.Utils.FullyObservableCollection<SubprogramModel> Subprograms { get; set; } = new();

        private readonly CollectionView _view;
        private SmartCollection<SubProgram> Qsubs => QuickerUtil.GetGlobalSubprograms();
        public GlobalSubprogramListModel()
        {
            _view = (CollectionView)CollectionViewSource.GetDefaultView(Subprograms);

            Subprograms.Reset(Qsubs.Select(x => new SubprogramModel(x)));

            Qsubs.CollectionChanged += QSubprograms_CollectionChanged;

            PropertyChanged += GlobalSubprogramListModel_PropertyChanged;

            _view.Filter = obj =>
            {
                if (obj is SubprogramModel item)
                {
                    return FilterItem.Filter(item) && AdvanceFilter(item);
                }
                return false;
            };

            ReSort();

            FilterItem.PropertyChanged += (s, e) => Refresh();
            Sorter.PropertyChanged += (s, e) => ReSort();
        }
        private ILog _log = LogManager.GetLogger(typeof(GlobalSubprogramListModel));

        private void QSubprograms_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var old_map = Subprograms.ToDictionary(x => x.Id, x => x);

                if (e.OldItems != null)
                    Subprograms.RemoveItems(e.OldItems.Cast<SubProgram>().Select(x => old_map[x.Id]));
                if (e.NewItems != null)
                    Subprograms.AddRange(e.NewItems.Cast<SubProgram>().Select(x => new SubprogramModel(x)));
                _log.Info("collection reset");
            });
        }

        private void GlobalSubprogramListModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(RefSubprogamId):
                    if (!string.IsNullOrEmpty(RefSubprogamId))
                    {
                        _refIds = new(QuickerUtil.GetGlobalSubprograms()
                                                 .Where(x => JsonConvert.SerializeObject(x).Contains(RefSubprogamId))
                                                 .Select(x => x.Id));
                    }
                    else
                    {
                        _refIds = null;
                    }
                    Refresh();
                    return;
                default:
                    break;
            }
        }

        protected override IEnumerable<SortDescription> GetSortDescriptions() => Sorter.GetSortDescription();

        private HashSet<string>? _refIds;

        private bool AdvanceFilter(SubprogramModel item)
        {
            if (_refIds == null)
            {
                return true;
            }
            else
            {
                return _refIds.Contains(item.Id);
            }
        }

        public string? RefSubprogamId { get; set; }

        public SubprogramFilter FilterItem { get; set; } = new();

        public SubprogramSorter Sorter { get; set; } = new();

        public SubprogramModel? SelectedItem { get; set; }

        public void SelectById(string? id)
        {
            SelectedItem = Subprograms.FirstOrDefault(x => x.Id == id);
        }

        public void Dispose() => Qsubs.CollectionChanged -= QSubprograms_CollectionChanged;
    }
}

