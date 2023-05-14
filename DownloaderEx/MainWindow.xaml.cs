using Microsoft.Win32;
using DownloaderEx.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace DownloaderEx
{
    public partial class MainWindow
    {
        private List<string> propertyNames;
        private List<string> propertyValues;
        private List<PropertyModel> propertiesList;
        private string[] args;


        public MainWindow()
        {
            InitializeComponent();

            args = Environment.GetCommandLineArgs();

            downloadsGrid.ItemsSource = DownloadManager.Instance.DownloadsList;
            DownloadManager.Instance.DownloadsList.CollectionChanged += new NotifyCollectionChangedEventHandler(DownloadsList_CollectionChanged);

            propertyNames = new List<string>();
            propertyNames.Add("URL");
            propertyNames.Add("Возможно возобновить");
            propertyNames.Add("Тип файла");
            propertyNames.Add("Путь");
            propertyNames.Add("Средняя скорость");
            propertyNames.Add("Время");

            propertyValues = new List<string>();
            propertiesList = new List<PropertyModel>();
            SetEmptyPropertiesGrid();
            propertiesGrid.ItemsSource = propertiesList;

            if (DownloadManager.Instance.TotalDownloads == 0)
            {
                EnableMenuItems(false);

                if (Directory.Exists(Settings.Default.DownloadLocation))
                {
                    DirectoryInfo downloadLocation = new DirectoryInfo(Settings.Default.DownloadLocation);
                    foreach (FileInfo file in downloadLocation.GetFiles())
                    {
                        if (file.FullName.EndsWith(".tmp"))
                            file.Delete();
                    }
                }
            }

        }


        private void SetEmptyPropertiesGrid()
        {
            if (propertiesList.Count > 0)
            {
                propertiesList.Clear();
            }
            for (int i = 0; i < 6; i++)
            {
                propertiesList.Add(new PropertyModel(propertyNames[i], String.Empty));
            }

            propertiesGrid.Items.Refresh();
        }



        private void EnableMenuItems(bool enabled)
        {
            btnDelete.IsEnabled = enabled;
            btnStart.IsEnabled = enabled;
            btnPause.IsEnabled = enabled;
        }

        private void EnableDataGridMenuItems(bool enabled)
        {
            cmStart.IsEnabled = enabled;
            cmPause.IsEnabled = enabled;
            cmDelete.IsEnabled = enabled;
            cmRestart.IsEnabled = enabled;
            cmOpenFile.IsEnabled = enabled;
            cmOpenDownloadFolder.IsEnabled = enabled;
        }

        private void mainWindow_ContentRendered(object sender, EventArgs e)
        {
            if (args.Length == 2)
            {
                if (args[1].StartsWith("http"))
                {
                    System.Windows.Clipboard.SetText(args[1]);

                    AddDownload newDownloadDialog = new AddDownload(this);
                    newDownloadDialog.ShowDialog();
                }
            }
        }


        private void mainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((Keyboard.Modifiers == ModifierKeys.Control) && (e.Key == Key.A))
            {
                this.downloadsGrid.SelectAll();
            }
        }

        private void downloadsGrid_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                btnDelete_Click(sender, e);
            }
        }

        private void downloadsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (downloadsGrid.SelectedItems.Count > 0)
            {
                foreach (WebDownloadClient downld in DownloadManager.Instance.DownloadsList)
                {
                    downld.IsSelected = false;
                }

                var download = (WebDownloadClient)downloadsGrid.SelectedItem;

                if (propertyValues.Count > 0)
                {
                    propertyValues.Clear();
                }
                propertyValues.Add(download.Url.ToString());
                string resumeSupported = "Нет";
                if (download.SupportsRange)
                {
                    resumeSupported = "Да";
                }
                    
                propertyValues.Add(resumeSupported);
                propertyValues.Add(download.FileType);
                propertyValues.Add(download.DownloadFolder);
                propertyValues.Add(download.AverageDownloadSpeed);
                propertyValues.Add(download.TotalElapsedTimeString);

                if (propertiesList.Count > 0)
                    propertiesList.Clear();

                for (int i = 0; i < 6; i++)
                {
                    propertiesList.Add(new PropertyModel(propertyNames[i], propertyValues[i]));
                }

                propertiesGrid.Items.Refresh();
                download.IsSelected = true;
            }
            else
            {
                if (DownloadManager.Instance.TotalDownloads > 0)
                {
                    foreach (WebDownloadClient downld in DownloadManager.Instance.DownloadsList)
                    {
                        downld.IsSelected = false;
                    }
                }
                SetEmptyPropertiesGrid();
            }
        }

        public void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            WebDownloadClient download = (WebDownloadClient)sender;
            if (e.PropertyName == "AverageSpeedAndTotalTime" && download.Status != DownloadState.Deleting)
            {
                this.Dispatcher.Invoke(new PropertyChangedEventHandler(UpdatePropertiesList), sender, e);
            }
        }

        private void UpdatePropertiesList(object sender, PropertyChangedEventArgs e)
        {
            propertyValues.RemoveRange(4, 2);
            var download = (WebDownloadClient)downloadsGrid.SelectedItem;
            propertyValues.Add(download.AverageDownloadSpeed);
            propertyValues.Add(download.TotalElapsedTimeString);
            propertiesList.RemoveRange(4, 2);
            propertiesList.Add(new PropertyModel(propertyNames[4], propertyValues[4]));
            propertiesList.Add(new PropertyModel(propertyNames[5], propertyValues[5]));
            propertiesGrid.Items.Refresh();
        }

        private void downloadsGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            dgScrollViewer.ScrollToVerticalOffset(dgScrollViewer.VerticalOffset - e.Delta / 3);
        }

        private void propertiesGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            propertiesScrollViewer.ScrollToVerticalOffset(propertiesScrollViewer.VerticalOffset - e.Delta / 3);
        }

        private void downloadsGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (DownloadManager.Instance.TotalDownloads == 0)
            {
                EnableDataGridMenuItems(false);
            }
            else
            {
                if (downloadsGrid.SelectedItems.Count == 1)
                {
                    EnableDataGridMenuItems(true);
                }
                else if (downloadsGrid.SelectedItems.Count > 1)
                {
                    EnableDataGridMenuItems(true);
                    cmOpenFile.IsEnabled = false;
                    cmOpenDownloadFolder.IsEnabled = false;
                }
                else
                {
                    EnableDataGridMenuItems(false);

                }
            }
        }

        private void DownloadsList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (DownloadManager.Instance.TotalDownloads == 1)
            {
                EnableMenuItems(true);
                this.statusBarDownloads.Content = "1 Загрузка";
            }
            else if (DownloadManager.Instance.TotalDownloads > 1)
            {
                EnableMenuItems(true);
                this.statusBarDownloads.Content = DownloadManager.Instance.TotalDownloads + " Загрузок";
            }
            else
            {
                EnableMenuItems(false);
                this.statusBarDownloads.Content = "Готово";
            }
        }

        public void StatusChangedHandler(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(new EventHandler(StatusChanged), sender, e);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SettingsPage st = new SettingsPage();
            st.ShowDialog();

        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }


        private void StatusChanged(object sender, EventArgs e)
        {
            WebDownloadClient dl = (WebDownloadClient)sender;
            if (dl.Status == DownloadState.Paused || dl.Status == DownloadState.Done || dl.Status == DownloadState.Deleted || dl.HasError)
            {
                foreach (WebDownloadClient d in DownloadManager.Instance.DownloadsList)
                {
                    if (d.Status == DownloadState.Queued)
                    {
                        d.Start();
                        break;
                    }
                }
            }



            foreach (WebDownloadClient d in DownloadManager.Instance.DownloadsList)
            {
                if (d.Status == DownloadState.Downloading)
                {
                    d.SpeedLimitChanged = true;
                }
            }

            int active = DownloadManager.Instance.ActiveDownloads;
            int completed = DownloadManager.Instance.CompletedDownloads;

            if (active > 0)
            {
                if (completed == 0)
                {
                    this.statusBarDownloads.Content = " (" + active + " Активно)";
                }  
                else
                {
                    this.statusBarDownloads.Content = " (" + active + " Активно, ";
                }
                   
            }
            else
            {
                this.statusBarDownloads.Content = String.Empty;
            }
               

            if (completed > 0)
            {
                if (active == 0)
                {
                    this.statusBarCompleted.Content = " (" + completed + " Завершено)";
                }
                    
                else
                {
                    this.statusBarCompleted.Content = completed + " Завершено)";
                }
                    
            }
            else
            {
                this.statusBarCompleted.Content = String.Empty;
            }
                
        }

        public void DownloadCompletedHandler(object sender, EventArgs e)
        {
            WebDownloadClient download = (WebDownloadClient)sender;
        }


        private void btnNewDownload_Click(object sender, RoutedEventArgs e)
        {
            AddDownload newDownloadDialog = new AddDownload(this);
            newDownloadDialog.ShowDialog();
        }


        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (downloadsGrid.SelectedItems.Count > 0)
            {
                MessageBoxResult result = MessageBoxResult.None;
                if (Settings.Default.ConfirmDelete)
                {
                    string message = "Вы уверенны, что хотите удалить загрузку?";
                    result = MessageBox.Show(message, "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                }

                if (result == MessageBoxResult.Yes || !Settings.Default.ConfirmDelete)
                {
                    var selectedDownloads = downloadsGrid.SelectedItems.Cast<WebDownloadClient>();
                    var downloadsToDelete = new List<WebDownloadClient>();

                    foreach (WebDownloadClient download in selectedDownloads)
                    {
                        if (download.HasError || download.Status == DownloadState.Paused || download.Status == DownloadState.Queued)
                        {
                            if (File.Exists(download.TempDownloadPath))
                            {
                                File.Delete(download.TempDownloadPath);
                            }
                            download.Status = DownloadState.Deleting;
                            downloadsToDelete.Add(download);
                        }
                        else if (download.Status == DownloadState.Done)
                        {
                            download.Status = DownloadState.Deleting;
                            downloadsToDelete.Add(download);
                        }
                        else
                        {
                            download.Status = DownloadState.Deleting;
                            while (true)
                            {
                                if (download.DownloadThread.ThreadState == System.Threading.ThreadState.Stopped)
                                {
                                    if (File.Exists(download.TempDownloadPath))
                                    {
                                        File.Delete(download.TempDownloadPath);
                                    }
                                    downloadsToDelete.Add(download);
                                    break;
                                }
                            }
                        }
                    }

                    foreach (var download in downloadsToDelete)
                    {
                        download.Status = DownloadState.Deleted;
                        DownloadManager.Instance.DownloadsList.Remove(download);
                    }
                }
            }
        }

      

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (downloadsGrid.SelectedItems.Count > 0)
            {
                var selectedDownloads = downloadsGrid.SelectedItems.Cast<WebDownloadClient>();

                foreach (WebDownloadClient download in selectedDownloads)
                {
                    if (download.Status == DownloadState.Paused || download.HasError)
                    {
                        download.Start();
                    }
                }
            }
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            if (downloadsGrid.SelectedItems.Count > 0)
            {
                var selectedDownloads = downloadsGrid.SelectedItems.Cast<WebDownloadClient>();

                foreach (WebDownloadClient download in selectedDownloads)
                {
                    download.Pause();
                }
            }
        }


        private void cmRestart_Click(object sender, RoutedEventArgs e)
        {
            if (downloadsGrid.SelectedItems.Count > 0)
            {
                var selectedDownloads = downloadsGrid.SelectedItems.Cast<WebDownloadClient>();

                foreach (WebDownloadClient download in selectedDownloads)
                {
                    download.Restart();
                }
            }
        }

        private void cmOpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (downloadsGrid.SelectedItems.Count == 1)
            {
                var download = (WebDownloadClient)downloadsGrid.SelectedItem;
                if (download.Status == DownloadState.Done && File.Exists(download.DownloadPathFinal))
                {
                    Process.Start(@download.DownloadPathFinal);
                }
            }
        }

        private void cmOpenDownloadFolder_Click(object sender, RoutedEventArgs e)
        {
            if (downloadsGrid.SelectedItems.Count == 1)
            {
                var download = (WebDownloadClient)downloadsGrid.SelectedItem;
                int lastIndex = download.DownloadPathFinal.LastIndexOf("\\");
                string directory = download.DownloadPathFinal.Remove(lastIndex + 1);
                if (Directory.Exists(directory))
                {
                    Process.Start(@directory);
                }
            }
        }

        private void btnPause2_Click(object sender, RoutedEventArgs e)
        {
            var selectedDownloads = downloadsGrid.SelectedItems.Cast<WebDownloadClient>();
            var downloadsToDelete = new List<WebDownloadClient>();

            foreach (WebDownloadClient download in selectedDownloads)
            {
                download.Pause();
            }

        }

        private void btnStart2_Click(object sender, RoutedEventArgs e)
        {
            var selectedDownloads = downloadsGrid.SelectedItems.Cast<WebDownloadClient>();
            var downloadsToDelete = new List<WebDownloadClient>();

            foreach (WebDownloadClient download in selectedDownloads)
            {
                if (download.Status == DownloadState.Paused || download.HasError)
                {
                    download.Start();
                }
            }

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void cmMoveFile_Click(object sender, RoutedEventArgs e)
        {
            if (downloadsGrid.SelectedItems.Count > 0)
            {
                var selectedDownloads = downloadsGrid.SelectedItems.Cast<WebDownloadClient>();
                FolderBrowserDialog fbDialog = new FolderBrowserDialog();
                fbDialog.Description = "Выберите путь для скачивания";
                fbDialog.ShowNewFolderButton = true;
                DialogResult result = fbDialog.ShowDialog();
                if (result.ToString().Equals("OK"))
                {
                    string path = fbDialog.SelectedPath;
                   
                    foreach (WebDownloadClient download in selectedDownloads)
                    {
                        if (download.Status == DownloadState.Done)
                        {
                            string finalpath = Path.Combine(path, download.FileName);
                            if (path.EndsWith("\\") == false)
                            {
                                File.Move(download.DownloadPathFinal, finalpath);
                                download.DownloadPathFinal = finalpath;

                            }
                        }
                        else
                        {
                            MessageBox.Show("Невохможно переместить файл, пока идет загрузка");
                        }
                        
                    }

                    Process.Start(@path);

                        
                   
                }
            }

        }

        private void cmDelFile_Click(object sender, RoutedEventArgs e)
        {
            if (downloadsGrid.SelectedItems.Count > 0)
            {
                MessageBoxResult result = MessageBoxResult.None;
                if (Settings.Default.ConfirmDelete)
                {
                    string message = "Вы уверенны, что хотите удалить файл?";
                    result = MessageBox.Show(message, "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                }

                if (result == MessageBoxResult.Yes || !Settings.Default.ConfirmDelete)
                {
                    var selectedDownloads = downloadsGrid.SelectedItems.Cast<WebDownloadClient>();
                    var downloadsToDelete = new List<WebDownloadClient>();

                    foreach (WebDownloadClient download in selectedDownloads)
                    {
                        if (download.HasError || download.Status == DownloadState.Paused || download.Status == DownloadState.Queued || download.Status == DownloadState.Done)
                        {
                            if (File.Exists(download.TempDownloadPath))
                            {
                                File.Delete(download.TempDownloadPath);
                            }
                            if (File.Exists(download.DownloadPathFinal))
                            {
                                File.Delete(download.DownloadPathFinal);
                            }
                            download.Status = DownloadState.Deleting;
                            downloadsToDelete.Add(download);
                        }
                        else if (download.Status == DownloadState.Done)
                        {
                            download.Status = DownloadState.Deleting;
                            downloadsToDelete.Add(download);
                        }
                        else
                        {
                            download.Status = DownloadState.Deleting;
                            while (true)
                            {
                                if (download.DownloadThread.ThreadState == System.Threading.ThreadState.Stopped )
                                {
                                    if (File.Exists(download.TempDownloadPath))
                                    {
                                        File.Delete(download.TempDownloadPath);
                                    }
                                    if (File.Exists(download.DownloadPathFinal))
                                    {
                                        File.Delete(download.DownloadPathFinal);
                                    }
                                    downloadsToDelete.Add(download);
                                    break;
                                }
                            }
                        }
                    }

                    foreach (var download in downloadsToDelete)
                    {
                        download.Status = DownloadState.Deleted;
                        DownloadManager.Instance.DownloadsList.Remove(download);
                    }
                }
            }
        }
    }
}

