using Newtonsoft.Json;
using Quicker.Domain.Actions.X.BuiltinRunners.Misc;

namespace BatchRenameTool.State
{
    /// <summary>
    /// Global state writer using Quicker's ActionStateWriter
    /// </summary>
    public class GlobalStateWriter : StateWriter
    {
        public readonly string Id;
        
        public GlobalStateWriter(string id)
        {
            Id = id;
        }

        public override object? Read(string key, object? defaultValue = null)
        {
            (bool isSuccess, string data) = ActionStateWriter.ReadActionStateValue(Id, key);
            return isSuccess ? data : defaultValue;
        }

        public override bool Remove(string key)
        {
            ActionStateWriter.WriteActionState(Id, key, "*NULL*");
            return true;
        }

        public override void Write(string key, object? value)
        {
            if (value == null) return;
            string val = (value is string v) ? v : JsonConvert.SerializeObject(value);
            ActionStateWriter.WriteActionState(Id, key, val);
        }

        public override void Delete()
        {
            ActionStateWriter.DeleteStateFile(Id);
        }
    }
}
