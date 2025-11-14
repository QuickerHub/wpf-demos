using System.ComponentModel.DataAnnotations;

namespace QuickerActionManage.ViewModel
{
    public enum ActionType1
    {
        [Display(Name = "所有动作")]
        All = 0,
        [Display(Name = "组合动作")]
        XAction = 0x1,
        [Display(Name = "基础动作")]
        BaseAction = 0x2,
    }
}

