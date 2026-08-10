using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gogame.Views
{
    public static class Navigator
    {
        public static ContentControl? RootContent { get; set; }

        public static void NavigateTo(Control view)
        {
            if (RootContent != null)
                RootContent.Content = view;
        }
    }
}
