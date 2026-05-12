namespace CeaViewRunner.ViewModels;

public abstract class CustomWindowModel : NObject
{
    /// <summary>
    /// Value returned after the window is closed.
    /// </summary>
    public string Result { get; set; } = "";
}
