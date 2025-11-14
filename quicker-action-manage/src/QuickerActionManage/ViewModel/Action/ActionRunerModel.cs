using System.Collections.Concurrent;
using System.ComponentModel;
using QuickerActionManage.Utils;
using QuickerActionManage.View.Editor;

namespace QuickerActionManage.ViewModel
{
    public class ActionRunerModel : NObject
    {
        public ActionRunerModel(string id) => Id = id;

        [Browsable(false)]
        public string Id { get; set; }

        [DisplayName("运行参数")]
        [TextPropertyEditor(MultiLines = true)]
        public string Param { get; set; } = "";

        internal static ConcurrentDictionary<string, ActionRunerModel> Runners { get; } = new();

        public override string Summary => $"quicker:runaction:{Id}?{Param}";

        public static ActionRunerModel GetRunner(string id)
        {
            return Runners.GetOrAdd(id, key => new ActionRunerModel(key));
        }
        public void Execute() => QuickerUtil.RunActionAndRecord(Id, Param);
        public void Debug() => QuickerUtil.DebugAction(Id, Param, false);
    }
}

