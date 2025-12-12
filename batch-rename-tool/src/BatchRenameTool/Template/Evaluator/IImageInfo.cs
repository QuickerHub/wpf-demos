namespace BatchRenameTool.Template.Evaluator;

/// <summary>
/// Interface for image information (lazy loaded)
/// </summary>
public interface IImageInfo
{
    int Width { get; }
    int Height { get; }
}
