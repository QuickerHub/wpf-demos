using System.ComponentModel.DataAnnotations;

namespace QuickerActionManage.ViewModel
{
    public enum SubprogramSortType
    {
        [Display(Name = "编辑时间")]
        LastEditTime,
        [Display(Name = "分享时间")]
        ShareTime,
        [Display(Name = "创建时间")]
        CreateTime,
        [Display(Name = "名称")]
        Name,
    }
}

