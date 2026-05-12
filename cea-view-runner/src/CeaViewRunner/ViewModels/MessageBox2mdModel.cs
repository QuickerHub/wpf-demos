using System.Collections.Generic;
using Quicker.Public.Entities;

namespace CeaViewRunner.ViewModels;

public class MessageBox2mdModel : MessageBoxMdModel
{
    public string Icon { get; set; } = "Quicker.ico";

    public string Title { get; set; } = "";

    public static MessageBox2mdModel Default { get; } = new()
    {
        MarkDown = "### 预览\n\n内容区域。",
        WindowParam = new(),
        CustomButtons = new List<CommonOperationItem>
        {
            new() { Title = "确认(_S)", Data = "Ok" },
            new() { Title = "取消(_C)", Data = "Cancel" },
        },
    };
}
