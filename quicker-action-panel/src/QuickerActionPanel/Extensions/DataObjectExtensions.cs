using Quicker.Common;
using QuickerActionPanel.Constants;
using System.Windows;

namespace QuickerActionPanel.Extensions
{
    /// <summary>
    /// Extension methods for IDataObject to work with Quicker action drag data
    /// </summary>
    public static class DataObjectExtensions
    {
        /// <summary>
        /// Gets the drag action item from the data object
        /// </summary>
        /// <param name="data">The data object containing the drag data</param>
        /// <returns>The action item if present; otherwise, null</returns>
        public static ActionItem? GetDragActionItem(this IDataObject data)
        {
            if (data.GetDataPresent(QuickerDataFormats.ActionDragItem))
            {
                if (data.GetData(QuickerDataFormats.ActionDragItem) is ActionItemDragObject obj)
                {
                    return obj.Action;
                }
            }
            return null;
        }
    }
}
