using System.Diagnostics;
using Spire.Doc;
using Spire.Doc.Documents;
using Spire.Doc.Fields;
using Spire.Doc.Fields.OMath;

public class Flow
{
    public void Start()
    {
        // 创建一个新的文档
        var doc = new Document();
        Section section = doc.AddSection();

        var latex_list = File.ReadAllLines(@"D:\work\研究生\202308综述\survey\survey.tex")
            .Select(x => x.Trim())
            .Where(x => !x.StartsWith("%") && !string.IsNullOrWhiteSpace(x))
            .Where(x => !x.StartsWith("\\usepackage"))
            .Take(30)
            ;

        var latex = string.Join(Environment.NewLine, latex_list);


        // 在文档中添加文本
        Paragraph paragraph = section.AddParagraph();

        TextRange text = paragraph.AppendText(latex);
        text.CharacterFormat.FontSize = (float)10.5; //五号字体
        text.CharacterFormat.FontName = "宋体";
        // 设置文档为双列布局
        section.AddColumn(100f, 15f);
        section.PageSetup.ColumnsLineBetween = false;

        var formula =

        """
x^{2}+\sqrt{x^{2}+1}=2
""";

        //\mathbf{p}= \bar{\mathbf{p}} + \mathbf{A}_{\textrm{id}}\bm{\alpha}_{\textrm{id}}

        OfficeMath officeMath = new(doc);
        officeMath.FromLatexMathCode(formula);
        //Add the math equation to the section
        paragraph = section.AddParagraph();
        paragraph.Items.Add(officeMath);
        section.AddParagraph();


        // 保存文档
        var savePath = @"D:\桌面\word\test.docx";
        doc.SaveToFile(savePath);

        Process.Start(savePath);

        doc.Dispose();

        Console.WriteLine("Word文档创建和操作完成！");
    }
}