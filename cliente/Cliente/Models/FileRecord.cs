using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cliente.Models
{
    public class FileRecord
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public int TotalChunks { get; set; }
        public int ReceivedChunks { get; set; }
    }

}
