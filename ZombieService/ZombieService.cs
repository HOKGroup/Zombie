﻿#region References

using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.Linq;
using Zombie;
using Zombie.Utilities;
using ZombieService.Host;

#endregion

namespace ZombieService
{
    public partial class ZombieService : ServiceBase
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

        public ZombieService(IReadOnlyList<string> args)
        {
            InitializeComponent();

            Program.Host = HostUtils.CreateHost(Program.Host);
            Program.Settings = SettingsUtils.GetSettings(args);
            Program.Runner = new ZombieRunner(Program.Settings);
        }

        protected override void OnStart(string[] args)
        {
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif

            // (Konrad) Set host, settings and runner if they don't exist
            Program.Host = HostUtils.CreateHost(Program.Host);
            Program.Settings = SettingsUtils.GetSettings(args);
            Program.Runner = new ZombieRunner(Program.Settings);

            // Update the service state to Start Pending.  
            var serviceStatus = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_START_PENDING,
                dwWaitHint = 100000
            };
            SetServiceStatus(ServiceHandle, ref serviceStatus);

            // Update the service state to Running.  
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(ServiceHandle, ref serviceStatus);
        }

        protected override void OnStop()
        {
            Program.Host = HostUtils.TerminateHost(Program.Host);

            // Update the service state to Start Pending.  
            var serviceStatus = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_START_PENDING,
                dwWaitHint = 100000
            };
            SetServiceStatus(ServiceHandle, ref serviceStatus);

            // Update the service state to Running.  
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(ServiceHandle, ref serviceStatus);
        }
    }

    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public int dwServiceType;
        public ServiceState dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    }
}
