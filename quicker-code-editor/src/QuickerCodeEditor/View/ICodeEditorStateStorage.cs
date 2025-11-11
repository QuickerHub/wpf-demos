namespace QuickerCodeEditor.View
{
    /// <summary>
    /// Interface for custom state storage
    /// </summary>
    public interface ICodeEditorStateStorage
    {
        /// <summary>
        /// Load state
        /// </summary>
        CodeEditorState? Load();

        /// <summary>
        /// Save state
        /// </summary>
        void Save(CodeEditorState state);
    }
}

