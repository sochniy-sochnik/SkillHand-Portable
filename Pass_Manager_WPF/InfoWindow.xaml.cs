using SixLabors.ImageSharp.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace Pass_Manager_WPF
{
    /// <summary>
    /// Логика взаимодействия для InfoWindow.xaml
    /// </summary>
    public partial class InfoWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        public InfoWindow()
        {
            InitializeComponent();
            //this.ShowInTaskbar = false;
            IntPtr hWnd = GetForegroundWindow();
            GetWindowThreadProcessId(hWnd, out int processId);

            Process process = Process.GetProcessById(processId);
            var processName = process.ProcessName;
            var TempIcon = System.Drawing.Icon.ExtractAssociatedIcon(process.MainModule.FileName);
            ToImageSource(TempIcon);
            this.Topmost = true;
            this.Title = process.MainWindowTitle;
            //IconAdd(processName);
            System.Drawing.Size resolution = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;
            var heightWindow = resolution.Height;
            this.Top = heightWindow - 65;
        }

        public ImageSource ToImageSource(Icon icon)
        {
            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            this.Icon = imageSource;
            return imageSource;
        }

        private ImageSource GetApplicationIcon(Process process)
        {
            try
            {
                IntPtr hIcon = GetIconHandle(process.MainModule.FileName);
                if (hIcon != IntPtr.Zero)
                {
                    return Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
            }
            catch (Exception ex)
            {
                // Handle any exception that might occur while getting the icon
                Console.WriteLine("Error getting the icon: " + ex.Message);
            }

            return null;
        }

        private IntPtr GetIconHandle(string filePath)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            uint SHGFI_ICON = 0x000000100;
            uint SHGFI_SMALLICON = 0x000000001;
            SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);
            return shinfo.hIcon;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        void IconAdd(string processName)
        {
            Process process = Process.GetProcessesByName(processName).FirstOrDefault();
            if (process != null)
            {
                ImageSource iconSource = GetApplicationIcon(process);
                if (iconSource != null)
                {
                    this.Icon = iconSource;
                }
            }
        }

        private void CanselWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            //MainWindow.BringProcessWindowToFront(MainWindow.fakeProcessName);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow.BringProcessWindowToFront(MainWindow.fakeProcessName);
        }

        private void MinimizedWindow_Click(object sender, RoutedEventArgs e)
        {
            System.Drawing.Size resolution = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;
            var heightWindow = resolution.Height;
            this.Top = heightWindow - 65;
            this.Left = -705;
        }
    }
}
