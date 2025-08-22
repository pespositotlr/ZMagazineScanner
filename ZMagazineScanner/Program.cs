using ZMagazineScanner.Classes;
using ZMagazineScanner.Loggers;
using ZMagazineScanner.Utilities;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace ZMagazineScanner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            URLScanner scanner = new URLScanner();
            int startingInt = 22135;
            int issuesToCheck = 1000;
            var found = false;
            //await scanner.CheckIssueRange(startingInt, startingInt + 1000, "");

            //while (!found)
                found = await scanner.CheckIssueRangeParallel(startingInt, 50, issuesToCheck, "");

        }


    }
}
