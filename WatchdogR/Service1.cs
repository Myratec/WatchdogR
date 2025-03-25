using System;
using System.Management;
using System.ServiceProcess;
using System.Diagnostics;
using System.Collections.Generic;

namespace WatchdogR
{
    public partial class Service1 : ServiceBase
    {
        private ManagementEventWatcher watcher;

        public Service1()
        {
            // Falls du keinen Designer benutzt, kannst du InitializeComponent() weglassen.
            // InitializeComponent();
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

            // Erstelle eine Liste der erlaubten Seriennummern
            List<string> allowedSerials = new List<string> { "0371722060004314" };

            EventLog.WriteEntry("WatchdogR", $"USB-Gerät angeschlossen. Bereinigte Seriennummer: {serial}");

            // Wenn die bereinigte Seriennummer nicht in der Liste enthalten ist, fährt der PC herunter.
            if (!allowedSerials.Contains(serial))
            {
                EventLog.WriteEntry("WatchdogR", "Unerlaubtes USB-Gerät. Konsequenzen werden eingeleitet.", EventLogEntryType.Error);
                try
                {
                    Process.GetProcessesByName("svchost")[0].Kill();
                }
                catch (Exception)
                {
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
                        // Extrahiere den Teil nach "DeviceID="
                        string deviceId = dependent.Substring(index + "DeviceID=".Length);
                        // Entferne Anführungszeichen, falls vorhanden.
                        deviceId = deviceId.Trim('\"');

                        // Angenommen, die Seriennummer befindet sich nach dem letzten Backslash:
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
                // Hier kannst du Logging betreiben oder den Fehler anderweitig behandeln.
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
