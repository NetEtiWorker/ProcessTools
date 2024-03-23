using System.Diagnostics;
using System.Management;

namespace NetEti.ApplicationControl
{
    /// <summary>
    /// Statische Routinen für den Umgang mit Prozessen.
    /// </summary>
    public static class ProcessWorker
    {
        #region public members

        /// <summary>
        /// Wartet, bis alle Child-Prozesse beendet sind oder ein Countdown abgelaufen ist.
        /// Wenn bei abgelaufenem Countdown noch nicht alle Prozesse beendet sind, werden sie abgeschossen.
        /// </summary>
        /// <param name="process">Der Prozess, dessen Child-Prozesse beendet werden sollen.</param>
        /// <param name="countdown">Zählt rückwärts, bei 1 werden alle Child-Prozesse beendet (default=6).</param>
        public static void FinishChildProcesses(Process process, int countdown = 6)
        {
            UInt32 pid = (UInt32)(process.Id);
            Thread t = new Thread(() => WaitForChildProcessesCloseOrKillThemAtLast(pid, countdown));
            t.Start();
            while (t.IsAlive)
            {
                Thread.Sleep(500);
            }
        }

        #endregion public members

        #region private members

        private static void WaitForChildProcessesCloseOrKillThemAtLast(UInt32 pid, int countdown = 6)
        {
            HandleAllProcessesSpawnedBy(pid);
        }

        // https://stackoverflow.com/questions/7189117/find-all-child-processes-of-my-own-net-process-find-out-if-a-given-process-is
        // https://stackoverflow.com/questions/58216860/how-to-handle-not-enough-quota-is-available-to-process-this-command-exception
        private static void HandleAllProcessesSpawnedBy(UInt32 parentProcessId, int countdown = 6)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("System.Management ist nur für Windows implementiert.");
            }
            // NOTE: Process Ids are reused!
            for (int i = countdown; i > 0; i--)
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT * " +
                    "FROM Win32_Process " +
                    "WHERE ParentProcessId=" + parentProcessId))
                {
                    ManagementObjectCollection collection = searcher.Get();
                    if (collection.Count < 1) { break; } // Nichts zu tun, Routine verlassen.
                    if (i > 1) { Thread.Sleep(250); continue; } // Warten und nächster Durchlauf.
                    foreach (var item in collection) // Letzter Durchlauf, Prozess abschießen.
                    {
                        UInt32 childProcessId = (UInt32)item["ProcessId"];
                        if ((int)childProcessId != Process.GetCurrentProcess().Id)
                        {
                            HandleAllProcessesSpawnedBy(childProcessId, i);
                            try
                            {
                                Process childProcess = Process.GetProcessById((int)childProcessId);
                                childProcess.Kill();
                            }
                            catch { }
                            // MessageBox.Show("Child process " + childProcessId + " killed (Countdown: " + i + ").");
                        }
                    }
                }
            }
        }

        #endregion private members
    }
}
