using System;
using System.Windows;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XpTicker
{
    public class ForegroundTracker
    {
        public static Process p;
        public static Stopwatch watcher;
        public static Queue processQueue;
        public static List<ProcessDetails> processList;
        public ForegroundTracker()
        {
            
        }

        // Delegate and imports from pinvoke.net:

        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr
           hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
           uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        // Constants from winuser.h
        const uint EVENT_SYSTEM_FOREGROUND = 3;
        const uint WINEVENT_OUTOFCONTEXT = 0;

        // Need to ensure delegate is not collected while we're using it,
        // storing it in a class field is simplest way to do this.
        static WinEventDelegate procDelegate = new WinEventDelegate(WinEventProc);

        public static void Main()
        {
            processQueue = new Queue();
            processList = new List<ProcessDetails>();
            // Listen for foreground changes across all processes/threads on current desktop...
            IntPtr hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero,
                    procDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);

            // MessageBox provides the necessary mesage loop that SetWinEventHook requires.

            //MessageBox.Show("Tracking focus, close message box to exit.");
            Application.Run(new Form1());

            UnhookWinEvent(hhook);
        }
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        static void WinEventProc(IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (p != null && watcher != null)
            {
                watcher.Stop();
                var process = new ProcessDetails
                {
                    ProcessId = p.Id,
                    ProcessName = p.ProcessName,
                    TimeElapsed = watcher.Elapsed
                };
                processQueue.Enqueue(process);
                Task.Run(async () =>
                {
                    await ProcessQueue().ConfigureAwait(false);
                });
                Console.WriteLine(p.Id + ":" + p.ProcessName + ". Time elapsed: " + watcher.Elapsed);
            }
            watcher = new Stopwatch();
            watcher.Start();
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            p = Process.GetProcessById((int)pid);
            Console.WriteLine(p.Id + ":" + p.ProcessName);
            
            //Console.WriteLine("Foreground changed to {0:x8}", hwnd.ToInt32());
        }

        public static async Task ProcessQueue()
        {
            await Task.Run( () => {
                if (processQueue.Count != 0)
                {
                    var obj = (ProcessDetails)processQueue.Dequeue();
                    var existingProcess = processList.FirstOrDefault(e => e.ProcessId == obj.ProcessId);
                    if (existingProcess == null)
                    {
                        processList.Add(obj);
                    }
                    else
                    {
                        existingProcess.TimeElapsed = existingProcess.TimeElapsed.Add(obj.TimeElapsed);
                    }
                }
            });
            
        }
    }
}