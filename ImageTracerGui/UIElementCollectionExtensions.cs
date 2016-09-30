using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ImageTracerGui
{
    internal static class UIElementCollectionExtensions
    {
        public static void AddRange(this UIElementCollection collection, IEnumerable<UIElement> items)
        {
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }
    }
}
