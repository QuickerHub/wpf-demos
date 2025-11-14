namespace QuickerActionManage.State
{
    /// <summary>
    /// Abstract base class for state writers
    /// </summary>
    public abstract class StateWriter
    {
        public abstract void Write(string key, object? value);
        public abstract object? Read(string key, object? defaultValue = null);
        public abstract bool Remove(string key);
        public abstract void Delete();
    }
}

