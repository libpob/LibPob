using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace LibPob
{
    internal class PerfTimer : IDisposable
    {
        private readonly string _message;
        private readonly Stopwatch _timer = new Stopwatch();

        public PerfTimer(string message = null)
        {
            _message = message;
            _timer.Restart();
        }

        public void Dispose()
        {
            _timer.Stop();

            Console.WriteLine($"[PerfTimer] {_message} finished in {_timer.ElapsedMilliseconds:N0}ms");
        }

        public static PerfTimer Start(
            [CallerMemberName] string callingMethodName = "",
            [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int callingLineNumber = 0)
        {
            var message = $"{callingMethodName} @ ({Path.GetFileName(callingFilePath)}:{callingLineNumber})";

            return new PerfTimer(message);
        }
    }
}
