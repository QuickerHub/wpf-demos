using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickerActionManage.ViewModel
{
    public partial class ActionRuleModel : NObject
    {
        public override string Summary => "";
        
        [ObservableProperty]
        public partial string Title { get; set; } = "";

        public ActionRuleModel() { }

        public static ActionRuleModel Create() => new() { FilterItem = new(), Sorter = new() };
        
        public ActionItemFilter FilterItem { get; set; } = new();
        public ActionItemSorter Sorter { get; set; } = new();
    }
}

