using System;
using System.ComponentModel;
using System.Windows;
using QuickerActionManage.ViewModel;

namespace QuickerActionManage.View
{
    /// <summary>
    /// ActionManageWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ActionManageWindow : Window
    {
        public ActionManageWindow()
        {
            InitializeComponent();
            GSModel.PropertyChanged += GSModel_PropertyChanged;
            SubModel.PropertyChanged += SubModel_PropertyChanged;
        }

        private GlobalSubprogramListModel SubModel => TheSubProgramControl.ViewModel;
        private void SubModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(GlobalSubprogramListModel.RefSubprogamId):
                    GSModel.SelectById(SubModel.RefSubprogamId);
                    if (SubModel.RefSubprogamId != null)
                    {
                        TheActionManageControl.ViewModel.SelectedRule = null;
                        TheActionManageControl.ViewModel.DefaultRule = ActionRuleModel.Create();
                    }
                    break;
            }
        }

        private GlobalSubprogramListModel GSModel => TheActionManageControl.ViewModel.GSModel;

        private void GSModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(GSModel.SelectedItem):
                    SubModel.RefSubprogamId = GSModel.SelectedItem?.Id;
                    SubModel.FilterItem.SearchText = null;
                    TheActionManageControl.ViewModel.FilterItem.SearchText = null;
                    break;
            }
        }
    }
}

