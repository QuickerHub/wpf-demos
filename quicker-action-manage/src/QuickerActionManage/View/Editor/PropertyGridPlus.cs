using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Interactivity;
using HandyControl.Tools.Extension;
using QuickerActionManage.Utils;

namespace QuickerActionManage.View.Editor
{
    public class PropertyGridPlus : PropertyGrid
    {
        /// <summary>
        /// 必需要构造函数，不然某些用户电脑上没法运行
        /// </summary>
        public PropertyGridPlus() : base()
        {
            this.Background = Brushes.Transparent;
            CommandBindings.Add(new CommandBinding(ControlCommands.SortByCategory, SortByCategory, (s, e) => e.CanExecute = ShowSortButton));
        }

        public Visibility FilterBarVisibility
        {
            get { return (Visibility)GetValue(FilterBarVisibilityProperty); }
            set { SetValue(FilterBarVisibilityProperty, value); }
        }

        public static readonly DependencyProperty FilterBarVisibilityProperty = DependencyProperty.Register("FilterBarVisibility", typeof(Visibility), typeof(PropertyGridPlus), new FrameworkPropertyMetadata(Visibility.Collapsed, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool IsScrollable
        {
            get { return (bool)GetValue(IsScrollableProperty); }
            set { SetValue(IsScrollableProperty, value); }
        }

        public static readonly DependencyProperty IsScrollableProperty = DependencyProperty.Register(nameof(IsScrollable), typeof(bool), typeof(PropertyGridPlus), new PropertyMetadata(true));

        public bool Grouping
        {
            get { return (bool)GetValue(GroupingProperty); }
            set { SetValue(GroupingProperty, value); }
        }

        public static readonly DependencyProperty GroupingProperty =
            DependencyProperty.Register(nameof(Grouping), typeof(bool), typeof(PropertyGridPlus), new PropertyMetadata(false, new PropertyChangedCallback(GroupingChangedCallback)));

        private static void GroupingChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PropertyGridPlus gridPlus) return;
            gridPlus.DoGrouping((bool)e.NewValue);
        }

        private void DoGrouping(bool grouping)
        {
            if (grouping)
            {
                SortByCategory(null, null);
            }
            else
            {
                SortByName(null, null);
            }
        }

        private const string ElementItemsControl = "PART_ItemsControl";
        private const string ElementSearchBar = "PART_SearchBar";
        private ItemsControl _itemsControl;
        private ICollectionView _dataView;
        private SearchBar _searchBar;
        private string _searchKey;

        public override void OnApplyTemplate()
        {
            //base.OnApplyTemplate();

            if (_searchBar != null)
            {
                _searchBar.SearchStarted -= SearchBar_SearchStarted;
            }
            _searchBar = GetTemplateChild(ElementSearchBar) as SearchBar;
            if (_searchBar != null)
            {
                _searchBar.SearchStarted += SearchBar_SearchStarted;
            }

            _itemsControl = GetTemplateChild(ElementItemsControl) as ItemsControl;

            if (IsScrollable == false)
            {
                _itemsControl.Template = new ControlTemplate()
                {
                    TargetType = typeof(ItemsControl),
                    VisualTree = new FrameworkElementFactory(typeof(ItemsPresenter)),
                };
            }

            UpdateItems(SelectedObject);

            var filterBar = UIHelper.FindVisualParent<DockPanel>(_searchBar);
            filterBar.Children[0].Visibility = Visibility.Collapsed;
            BindingOperations.SetBinding(filterBar, VisibilityProperty, new Binding(nameof(FilterBarVisibility))
            {
                Source = this,
            });
        }

        internal void UpdateProperty(object obj)
        {
            PropertyGridAttribute prop = null;
            if (obj != null)
            {
                prop = obj.GetType().GetCustomAttributes(typeof(PropertyGridAttribute), false).FirstOrDefault() as PropertyGridAttribute;
            }
            prop?.SetGrid(this);
        }

        #region override source code
        protected override void OnSelectedObjectChanged(object oldValue, object newValue)
        {
            UpdateProperty(newValue);
            UpdateItems(newValue);
            RaiseEvent(new RoutedPropertyChangedEventArgs<object>(oldValue, newValue, SelectedObjectChangedEvent));
        }

        private void UpdateItems(object obj)
        {
            if (obj == null || _itemsControl == null) return;

            _dataView = CollectionViewSource.GetDefaultView(TypeDescriptor.GetProperties(obj.GetType()).OfType<PropertyDescriptor>()
                .Where(item => PropertyResolver.ResolveIsBrowsable(item))
                .Select(CreatePropertyItem)
                .Do(item => item.InitElement()));

            DoGrouping(Grouping);

            _itemsControl.ItemsSource = _dataView;
        }

        private void SortByCategory(object sender, ExecutedRoutedEventArgs e)
        {
            if (_dataView == null) return;

            using (_dataView.DeferRefresh())
            {
                _dataView.GroupDescriptions.Clear();
                _dataView.SortDescriptions.Clear();
                //_dataView.SortDescriptions.Add(new SortDescription(PropertyItem.CategoryProperty.Name, ListSortDirection.Ascending));
                //_dataView.SortDescriptions.Add(new SortDescription(PropertyItem.DisplayNameProperty.Name, ListSortDirection.Ascending));
                _dataView.GroupDescriptions.Add(new PropertyGroupDescription(PropertyItem.CategoryProperty.Name));
            }
        }

        private void SortByName(object sender, ExecutedRoutedEventArgs e)
        {
            if (_dataView == null) return;

            using (_dataView.DeferRefresh())
            {
                _dataView.GroupDescriptions.Clear();
                _dataView.SortDescriptions.Clear();
                //_dataView.SortDescriptions.Add(new SortDescription(PropertyItem.PropertyNameProperty.Name, ListSortDirection.Ascending));
            }
        }

        private void SearchBar_SearchStarted(object sender, FunctionEventArgs<string> e)
        {
            if (_dataView == null) return;

            _searchKey = e.Info;
            if (string.IsNullOrEmpty(_searchKey))
            {
                foreach (UIElement item in _dataView)
                {
                    item.Show();
                }
            }
            else
            {
                foreach (PropertyItem item in _dataView)
                {
                    item.Show(Utils.PinyinHelper.Search(_searchKey, item.Name, item.DisplayName));
                }
            }
        }
        #endregion

        public override PropertyResolver PropertyResolver { get; } = new PropertyResolverPlus();
        protected override PropertyItem CreatePropertyItem(PropertyDescriptor propertyDescriptor)
        {
            var item = base.CreatePropertyItem(propertyDescriptor);
            item.PropertyDescriptor = propertyDescriptor;
            SetEnableBinding(item);
            return item;
        }

        private void SetEnableBinding(PropertyItem item)
        {
            item.GetAttribute<PropertyBindingAttribute>()?.SetIsEnableBinding(item, SelectedObject);
        }
    }
}
