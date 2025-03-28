﻿using System;
using System.Management;
using System.ServiceProcess;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WatchdogR
{
    public partial class Service1 : ServiceBase
    {
        private ManagementEventWatcher watcher;

        [DllImport("ntdll.dll")]
        public static extern uint RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);

        [DllImport("ntdll.dll")]
        public static extern uint NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters, uint UnicodeStringParameterMask, IntPtr Parameters, uint ValidResponseOption, out uint Response);

        public Service1()
        {

        }

        protected override void OnStart(string[] args)
        {
            // WMI-Abfrage: Überwacht das Einstecken eines USB-Geräts
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBControllerDevice'");
            watcher = new ManagementEventWatcher(query);
            watcher.EventArrived += new EventArrivedEventHandler(DeviceInserted);
            watcher.Start();
        }

        private void DeviceInserted(object sender, EventArrivedEventArgs e)
        {
            // Hole die Rohseriennummer des angeschlossenen USB-Geräts
            string rawSerial = GetDeviceSerial(e);
            EventLog.WriteEntry("WatchdogR", $"USB-Gerät angeschlossen. Roh Seriennummer: {rawSerial}");

            // Trimme die Seriennummer: Alles ab dem ersten Vorkommen eines '&' wird entfernt.
            string serial = rawSerial;
            int pos = serial.IndexOf('&');
            if (pos >= 0)
            {
                serial = serial.Substring(0, pos);
            }
            serial = serial.Trim();

            // Liste der erlaubten Seriennummern
            List<string> allowedSerials = new List<string> { "0371722060004314" };

            EventLog.WriteEntry("WatchdogR", $"USB-Gerät angeschlossen. Bereinigte Seriennummer: {serial}");

            // Wenn die bereinigte Seriennummer nicht in der Liste enthalten ist --> BSOD
            if (!allowedSerials.Contains(serial))
            {
                EventLog.WriteEntry("WatchdogR", "Unerlaubtes USB-Gerät. Konsequenzen werden eingeleitet.", EventLogEntryType.Warning);
                try
                {
                    Boolean t1;
                    uint t2;
                    RtlAdjustPrivilege(19, true, false, out t1);
                    NtRaiseHardError(0xc0000022, 0, 0, IntPtr.Zero, 6, out t2);
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("WatchdogR", $"Fehler Ausführung BSOD: {ex.Message}", EventLogEntryType.Error);
                    Process.Start("shutdown", "/s /f /t 0");
                }
            }
        }

        private string GetDeviceSerial(EventArrivedEventArgs e)
        {
            string serial = "";
            try
            {
                // Das WMI-Event enthält das Property "TargetInstance", das ein Win32_USBControllerDevice-Objekt darstellt.
                ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                // Das Property "Dependent" enthält typischerweise einen Verweis auf das angeschlossene Gerät,
                // z.B. "Win32_PnPEntity.DeviceID=\"USB\\VID_1234&PID_5678\\SERIALNUMBER\""
                string dependent = instance["Dependent"]?.ToString() ?? "";

                if (!string.IsNullOrEmpty(dependent))
                {
                    // Sucht nach "DeviceID=" im String und extrahiert den anschließenden Teil.
                    int index = dependent.IndexOf("DeviceID=");
                    if (index >= 0)
                    {
                        string deviceId = dependent.Substring(index + "DeviceID=".Length);
                        deviceId = deviceId.Trim('\"');

                        int lastBackslash = deviceId.LastIndexOf('\\');
                        if (lastBackslash >= 0 && lastBackslash < deviceId.Length - 1)
                        {
                            serial = deviceId.Substring(lastBackslash + 1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("WatchdogR", $"Fehler beim Auslesen der Seriennummer: {ex.Message}", EventLogEntryType.Error);
            }
            return serial;
        }

        protected override void OnStop()
        {
            if (watcher != null)
            {
                watcher.Stop();
                watcher.Dispose();
            }
        }
    }
}
