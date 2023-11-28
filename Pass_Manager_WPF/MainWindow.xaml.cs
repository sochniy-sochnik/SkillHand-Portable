using InputSimulatorStandard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MessageBox = System.Windows.MessageBox;

namespace Pass_Manager_WPF
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        public static string nameActiveWindow = "";

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int VK_NUM5 = 0x65;
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12; // VK_MENU is the virtual key code for the Alt key

        private static IntPtr hookId = IntPtr.Zero;
        private static bool isCapturing = false;
        private static List<string> digits = new List<string>();
        private static string predefinedValue = "12345"; // Replace with your desired sequence of digits
        private LowLevelKeyboardProc keyboardHookProc;
        public static int dopRemoveInt = 0;
        public static string[] mas = new string[3000];
        public static int indexWriteInMas = 0;
        public static bool resultOnFinish = false;
        public static bool writePreStart = false;
        public static bool writeStart = false;

        private static string enteredValue = "";

        public static CancellationTokenSource ctsPub = new CancellationTokenSource();

        public static string fakeProcessName = "";

        [DllImport("Kernel32.dll")]
        static extern bool SetPriorityClass(IntPtr hProcess, int dwPriorityClass);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SwHide = 0;

        // Импортируем функцию из Shell32.dll для изменения свойств окна
        [DllImport("Shell32.dll")]
        private static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        [DllImport("user32.dll")]
        private static extern int SetWindowLongPtr(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        public ImageSource ToImageSource(Icon icon)
        {
            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            this.Icon = imageSource;
            return imageSource;
        }

        public MainWindow()
        {
            Thread.Sleep(2000);
            //this.WindowState = WindowState.Minimized;
            SetPriorityClass(Process.GetCurrentProcess().Handle, 0x00000100);
            CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
            CancellationToken token = cancelTokenSource.Token;
            ctsPub = cancelTokenSource;
            // AutoMinimized(ctsPub);
            InitializeComponent();
            keyboardHookProc = KeyboardHookCallback;
            Loaded += MainWindow_Loaded;

            // подделка иконки и заголовка
            IntPtr hWnd = GetForegroundWindow();
            GetWindowThreadProcessId(hWnd, out int processId);

            Process process = Process.GetProcessById(processId);
            var processName = process.ProcessName;
            oldStaticProcessName = processName;
            fakeProcessName = processName;
            try
            {
                var TempIcon = System.Drawing.Icon.ExtractAssociatedIcon(process.MainModule.FileName);
                ToImageSource(TempIcon);
                this.Topmost = true;
                this.Title = process.MainWindowTitle;
            }
            catch
            {

            }

            // скрываем оригинальную иконку с панели задач
            //HideTaskbarIcon(processName);
            HideTaskbarIcon2(processName);
        }

        private static void HideTaskbarIcon(string processN)
        {
            // Имя процесса программы, иконку которой нужно скрыть
            string processName = processN;

            // Получаем список всех процессов с указанным именем
            Process[] processes = Process.GetProcessesByName(processName);

            // Ищем главное окно для каждого процесса
            foreach (var process in processes)
            {
                IntPtr windowHandle = FindWindow(null, process.MainWindowTitle);

                // Скрываем окно программы
                if (windowHandle != IntPtr.Zero)
                {
                    ShowWindow(windowHandle, SwHide);
                    // Изменяем свойство окна, чтобы обновить панель задач
                    SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
                }
            }
        }

        static int oldExStyle = 0;
        static string oldStaticProcessName = "";
        private void HideTaskbarIcon2(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            foreach (Process process in processes)
            {
                IntPtr hWnd = process.MainWindowHandle;
                if (hWnd != IntPtr.Zero)
                {
                    int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                    oldExStyle = exStyle;
                    exStyle &= ~(WS_EX_APPWINDOW);
                    exStyle |= WS_EX_TOOLWINDOW;
                    SetWindowLongPtr(hWnd, GWL_EXSTYLE, exStyle);
                }
            }
        }

        private void UnHideTaskbarIcon2(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            foreach (Process process in processes)
            {
                IntPtr hWnd = process.MainWindowHandle;
                if (hWnd != IntPtr.Zero)
                {
                    int exStyle = oldExStyle;
                    SetWindowLongPtr(hWnd, GWL_EXSTYLE, exStyle);
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Register the keyboard hook when the window is loaded
            hookId = SetHook(keyboardHookProc);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.NumPad0)
                {
                    keybd_event(NumLock, 0, KEYEVENTF_KEYDOWN, 0);
                    keybd_event(NumLock, 0, KEYEVENTF_KEYUP, 0);
                    Thread.Sleep(50);
                    UnhookWindowsHookEx(hookId);
                    UnHideTaskbarIcon2(oldStaticProcessName);
                    Thread.Sleep(50);
                    Environment.Exit(0);
                }

                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.Insert)
                    {
                        ctsPub.Cancel();
                        Thread.Sleep(1000);
                        ctsPub.Dispose();
                        this.ShowInTaskbar = false;
                        InfoWindow infoWindow = new InfoWindow();
                        infoWindow.ShowDialog();
                        this.ShowInTaskbar = true;

                        CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
                        CancellationToken token = cancelTokenSource.Token;
                        ctsPub = cancelTokenSource;
                        _ = AutoMinimized(ctsPub);
                    }
                }

                // Check for Ctrl+Alt+P key combination to start capturing
                if (Keyboard.IsKeyDown(Key.NumPad1))
                {
                    if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.NumPad2)
                    {
                        keybd_event(BACK, 0, KEYEVENTF_KEYDOWN, 0);
                        keybd_event(BACK, 0, KEYEVENTF_KEYUP, 0);
                        Thread.Sleep(200);
                        keybd_event(BACK, 0, KEYEVENTF_KEYDOWN, 0);
                        keybd_event(BACK, 0, KEYEVENTF_KEYUP, 0);
                        Thread.Sleep(200);
                        keybd_event(BACK, 0, KEYEVENTF_KEYDOWN, 0);
                        keybd_event(BACK, 0, KEYEVENTF_KEYUP, 0);
                        digits.Clear();
                        indexWriteInMas = 0;
                        for (int i = 0; i < mas.Length; i++)
                        {
                            if (mas[i] == "")
                            {
                                break;
                            }
                            mas[i] = "";
                        }
                        isCapturing = true;
                        // TEST
                        IntPtr hWnd = GetForegroundWindow();
                        GetWindowThreadProcessId(hWnd, out int processId);

                        Process process = Process.GetProcessById(processId);
                        string processName = process.ProcessName;
                        nameActiveWindow = processName;
                        // TEST



                        if (!System.Windows.Clipboard.ContainsImage())
                        {
                            ClipboardHelper.ClearClipboard();
                        }
                    }
                }


                if (isCapturing && vkCode != 99)
                {
                    Thread thread = new Thread(LanguageCheck);
                    thread.Start();
                    thread.Join();
                    thread.Abort();
                    string currentLanguage = ThreadLanguage;

                    //InputLanguage myCurrentLanguage = InputLanguage.CurrentInputLanguage;
                    /*if (myCurrentLanguage.LayoutName == "США" || myCurrentLanguage.LayoutName == "USA")
                    {
                        currentLanguage = "USA";
                    }
                    else if (myCurrentLanguage.LayoutName == "Русская" || myCurrentLanguage.LayoutName == "Russian")
                    {
                        currentLanguage = "Russia";
                    } */

                    if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.A)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("ф");
                        }
                        else
                        {
                            digits.Add("a");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.B)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("и");
                        }
                        else
                        {
                            digits.Add("b");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.C)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("с");
                        }
                        else
                        {
                            digits.Add("c");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.D)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("в");
                        }
                        else
                        {
                            digits.Add("d");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.E)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("у");
                        }
                        else
                        {
                            digits.Add("e");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.F)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("a");
                        }
                        else
                        {
                            digits.Add("f");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.G)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("п");
                        }
                        else
                        {
                            digits.Add("g");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.H)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("р");
                        }
                        else
                        {
                            digits.Add("h");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.I)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("ш");
                        }
                        else
                        {
                            digits.Add("i");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.J)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("о");
                        }
                        else
                        {
                            digits.Add("j");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.K)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("л");
                        }
                        else
                        {
                            digits.Add("k");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.L)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("д");
                        }
                        else
                        {
                            digits.Add("l");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.M)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("ь");
                        }
                        else
                        {
                            digits.Add("m");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.N)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("т");
                        }
                        else
                        {
                            digits.Add("n");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.O)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("щ");
                        }
                        else
                        {
                            digits.Add("o");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.P)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("з");
                        }
                        else
                        {
                            digits.Add("p");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.Q)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("й");
                        }
                        else
                        {
                            digits.Add("q");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.R)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("к");
                        }
                        else
                        {
                            digits.Add("r");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.S)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("ы");
                        }
                        else
                        {
                            digits.Add("s");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.T)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("е");
                        }
                        else
                        {
                            digits.Add("t");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.U)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("г");
                        }
                        else
                        {
                            digits.Add("u");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.V)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("м");
                        }
                        else
                        {
                            digits.Add("v");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.W)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("ц");
                        }
                        else
                        {
                            digits.Add("w");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.X)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("ч");
                        }
                        else
                        {
                            digits.Add("x");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.Y)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("н");
                        }
                        else
                        {
                            digits.Add("y");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.Z)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("я");
                        }
                        else
                        {
                            digits.Add("z");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.Space)
                    {

                        digits.Add(" ");
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.D2)
                    {
                        digits.Add("2");
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.D4)
                    {
                        digits.Add("4");
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.D5)
                    {
                        digits.Add("5");
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.D6)
                    {
                        digits.Add("6");
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.D7)
                    {
                        digits.Add("7");
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.D8)
                    {
                        digits.Add("8");
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.D9)
                    {
                        digits.Add("9");
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.D0)
                    {
                        digits.Add("0");
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.Decimal)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add(".");
                        }
                        else
                        {
                            digits.Add("/");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.OemCloseBrackets)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("ъ");
                        }
                        else
                        {
                            digits.Add("]");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.OemOpenBrackets)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("х");
                        }
                        else
                        {
                            digits.Add("[");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.OemComma)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("б");
                        }
                        else
                        {
                            digits.Add(",");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.OemTilde)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("ё");
                        }
                        else
                        {
                            digits.Add("`");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.OemPeriod)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("ю");
                        }
                        else
                        {
                            digits.Add(".");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.OemMinus)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("-");
                        }
                        else
                        {
                            digits.Add("-");
                        }
                    }
                    else if (Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.D1)
                            digits.Add("!");
                        else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.OemQuestion)
                        {
                            if (currentLanguage == "Russia")
                            {
                                digits.Add(",");
                            }
                            else
                            {
                                digits.Add("?");
                            }
                        }
                        else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.OemSemicolon)
                            digits.Add(":");
                        else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.OemQuotes)
                            digits.Add("\"");
                        else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.OemPlus)
                            digits.Add("+");
                        else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.D3)
                            digits.Add("#");
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.D1)
                        digits.Add("1");
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.D3)
                        digits.Add("3");
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.OemPlus)
                    {
                        digits.Add("=");
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.OemSemicolon)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("ж");
                        }
                        else
                        {
                            digits.Add(";");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.OemQuotes)
                    {
                        if (currentLanguage == "Russia")
                        {
                            digits.Add("э");
                        }
                        else
                        {
                            digits.Add("'");
                        }
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.OemQuestion)
                    {
                        digits.Add(".");
                    }
                    else if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.Back)
                    {
                        var resultlist = digits.ToList();
                        if (resultlist.Count - 1 != -1)
                        {
                            resultlist.RemoveAt(resultlist.Count - 1);
                            digits = resultlist;
                            dopRemoveInt++;
                        }
                    }
                    else
                    {
                        digits.Add("");
                    }

                    if (Keyboard.IsKeyDown(Key.NumPad4))
                    {
                        if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.NumPad5)
                        {
                            IntPtr hWnd = GetForegroundWindow();
                            GetWindowThreadProcessId(hWnd, out int processId);

                            Process process = Process.GetProcessById(processId);
                            string processName = process.ProcessName;
                            if (processName != nameActiveWindow)
                            {
                                this.Icon = new BitmapImage(new Uri("pack://application:,,,/icons/WrongWindow.ico"));
                                _ = ChangeIconSync.ChangeOnWrongWindow(this);
                                return CallNextHookEx(hookId, nCode, wParam, lParam);
                            }

                            Thread.Sleep(500);
                            string clipboardText = ClipboardHelper.GetClipboardText();
                            if (clipboardText == "")
                            {
                                enteredValue = string.Join("", digits);
                                if (enteredValue == "")
                                {
                                    for (int i = 0; i < 3; i++)
                                    {
                                        Thread.Sleep(100);
                                        keybd_event(BACK, 0, KEYEVENTF_KEYDOWN, 0);
                                        keybd_event(BACK, 0, KEYEVENTF_KEYUP, 0);
                                    }
                                    string text = "";
                                    try
                                    {
                                        text = ClipboardHelper.GetClipboardImageToText();
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show("Обратитесь к издателю ПО"); // debug text
                                                                                     //Environment.Exit(0);
                                    }
                                    enteredValue = text;
                                    Thread.Sleep(50);
                                }
                                else
                                {
                                    for (int i = 0; i < enteredValue.Length + 3; i++)
                                    {
                                        Thread.Sleep(60);
                                        keybd_event(BACK, 0, KEYEVENTF_KEYDOWN, 0);
                                        keybd_event(BACK, 0, KEYEVENTF_KEYUP, 0);
                                    }
                                }
                                isCapturing = false;
                                resultOnFinish = true;
                            }
                            else
                            {
                                for (int i = 0; i < 3; i++)
                                {
                                    Thread.Sleep(100);
                                    keybd_event(BACK, 0, KEYEVENTF_KEYDOWN, 0);
                                    keybd_event(BACK, 0, KEYEVENTF_KEYUP, 0);
                                }
                                //MessageBox.Show("Буфер обмена содержит текст, отправляю его дальше"); // debug
                                enteredValue = clipboardText;
                                isCapturing = false;
                                resultOnFinish = true;
                            }
                        }
                    }
                }

                if (resultOnFinish)
                {
                    string tempenteredValue = enteredValue;
                    //MessageBox.Show("Начинаю отправку текста ChatGPT"); // debug text

                    UnhookWindowsHookEx(hookId);
                    Task task = OpenAIRequest.OpenAIRequetst(tempenteredValue);
                    task.Wait();

                    keyboardHookProc = KeyboardHookCallback;
                    hookId = SetHook(keyboardHookProc);

                    tempenteredValue = OpenAIRequest.responseFinish;

                    Thread.Sleep(50);

                    for (int i = 0; i < OpenAIRequest.responseFinish.Length; i++)
                    {
                        if (tempenteredValue == "")
                        {
                            break;
                        }
                        if (tempenteredValue.Length != 1 && tempenteredValue.Length != 2 && tempenteredValue.Length != 3 && tempenteredValue.Substring(0, 3) == "\n\n")
                        {
                            mas[i] = "\n";
                            tempenteredValue = tempenteredValue.Remove(0, 3);
                            continue;
                        }
                        if (tempenteredValue.Length != 1 && tempenteredValue.Substring(0, 1) == "\n")
                        {
                            mas[i] = "\n";
                            tempenteredValue = tempenteredValue.Remove(0, 1);
                            if (tempenteredValue.Length > 19 && tempenteredValue.Substring(0, 20) == "                    ")
                            {
                                tempenteredValue = tempenteredValue.Remove(0, 20);
                            }
                            else if (tempenteredValue.Length > 15 && tempenteredValue.Substring(0, 16) == "                ")
                            {
                                tempenteredValue = tempenteredValue.Remove(0, 16);
                            }
                            else if (tempenteredValue.Length > 11 && tempenteredValue.Substring(0, 12) == "            ")
                            {
                                tempenteredValue = tempenteredValue.Remove(0, 12);
                            }
                            else if (tempenteredValue.Length != 8 && tempenteredValue.Length != 7 && tempenteredValue.Length != 6 && tempenteredValue.Length != 5 && tempenteredValue.Length != 4 && tempenteredValue.Length != 3 && tempenteredValue.Length != 2 && tempenteredValue.Length != 1 && tempenteredValue.Substring(0, 8) == "        ")
                            {
                                tempenteredValue = tempenteredValue.Remove(0, 8);
                            }
                            else if (tempenteredValue.Length != 3 && tempenteredValue.Length != 2 && tempenteredValue.Length != 1 && tempenteredValue.Substring(0, 4) == "    ")
                            {
                                tempenteredValue = tempenteredValue.Remove(0, 4);
                            }
                            else if (tempenteredValue.Length != 2 && tempenteredValue.Length != 1 && tempenteredValue.Substring(0, 3) == "   ")
                            {
                                tempenteredValue = tempenteredValue.Remove(0, 3);
                            }
                            else if (tempenteredValue.Length != 1 && tempenteredValue.Substring(0, 2) == "  ")
                            {
                                tempenteredValue = tempenteredValue.Remove(0, 2);
                            }
                            continue;
                        }
                        /*if (tempenteredValue.Length != 1 && tempenteredValue.Substring(0, 1) == " ")
                        {
                            tempenteredValue = tempenteredValue.Remove(0, 1);
                            continue;
                        } */
                        mas[i] = tempenteredValue.Substring(0, 1);
                        tempenteredValue = tempenteredValue.Remove(0, 1);
                    }
                    resultOnFinish = false;
                    writePreStart = true;

                    this.Icon = new BitmapImage(new Uri("pack://application:,,,/icons/OK.ico"));
                }

                if (writePreStart)
                {
                    if (Keyboard.IsKeyDown(Key.NumPad7))
                    {
                        if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.NumPad8)
                        {
                            // подделка иконки и заголовка
                            IntPtr hWnd = GetForegroundWindow();
                            GetWindowThreadProcessId(hWnd, out int processId);

                            Process process = Process.GetProcessById(processId);
                            var processName = process.ProcessName;
                            oldStaticProcessName = processName;
                            fakeProcessName = processName;
                            var TempIcon = System.Drawing.Icon.ExtractAssociatedIcon(process.MainModule.FileName);
                            ToImageSource(TempIcon);
                            this.Topmost = true;
                            this.Title = process.MainWindowTitle;

                            //this.Icon = new BitmapImage(new Uri("pack://application:,,,/icons/Visual_Studio_Icon_2022.ico"));
                            for (int i = 0; i < 3; i++)
                            {
                                Thread.Sleep(100);
                                keybd_event(BACK, 0, KEYEVENTF_KEYDOWN, 0);
                                keybd_event(BACK, 0, KEYEVENTF_KEYUP, 0);
                            }
                            writeStart = true;
                            writePreStart = false;
                            return CallNextHookEx(hookId, nCode, wParam, lParam);
                        }
                    }
                }

                if (writeStart)
                {
                    if (KeyInterop.KeyFromVirtualKey(vkCode) == Key.NumLock)
                    {
                        while (true)
                        {
                            Thread.Sleep(20);
                            UnhookWindowsHookEx(hookId);

                            Thread.Sleep(20);
                            SimulateKeystrokes(mas[indexWriteInMas]);
                            Thread.Sleep(20);

                            if (mas[indexWriteInMas + 1] == "\n")
                            {
                                keybd_event(ENTER, 0, KEYEVENTF_KEYDOWN, 0);
                                keybd_event(ENTER, 0, KEYEVENTF_KEYUP, 0);
                                indexWriteInMas++;
                            }

                            keyboardHookProc = KeyboardHookCallback;
                            hookId = SetHook(keyboardHookProc);

                            Thread.Sleep(20);

                            indexWriteInMas++;
                            if (mas[indexWriteInMas] == "")
                            {
                                writeStart = false;
                                Thread.Sleep(200);

                                return CallNextHookEx(hookId, nCode, wParam, lParam);
                            }
                            //return CallNextHookEx(hookId, nCode, wParam, lParam);
                        }
                    }
                    else
                    {
                        Thread.Sleep(30);
                        UnhookWindowsHookEx(hookId);

                        keybd_event(BACK, 0, KEYEVENTF_KEYDOWN, 0);
                        keybd_event(BACK, 0, KEYEVENTF_KEYUP, 0);
                        Thread.Sleep(30);
                        SimulateKeystrokes(mas[indexWriteInMas]);
                        Thread.Sleep(30);

                        //if (mas[indexWriteInMas] == "{" || mas[indexWriteInMas] == "}" || mas[indexWriteInMas] == ";" || mas[indexWriteInMas + 1] == "{")
                        //{
                        //keybd_event(ENTER, 0, KEYEVENTF_KEYDOWN, 0);
                        //keybd_event(ENTER, 0, KEYEVENTF_KEYUP, 0);
                        //}
                        if (mas[indexWriteInMas + 1] == "\n")
                        {
                            keybd_event(ENTER, 0, KEYEVENTF_KEYDOWN, 0);
                            keybd_event(ENTER, 0, KEYEVENTF_KEYUP, 0);
                            indexWriteInMas++;
                        }

                        keyboardHookProc = KeyboardHookCallback;
                        hookId = SetHook(keyboardHookProc);

                        Thread.Sleep(30);

                        indexWriteInMas++;
                        if (mas[indexWriteInMas] == "")
                        {
                            writeStart = false;
                            Thread.Sleep(50);
                            //keybd_event(BACK, 0, KEYEVENTF_KEYDOWN, 0);
                            //keybd_event(BACK, 0, KEYEVENTF_KEYUP, 0);
                            Thread.Sleep(200);
                            keybd_event(BACK, 0, KEYEVENTF_KEYDOWN, 0);
                            keybd_event(BACK, 0, KEYEVENTF_KEYUP, 0);

                            return CallNextHookEx(hookId, nCode, wParam, lParam);
                        }
                        return CallNextHookEx(hookId, nCode, wParam, lParam);
                    }
                }

            }

            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Unhook the keyboard hook when the window is closed
            UnhookWindowsHookEx(hookId);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static string fakeProcessPath = "";
        public static bool BringProcessWindowToFront(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                Process.Start(@fakeProcessPath);
                return false;
            }

            IntPtr mainWindowHandle = new IntPtr();
            for (int i = 0; i < processes.Length; i++)
            {
                mainWindowHandle = processes[i].MainWindowHandle;
                if (mainWindowHandle != IntPtr.Zero)
                {
                    try
                    {
                        fakeProcessPath = processes[i].MainModule.FileName;
                        // Show the window
                        ShowWindowAsync(mainWindowHandle, 9);

                        // Bring the window to front
                        SetForegroundWindow(mainWindowHandle);
                    }
                    catch
                    {

                    }
                    return true;
                }
            }

            return false;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            BringProcessWindowToFront(fakeProcessName);
        }

        public async Task AutoMinimized(CancellationTokenSource cancellationTokenSource)
        {

        }

        private async void SimulateKeystrokes(string text)
        {
            // Wait for a few seconds to give you time to bring the target window to the front
            //Task.Delay(100);

            // Set the target window to be active (replace "YourWindowTitle" with the window title of your target application)
            //SetForegroundWindow();
            //await Task.Delay(5000);
            // Simulate typing "Hello, World!" into the active window
            string textToType = text;
            SimulateTyping(textToType);
        }

        private void SetForegroundWindow()
        {
            var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            User32.SetForegroundWindow(windowHandle);
        }

        private void SimulateTyping(string text)
        {
            var simulator = new InputSimulator();
            simulator.Keyboard.TextEntry(text);
        }


        public static string ThreadLanguage = "";
        static void LanguageCheck()
        {
            InputLanguage myCurrentLanguage = InputLanguage.CurrentInputLanguage;
            if (myCurrentLanguage.LayoutName == "США" || myCurrentLanguage.LayoutName == "USA" || myCurrentLanguage.LayoutName == "English")
            {
                ThreadLanguage = "USA";
            }
            else if (myCurrentLanguage.LayoutName == "Русская" || myCurrentLanguage.LayoutName == "Russia")
            {
                ThreadLanguage = "Russia";
            }
            else
            {
                ThreadLanguage = "Russia";
            }
        }

        private class User32
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(System.IntPtr hWnd);
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        public const int KEYEVENTF_KEYDOWN = 0x0001; //Key down flag
        public const int KEYEVENTF_KEYUP = 0x0002; //Key up flag
        public const int BACK = 0x08; // Backspace keycode.
        public const int ENTER = 0x0D; // Enter keycode.
        public const int NumLock = 0x90; // NumLock keycode.

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UnHideTaskbarIcon2(oldStaticProcessName);
        }
    }
}