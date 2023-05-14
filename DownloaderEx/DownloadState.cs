using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloaderEx
{
    public enum DownloadState
    {
        Deleted,

        Starting,

        Waiting,

        Downloading,

        Pausing,

        Paused,

        Done,

        Error,

        Deleting,

        Queued,
    }
}
