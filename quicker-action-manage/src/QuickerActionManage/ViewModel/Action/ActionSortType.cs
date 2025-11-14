using System.ComponentModel.DataAnnotations;

namespace QuickerActionManage.ViewModel
{
    public enum ActionSortType
    {
        [Display(Name = "编辑时间")]
        LastEditTime,
        [Display(Name = "分享时间")]
        ShareTime,
        [Display(Name = "动作名称")]
        Title,
        [Display(Name = "创建时间")]
        CreateTime,
        [Display(Name = "动作大小")]
        Size,
        [Display(Name = "使用次数")]
        UsageCount,
        [Display(Name = "进程")]
        ExeName,
        [Display(Name = "描述")]
        Description,
        [Display(Name = "默认")]
        None,
    }
}

