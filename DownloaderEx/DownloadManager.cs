using System;
using System.Collections.ObjectModel;
using System.Globalization;

namespace DownloaderEx
{
    public class DownloadManager
    {
        private static DownloadManager instance = new DownloadManager();

        public static DownloadManager Instance{ get {return instance;} set { instance = value; } }

        public int TotalDownloads { get { return DownloadsList.Count; } }

        private static NumberFormatInfo numberFormat = NumberFormatInfo.InvariantInfo;

        public ObservableCollection<WebDownloadClient> DownloadsList = new ObservableCollection<WebDownloadClient>();

        public int ActiveDownloads
        {
            get
            {
                int active = 0;
                foreach (WebDownloadClient el in DownloadsList)
                {
                    if (!el.HasError)
                    {
                        if (el.Status == DownloadState.Waiting || el.Status == DownloadState.Downloading)
                        {
                            active++;
                        }        
                    }
                }
                return active;
            }
        }

        public int CompletedDownloads
        {
            get
            {
                int donecount = 0;
                foreach (WebDownloadClient el in DownloadsList)
                {
                    if (el.Status == DownloadState.Done)
                    {
                        donecount++;
                    }       
                }
                return donecount;
            }
        }

        public static string FormatSizeString(long bytes)
        {
            double kbSize = (double)bytes / 1024D;
            double mbSize = kbSize / 1024D;
            double gbSize = mbSize / 1024D;

            if (bytes < 1024)
            {
                return String.Format(numberFormat, "{0} B", bytes);
            }else if (bytes < 1048576)
            {
                return String.Format(numberFormat, "{0:0.00} kB", kbSize);
            }else if (bytes < 1073741824)
            {
                return String.Format(numberFormat, "{0:0.00} MB", mbSize);
            }else
            {
                return String.Format(numberFormat, "{0:0.00} GB", gbSize);
            }
                
        }

        public static string FormatSpeedString(int sp)
        {
            float kbSpeed = (float)sp / 1024F;
            float mbSpeed = kbSpeed / 1024F;

            if (sp <= 0)
            {
                return String.Empty;
            }else if (sp < 1024)
            {
                return sp.ToString() + " B/s";
            }
            else if (sp < 1048576)
            {
                return kbSpeed.ToString("#.00", numberFormat) + " kB/s";
            }
            else
            {
                return mbSpeed.ToString("#.00", numberFormat) + " MB/s";
            }
                
        }

        public static string FormatTimeSpanString(TimeSpan time)
        {
            string h = ((int)time.TotalHours).ToString();
            string m = time.Minutes.ToString();
            string s = time.Seconds.ToString();
            if ((int)time.TotalHours < 10)
            {
                h = "0" + h;
            }   
            if (time.Minutes < 10)
            {
                m = "0" + m;
            }   
            if (time.Seconds < 10)
            {
                s = "0" + s;
            }

            return String.Format($"{h}:{m}:{s}");
        }
    }
}
