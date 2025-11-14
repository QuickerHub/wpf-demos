using System;
using System.ComponentModel;
using Quicker.Domain.Actions.X;
using QuickerActionManage.Utils.Extension;

namespace QuickerActionManage.ViewModel
{
    public class SubprogramModel : INotifyPropertyChanged
    {
        private readonly SubProgram _sub;

        public event PropertyChangedEventHandler? PropertyChanged;

        [Browsable(false)]
        public SubProgram Sub => _sub;

        public SubprogramModel() : this(new()) { }
        public SubprogramModel(SubProgram sub)
        {
            this._sub = sub;
        }

        [DisplayName("名称")]
        [ReadOnly(true)]
        public string Name { get => _sub.Name; set => _sub.Name = value; }

        [DisplayName("描述")]
        public string Description => _sub.Description;

        [DisplayName("子程序ID")]
        public string Id => _sub.Id;

        [DisplayName("创建时间")]
        public DateTime CreateTime => _sub.CreateTimeUtc.UtcToLocalTime();

        [DisplayName("编辑时间")]
        public DateTime? LastEditTime => _sub.LastEditTimeUtc?.UtcToLocalTime();

        [DisplayName("分享时间")]
        public DateTime? ShareTime => _sub.ShareTimeUtc?.UtcToLocalTime();

        [Browsable(false)]
        public string SharedId => _sub.SharedId;
    }
}

