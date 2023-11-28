using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Pass_Manager_WPF
{
    internal class ChangeIconSync
    {
        public static async Task ChangeOnWrongWindow(MainWindow mainWindow)
        {
            Task.Delay(4000).Wait();
            mainWindow.Dispatcher.Invoke(() =>
            {
                mainWindow.Icon = new BitmapImage(new Uri("pack://application:,,,/icons/Visual_Studio_Icon_2022.ico"));
            });
        }
    }
}
