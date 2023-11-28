using IronOcr;
using OpenAI_API.Moderation;
using Pass_Manager_WPF;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;

class ClipboardHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool OpenClipboard(IntPtr hWndNewOwner);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool CloseClipboard();
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetClipboardData(uint uFormat);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool IsClipboardFormatAvailable(uint format);
    [DllImport("kernel32.dll")]
    public static extern IntPtr GlobalLock(IntPtr hMem);
    [DllImport("kernel32.dll")]
    public static extern bool GlobalUnlock(IntPtr hMem);
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GlobalSize(IntPtr hMem);
    [DllImport("user32.dll")]
    public static extern bool EmptyClipboard();
    [DllImport("user32.dll")]
    public static extern int CountClipboardFormats();
    [DllImport("user32.dll")]
    public static extern int EnumClipboardFormats(int format);

    public static string[] russianPrepositions = new string[] { "в", "на", "под", "за", "от", "перед", "за", "с", "из", "у", "к", 
                                                                "и", "или", "но", "а", "чтобы", "также", "как", "над" , 
                                                                "не", "по", "о",
                                                                "да", "либо", "то", "бы", "что",
                                                                "так", "тоже", "еще", "уже", "ну", "вот", "там", "тут", "потом",
                                                                "потому", "ибо", "поскольку", "хотя", "ладно", "допустим", "раз", "где",
                                                                "когда", "пока", "если", "хоть", "только", "лишь", "только ради", "лишь только",
                                                                "точно", "никак", "кое-как", "точнее", "по-настоящему", "наверное", "по-разному",
                                                                "по-своему", "постепенно", "медленно", "резко", "приблизительно", "чуть-чуть", "несколько", "как-то", "кое-где", "кое-что",
                                                                "точность", "например", "короче", "вероятно", "неясно", "таки", "итак", "следовательно", "значит",
                                                                "хорошо", "плохо", "быстро", "легко", "трудно", "бесплатно", "вместе", "отдельно", "по-старому",
                                                                "вовремя", "раздельно", "вроде", "постоянно", "даже", "возможно", "без", "для", "после", "же"};

    public static string[] englishPrepositions = new string[] { "a", "an", "and", "are", "as", "at", "be", "but", "by", "for",
                                                                "if", "in", "into", "is", "it", "no", "not", "of", "on",
                                                                "such", "that", "the", "their", "then", "there", "these",
                                                                "they", "this", "to", "was", "will", "with", "about", "after", 
                                                                "agains", "because", "before",
                                                                "down", "except", "from", "or", "out", "so" };

    public static void ClearClipboard()
    {
        IntPtr clipboardOwner = IntPtr.Zero;
        if (OpenClipboard(clipboardOwner))
        {
            int format = EnumClipboardFormats(0);
            while (format != 0)
            {
                IntPtr handle = GetClipboardData((uint)format);
                if (handle != IntPtr.Zero)
                {
                    // Освобождаем ресурсы, связанные с данным форматом буфера обмена
                    Marshal.FreeCoTaskMem(handle);
                }
                format = EnumClipboardFormats(format);
            }
            EmptyClipboard();
            CloseClipboard();
        }
        else
        {
            // Обработка ошибки при открытии буфера обмена
            throw new InvalidOperationException("Failed to open clipboard.");
        }
    }

    public static string GetClipboardText()
    {
        string? result = null;

        var thread = new Thread(() => { result = System.Windows.Forms.Clipboard.GetText(); });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return result;
    }

    public static string GetClipboardImageToText()
    {
        if (System.Windows.Clipboard.ContainsImage())
        {
            string path = Path.GetTempPath();
            path += "image.png";
            var bitmapSource = System.Windows.Clipboard.GetImage();
            SaveBitmapSourceToFile(bitmapSource, path);

            //Page1 page1 = new Page1();
            //page1.OneImage.Dispatcher.Invoke(() =>
            //{
            //page1.OneImage.Source = bitmapSource;
            //});
            string TextRussia = "";
            string TextEnglish = "";

            IronTesseract ocr = new IronTesseract();
            IronOcr.License.LicenseKey = ""; // лицензионный ключ IronOcr
            ocr.Language = OcrLanguage.RussianBest;
            using (var input = new OcrInput())
            {
                input.AddImage(path);
                string TestResult = "";
                try
                {
                    OcrResult result = ocr.Read(input);
                    TestResult = result.Text;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.ToString()); // debug text
                }
                TextRussia = TestResult;
                TextRussia = TextRussia.ToLower();
            }

            IronTesseract ocr2 = new IronTesseract();
            ocr2.Language = OcrLanguage.EnglishBest;
            using (var input2 = new OcrInput())
            {
                input2.AddImage(path);
                OcrResult result2 = ocr2.Read(input2);
                string TestResult2 = result2.Text;
                TextEnglish = TestResult2;
                TextEnglish = TextEnglish.ToLower();
            }

            // check engCheck and engPrep
            int engCheckAndEngPrep_True = 0;
            for (int i = 0; i < englishPrepositions.Length; i++)
            {
                if (TextEnglish.Contains(" " + englishPrepositions[i] + " "))
                {
                    engCheckAndEngPrep_True++;
                }
            }
            int resultEngcheck = engCheckAndEngPrep_True;

            // check rusCheck and rusPrep
            int rusCheckAndRusPrep_True = 0;
            for (int i = 0; i < russianPrepositions.Length; i++)
            {
                if (TextRussia.Contains(" " + russianPrepositions[i] + " "))
                {
                    rusCheckAndRusPrep_True++;
                }
            }
            int resultRuscheck = rusCheckAndRusPrep_True;

            File.Delete(path);

            if (resultEngcheck > resultRuscheck)
            {
                return TextEnglish;
            }
            else if (resultEngcheck < resultRuscheck)
            {
                return TextRussia;
            }
            else
            {
                return TextRussia;
            }
        }
        else
        {
            return "Error";
        }
    }

    private static void SaveBitmapSourceToFile(BitmapSource bitmapSource, string filePath)
    {
        // Создание кодировщика для сохранения в формате JPEG
        JpegBitmapEncoder encoder = new JpegBitmapEncoder();

        // Добавление BitmapSource в коллекцию фреймов кодировщика
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

        // Создание потока для записи изображения
        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        {
            // Сохранение фрейма кодировщика в файл
            encoder.Save(fileStream);
        }
    }
}