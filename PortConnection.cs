using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace Brokenmai
{
    enum FunctionPackage
    {
        Rset = 69,
        Halt = 76,
        Ratio = 114,
        Sens = 107,
        Stat = 65
    }

    public class PortConnection
    {
        private readonly SerialPort _port1;
        private readonly SerialPort _port2;

        private bool _stage1 = false;
        private bool _stage2 = false;

        public PortConnection()
        {
            // Create ports for both players
            // DX receives input on COM3(1P) & COM4(2P)
            _port1 = new SerialPort("COM5", 9600, Parity.None, 8, StopBits.OnePointFive);
            _port2 = new SerialPort("COM6", 9600, Parity.None, 8, StopBits.OnePointFive);
            

            _port1.DataReceived += new SerialDataReceivedEventHandler(BytesReceived);
            _port2.DataReceived += new SerialDataReceivedEventHandler(BytesReceived);
        }

        public void Start()
        {
            _port1.Open();
            _port2.Open();
        }

        private void BytesReceived(Object sender, SerialDataReceivedEventArgs args)
        {
            SerialPort actualSender = (SerialPort) sender;
            if (!actualSender.IsOpen) return;
            int buffer = actualSender.BytesToRead;
            Byte[] bytes = new byte[buffer];
            actualSender.Read(bytes, 0, buffer);
            HandleSerialData(bytes, actualSender);
        }

        private void HandleSerialData(byte[] data, SerialPort sender)
        {
            // Check data[3] for incoming data type
            switch ((FunctionPackage)data[3])
            {
                case FunctionPackage.Rset:
                case FunctionPackage.Halt:
                    if (_stage1) return;
                    Console.WriteLine("Touchscreen startup 1/2");
                    _stage1 = !_stage1;
                    return;
                
                case FunctionPackage.Ratio:
                case FunctionPackage.Sens:
                    byte[] msg = {40, data[1], data[2], data[3], data[4], 41};
                    sender.Write(msg, 0, msg.Length);
                    if (_stage2) return;
                    Console.WriteLine("Touchscreen startup 2/2");
                    _stage2 = !_stage2;
                    return;

                case FunctionPackage.Stat:
                    // Check complete
                    return;
            }
        }

        public void Send(String packet, int player)
        {
            SerialPort sender = player == 1 ? _port1 : _port2;
            List<int> touchInfo = new List<int>();
            for (int i = 1; i <= 7; i++)
            {
                touchInfo.Add(Convert.ToInt32(packet.Substring(5 * i - 5, 5), 2));
            }

            byte[] bytes =
            {
                40,
                (byte) touchInfo[0],
                (byte) touchInfo[1],
                (byte) touchInfo[2],
                (byte) touchInfo[3],
                (byte) touchInfo[4],
                (byte) touchInfo[5],
                (byte) touchInfo[6],
                41
            };
            
            sender.Write(bytes, 0, bytes.Length);
            // Console.WriteLine("Touch sent.");
        }
    }
}