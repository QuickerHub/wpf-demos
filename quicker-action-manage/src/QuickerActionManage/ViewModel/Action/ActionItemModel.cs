using System;
using System.ComponentModel;
using Quicker.Common;
using QuickerActionManage.Utils;
using QuickerActionManage.Utils.Extension;

namespace QuickerActionManage.ViewModel
{
    public class ActionItemModel
    {
        private readonly ActionItem _item;

        public ActionItemModel() : this(new ActionItem()) { }

        public ActionItemModel(ActionItem item)
        {
            this._item = item;
            Profile = QuickerUtil.GetActioinProfileById(Id);
        }

        [Browsable(false)]
        public ActionItem Item => _item;

        [DisplayName("图标")]
        [ReadOnly(true)]
        public string Icon { get => _item.Icon; set => _item.Icon = value; }

        [DisplayName("名称")]
        [ReadOnly(true)]
        public string Title { get => _item.Title; set => _item.Title = value; }

        [DisplayName("描述")]
        public string? Description => _item.Description;

        [DisplayName("动作类型")]
        public ActionType1 ActionType => _item.ActionType switch
        {
            Quicker.Common.ActionType.XAction => ActionType1.XAction,
            _ => ActionType1.BaseAction,
        };

        [DisplayName("动作ID")]
        public string Id => _item.Id;

        [DisplayName("动作模板ID")]
        public string TemplateId => _item.TemplateId;

        [DisplayName("分享动作ID")]
        public string SharedActionId => _item.SharedActionId;

        [DisplayName("动作网址")]
        public string SharedActionUrl
        {
            get
            {
                var id = string.IsNullOrEmpty(TemplateId) ? SharedActionId : TemplateId;
                return string.IsNullOrEmpty(id) ? "" : "https://getquicker.net/Sharedaction?code=" + id;
            }
        }

        [Browsable(false)]
        public bool Shared => !string.IsNullOrEmpty(SharedActionId);

        [Browsable(false)]
        public int Size => _item.Data?.Length ?? 0;

        [DisplayName("大小 (kb)")]
        public double SizeKb => Math.Round(Size / 1024.0, 2);

        [DisplayName("动作行数"), Description("动作中使用的模块的个数")]
        public int Lines => TextUtil.FindCount(_item.Data, "\"StepRunnerKey\"");

        [DisplayName("自动更新")]
        public bool AutoUpdate => _item.AutoUpdate;

        [Browsable(false)]
        public ActionProfile Profile { get; }

        [DisplayName("进程名")]
        public string ExeName => Profile.ExeDisplayName;

        [DisplayName("编辑时间")]
        public DateTime? LastEditTime => _item.LastEditTimeUtc?.UtcToLocalTime();

        [DisplayName("分享时间")]
        public DateTime? ShareTime => _item.ShareTimeUtc?.UtcToLocalTime();

        [DisplayName("创建时间")]
        public DateTime? CreateTime => _item.CreateTimeUtc?.UtcToLocalTime();

        [DisplayName("关联网址")]
        public string? AssUrl => _item.Association?.UrlPattern;

        [Browsable(false)]
        public bool IsAssUrl => !string.IsNullOrWhiteSpace(AssUrl);

        internal void Edit() => QuickerUtil.EditAction(Id);

        [Browsable(false)]
        public ActionRunerModel Runner => ActionRunerModel.GetRunner(Id);

        [DisplayName("使用次数")]
        public int UsageCount { get; set; }
    }
}

