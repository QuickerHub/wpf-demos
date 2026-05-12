using System.Collections.Generic;
using Quicker.Public.Entities;

namespace CeaViewRunner.ViewModels;

public class MessageBox3mdModel : MessageBoxMdModel
{
    public static MessageBox3mdModel Default { get; } = new()
    {
        MarkDown =
            """
            # Markdown 示例

            - 列表 1
            - 列表 2

            **粗体** *斜体*
            """,
        WindowParam = new(),
        CustomButtons = new List<CommonOperationItem>
        {
            new() { Title = "确认(_S)", Data = "Ok" },
            new() { Title = "取消(_C)", Data = "Cancel" },
        },
    };
}
