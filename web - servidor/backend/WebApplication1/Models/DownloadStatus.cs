using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Models
{
    public class DownloadStatus
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public int TotalParts { get; set; }
        public int ReceivedParts { get; set; }
        public string Status { get; set; }
    }

}
