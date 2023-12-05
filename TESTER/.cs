using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TESTER
{
    internal class HarnessInfo
    {
     //   public string BrowserVersion { get; set; }
        public string BuildNumber { get; set; }
        public string Revision { get; set; }
        public string DBAddress { get; set; }
        public DateTime BuildDate { get; set; }

        // Konstruktor klasy
        public HarnessInfo(string buildNumber, DateTime buildDate, string revision, string dBAddress)//, string browserVersion)
        {
            // BrowserVersion = browserVersion;
            BuildNumber = buildNumber;
            BuildDate = buildDate;
            Revision = revision;
            DBAddress = dBAddress;
        }
    }
}
