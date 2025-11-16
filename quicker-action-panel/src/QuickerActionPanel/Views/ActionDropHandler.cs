using GongSolutions.Wpf.DragDrop;
using Quicker.Common;
using QuickerActionPanel.Constants;
using QuickerActionPanel.Extensions;
using System.Windows;

namespace QuickerActionPanel.Views
{
    /// <summary>
    /// Drop handler for Quicker action items using GongSolutions.Wpf.DragDrop
    /// </summary>
    public class ActionDropHandler : DefaultDropHandler
    {
        /// <summary>
        /// Check if the drop data contains a Quicker action
        /// </summary>
        protected bool CanAcceptQuickerAction(IDropInfo dropInfo)
        {
            return dropInfo.Data is DataObject dataObject && 
                   dataObject.GetDataPresent(QuickerDataFormats.ActionDragItem);
        }

        /// <summary>
        /// Extract Quicker action item from drop info
        /// </summary>
        protected ActionItem? ExtractQuickerAction(IDropInfo dropInfo)
        {
            if (dropInfo.Data is DataObject dataObject)
            {
                return dataObject.GetDragActionItem();
            }
            return null;
        }

        public override void DragOver(IDropInfo dropInfo)
        {
            if (CanAcceptQuickerAction(dropInfo))
            {
                dropInfo.Effects = DragDropEffects.Copy;
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            }
            else
            {
                dropInfo.Effects = DragDropEffects.None;
            }
        }

        public override void Drop(IDropInfo dropInfo)
        {
            var actionItem = ExtractQuickerAction(dropInfo);
            if (actionItem != null)
            {
                OnActionDropped(actionItem);
            }
        }

        /// <summary>
        /// Event raised when a Quicker action is dropped
        /// </summary>
        public event System.Action<ActionItem>? ActionDropped;

        /// <summary>
        /// Raises the ActionDropped event
        /// </summary>
        protected virtual void OnActionDropped(ActionItem actionItem)
        {
            ActionDropped?.Invoke(actionItem);
        }
    }
}

