namespace QuickerActionManage.ViewModel
{
    public class ActionRuleModel : NObject
    {
        public override string Summary => "";
        public string Title { get; set; } = "";
        public ActionRuleModel() { }

        public static ActionRuleModel Create() => new() { FilterItem = new(), Sorter = new() };
        public ActionItemFilter FilterItem { get; set; }
        public ActionItemSorter Sorter { get; set; }
    }
}

