using System.Collections.Generic;
using Quicker.Public.Entities;

namespace CeaViewRunner.ViewModels;

public class MessageBoxMdModel : CustomWindowModel
{
    public string MarkDown { get; set; } = "";

    public MdWindowParamClass? WindowParam { get; set; }

    public IList<CommonOperationItem>? CustomButtons { get; set; }

    public static readonly List<CommonOperationItem> OkCancelButtons = new()
    {
        new() { Title = "确认(_S)", Data = "Ok" },
        new() { Title = "取消(_C)", Data = "Cancel" },
    };

    public bool ShowDoNotRemind { get; set; }

    public bool DoNotRemind { get; set; }
}
