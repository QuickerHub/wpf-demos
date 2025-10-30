using System.Diagnostics;
using Spire.Doc.Documents;
using Spire.Doc;
using Spire.Doc.Fields;
using System.Text.RegularExpressions;
using System.Drawing;

var dir = @"D:\work\研究生\202308综述\survey";
var file = Path.Combine(dir, "body.docx");
Document doc = new();
doc.LoadFromFile(file);

// 获取文档中的所有段落
foreach (Section section in doc.Sections)
{
    foreach (Paragraph paragraph in section.Paragraphs)
    {
        // 检查段落中是否包含标题
        if (paragraph.Text.StartsWith("Figure")) //处理图片caption
        {
            // 修改标题文本
            var text1 = paragraph.Text.Substring("Figure 1: ".Length);
            var index = Regex.Match(text1, @"\d+").Value;
            paragraph.Text = text1.Replace("@@", $"Fig {index} ");
        }
        else if (paragraph.Text.StartsWith("Table")) //处理表格 caption
        {
            var text1 = paragraph.Text.Substring("Table 1: ".Length);
            var index = Regex.Match(paragraph.Text, @"\d+").Value;
            paragraph.Text = $"表 {index} " + text1.Replace("@@", $"Table {index} ");
        }
        else if (paragraph.StyleName == "Heading1" && paragraph.Text.StartsWith("Reference"))
        {
            paragraph.ListFormat.RemoveList();
            paragraph.Text = "";
            void append(string text, bool unbold = false, double fontsize = 10.5)
            {
                var tr = paragraph.AppendText(text);
                tr.CharacterFormat.FontName = "宋体";
                tr.CharacterFormat.FontSize = (float)fontsize;
                if (unbold) tr.CharacterFormat.Bold = false;
            }
            append("参考文献(");
            append("Reference", true);
            append(")");
            //append("\n", false, 8);
        }
        else if (paragraph.Text.StartsWith("引言"))
        {
            paragraph.ApplyStyle("Heading1");
            paragraph.Text = "0  引  言";
        }
        else if (paragraph.Text.Contains(@"@red@"))
        {
            var separator = "@red@";
            var text = paragraph.Text;
            paragraph.Text = "";
            bool set = false;
            foreach (var item in text.Split(new[] { separator }, StringSplitOptions.None))
            {
                TextRange textRange = paragraph.AppendText(item);
                if (set)
                {
                    textRange.CharacterFormat.TextColor = Color.Red;
                }
                set ^= true;
            }
        }
    }
    //Console.WriteLine(section.Columns.Count);
}

//TODO 将相应的 paragraph 独立为一个 section，然后设置 section 的列数为 1

int idx = 0;
int[] toadjust = new[] { 1, 3, 5, 9 };
////1,2,5,6,7,9
////3,4,8

foreach (Section section in doc.Sections)
{
    foreach (Paragraph paragraph in section.Paragraphs)
    {
        // 检查段落是否包含图片
        foreach (DocPicture picture in paragraph.ChildObjects.OfType<DocPicture>())
        {
            idx++;
            if (!toadjust.Contains(idx))
            {
                picture.TextWrappingStyle = TextWrappingStyle.Inline;
                continue;
            }

            float columnWidth = section.Columns[0].Width;
            // 设置图片的宽度为当前列的宽度，保持宽高比例不变
            picture.Height *= (columnWidth / picture.Width);
            picture.Width = columnWidth;
        }
        //Console.WriteLine(paragraph.Text.Substring(0, Math.Min(paragraph.Text.Length, 10)));
    }
}

idx = 1; //先暂时不处理表格了

//控制表格中单元格的合成
//参考：https://www.e-iceblue.com/Knowledgebase/Spire.Doc/Spire.Doc-Program-Guide/Table.html
foreach (Section setion in doc.Sections)
{
    foreach (Table table in setion.Tables)
    {
        if (idx == 1)
        {
            var col = new[] { 4,5 };
            var rowh = new[] { 1, 3, 7, 6 };

            foreach (var colidx in col)
            {
                var rowidx = 0;
                foreach (var h in rowh)
                {
                    table.ApplyVerticalMerge(colidx, rowidx + 1, rowidx + h);
                    rowidx += h;
                }
            }

            table.SetColumnWidth(0, 20, CellWidthType.Auto);
            table.SetColumnWidth(3, 20, CellWidthType.Auto);
            table.SetColumnWidth(4, 20, CellWidthType.Auto);
        }
        else if (idx == 2)
        {

        }

        idx++;
    }
}


var last_setup = doc.Sections[0].PageSetup;

//添加一个空白页面
var first_sec = new Section(doc);
doc.Sections.Insert(0, first_sec);
var first_par = first_sec.AddParagraph();
first_par.AppendBreak(BreakType.PageBreak);

first_sec.PageSetup.PageSize = last_setup.PageSize;
first_sec.PageSetup.Margins = last_setup.Margins;

//Document docToAppend = new();
//docToAppend.LoadFromFile(Path.Combine(dir, "about-anthor.docx")); // 替换成你的第二个文档路径

//foreach (Section section in docToAppend.Sections)
//{
//    var new_section = section.Clone();
//    foreach (Paragraph paragraph in new_section.Paragraphs)
//    {
//        //paragraph.ApplyStyle("BodyText");
//        Console.WriteLine(paragraph.StyleName);
//    }
//}
//doc.InsertTextFromFile(Path.Combine(dir, @"word\about-anthor.docx"), FileFormat.Docx);

doc.SaveToFile(file);

//Console.ReadKey();
Process.Start(Path.Combine(dir, "body.docx"));
