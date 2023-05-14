using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using DownloaderEx.Properties;

namespace DownloaderEx
{
    /// <summary>
    /// Логика взаимодействия для AddDownload.xaml
    /// </summary>
    public partial class AddDownload : Window
    {
        private bool urlValid;
        private MainWindow mainWindow;
        private NumberFormatInfo numberFormat = NumberFormatInfo.InvariantInfo;

        public AddDownload(MainWindow mainWin)
        {
            InitializeComponent();
            mainWindow = mainWin;
            tbDownloadFolder.Text = Settings.Default.DownloadLocation;
            urlValid = false;

            if (System.Windows.Clipboard.ContainsText())
            {
                string clipboardText = System.Windows.Clipboard.GetText();

                if (IsUrlValid(clipboardText))
                {
                    urlValid = true;
                    tbURL.Text = clipboardText;
                    tbSaveAs.Text = tbURL.Text.Substring(tbURL.Text.LastIndexOf("/") + 1);
                }
            }
        }
        private string GetFreeDiskSpace(string driveName)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name == driveName)
                {
                    long freeSpace = drive.AvailableFreeSpace;
                    double mbFreeSpace = (double)freeSpace / Math.Pow(1024, 2);
                    double gbFreeSpace = mbFreeSpace / 1024D;

                    if (freeSpace < Math.Pow(1024, 3))
                    {
                        return mbFreeSpace.ToString("#.00", numberFormat) + " MB";
                    }
                    return gbFreeSpace.ToString("#.00", numberFormat) + " GB";
                }
            }
            return String.Empty;
        }

        private bool IsUrlValid(string Url)
        {
            if (Url.StartsWith("http") && Url.Contains(":") && (Url.Length > 15) && (Utilities.CountOccurence(Url, '/') >= 3) && (Url.LastIndexOf('/') != Url.Length - 1))
            {
                string lastChars = Url.Substring(Url.Length - 9);

                if (lastChars.Contains(".") && (lastChars.LastIndexOf('.') != lastChars.Length - 1))
                {
                    string ext = lastChars.Substring(lastChars.LastIndexOf('.') + 1);
                    string chars = " ?#&%=[]_-+~:;\\/!$<>\"\'*";
                    foreach (char el in ext)
                    {
                        foreach (char e in chars)
                        {
                            if (el  == e)
                            {
                                return false;
                            }      
                        }
                    }

                    return true;
                }
                return false;
            }
            return false;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (urlValid)
            {
                if (tbSaveAs.Text.Length < 3 || !tbSaveAs.Text.Contains("."))
                {
                    System.Windows.MessageBox.Show("Неверное название файла", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    WebDownloadClient download = new WebDownloadClient(tbURL.Text.Trim());

                    download.FileName = tbSaveAs.Text.Trim();
                    download.DownloadProgressChanged += download.DownloadProgressChangedHandler;
                    download.DownloadCompleted += download.DownloadCompletedHandler;
                    download.StatusChanged += this.mainWindow.StatusChangedHandler;
                    download.DownloadCompleted += this.mainWindow.DownloadCompletedHandler;
                   


                    if (!Directory.Exists(tbDownloadFolder.Text))
                    {
                        Directory.CreateDirectory(tbDownloadFolder.Text);
                    }
                    string filePath = tbDownloadFolder.Text + download.FileName;
                    string tempPath = filePath + ".tmp";

                    if (File.Exists(tempPath))
                    {
                        string message = "Уже идет загрузка этого файла";
                        System.Windows.MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (File.Exists(filePath))
                    {
                        string message = "Уже существует файл с таким же назваением, перезаписать его? "
                                       + "Иначе, переименуйте его или изменити путь для скачивания";
                        MessageBoxResult result = System.Windows.MessageBox.Show(message, "Конфликт названий файлов: " + filePath, MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            File.Delete(filePath);
                        }
                        else
                        {
                            return;
                        }      
                    }

                    download.CheckUrl();
                    if (download.HasError)
                    {
                        return;
                    }
                        
                    download.TempDownloadPath = tempPath;
                    download.DownloadPathFinal = download.DownloadPath;
                    download.AddedOn = DateTime.UtcNow;
                    download.CompletedOn = DateTime.MinValue;

                    DownloadManager.Instance.DownloadsList.Add(download);

                    download.Start();

                    this.Close();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Неверный URL-адресс", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbDialog = new FolderBrowserDialog();
            fbDialog.Description = "Выберите путь для скачивания";
            fbDialog.ShowNewFolderButton = true;
            DialogResult result = fbDialog.ShowDialog();
            if (result.ToString().Equals("OK"))
            {
                string path = fbDialog.SelectedPath;
                if (path.EndsWith("\\") == false)
                {
                    path += "\\";
                }
                tbDownloadFolder.Text = path;
            }
        }

        private void tbURL_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsUrlValid(tbURL.Text))
            {
                urlValid = true;
                tbSaveAs.Text = tbURL.Text.Substring(tbURL.Text.LastIndexOf("/") + 1);
            }
            else
            {
                urlValid = false;
                tbSaveAs.Text = String.Empty;
            }
        }

        private void tbDownloadFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            string drive = String.Empty;
            if (tbDownloadFolder.Text.Length > 3)
            {
                drive = tbDownloadFolder.Text.Remove(3);
            }
            else
            {
                drive = tbDownloadFolder.Text;
            }
            lblFreeSpace.Text = "Доступно: " + GetFreeDiskSpace(drive);
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
