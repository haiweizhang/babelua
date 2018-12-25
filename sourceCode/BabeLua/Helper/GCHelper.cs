using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Babe.Lua.Helper
{
    class GCHelper:IDisposable
    {
        static Timer _timer = new Timer(10000);
        static bool IsWaiting = false;

        static GCHelper()
        {
            _timer.Elapsed += _timer_Elapsed;
            _timer.AutoReset = false;
        }

        static void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            GC.Collect();
            IsWaiting = false;
        }

        public static void Collect()
        {
            if (IsWaiting)
            {
                _timer.Stop();
                _timer.Interval = 10000;
            }
            else
            {
                IsWaiting = true;
            }
            _timer.Start();
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
