using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Brokenmai;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;

namespace Brokenmai_win
{
    internal class Program
    {
        
        static Dictionary<string, iDeviceHandle> _devices = new Dictionary<string, iDeviceHandle>();

        private static Dictionary<string, iDeviceConnectionHandle> _connections =
            new Dictionary<string, iDeviceConnectionHandle>();
        static IiDeviceApi _idevice = LibiMobileDevice.Instance.iDevice;

        private static bool _exiting = false;

        private static PortConnection _port = new PortConnection();

        public static void Main(string[] args)
        {
            NativeLibraries.Load();
            
            var status = _idevice.idevice_event_subscribe(EventCallback, IntPtr.Zero);
            Console.WriteLine("Waiting for device...\nPress [Q] to quit");
            _port.Start();
            while (Console.ReadKey().Key != ConsoleKey.Q) { }

            _exiting = true;
        }
        
        private static void EventCallback(ref iDeviceEvent e, IntPtr data)
        {
            if (_exiting)
            {
                Thread.CurrentThread.Abort();
            }
            
            string udid = e.udidString;
            iDeviceError status;
            switch (e.@event)
            {
                case iDeviceEventType.DeviceAdd:
                {
                    Console.WriteLine("Device detected: {0}", udid);
                    if (_devices.ContainsKey(udid))
                    {
                        return;
                    }

                    iDeviceHandle handle;
                    status = _idevice.idevice_new(out handle, udid);
                    if (status != iDeviceError.Success)
                    {
                        Console.WriteLine("Connection failed: {0}", status);
                        return;
                    }

                    _devices[udid] = handle;

                    Thread thread = new Thread(Connect);
                    thread.Start(udid);
                    break;
                }
                case iDeviceEventType.DeviceRemove:
                {
                    if (_devices.ContainsKey(udid))
                    {
                        iDeviceHandle handle = _devices[udid];
                        handle.Dispose();
                        _devices.Remove(udid);
                    }
                    break;
                }
                case iDeviceEventType.DevicePaired:
                    Console.WriteLine("Device paired: {0}", udid);
                    break;
            }
        }

        private static void Connect(object arg)
        {
            string udid = (string) arg;
            if (!_devices.ContainsKey(udid))
            {
                return;
            }

            if (_exiting)
            {
                Thread.CurrentThread.Abort();
            }

            iDeviceHandle handle = _devices[udid];

            iDeviceConnectionHandle connHandle = iDeviceConnectionHandle.Zero;
            iDeviceError status;
            status = _idevice.idevice_connect(handle, 24864, out connHandle);
            if (status != iDeviceError.Success)
            {
                // Console.WriteLine("Waiting on device...");
                
                Thread.Sleep(1000);
                Thread thread = new Thread(Connect);
                thread.Start(udid);
                return;
            }
            
            Console.WriteLine("Device connected.");
            
            // Thread.Sleep(200);

            var data = new byte[256];
            uint read = 0;
            status = _idevice.idevice_connection_receive(connHandle, data, 14, ref read);
            if (status != iDeviceError.Success)
            {
                Console.WriteLine("Failed receiving data: {0}", status);
                _exiting = true;
                return;
            }

            if (
                data[0] != 'W' ||
                data[1] != 'E' ||
                data[2] != 'L' ||
                data[3] != 'C'
                )
            {
                Console.WriteLine("Invalid data from {0}", udid);
                connHandle.Dispose();
                _exiting = true;
                Thread.CurrentThread.Abort();
            }
            Console.WriteLine("Successfully connected to device {0}", udid);
            _connections[udid] = connHandle;

            {
                Thread thread = new Thread(ReadFromDevice);
                thread.Start(udid);
            }

            //connHandle.Dispose();
        }

        public static void ReadFromDevice(object arg)
        {
            string udid = (string) arg;
            if (!_devices.ContainsKey(udid))
            {
                return;
            }

            iDeviceConnectionHandle connHandle = _connections[udid];
            iDeviceError status;

            byte[] data = new byte[256];
            uint read = 0;
            while (true)
            {
                if (_exiting) break;
                status = _idevice.idevice_connection_receive_timeout(connHandle, data, 36, ref read, 5);
                if (status != iDeviceError.Success)
                {
                    if (status == iDeviceError.Timeout)
                    {
                        continue;
                    }
                    break;
                }

                if (data[0] != 'I')
                {
                    // Invalid header
                    Console.WriteLine("Invalid header!!");
                    break;
                }
                // Send package to com
                var sub = new byte[35];
                for (int i = 0; i <= 34; i++)
                {
                    sub[i] = data[i + 1];
                }
                // Console.WriteLine(sub.ToString());
                Console.WriteLine(Encoding.ASCII.GetString(sub));
                _port.Send(Encoding.ASCII.GetString(sub), 1);
            }
            connHandle.Dispose();
            _connections.Remove(udid);
            Console.WriteLine("Disconnected from device.");
            if (_exiting) return;
            Thread.Sleep(1000);
            {
                Thread thread = new Thread(Connect);
                thread.Start(udid);
            }
        }
    }
}