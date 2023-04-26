using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Models
{
    public class FileChunkInfo
    {
        public string FileName { get; set; }
        public int TotalChunks { get; set; }
        public int CurrentChunkIndex { get; set; }
        public string FileChunkData { get; set; }
    }

}
