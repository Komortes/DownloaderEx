using Microsoft.Win32;
using DownloaderEx.Properties;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace DownloaderEx
{
    /// <summary>
    /// Логика взаимодействия для SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Window
    {
        private int maxDownloads;
        private bool enableSpeedLimit;
        private int speedLimit;

        public SettingsPage()
        {
            InitializeComponent();

            cbConfirmDelete.IsChecked = Settings.Default.ConfirmDelete;
            cbConfirmExit.IsChecked = Settings.Default.ConfirmExit;

            tbLocation.Text = Settings.Default.DownloadLocation;

            intMaxDownloads.Value = maxDownloads = Settings.Default.MaxDownloads;
            cbSpeedLimit.IsChecked = intSpeedLimit.IsEnabled = enableSpeedLimit = Settings.Default.EnableSpeedLimit;
            intSpeedLimit.Value = speedLimit = Settings.Default.SpeedLimit;
            intMemoryCacheSize.Value = Settings.Default.MemoryCacheSize;
 
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void SaveSettings()
        {
            Settings.Default.ConfirmDelete = cbConfirmDelete.IsChecked.Value;
            Settings.Default.ConfirmExit = cbConfirmExit.IsChecked.Value;

            Settings.Default.DownloadLocation = tbLocation.Text;

            Settings.Default.MaxDownloads = Convert.ToInt32(intMaxDownloads.Value);
            Settings.Default.EnableSpeedLimit = cbSpeedLimit.IsChecked.Value;
            Settings.Default.SpeedLimit = Convert.ToInt32(intSpeedLimit.Value);
            Settings.Default.MemoryCacheSize = Convert.ToInt32(intMemoryCacheSize.Value);
            if (DownloadManager.Instance.TotalDownloads > 0)
            {
                if (enableSpeedLimit != Settings.Default.EnableSpeedLimit || speedLimit != Settings.Default.SpeedLimit)
                {
                    foreach (WebDownloadClient el in DownloadManager.Instance.DownloadsList)
                    {
                        if (el.Status == DownloadState.Downloading)
                        {
                            el.SpeedLimitChanged = true;
                        }
                    }
                }

                if (maxDownloads != Settings.Default.MaxDownloads)
                {
                    foreach (WebDownloadClient el in DownloadManager.Instance.DownloadsList)
                    {
                        if (DownloadManager.Instance.ActiveDownloads < Settings.Default.MaxDownloads)
                        {
                            if (el.Status == DownloadState.Queued)
                            {
                                el.Start();
                            }
                        }
                    }
                    for (int i = DownloadManager.Instance.TotalDownloads - 1; i >= 0; i--)
                    {
                        if (DownloadManager.Instance.ActiveDownloads > Settings.Default.MaxDownloads)
                        {
                            if (DownloadManager.Instance.DownloadsList[i].Status == DownloadState.Waiting || DownloadManager.Instance.DownloadsList[i].Status == DownloadState.Downloading)
                            {
                                DownloadManager.Instance.DownloadsList[i].Status = DownloadState.Queued;
                            }
                        }
                    }
                }
            }

            Settings.Default.Save();


        }
       



        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbDialog = new FolderBrowserDialog();
            fbDialog.Description = "Укажите путь для скачивания";
            fbDialog.ShowNewFolderButton = true;
            DialogResult result = fbDialog.ShowDialog();

            if (result.ToString().Equals("OK"))
            {
                string path = fbDialog.SelectedPath;
                if (path.EndsWith("\\") == false)
                    path += "\\";
                tbLocation.Text = path;
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            this.Close();
        }

        private void cbSpeedLimit_Click(object sender, RoutedEventArgs e)
        {
            intSpeedLimit.IsEnabled = cbSpeedLimit.IsChecked.Value;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
