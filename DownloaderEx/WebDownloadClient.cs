using DownloaderEx.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;

namespace DownloaderEx
{
    public class WebDownloadClient : INotifyPropertyChanged
    {
        public string FileName { get; set; }
        public Uri Url { get; private set; }
        public string FileType { get { return Url.ToString().Substring(Url.ToString().LastIndexOf('.') + 1).ToUpper();}}
        public NetworkCredential ServerLogin = null;
        public WebProxy Proxy = null;
        public Thread DownloadThread = null;
        public string TempDownloadPath { get; set; }
        public string DownloadPath { get { return this.TempDownloadPath.Remove(this.TempDownloadPath.Length - 4);} }
        private string downloadPathFinal;
        public string DownloadPathFinal { get { return downloadPathFinal; } set { downloadPathFinal = value; } }
        public string DownloadFolder { get { return this.TempDownloadPath.Remove(TempDownloadPath.LastIndexOf("\\") + 1);}}
        public long FileSize { get; set; }
        public string FileSizeString{ get{ return DownloadManager.FormatSizeString(FileSize); }}
        public long DownloadedSize { get; set; }
        public string DownloadedSizeString { get {return DownloadManager.FormatSizeString(DownloadedSize + CachedSize);}}
        public float Percent { get{ return ((float)(DownloadedSize + CachedSize) / (float)FileSize) * 100F; }}

        public string PercentString
        {
            get
            {
                if (Percent < 0 || float.IsNaN(Percent))
                {
                    return "0.0%";
                } 
                else if (Percent > 100)
                {
                    return "100.0%";
                } 
                else
                {
                    return String.Format(numberFormat, "{0:0.0}%", Percent);
                }
                    
            }
        }

        public float Progress {get { return Percent;}}
        public int downloadSpeed;
        public string DownloadSpeed
        {
            get
            {
                if (this.Status == DownloadState.Downloading && !this.HasError)
                {
                    return DownloadManager.FormatSpeedString(downloadSpeed);
                }
                return String.Empty;
            }
        }

        private int speedUpdateCount;
        public string AverageDownloadSpeed { get { return DownloadManager.FormatSpeedString((int)Math.Floor((double)(DownloadedSize + CachedSize) / TotalElapsedTime.TotalSeconds));}}

        private List<int> downloadRates = new List<int>();
        private int recentAverageRate;
        public string TimeLeft
        {
            get
            {
                if (recentAverageRate > 0 && this.Status == DownloadState.Downloading && !this.HasError)
                {
                    double secondsLeft = (FileSize - DownloadedSize + CachedSize) / recentAverageRate;

                    TimeSpan span = TimeSpan.FromSeconds(secondsLeft);

                    return DownloadManager.FormatTimeSpanString(span);
                }
                return String.Empty;
            }
        }


        private DownloadState status;
        public DownloadState Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
                if (status != DownloadState.Deleting)
                {
                    RaiseStatusChanged();
                }
                    
            }
        }


        public string StatusText;
        public string StatusString
        {
            get
            {
                if (this.HasError)
                {
                    return StatusText;
                }   
                else
                {
                    return Status.ToString();
                }
                    
            }
            set
            {
                StatusText = value;
                RaiseStatusChanged();
            }
        }

        public TimeSpan ElapsedTime = new TimeSpan();
        private DateTime lastStartTime;
        public TimeSpan TotalElapsedTime
        {
            get
            {
                if (this.Status != DownloadState.Downloading)
                {
                    return ElapsedTime;
                }
                else
                {
                    return ElapsedTime.Add(DateTime.UtcNow - lastStartTime);
                }
            }
        }

        public string TotalElapsedTimeString
        {
            get
            {
                return DownloadManager.FormatTimeSpanString(TotalElapsedTime);
            }
        }

        private DateTime lastNotificationTime;
        private long lastNotificationDownloadedSize;
        public DateTime LastUpdateTime { get; set; }
        public DateTime AddedOn { get; set; }
        public string AddedOnString
        {
            get
            {
                string format = "dd.MM.yyyy. HH:mm:ss";
                return AddedOn.ToString(format);
            }
        }

        public DateTime CompletedOn { get; set; }
        public string CompletedOnString
        {
            get
            {
                if (CompletedOn != DateTime.MinValue)
                {
                    string format = "dd.MM.yyyy. HH:mm:ss";
                    return CompletedOn.ToString(format);
                }
                else
                    return String.Empty;
            }
        }

        public bool SupportsRange { get; set; }
        public bool HasError { get; set; }
        public bool OpenFileOnCompletion { get; set; }
        public bool TempFileCreated { get; set; }
        public bool IsSelected { get; set; }
        public bool SpeedLimitChanged { get; set; }
        public int BufferCountPerNotification { get; set; }
        public int BufferSize { get; set; }
        public int CachedSize { get; set; }
        public int MaxCacheSize { get; set; }
        private NumberFormatInfo numberFormat = NumberFormatInfo.InvariantInfo;
        private static object fileLocker = new object();

        public WebDownloadClient(string url)
        {
            this.BufferSize = 1024; 
            this.MaxCacheSize = Settings.Default.MemoryCacheSize * 1024;
            this.BufferCountPerNotification = 64;

            this.Url = new Uri(url, UriKind.Absolute);

            this.SupportsRange = false;
            this.HasError = false;
            this.OpenFileOnCompletion = false;
            this.TempFileCreated = false;
            this.IsSelected = false;
            this.SpeedLimitChanged = false;
            this.speedUpdateCount = 0;
            this.recentAverageRate = 0;
            this.StatusText = String.Empty;

            this.Status = DownloadState.Starting;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler StatusChanged;

        public event EventHandler DownloadProgressChanged;

        public event EventHandler DownloadCompleted;

        protected void RaisePropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        protected virtual void RaiseStatusChanged()
        {
            if (StatusChanged != null)
            {
                StatusChanged(this, EventArgs.Empty);
            }
        }

        protected virtual void RaiseDownloadProgressChanged()
        {
            if (DownloadProgressChanged != null)
            {
                DownloadProgressChanged(this, EventArgs.Empty);
            }
        }

        protected virtual void RaiseDownloadCompleted()
        {
            if (DownloadCompleted != null)
            {
                DownloadCompleted(this, EventArgs.Empty);
            }
        }

        public void DownloadProgressChangedHandler(object sender, EventArgs e)
        {
            if (DateTime.UtcNow > this.LastUpdateTime.AddSeconds(1))
            {
                CalculateDownloadSpeed();
                CalculateAverageRate();
                UpdateDownloadDisplay();
                this.LastUpdateTime = DateTime.UtcNow;
            }
        }

        public void DownloadCompletedHandler(object sender, EventArgs e)
        {
            if (!this.HasError)
            {
                if (File.Exists(this.DownloadPath))
                {
                    File.Delete(this.DownloadPath);
                }

                if (File.Exists(this.TempDownloadPath))
                {
                    File.Move(this.TempDownloadPath, this.DownloadPath);
                }

                this.Status = DownloadState.Done;
                UpdateDownloadDisplay();

                if (this.OpenFileOnCompletion && File.Exists(this.DownloadPath))
                {
                    Process.Start(@DownloadPath);
                }
            }
            else
            {
                this.Status = DownloadState.Error;
                UpdateDownloadDisplay();
            }
        }

        public void CheckUrl()
        {
            try
            {
                var webRequest = (HttpWebRequest)WebRequest.Create(this.Url);
                webRequest.Method = "HEAD";
                webRequest.Timeout = 5000;

                if (this.ServerLogin != null)
                {
                    webRequest.PreAuthenticate = true;
                    webRequest.Credentials = this.ServerLogin;
                }
                else
                {
                    webRequest.Credentials = CredentialCache.DefaultCredentials;
                }

                
                if (this.Proxy != null)
                {
                    webRequest.Proxy = this.Proxy;
                }
                else
                {
                    webRequest.Proxy = WebRequest.DefaultWebProxy;
                }

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                using (WebResponse response = webRequest.GetResponse())
                {
                    foreach (var header in response.Headers.AllKeys)
                    {
                        if (header.Equals("Accept-Ranges", StringComparison.OrdinalIgnoreCase))
                        {
                            this.SupportsRange = true;
                        }
                    }

                    this.FileSize = response.ContentLength;

                    if (this.FileSize <= 0)
                    {
                        this.FileSize = 1;
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось получить информацию о файле через URL", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.HasError = true;
            }
        }

        private void CheckBatchUrl()
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(this.Url);
            webRequest.Method = "HEAD";

            using (WebResponse response = webRequest.GetResponse())
            {
                foreach (var header in response.Headers.AllKeys)
                {
                    if (header.Equals("Accept-Ranges", StringComparison.OrdinalIgnoreCase))
                    {
                        this.SupportsRange = true;
                    }
                }

                this.FileSize = response.ContentLength;

                if (this.FileSize <= 0)
                {
                    this.StatusString = "Запрошеный файл не существует";
                    this.FileSize = 0;
                    this.HasError = true;
                }

                RaisePropertyChanged("FileSizeString");
            }
        }

        void CreateTempFile()
        {
            lock (fileLocker)
            {
                using (FileStream fileStream = File.Create(this.TempDownloadPath))
                {
                    long createdSize = 0;
                    byte[] buffer = new byte[4096];
                    while (createdSize < this.FileSize)
                    {
                        int bufferSize = (this.FileSize - createdSize) < 4096
                            ? (int)(this.FileSize - createdSize) : 4096;
                        fileStream.Write(buffer, 0, bufferSize);
                        createdSize += bufferSize;
                    }
                }
            }
        }

        void WriteCacheToFile(MemoryStream downloadCache, int cachedSize)
        {
            lock (fileLocker)
            {
                using (FileStream fileStream = new FileStream(TempDownloadPath, FileMode.Open))
                {
                    byte[] cacheContent = new byte[cachedSize];
                    downloadCache.Seek(0, SeekOrigin.Begin);
                    downloadCache.Read(cacheContent, 0, cachedSize);
                    fileStream.Seek(DownloadedSize, SeekOrigin.Begin);
                    fileStream.Write(cacheContent, 0, cachedSize);
                }
            }
        }

        private void CalculateDownloadSpeed()
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan interval = now - lastNotificationTime;
            double timeDiff = interval.TotalSeconds;
            double sizeDiff = (double)(DownloadedSize + CachedSize - lastNotificationDownloadedSize);

            downloadSpeed = (int)Math.Floor(sizeDiff / timeDiff);

            downloadRates.Add(downloadSpeed);

            lastNotificationDownloadedSize = DownloadedSize + CachedSize;
            lastNotificationTime = now;
        }

        private void CalculateAverageRate()
        {
            if (downloadRates.Count > 0)
            {
                if (downloadRates.Count > 10)
                    downloadRates.RemoveAt(0);

                int rateSum = 0;
                recentAverageRate = 0;
                foreach (int rate in downloadRates)
                {
                    rateSum += rate;
                }

                recentAverageRate = rateSum / downloadRates.Count;
            }
        }


        private void UpdateDownloadDisplay()
        {
            RaisePropertyChanged("DownloadedSizeString");
            RaisePropertyChanged("PercentString");
            RaisePropertyChanged("Progress");

            TimeSpan startInterval = DateTime.UtcNow - lastStartTime;
            if (speedUpdateCount == 0 || startInterval.TotalSeconds < 4 || this.HasError || this.Status == DownloadState.Paused
                || this.Status == DownloadState.Queued || this.Status == DownloadState.Done)
            {
                RaisePropertyChanged("DownloadSpeed");
            }
            speedUpdateCount++;
            if (speedUpdateCount == 4)
                speedUpdateCount = 0;

            RaisePropertyChanged("TimeLeft");
            RaisePropertyChanged("StatusString");
            RaisePropertyChanged("CompletedOnString");

            if (this.IsSelected)
            {
                RaisePropertyChanged("AverageSpeedAndTotalTime");
            }
        }

        private void ResetProperties()
        {
            HasError = false;
            TempFileCreated = false;
            DownloadedSize = 0;
            CachedSize = 0;
            speedUpdateCount = 0;
            recentAverageRate = 0;
            downloadRates.Clear();
            ElapsedTime = new TimeSpan();
            CompletedOn = DateTime.MinValue;
        }

        public void Start()
        {
            if (this.Status == DownloadState.Starting || this.Status == DownloadState.Paused
                || this.Status == DownloadState.Queued || this.HasError)
            {
                if (!this.SupportsRange && this.DownloadedSize > 0)
                {
                    this.StatusString = "Ошибка! Невозможно возобновить";
                    this.HasError = true;
                    this.RaiseDownloadCompleted();
                    return;
                }

                this.HasError = false;
                this.Status = DownloadState.Waiting;
                RaisePropertyChanged("StatusString");

                if (DownloadManager.Instance.ActiveDownloads > Settings.Default.MaxDownloads)
                {
                    this.Status = DownloadState.Queued;
                    RaisePropertyChanged("StatusString");
                    return;
                }

                DownloadThread = new Thread(new ThreadStart(DownloadFile));
                DownloadThread.IsBackground = true;
                DownloadThread.Start();
            }
        }

        public void Pause()
        {
            if (this.Status == DownloadState.Waiting || this.Status == DownloadState.Downloading)
            {
                this.Status = DownloadState.Pausing;
            }
            if (this.Status == DownloadState.Queued)
            {
                this.Status = DownloadState.Paused;
                RaisePropertyChanged("StatusString");
            }
        }

        public void Restart()
        {
            if (this.HasError || this.Status == DownloadState.Done)
            {
                if (File.Exists(this.TempDownloadPath))
                {
                    File.Delete(this.TempDownloadPath);
                }
                if (File.Exists(this.DownloadPath))
                {
                    File.Delete(this.DownloadPath);
                }

                ResetProperties();
                this.Status = DownloadState.Waiting;
                UpdateDownloadDisplay();

                if (DownloadManager.Instance.ActiveDownloads > Settings.Default.MaxDownloads)
                {
                    this.Status = DownloadState.Queued;
                    RaisePropertyChanged("StatusString");
                    return;
                }

                DownloadThread = new Thread(new ThreadStart(DownloadFile));
                DownloadThread.IsBackground = true;
                DownloadThread.Start();
            }
        }

        private void DownloadFile()
        {
            HttpWebRequest webRequest = null;
            HttpWebResponse webResponse = null;
            Stream responseStream = null;
            ThrottledStream throttledStream = null;
            MemoryStream downloadCache = null;
            speedUpdateCount = 0;
            recentAverageRate = 0;
            if (downloadRates.Count > 0)
            {
                downloadRates.Clear();
            }
               
            try
            {
               
                if (!TempFileCreated)
                {
                    CreateTempFile();
                    this.TempFileCreated = true;
                }

                this.lastStartTime = DateTime.UtcNow;

                if (this.Status == DownloadState.Waiting)
                {
                    this.Status = DownloadState.Downloading;
                }
                   
                webRequest = (HttpWebRequest)WebRequest.Create(this.Url);
                webRequest.Method = "GET";

                if (this.ServerLogin != null)
                {
                    webRequest.PreAuthenticate = true;
                    webRequest.Credentials = this.ServerLogin;
                }
                else
                {
                    webRequest.Credentials = CredentialCache.DefaultCredentials;
                }

                if (this.Proxy != null)
                {
                    webRequest.Proxy = this.Proxy;
                }
                else
                {
                    webRequest.Proxy = WebRequest.DefaultWebProxy;
                }

                webRequest.AddRange(DownloadedSize);
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                responseStream = webResponse.GetResponseStream();
                responseStream.ReadTimeout = 5000;
                long maxBytesPerSecond = 0;
                if (Settings.Default.EnableSpeedLimit)
                {
                    maxBytesPerSecond = (long)((Settings.Default.SpeedLimit * 1024) / DownloadManager.Instance.ActiveDownloads);
                }
                else
                {
                    maxBytesPerSecond = ThrottledStream.Infinite;
                }
                throttledStream = new ThrottledStream(responseStream, maxBytesPerSecond);

                downloadCache = new MemoryStream(this.MaxCacheSize);

                byte[] downloadBuffer = new byte[this.BufferSize];

                int bytesSize = 0;
                CachedSize = 0;
                int receivedBufferCount = 0;

                while (true)
                {
                    if (SpeedLimitChanged)
                    {
                        if (Settings.Default.EnableSpeedLimit)
                        {
                            maxBytesPerSecond = (long)((Settings.Default.SpeedLimit * 1024) / DownloadManager.Instance.ActiveDownloads);
                        }
                        else
                        {
                            maxBytesPerSecond = ThrottledStream.Infinite;
                        }
                        throttledStream.MaximumBytesPerSecond = maxBytesPerSecond;
                        SpeedLimitChanged = false;
                    }

                    bytesSize = throttledStream.Read(downloadBuffer, 0, downloadBuffer.Length);

                    if (this.Status != DownloadState.Downloading || bytesSize == 0 || this.MaxCacheSize < CachedSize + bytesSize)
                    {
                        WriteCacheToFile(downloadCache, CachedSize);

                        this.DownloadedSize += CachedSize;
                        downloadCache.Seek(0, SeekOrigin.Begin);
                        CachedSize = 0;
                        if (this.Status != DownloadState.Downloading || bytesSize == 0)
                        {
                            break;
                        }
                    }

                    downloadCache.Write(downloadBuffer, 0, bytesSize);
                    CachedSize += bytesSize;

                    receivedBufferCount++;
                    if (receivedBufferCount == this.BufferCountPerNotification)
                    {
                        this.RaiseDownloadProgressChanged();
                        receivedBufferCount = 0;
                    }
                }

                ElapsedTime = ElapsedTime.Add(DateTime.UtcNow - lastStartTime);

                if (this.Status != DownloadState.Deleting)
                {
                    if (this.Status == DownloadState.Pausing)
                    {
                        this.Status = DownloadState.Paused;
                        UpdateDownloadDisplay();
                    }
                    else if (this.Status == DownloadState.Queued)
                    {
                        UpdateDownloadDisplay();
                    }
                    else
                    {
                        this.CompletedOn = DateTime.UtcNow;
                        this.RaiseDownloadCompleted();
                    }
                }
            }
            catch (Exception ex)
            {
                this.StatusString = "Ошибка: " + ex.Message;
                this.HasError = true;
                this.RaiseDownloadCompleted();
            }
            finally
            {
                if (responseStream != null)
                {
                    responseStream.Close();
                }
                if (throttledStream != null)
                {
                    throttledStream.Close();
                }
                if (webResponse != null)
                {
                    webResponse.Close();
                }
                if (downloadCache != null)
                {
                    downloadCache.Close();
                }
                if (DownloadThread != null)
                {
                    DownloadThread.Abort();
                }
            }
        }
    }
}