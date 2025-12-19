using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xcelerator.LogEngine.Models
{
    public class LogResult
    {
        public string MachineName { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string LocalFilePath { get; set; }
    }
}
