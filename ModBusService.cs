using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModBusDevExpress.Service
{
    internal class ModBusService
    {
        private static readonly ushort[] CRCTable = new ushort[256]
           {
              (ushort) 0,
              (ushort) 49345,
              (ushort) 49537,
              (ushort) 320,
              (ushort) 49921,
              (ushort) 960,
              (ushort) 640,
              (ushort) 49729,
              (ushort) 50689,
              (ushort) 1728,
              (ushort) 1920,
              (ushort) 51009,
              (ushort) 1280,
              (ushort) 50625,
              (ushort) 50305,
              (ushort) 1088,
              (ushort) 52225,
              (ushort) 3264,
              (ushort) 3456,
              (ushort) 52545,
              (ushort) 3840,
              (ushort) 53185,
              (ushort) 52865,
              (ushort) 3648,
              (ushort) 2560,
              (ushort) 51905,
              (ushort) 52097,
              (ushort) 2880,
              (ushort) 51457,
              (ushort) 2496,
              (ushort) 2176,
              (ushort) 51265,
              (ushort) 55297,
              (ushort) 6336,
              (ushort) 6528,
              (ushort) 55617,
              (ushort) 6912,
              (ushort) 56257,
              (ushort) 55937,
              (ushort) 6720,
              (ushort) 7680,
              (ushort) 57025,
              (ushort) 57217,
              (ushort) 8000,
              (ushort) 56577,
              (ushort) 7616,
              (ushort) 7296,
              (ushort) 56385,
              (ushort) 5120,
              (ushort) 54465,
              (ushort) 54657,
              (ushort) 5440,
              (ushort) 55041,
              (ushort) 6080,
              (ushort) 5760,
              (ushort) 54849,
              (ushort) 53761,
              (ushort) 4800,
              (ushort) 4992,
              (ushort) 54081,
              (ushort) 4352,
              (ushort) 53697,
              (ushort) 53377,
              (ushort) 4160,
              (ushort) 61441,
              (ushort) 12480,
              (ushort) 12672,
              (ushort) 61761,
              (ushort) 13056,
              (ushort) 62401,
              (ushort) 62081,
              (ushort) 12864,
              (ushort) 13824,
              (ushort) 63169,
              (ushort) 63361,
              (ushort) 14144,
              (ushort) 62721,
              (ushort) 13760,
              (ushort) 13440,
              (ushort) 62529,
              (ushort) 15360,
              (ushort) 64705,
              (ushort) 64897,
              (ushort) 15680,
              (ushort) 65281,
              (ushort) 16320,
              (ushort) 16000,
              (ushort) 65089,
              (ushort) 64001,
              (ushort) 15040,
              (ushort) 15232,
              (ushort) 64321,
              (ushort) 14592,
              (ushort) 63937,
              (ushort) 63617,
              (ushort) 14400,
              (ushort) 10240,
              (ushort) 59585,
              (ushort) 59777,
              (ushort) 10560,
              (ushort) 60161,
              (ushort) 11200,
              (ushort) 10880,
              (ushort) 59969,
              (ushort) 60929,
              (ushort) 11968,
              (ushort) 12160,
              (ushort) 61249,
              (ushort) 11520,
              (ushort) 60865,
              (ushort) 60545,
              (ushort) 11328,
              (ushort) 58369,
              (ushort) 9408,
              (ushort) 9600,
              (ushort) 58689,
              (ushort) 9984,
              (ushort) 59329,
              (ushort) 59009,
              (ushort) 9792,
              (ushort) 8704,
              (ushort) 58049,
              (ushort) 58241,
              (ushort) 9024,
              (ushort) 57601,
              (ushort) 8640,
              (ushort) 8320,
              (ushort) 57409,
              (ushort) 40961,
              (ushort) 24768,
              (ushort) 24960,
              (ushort) 41281,
              (ushort) 25344,
              (ushort) 41921,
              (ushort) 41601,
              (ushort) 25152,
              (ushort) 26112,
              (ushort) 42689,
              (ushort) 42881,
              (ushort) 26432,
              (ushort) 42241,
              (ushort) 26048,
              (ushort) 25728,
              (ushort) 42049,
              (ushort) 27648,
              (ushort) 44225,
              (ushort) 44417,
              (ushort) 27968,
              (ushort) 44801,
              (ushort) 28608,
              (ushort) 28288,
              (ushort) 44609,
              (ushort) 43521,
              (ushort) 27328,
              (ushort) 27520,
              (ushort) 43841,
              (ushort) 26880,
              (ushort) 43457,
              (ushort) 43137,
              (ushort) 26688,
              (ushort) 30720,
              (ushort) 47297,
              (ushort) 47489,
              (ushort) 31040,
              (ushort) 47873,
              (ushort) 31680,
              (ushort) 31360,
              (ushort) 47681,
              (ushort) 48641,
              (ushort) 32448,
              (ushort) 32640,
              (ushort) 48961,
              (ushort) 32000,
              (ushort) 48577,
              (ushort) 48257,
              (ushort) 31808,
              (ushort) 46081,
              (ushort) 29888,
              (ushort) 30080,
              (ushort) 46401,
              (ushort) 30464,
              (ushort) 47041,
              (ushort) 46721,
              (ushort) 30272,
              (ushort) 29184,
              (ushort) 45761,
              (ushort) 45953,
              (ushort) 29504,
              (ushort) 45313,
              (ushort) 29120,
              (ushort) 28800,
              (ushort) 45121,
              (ushort) 20480,
              (ushort) 37057,
              (ushort) 37249,
              (ushort) 20800,
              (ushort) 37633,
              (ushort) 21440,
              (ushort) 21120,
              (ushort) 37441,
              (ushort) 38401,
              (ushort) 22208,
              (ushort) 22400,
              (ushort) 38721,
              (ushort) 21760,
              (ushort) 38337,
              (ushort) 38017,
              (ushort) 21568,
              (ushort) 39937,
              (ushort) 23744,
              (ushort) 23936,
              (ushort) 40257,
              (ushort) 24320,
              (ushort) 40897,
              (ushort) 40577,
              (ushort) 24128,
              (ushort) 23040,
              (ushort) 39617,
              (ushort) 39809,
              (ushort) 23360,
              (ushort) 39169,
              (ushort) 22976,
              (ushort) 22656,
              (ushort) 38977,
              (ushort) 34817,
              (ushort) 18624,
              (ushort) 18816,
              (ushort) 35137,
              (ushort) 19200,
              (ushort) 35777,
              (ushort) 35457,
              (ushort) 19008,
              (ushort) 19968,
              (ushort) 36545,
              (ushort) 36737,
              (ushort) 20288,
              (ushort) 36097,
              (ushort) 19904,
              (ushort) 19584,
              (ushort) 35905,
              (ushort) 17408,
              (ushort) 33985,
              (ushort) 34177,
              (ushort) 17728,
              (ushort) 34561,
              (ushort) 18368,
              (ushort) 18048,
              (ushort) 34369,
              (ushort) 33281,
              (ushort) 17088,
              (ushort) 17280,
              (ushort) 33601,
              (ushort) 16640,
              (ushort) 33217,
              (ushort) 32897,
              (ushort) 16448
           };

        public static byte[] CRC16(byte[] data, int length)
        {
            ushort num1 = ushort.MaxValue;
            for (int index = 0; index < length; ++index)
            {
                byte num2 = (byte)((uint)data[index] ^ (uint)num1);
                num1 = (ushort)((uint)(ushort)((uint)num1 >> 8) ^ (uint)ModBusService.CRCTable[(int)num2]);
            }
            return BitConverter.GetBytes(num1);
        }
    }
    internal class ByteCvt
    {
        public static byte HI4BITS(byte n) => (byte)((int)n >> 4 & 15);
        public static byte LO4BITS(byte n) => (byte)((uint)n & 15U);
        public static uint MakeLong(ushort high, ushort low) => (uint)((int)low & (int)ushort.MaxValue | ((int)high & (int)ushort.MaxValue) << 16);
        public static ushort MakeWord(byte high, byte low) => (ushort)((int)low & (int)byte.MaxValue | ((int)high & (int)byte.MaxValue) << 8);
        public static ushort LoWord(uint nValue) => (ushort)(nValue & (uint)ushort.MaxValue);
        public static ushort HiWord(uint nValue) => (ushort)(nValue >> 16);
        public static byte LoByte(ushort nValue) => (byte)((uint)nValue & (uint)byte.MaxValue);
        public static byte HiByte(ushort nValue) => (byte)((uint)nValue >> 8);
    }
    internal class AsciiCvt
    {
        public static byte Num2Ascii(byte nNum)
        {
            if (nNum <= (byte)9)
                return (byte)((uint)nNum + 48U);
            return nNum >= (byte)10 && nNum <= (byte)15 ? (byte)((int)nNum - 10 + 65) : (byte)48;
        }
        public static byte Ascii2Num(byte nChar)
        {
            if (nChar >= (byte)48 && nChar <= (byte)57)
                return (byte)((uint)nChar - 48U);
            return nChar >= (byte)65 && nChar <= (byte)70 ? (byte)((int)nChar - 65 + 10) : (byte)0;
        }
        public static byte HiLo4BitsToByte(byte nHi, byte nLo) => (byte)((15 & (int)nHi) << 4 | 15 & (int)nLo);
        public static void RTU2ASCII(byte[] nRtu, int Size, byte[] nAscii)
        {
            for (int index = 0; index < Size; ++index)
            {
                nAscii[1 + index * 2] = AsciiCvt.Num2Ascii(ByteCvt.HI4BITS(nRtu[index]));
                nAscii[1 + index * 2 + 1] = AsciiCvt.Num2Ascii(ByteCvt.LO4BITS(nRtu[index]));
            }
        }
        public static byte LRC(byte[] nMsg, int DataLen)
        {
            byte num = 0;
            for (int index = 0; index < DataLen; ++index)
                num += nMsg[index];
            return (byte)-num;
        }
        public static byte LRAsciiCvt(byte[] MsgASCII, int DataLen)
        {
            byte num1 = 0;
            int num2 = (DataLen - 5) / 2;
            for (int index = 0; index < num2; ++index)
            {
                byte num3 = AsciiCvt.HiLo4BitsToByte(AsciiCvt.Ascii2Num(MsgASCII[1 + index * 2]), AsciiCvt.Ascii2Num(MsgASCII[1 + index * 2 + 1]));
                num1 += num3;
            }
            return (byte)-num1;
        }
        public static bool VerifyRespLRC(byte[] Resp, int Length) => Length >= 5 && (int)AsciiCvt.LRAsciiCvt(Resp, Length) == (int)AsciiCvt.HiLo4BitsToByte(AsciiCvt.Ascii2Num(Resp[Length - 4]), AsciiCvt.Ascii2Num(Resp[Length - 3]));
    }

    internal class clsTxRx
    {
        private Mode _Mode;
        private TcpClient client;
        private UdpClient udpClient;
        private bool _connected;
        private string Error = "";
        private int _Timeout = 2000;
        private ushort TransactionID;
        private byte[] _TxBuf = new byte[600];
        private byte[] _RxBuf = new byte[600];
        private int _TxBufSize;
        private int _RxBufSize;

        public void SetClient(TcpClient _client) => this.client = _client;

        public void SetClient(UdpClient _client) => this.udpClient = _client;

        public Mode Mode
        {
            get => this._Mode;
            set => this._Mode = value;
        }

        public int Timeout
        {
            get => this._Timeout;
            set => this._Timeout = value;
        }

        public bool connected
        {
            get => this._connected;
            set => this._connected = value;
        }

        public string GetErrorMessage() => this.Error;

        public int GetTxBuffer(byte[] byteArray)
        {
            if (byteArray.GetLength(0) < this._TxBufSize)
                return 0;
            Array.Copy((Array)this._TxBuf, (Array)byteArray, this._TxBufSize);
            return this._TxBufSize;
        }

        public int GetRxBuffer(byte[] byteArray)
        {
            if (byteArray.GetLength(0) < this._RxBufSize)
                return 0;
            Array.Copy((Array)this._RxBuf, (Array)byteArray, this._RxBufSize);
            return this._RxBufSize;
        }

        public Result TxRx(byte[] TXBuf, int QueryLength, byte[] RXBuf, int ResponseLength)
        {
            int num1 = Environment.TickCount & int.MaxValue;
            this._TxBufSize = 0;
            this._RxBufSize = 0;
            if (!this._connected)
                return Result.ISCLOSED;
            switch (this._Mode)
            {
                case Mode.TCP_IP:
                case Mode.RTU_OVER_TCP_IP:
                case Mode.ASCII_OVER_TCP_IP:
                    if (this.client == null)
                        return Result.ISCLOSED;
                    try
                    {
                        if (!this.client.Connected)
                            return Result.ISCLOSED;
                        break;
                    }
                    catch (Exception ex)
                    {
                        this.Error = ex.Message;
                        return Result.ISCLOSED;
                    }
                case Mode.UDP_IP:
                    if (this.udpClient == null)
                        return Result.ISCLOSED;
                    break;
            }
            Result result;
            switch (this._Mode)
            {
                case Mode.TCP_IP: // 이더넷 연결
                    if (ResponseLength == int.MaxValue)
                        return Result.ILLEGAL_FUNCTION;
                    result = this.TxRxTCP(TXBuf, QueryLength, RXBuf, ResponseLength);
                    break;
                case Mode.RTU_OVER_TCP_IP: //rs485 연결
                    result = this.TxRxRTUOverTCP(TXBuf, QueryLength, RXBuf, ResponseLength);
                    break;
                case Mode.ASCII_OVER_TCP_IP:
                    result = this.TxRxASCIIOverTCP(TXBuf, QueryLength, RXBuf, ResponseLength);
                    break;
                case Mode.UDP_IP:
                    if (ResponseLength == int.MaxValue)
                        return Result.ILLEGAL_FUNCTION;
                    result = this.TxRxUDP(TXBuf, QueryLength, RXBuf, ResponseLength);
                    break;
                default:
                    result = Result.SUCCESS;
                    break;
            }
            return result;
        }

        private void DiscardReadBuffer()
        {
            switch (this._Mode)
            {
                case Mode.TCP_IP:
                case Mode.RTU_OVER_TCP_IP:
                case Mode.ASCII_OVER_TCP_IP:
                    byte[] buffer = new byte[100];
                    if (!this.client.GetStream().CanRead)
                        break;
                    while (this.client.GetStream().DataAvailable)
                        this.client.GetStream().Read(buffer, 0, 100);
                    break;
            }
        }

        private bool ResponseTimeout()
        {
            int num = Environment.TickCount & int.MaxValue;
            int millisecondsTimeout = this._Timeout / 100;
            if (millisecondsTimeout == 0)
                millisecondsTimeout = 1;
            while (!this.client.GetStream().DataAvailable)
            {
                if (Math.Abs((Environment.TickCount & int.MaxValue) - num) > this._Timeout)
                    return true;
                Thread.Sleep(millisecondsTimeout);
            }
            return false;
        }

        private Result TxRxTCP(byte[] TXBuf, int QueryLength, byte[] RXBuf, int ResponseLength)
        {
            int num = 0;
            ++this.TransactionID;
            this.DiscardReadBuffer();
            for (int index = 0; index < 5; ++index)
                this._TxBuf[index] = (byte)0;
            this._TxBuf[0] = (byte)((uint)this.TransactionID >> 8);
            this._TxBuf[1] = (byte)((uint)this.TransactionID & (uint)byte.MaxValue);
            this._TxBuf[4] = (byte)(QueryLength >> 8);
            this._TxBuf[5] = (byte)(QueryLength & (int)byte.MaxValue);
            for (int index = 0; index < QueryLength; ++index)
                this._TxBuf[6 + index] = TXBuf[index];
            if (this.client.GetStream().CanWrite)
            {
                try
                {
                    this.client.GetStream().Write(this._TxBuf, 0, QueryLength + 6);
                }
                catch (Exception ex)
                {
                    this.Error = ex.Message;
                    return Result.WRITE;
                }
                this._TxBufSize = QueryLength + 6;
                if (this.client.GetStream().CanRead)
                {
                    try
                    {
                        do
                        {
                            if (this.ResponseTimeout())
                                return Result.RESPONSE_TIMEOUT;
                            num += this.client.GetStream().Read(this._RxBuf, num, ResponseLength + 6 - num);
                            switch (num)
                            {
                                case 0:
                                    return Result.RESPONSE_TIMEOUT;
                                case 9:
                                    if (this._RxBuf[7] > (byte)128)
                                    {
                                        this._RxBufSize = num;
                                        return (Result)this._RxBuf[8];
                                    }
                                    break;
                            }
                        }
                        while (num < ResponseLength + 6);
                        if ((int)this.TransactionID != (int)ByteCvt.MakeWord(this._RxBuf[0], this._RxBuf[1]))
                        {
                            this.DiscardReadBuffer();
                            this._RxBufSize = num;
                            return Result.TRANSACTIONID;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Error = ex.Message;
                        return Result.READ;
                    }
                    finally
                    {
                        this._RxBufSize = num;
                    }
                    if (num - 6 < ResponseLength)
                        return Result.RESPONSE_TIMEOUT;
                    for (int index = 0; index < Math.Min(num, ResponseLength + 6) - 6; ++index)
                        RXBuf[index] = this._RxBuf[index + 6];
                    return Result.SUCCESS;
                }
                this.Error = "You cannot read from this NetworkStream.";
                return Result.READ;
            }
            this.Error = "You cannot write to this NetworkStream.";
            return Result.WRITE;
        }

        private Result TxRxRTUOverTCP(
          byte[] TXBuf,
          int QueryLength,
          byte[] RXBuf,
          int ResponseLength)
        {
            int offset = 0;
            byte[] numArray1 = ModBusService.CRC16(TXBuf, QueryLength);
            TXBuf[QueryLength] = numArray1[0];
            TXBuf[QueryLength + 1] = numArray1[1];
            this.DiscardReadBuffer();
            if (this.client.GetStream().CanWrite)
            {
                try
                {
                    this.client.GetStream().Write(TXBuf, 0, QueryLength + 2);
                }
                catch (Exception ex)
                {
                    this.Error = ex.Message;
                    return Result.WRITE;
                }
                this._TxBufSize = QueryLength + 2;
                Array.Copy((Array)TXBuf, (Array)this._TxBuf, this._TxBufSize);
                if (TXBuf[0] == (byte)0)
                    return Result.SUCCESS;
                if (this.client.GetStream().CanRead)
                {
                    try
                    {
                        do
                        {
                            if (this.ResponseTimeout())
                                return Result.RESPONSE_TIMEOUT;
                            int num = this.client.GetStream().Read(RXBuf, offset, 5 - offset);
                            offset += num;
                            if (offset == 0)
                                return Result.RESPONSE_TIMEOUT;
                        }
                        while (5 - offset > 0);
                    }
                    catch (Exception ex)
                    {
                        this.Error = ex.Message;
                        return Result.READ;
                    }
                    finally
                    {
                        this._RxBufSize = offset;
                        Array.Copy((Array)RXBuf, (Array)this._RxBuf, this._RxBufSize);
                    }
                    if (RXBuf[1] > (byte)128)
                    {
                        byte[] numArray2 = ModBusService.CRC16(RXBuf, 5);
                        return numArray2[0] != (byte)0 || numArray2[1] != (byte)0 ? Result.CRC : (Result)RXBuf[2];
                    }
                    if (ResponseLength == int.MaxValue)
                    {
                        if (RXBuf[1] == (byte)17)
                        {
                            ResponseLength = (int)RXBuf[2] + 3;
                        }
                        else
                        {
                            this.DiscardReadBuffer();
                            return Result.RESPONSE;
                        }
                    }
                    try
                    {
                        int num1 = ResponseLength + 2;
                        do
                        {
                            if (this.ResponseTimeout())
                                return Result.RESPONSE_TIMEOUT;
                            int num2 = this.client.GetStream().Read(RXBuf, offset, num1 - offset);
                            offset += num2;
                        }
                        while (num1 - offset > 0);
                    }
                    catch (Exception ex)
                    {
                        this.Error = ex.Message;
                        return Result.READ;
                    }
                    finally
                    {
                        this._RxBufSize = offset;
                        Array.Copy((Array)RXBuf, (Array)this._RxBuf, this._RxBufSize);
                    }
                    byte[] numArray3 = ModBusService.CRC16(RXBuf, ResponseLength + 2);
                    return numArray3[0] != (byte)0 || numArray3[1] != (byte)0 ? Result.CRC : Result.SUCCESS;
                }
                this.Error = "You cannot read from this NetworkStream.";
                return Result.READ;
            }
            this.Error = "You cannot write to this NetworkStream.";
            return Result.WRITE;
        }

        private Result TxRxASCIIOverTCP(
          byte[] TXBuf,
          int QueryLength,
          byte[] RXBuf,
          int ResponseLength)
        {
            int num1 = 0;
            byte[] numArray1 = new byte[531];
            byte[] numArray2 = new byte[523];
            AsciiCvt.RTU2ASCII(TXBuf, QueryLength, numArray1);
            byte n = AsciiCvt.LRC(TXBuf, QueryLength);
            numArray1[0] = (byte)58;
            numArray1[QueryLength * 2 + 1] = AsciiCvt.Num2Ascii(ByteCvt.HI4BITS(n));
            numArray1[QueryLength * 2 + 2] = AsciiCvt.Num2Ascii(ByteCvt.LO4BITS(n));
            numArray1[QueryLength * 2 + 3] = (byte)13;
            numArray1[QueryLength * 2 + 4] = (byte)10;
            this.DiscardReadBuffer();
            if (this.client.GetStream().CanWrite)
            {
                try
                {
                    this.client.GetStream().Write(numArray1, 0, QueryLength * 2 + 5);
                }
                catch (Exception ex)
                {
                    this.Error = ex.Message;
                    return Result.WRITE;
                }
                this._TxBufSize = QueryLength * 2 + 5;
                Array.Copy((Array)numArray1, (Array)this._TxBuf, this._TxBufSize);
                if (TXBuf[0] == (byte)0)
                    return Result.SUCCESS;
                if (this.client.GetStream().CanRead)
                {
                    try
                    {
                        do
                        {
                            if (this.ResponseTimeout())
                                return Result.RESPONSE_TIMEOUT;
                            int num2 = this.client.GetStream().Read(numArray2, num1, 11 - num1);
                            num1 += num2;
                            if (num1 == 0)
                                return Result.RESPONSE_TIMEOUT;
                        }
                        while (11 - num1 > 0);
                    }
                    catch (Exception ex)
                    {
                        this.Error = ex.Message;
                        return Result.READ;
                    }
                    finally
                    {
                        this._RxBufSize = num1;
                        Array.Copy((Array)numArray2, (Array)this._RxBuf, this._RxBufSize);
                    }
                    if (AsciiCvt.HiLo4BitsToByte(AsciiCvt.Ascii2Num(numArray2[3]), AsciiCvt.Ascii2Num(numArray2[4])) > (byte)128)
                        return !AsciiCvt.VerifyRespLRC(numArray2, 11) ? Result.CRC : (Result)AsciiCvt.HiLo4BitsToByte(AsciiCvt.Ascii2Num(numArray2[5]), AsciiCvt.Ascii2Num(numArray2[6]));
                    if (ResponseLength == int.MaxValue)
                    {
                        if (AsciiCvt.HiLo4BitsToByte(AsciiCvt.Ascii2Num(numArray2[3]), AsciiCvt.Ascii2Num(numArray2[4])) == (byte)17)
                        {
                            ResponseLength = (int)AsciiCvt.HiLo4BitsToByte(AsciiCvt.Ascii2Num(numArray2[5]), AsciiCvt.Ascii2Num(numArray2[6])) + 3;
                        }
                        else
                        {
                            this.DiscardReadBuffer();
                            return Result.RESPONSE;
                        }
                    }
                    try
                    {
                        int num2 = ResponseLength * 2 + 5;
                        do
                        {
                            if (this.ResponseTimeout())
                                return Result.RESPONSE_TIMEOUT;
                            int num3 = this.client.GetStream().Read(numArray2, num1, num2 - num1);
                            num1 += num3;
                        }
                        while (num2 - num1 > 0);
                    }
                    catch (Exception ex)
                    {
                        this.Error = ex.Message;
                        return Result.READ;
                    }
                    finally
                    {
                        this._RxBufSize = num1;
                        Array.Copy((Array)numArray2, (Array)this._RxBuf, this._RxBufSize);
                    }
                    if (!AsciiCvt.VerifyRespLRC(numArray2, num1))
                        return Result.CRC;
                    if (numArray2[num1 - 2] != (byte)13 || numArray2[num1 - 1] != (byte)10)
                        return Result.RESPONSE;
                    int num4 = (num1 - 5) / 2;
                    for (int index = 0; index < num4; ++index)
                        RXBuf[index] = AsciiCvt.HiLo4BitsToByte(AsciiCvt.Ascii2Num(numArray2[1 + index * 2]), AsciiCvt.Ascii2Num(numArray2[2 + index * 2]));
                    return Result.SUCCESS;
                }
                this.Error = "You cannot read from this NetworkStream.";
                return Result.READ;
            }
            this.Error = "You cannot write to this NetworkStream.";
            return Result.WRITE;
        }

        private Result TxRxUDP(byte[] TXBuf, int QueryLength, byte[] RXBuf, int ResponseLength)
        {
            ++this.TransactionID;
            this.DiscardReadBuffer();
            for (int index = 0; index < 5; ++index)
                this._TxBuf[index] = (byte)0;
            this._TxBuf[0] = (byte)((uint)this.TransactionID >> 8);
            this._TxBuf[1] = (byte)((uint)this.TransactionID & (uint)byte.MaxValue);
            this._TxBuf[4] = (byte)(QueryLength >> 8);
            this._TxBuf[5] = (byte)(QueryLength & (int)byte.MaxValue);
            for (int index = 0; index < QueryLength; ++index)
                this._TxBuf[6 + index] = TXBuf[index];
            try
            {
                this.udpClient.Send(this._TxBuf, QueryLength + 6);
            }
            catch (Exception ex)
            {
                this.Error = ex.Message;
                return Result.WRITE;
            }
            this._TxBufSize = QueryLength + 6;
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] numArray;
            try
            {
                numArray = this.udpClient.Receive(ref remoteEP);
            }
            catch (Exception ex)
            {
                this.Error = ex.Message;
                return Result.READ;
            }
            int length = numArray.Length;
            this._RxBufSize = length;
            Array.Copy((Array)numArray, (Array)this._RxBuf, this._RxBufSize);
            if (length >= 9 && numArray[7] > (byte)128)
            {
                this._RxBufSize = length;
                return (Result)numArray[8];
            }
            if (length - 6 < ResponseLength)
                return Result.RESPONSE_TIMEOUT;
            if ((int)this.TransactionID != (int)ByteCvt.MakeWord(numArray[0], numArray[1]))
            {
                this.udpClient.Receive(ref remoteEP);
                return Result.TRANSACTIONID;
            }
            for (int index = 0; index < Math.Min(length, ResponseLength + 6) - 6; ++index)
                RXBuf[index] = numArray[index + 6];
            return Result.SUCCESS;
        }
    }
    public enum Mode
    {
        TCP_IP,
        RTU_OVER_TCP_IP,
        ASCII_OVER_TCP_IP,
        UDP_IP,
    }
    public enum Result
    {
        SUCCESS = 0,
        ILLEGAL_FUNCTION = 1,
        ILLEGAL_DATA_ADDRESS = 2,
        ILLEGAL_DATA_VALUE = 3,
        SLAVE_DEVICE_FAILURE = 4,
        ACKNOWLEDGE = 5,
        SLAVE_DEVICE_BUSY = 6,
        NEGATIVE_ACKNOWLEDGE = 7,
        MEMORY_PARITY_ERROR = 8,
        GATEWAY_PATH_UNAVAILABLE = 10, // 0x0000000A
        GATEWAY_DEVICE_FAILED = 11, // 0x0000000B
        CONNECT_ERROR = 200, // 0x000000C8
        CONNECT_TIMEOUT = 201, // 0x000000C9
        WRITE = 202, // 0x000000CA
        READ = 203, // 0x000000CB
        RESPONSE_TIMEOUT = 300, // 0x0000012C
        ISCLOSED = 301, // 0x0000012D
        CRC = 302, // 0x0000012E
        RESPONSE = 303, // 0x0000012F
        BYTECOUNT = 304, // 0x00000130
        QUANTITY = 305, // 0x00000131
        FUNCTION = 306, // 0x00000132
        TRANSACTIONID = 307, // 0x00000133
        DEMO_TIMEOUT = 1000, // 0x000003E8
    }
    internal class clsModbus
    {
        private clsTxRx TxRx;

        public clsModbus(clsTxRx Tx) => this.TxRx = Tx;

        public Result ReadFlags(
          byte unitId,
          byte function,
          ushort address,
          ushort quantity,
          bool[] Bools,
          int offset)
        {
            ushort num1 = 0;
            ushort num2 = 0;
            if (function < (byte)1 || function > (byte)127)
                return Result.FUNCTION;
            if (quantity < (ushort)1 || quantity > (ushort)2000 || (int)quantity + offset > Bools.GetLength(0))
                return Result.QUANTITY;
            byte[] TXBuf = new byte[8];
            byte[] RXBuf = new byte[261];
            TXBuf[0] = unitId;
            TXBuf[1] = function;
            TXBuf[2] = ByteCvt.HiByte(address);
            TXBuf[3] = ByteCvt.LoByte(address);
            TXBuf[4] = ByteCvt.HiByte(quantity);
            TXBuf[5] = ByteCvt.LoByte(quantity);
            int ResponseLength = ((int)quantity + 7) / 8 + 3;
            Result result = this.TxRx.TxRx(TXBuf, 6, RXBuf, ResponseLength);
            if (result == Result.SUCCESS)
            {
                if ((int)TXBuf[0] != (int)RXBuf[0] || (int)TXBuf[1] != (int)RXBuf[1])
                    result = Result.RESPONSE;
                else if (ResponseLength - 3 != (int)RXBuf[2])
                {
                    result = Result.BYTECOUNT;
                }
                else
                {
                    int num3 = (int)RXBuf[3];
                    for (int index = 0; index < (int)quantity; ++index)
                    {
                        Bools[index + offset] = (num3 & 1) == 1;
                        num3 >>= 1;
                        if (++num1 == (ushort)8)
                        {
                            ++num2;
                            num1 = (ushort)0;
                            num3 = (int)RXBuf[3 + (int)num2];
                        }
                    }
                }
            }
            return result;
        }

        public Result ReadRegisters(
          byte unitId,
          ushort function,
          ushort address,
          ushort quantity,
          short[] registers,
          int offset)
        {
            if (function < (ushort)1 || function > (ushort)sbyte.MaxValue)
                return Result.FUNCTION;
            if (quantity < (ushort)1 || quantity > (ushort)125 || (int)quantity + offset > registers.GetLength(0))
                return Result.QUANTITY;
            byte[] TXBuf = new byte[8];
            byte[] RXBuf = new byte[261];
            TXBuf[0] = unitId;
            TXBuf[1] = (byte)((uint)function & (uint)byte.MaxValue);
            TXBuf[2] = ByteCvt.HiByte(address);
            TXBuf[3] = ByteCvt.LoByte(address);
            TXBuf[4] = ByteCvt.HiByte(quantity);
            TXBuf[5] = ByteCvt.LoByte(quantity);
            int ResponseLength = 3 + (int)quantity * 2;
            Result result = this.TxRx.TxRx(TXBuf, 6, RXBuf, ResponseLength);
            if (result == Result.SUCCESS)
            {
                if ((int)TXBuf[0] != (int)RXBuf[0] || (int)TXBuf[1] != (int)RXBuf[1])
                    result = Result.RESPONSE;
                else if ((int)quantity * 2 != (int)RXBuf[2])
                {
                    result = Result.BYTECOUNT;
                }
                else
                {
                    for (int index = 0; index < (int)quantity; ++index)
                        registers[index + offset] = (short)((int)RXBuf[2 * index + 4] & (int)byte.MaxValue | ((int)RXBuf[2 * index + 3] & (int)byte.MaxValue) << 8);
                    //    registers[index + offset] = (short)((uint)RXBuf[2 * index + 4] & (uint)byte.MaxValue | ((uint)RXBuf[2 * index + 3] & (uint)byte.MaxValue) << 8);
                }
            }
            return result;
        }

        public Result WriteSingleCoil(byte unitId, ushort address, bool coil)
        {
            byte[] TXBuf = new byte[8];
            byte[] RXBuf = new byte[8];
            TXBuf[0] = unitId;
            TXBuf[1] = (byte)5;
            TXBuf[2] = ByteCvt.HiByte(address);
            TXBuf[3] = ByteCvt.LoByte(address);
            TXBuf[4] = coil ? byte.MaxValue : (byte)0;
            TXBuf[5] = (byte)0;
            Result result = this.TxRx.TxRx(TXBuf, 6, RXBuf, 6);
            if (result == Result.SUCCESS && TXBuf[0] != (byte)0)
            {
                for (int index = 0; index < 6; ++index)
                {
                    if ((int)TXBuf[index] != (int)RXBuf[index])
                        result = Result.RESPONSE;
                }
            }
            return result;
        }

        public Result WriteSingleRegister(byte unitId, ushort address, short register)
        {
            byte[] TXBuf = new byte[8];
            byte[] RXBuf = new byte[8];
            TXBuf[0] = unitId;
            TXBuf[1] = (byte)6;
            TXBuf[2] = ByteCvt.HiByte(address);
            TXBuf[3] = ByteCvt.LoByte(address);
            TXBuf[4] = ByteCvt.HiByte((ushort)register);
            TXBuf[5] = ByteCvt.LoByte((ushort)register);
            Result result = this.TxRx.TxRx(TXBuf, 6, RXBuf, 6);
            if (result == Result.SUCCESS && TXBuf[0] != (byte)0)
            {
                for (int index = 0; index < 6; ++index)
                {
                    if ((int)TXBuf[index] != (int)RXBuf[index])
                        result = Result.RESPONSE;
                }
            }
            return result;
        }

        public Result WriteFlags(
          byte unitId,
          byte function,
          ushort address,
          ushort quantity,
          bool[] Bools,
          int offset)
        {
            if (function < (byte)1 || function > (byte)127)
                return Result.FUNCTION;
            if (quantity < (ushort)1 || quantity > (ushort)1968 || (int)quantity + offset > Bools.GetLength(0))
                return Result.QUANTITY;
            byte[] TXBuf = new byte[265];
            byte[] RXBuf = new byte[8];
            ushort num1 = 0;
            byte num2 = 7;
            byte num3 = 0;
            TXBuf[0] = unitId;
            TXBuf[1] = function;
            TXBuf[2] = ByteCvt.HiByte(address);
            TXBuf[3] = ByteCvt.LoByte(address);
            TXBuf[4] = ByteCvt.HiByte(quantity);
            TXBuf[5] = ByteCvt.LoByte(quantity);
            TXBuf[6] = (byte)(((int)quantity + 7) / 8);
            for (int index = 0; index < (int)quantity; ++index)
            {
                if (Bools[index + offset])
                    num3 |= (byte)(1U << (int)num1);
                ++num1;
                if (num1 == (ushort)8)
                {
                    num1 = (ushort)0;
                    TXBuf[(int)num2] = num3;
                    ++num2;
                    num3 = (byte)0;
                }
            }
            TXBuf[(int)num2] = num3;
            Result result = this.TxRx.TxRx(TXBuf, (int)TXBuf[6] + 7, RXBuf, 6);
            if (result == Result.SUCCESS && TXBuf[0] != (byte)0)
            {
                for (int index = 0; index < 6; ++index)
                {
                    if ((int)TXBuf[index] != (int)RXBuf[index])
                        result = Result.RESPONSE;
                }
            }
            return result;
        }

        public Result WriteRegisters(
          byte unitId,
          byte function,
          ushort address,
          ushort quantity,
          short[] registers,
          int offset)
        {
            if (function < (byte)1 || function > (byte)127)
                return Result.FUNCTION;
            if (quantity < (ushort)1 || quantity > (ushort)123 || (int)quantity + offset > registers.GetLength(0))
                return Result.QUANTITY;
            byte[] TXBuf = new byte[265];
            byte[] RXBuf = new byte[8];
            TXBuf[0] = unitId;
            TXBuf[1] = function;
            TXBuf[2] = ByteCvt.HiByte(address);
            TXBuf[3] = ByteCvt.LoByte(address);
            TXBuf[4] = ByteCvt.HiByte(quantity);
            TXBuf[5] = ByteCvt.LoByte(quantity);
            TXBuf[6] = (byte)((uint)quantity * 2U);
            for (int index = 0; index < (int)quantity; ++index)
            {
                TXBuf[7 + index * 2] = ByteCvt.HiByte((ushort)registers[index + offset]);
                TXBuf[7 + index * 2 + 1] = ByteCvt.LoByte((ushort)registers[index + offset]);
            }
            Result result = this.TxRx.TxRx(TXBuf, (int)TXBuf[6] + 7, RXBuf, 6);
            if (result == Result.SUCCESS && TXBuf[0] != (byte)0)
            {
                for (int index = 0; index < 6; ++index)
                {
                    if ((int)TXBuf[index] != (int)RXBuf[index])
                        result = Result.RESPONSE;
                }
            }
            return result;
        }

        public Result MaskWriteRegister(
          byte unitId,
          ushort address,
          ushort ANDMask,
          ushort ORMask)
        {
            byte[] TXBuf = new byte[10];
            byte[] RXBuf = new byte[10];
            TXBuf[0] = unitId;
            TXBuf[1] = (byte)22;
            TXBuf[2] = ByteCvt.HiByte(address);
            TXBuf[3] = ByteCvt.LoByte(address);
            TXBuf[4] = ByteCvt.HiByte(ANDMask);
            TXBuf[5] = ByteCvt.LoByte(ANDMask);
            TXBuf[6] = ByteCvt.HiByte(ORMask);
            TXBuf[7] = ByteCvt.LoByte(ORMask);
            Result result = this.TxRx.TxRx(TXBuf, 8, RXBuf, 8);
            if (result == Result.SUCCESS && TXBuf[0] != (byte)0)
            {
                for (int index = 0; index < 8; ++index)
                {
                    if ((int)TXBuf[index] != (int)RXBuf[index])
                        result = Result.RESPONSE;
                }
            }
            return result;
        }

        public Result ReadWriteMultipleRegisters(
          byte unitId,
          ushort readAddress,
          ushort readSize,
          short[] readRegisters,
          ushort writeAddress,
          ushort writeSize,
          short[] writeRegisters)
        {
            if (readSize < (ushort)1 || readSize > (ushort)125 || (writeSize < (ushort)1 || writeSize > (ushort)121) || ((int)readSize > readRegisters.GetLength(0) || (int)writeSize > writeRegisters.GetLength(0)))
                return Result.QUANTITY;
            byte[] TXBuf = new byte[269];
            byte[] RXBuf = new byte[261];
            TXBuf[0] = unitId;
            TXBuf[1] = (byte)23;
            TXBuf[2] = ByteCvt.HiByte(readAddress);
            TXBuf[3] = ByteCvt.LoByte(readAddress);
            TXBuf[4] = ByteCvt.HiByte(readSize);
            TXBuf[5] = ByteCvt.LoByte(readSize);
            TXBuf[6] = ByteCvt.HiByte(writeAddress);
            TXBuf[7] = ByteCvt.LoByte(writeAddress);
            TXBuf[8] = ByteCvt.HiByte(writeSize);
            TXBuf[9] = ByteCvt.LoByte(writeSize);
            TXBuf[10] = (byte)((uint)writeSize * 2U);
            int ResponseLength = 3 + (int)readSize * 2;
            for (int index = 0; index < (int)writeSize; ++index)
            {
                TXBuf[11 + index * 2] = ByteCvt.HiByte((ushort)writeRegisters[index]);
                TXBuf[11 + index * 2 + 1] = ByteCvt.LoByte((ushort)writeRegisters[index]);
            }
            Result result = this.TxRx.TxRx(TXBuf, (int)TXBuf[10] + 11, RXBuf, ResponseLength);
            if (result == Result.SUCCESS)
            {
                if ((int)TXBuf[0] != (int)RXBuf[0] || (int)TXBuf[1] != (int)RXBuf[1])
                {
                    result = Result.RESPONSE;
                }
                else
                {
                    for (int index = 0; index < (int)readSize; ++index)
                        readRegisters[index] = (short)((int)RXBuf[2 * index + 4] & (int)byte.MaxValue | ((int)RXBuf[2 * index + 3] & (int)byte.MaxValue) << 8);
                }
            }
            return result;
        }

        public Result ReportSlaveID(byte unitId, out byte byteCount, byte[] deviceSpecific)
        {
            byte[] TXBuf = new byte[4];
            byte[] RXBuf = new byte[(int)byte.MaxValue];
            TXBuf[0] = unitId;
            TXBuf[1] = (byte)17;
            byteCount = (byte)0;
            Result result = this.TxRx.TxRx(TXBuf, 2, RXBuf, int.MaxValue);
            if (result == Result.SUCCESS)
            {
                if ((int)TXBuf[0] != (int)RXBuf[0])
                {
                    result = Result.RESPONSE;
                }
                else
                {
                    byteCount = RXBuf[2];
                    int num = Math.Min((int)RXBuf[2], deviceSpecific.GetLength(0));
                    for (int index = 0; index < num; ++index)
                        deviceSpecific[index] = RXBuf[index + 3];
                }
            }
            return result;
        }
    }
    public class ModbusCtrl : Component
    {
        private clsModbus Modbus;
        private TcpClient client;
        private UdpClient udpClient;
        private clsTxRx TxRx;
        private int _ResponseTimeout = 1000;
        private int _ConnectTimeout = 1000;
        private Mode _Mode;
        private string Error = "";
        private Result Res;
        private IContainer components;

        public ModbusCtrl()
        {
            this.InitializeComponent();
            this.Modbus = new clsModbus(this.TxRx = new clsTxRx());
            this.TxRx.Mode = Mode.TCP_IP;
            this.TxRx.connected = false;
        }

        public ModbusCtrl(IContainer container)
        {
            container.Add((IComponent)this);
            this.InitializeComponent();
            this.Modbus = new clsModbus(this.TxRx = new clsTxRx());
            this.TxRx.Mode = Mode.TCP_IP;
            this.TxRx.connected = false;
        }

        [Category("Modbus")]
        [Description("Select which protocol mode to use.")]
        public Mode Mode
        {
            get => this._Mode;
            set => this._Mode = value;
        }

        [Category("Modbus")]
        [Description("Max time to wait for response 100 - 30000ms.")]
        public int ResponseTimeout
        {
            get => this._ResponseTimeout;
            set
            {
                if (value < 100 || value > 30000)
                    return;
                this._ResponseTimeout = value;
            }
        }

        [Category("Modbus")]
        [Description("Max time to wait for connection 100 - 30000ms.")]
        public int ConnectTimeout
        {
            get => this._ConnectTimeout;
            set
            {
                if (value < 100 || value > 30000)
                    return;
                this._ConnectTimeout = value;
            }
        }

        public Result ReadCoils(byte unitId, ushort address, ushort quantity, bool[] coils) => this.Res = this.Modbus.ReadFlags(unitId, (byte)1, address, quantity, coils, 0);

        public Result ReadCoils(
          byte unitId,
          ushort address,
          ushort quantity,
          bool[] coils,
          int offset)
        {
            return this.Res = this.Modbus.ReadFlags(unitId, (byte)1, address, quantity, coils, offset);
        }

        public Result ReadDiscreteInputs(
          byte unitId,
          ushort address,
          ushort quantity,
          bool[] discreteInputs)
        {
            return this.Res = this.Modbus.ReadFlags(unitId, (byte)2, address, quantity, discreteInputs, 0);
        }

        public Result ReadDiscreteInputs(
          byte unitId,
          ushort address,
          ushort quantity,
          bool[] discreteInputs,
          int offset)
        {
            return this.Res = this.Modbus.ReadFlags(unitId, (byte)2, address, quantity, discreteInputs, offset);
        }

        public Result ReadHoldingRegisters(
          byte unitId,
          ushort address,
          ushort quantity,
          short[] registers)
        {
            return this.Res = this.Modbus.ReadRegisters(unitId, (ushort)3, address, quantity, registers, 0);
        }

        public Result ReadHoldingRegisters(
          byte unitId,
          ushort address,
          ushort quantity,
          short[] registers,
          int offset)
        {
            return this.Res = this.Modbus.ReadRegisters(unitId, (ushort)3, address, quantity, registers, offset);
        }

        public Result ReadInputRegisters(
          byte unitId,
          ushort address,
          ushort quantity,
          short[] registers)
        {
            return this.Res = this.Modbus.ReadRegisters(unitId, (ushort)4, address, quantity, registers, 0);
        }

        public Result ReadInputRegisters(
          byte unitId,
          ushort address,
          ushort quantity,
          short[] registers,
          int offset)
        {
            return this.Res = this.Modbus.ReadRegisters(unitId, (ushort)4, address, quantity, registers, offset);
        }

        public Result WriteSingleCoil(byte unitId, ushort address, bool coil) => this.Res = this.Modbus.WriteSingleCoil(unitId, address, coil);

        public Result WriteSingleRegister(byte unitId, ushort address, short register) => this.Res = this.Modbus.WriteSingleRegister(unitId, address, register);

        public Result WriteMultipleCoils(
          byte unitId,
          ushort address,
          ushort quantity,
          bool[] coils)
        {
            return this.Res = this.Modbus.WriteFlags(unitId, (byte)15, address, quantity, coils, 0);
        }

        public Result WriteMultipleCoils(
          byte unitId,
          ushort address,
          ushort quantity,
          bool[] coils,
          int offset)
        {
            return this.Res = this.Modbus.WriteFlags(unitId, (byte)15, address, quantity, coils, offset);
        }

        public Result WriteMultipleRegisters(
          byte unitId,
          ushort address,
          ushort quantity,
          short[] registers)
        {
            return this.Res = this.Modbus.WriteRegisters(unitId, (byte)16, address, quantity, registers, 0);
        }

        public Result WriteMultipleRegisters(
          byte unitId,
          ushort address,
          ushort quantity,
          short[] registers,
          int offset)
        {
            return this.Res = this.Modbus.WriteRegisters(unitId, (byte)16, address, quantity, registers, offset);
        }

        public Result ReadWriteMultipleRegisters(
          byte unitId,
          ushort readAddress,
          ushort readQuantity,
          short[] readRegisters,
          ushort writeAddress,
          ushort writeQuantity,
          short[] writeRegisters)
        {
            return this.Res = this.Modbus.ReadWriteMultipleRegisters(unitId, readAddress, readQuantity, readRegisters, writeAddress, writeQuantity, writeRegisters);
        }

        public Result ReadUserDefinedCoils(
          byte unitId,
          byte function,
          ushort address,
          ushort quantity,
          bool[] coils)
        {
            return this.Res = this.Modbus.ReadFlags(unitId, function, address, quantity, coils, 0);
        }

        public Result ReadUserDefinedCoils(
          byte unitId,
          byte function,
          ushort address,
          ushort quantity,
          bool[] coils,
          int offset)
        {
            return this.Res = this.Modbus.ReadFlags(unitId, function, address, quantity, coils, offset);
        }

        public Result ReadUserDefinedRegisters(
          byte unitId,
          byte function,
          ushort address,
          ushort quantity,
          short[] registers)
        {
            return this.Res = this.Modbus.ReadRegisters(unitId, (ushort)function, address, quantity, registers, 0);
        }

        public Result ReadUserDefinedRegisters(
          byte unitId,
          byte function,
          ushort address,
          ushort quantity,
          short[] registers,
          int offset)
        {
            return this.Res = this.Modbus.ReadRegisters(unitId, (ushort)function, address, quantity, registers, offset);
        }

        public Result WriteUserDefinedCoils(
          byte unitId,
          byte function,
          ushort address,
          ushort quantity,
          bool[] coils)
        {
            return this.Res = this.Modbus.WriteFlags(unitId, function, address, quantity, coils, 0);
        }

        public Result WriteUserDefinedCoils(
          byte unitId,
          byte function,
          ushort address,
          ushort quantity,
          bool[] coils,
          int offset)
        {
            return this.Res = this.Modbus.WriteFlags(unitId, function, address, quantity, coils, offset);
        }

        public Result WriteUserDefinedRegisters(
          byte unitId,
          byte function,
          ushort address,
          ushort quantity,
          short[] registers)
        {
            return this.Res = this.Modbus.WriteRegisters(unitId, function, address, quantity, registers, 0);
        }

        public Result WriteUserDefinedRegisters(
          byte unitId,
          byte function,
          ushort address,
          ushort quantity,
          short[] registers,
          int offset)
        {
            return this.Res = this.Modbus.WriteRegisters(unitId, function, address, quantity, registers, offset);
        }

        public Result ReportSlaveID(byte unitId, out byte byteCount, byte[] deviceSpecific) => this.Res = this.Modbus.ReportSlaveID(unitId, out byteCount, deviceSpecific);

        public Result MaskWriteRegister(
          byte unitId,
          ushort address,
          ushort andMask,
          ushort orMask)
        {
            return this.Res = this.Modbus.MaskWriteRegister(unitId, address, andMask, orMask);
        }

        public Result Connect(string ipAddress, int port)
        {
            this.Close();
            switch (this._Mode)
            {
                case Mode.TCP_IP:
                case Mode.RTU_OVER_TCP_IP:
                case Mode.ASCII_OVER_TCP_IP:
                    this.client = new TcpClient();
                    this.TxRx.SetClient(this.client);
                    this.TxRx.Timeout = this._ResponseTimeout;
                    this.client.SendTimeout = 2000;
                    this.client.ReceiveTimeout = this._ResponseTimeout;
                    try
                    {
                        IAsyncResult asyncResult = this.client.BeginConnect(ipAddress, port, (AsyncCallback)null, (object)null);
                        WaitHandle asyncWaitHandle = asyncResult.AsyncWaitHandle;
                        if (!asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds((double)this._ConnectTimeout), false))
                        {
                            this.client.Close();
                            return this.Res = Result.CONNECT_TIMEOUT;
                        }
                        this.client.EndConnect(asyncResult);
                        asyncWaitHandle.Close();
                    }
                    catch (Exception ex)
                    {
                        this.Error = ex.Message;
                        return this.Res = Result.CONNECT_ERROR;
                    }
                    this.TxRx.Mode = this._Mode;
                    this.TxRx.connected = true;
                    break;
                case Mode.UDP_IP:
                    this.udpClient = new UdpClient();
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
                    try
                    {
                        this.udpClient.Connect(endPoint);
                    }
                    catch (Exception ex)
                    {
                        this.Error = ex.Message;
                        return this.Res = Result.CONNECT_ERROR;
                    }
                    this.TxRx.SetClient(this.udpClient);
                    this.TxRx.Timeout = this._ResponseTimeout;
                    this.udpClient.Client.SendTimeout = 2000;
                    this.udpClient.Client.ReceiveTimeout = this._ResponseTimeout;
                    this.TxRx.Mode = this._Mode;
                    this.TxRx.connected = true;
                    break;
            }
            return this.Res = Result.SUCCESS;
        }

        public void Close()
        {
            if (this.client != null)
                this.client.Close();
            if (this.udpClient != null)
                this.udpClient.Close();
            this.TxRx.connected = false;
        }

        public string GetLastErrorString()
        {
            switch (this.Res)
            {
                case Result.SUCCESS:
                    return "Success";
                case Result.ILLEGAL_FUNCTION:
                    return "Illegal function.";
                case Result.ILLEGAL_DATA_ADDRESS:
                    return "Illegal data address.";
                case Result.ILLEGAL_DATA_VALUE:
                    return "Illegal data value.";
                case Result.SLAVE_DEVICE_FAILURE:
                    return "Server device failure.";
                case Result.ACKNOWLEDGE:
                    return "Acknowledge.";
                case Result.SLAVE_DEVICE_BUSY:
                    return "Server device busy.";
                case Result.NEGATIVE_ACKNOWLEDGE:
                    return "Negative acknowledge.";
                case Result.MEMORY_PARITY_ERROR:
                    return "Memory parity error.";
                case Result.CONNECT_ERROR:
                    return this.Error;
                case Result.CONNECT_TIMEOUT:
                    return "Could not connect within the specified time";
                case Result.WRITE:
                    return "Write error. " + this.TxRx.GetErrorMessage();
                case Result.READ:
                    return "Read error. " + this.TxRx.GetErrorMessage();
                case Result.RESPONSE_TIMEOUT:
                    return "Response timeout.";
                case Result.ISCLOSED:
                    return "Connection is closed.";
                case Result.CRC:
                    return "CRC Error.";
                case Result.RESPONSE:
                    return "Not the expected response received.";
                case Result.BYTECOUNT:
                    return "Byte count error.";
                case Result.QUANTITY:
                    return "Quantity is out of range.";
                case Result.FUNCTION:
                    return "Modbus function code out of range. 1 - 127.";
                case Result.DEMO_TIMEOUT:
                    return "Demo mode expired. Restart your application to continue.";
                default:
                    return "Unknown Error - " + this.Res.ToString();
            }
        }


        public float RegistersToFloat(short hiReg, short loReg) => BitConverter.ToSingle(((IEnumerable<byte>)BitConverter.GetBytes(loReg)).Concat<byte>((IEnumerable<byte>)BitConverter.GetBytes(hiReg)).ToArray<byte>(), 0);

        public ushort RegisterToUInt16(short reg) => BitConverter.ToUInt16(BitConverter.GetBytes(reg), 0);

        public int RegistersToInt32(short hiReg, short loReg) => BitConverter.ToInt32(((IEnumerable<byte>)BitConverter.GetBytes(loReg)).Concat<byte>((IEnumerable<byte>)BitConverter.GetBytes(hiReg)).ToArray<byte>(), 0);

        public uint RegistersToUInt32(short hiReg, short loReg) => BitConverter.ToUInt32(((IEnumerable<byte>)BitConverter.GetBytes(loReg)).Concat<byte>((IEnumerable<byte>)BitConverter.GetBytes(hiReg)).ToArray<byte>(), 0);

        public short[] FloatToRegisters(float value)
        {
            short[] numArray = new short[2];
            byte[] bytes = BitConverter.GetBytes(value);
            numArray[1] = BitConverter.ToInt16(bytes, 0);
            numArray[0] = BitConverter.ToInt16(bytes, 2);
            return numArray;
        }

        public short UInt16ToRegister(ushort value) => BitConverter.ToInt16(BitConverter.GetBytes(value), 0);

        public short[] Int32ToRegisters(int value)
        {
            short[] numArray = new short[2];
            byte[] bytes = BitConverter.GetBytes(value);
            numArray[1] = BitConverter.ToInt16(bytes, 0);
            numArray[0] = BitConverter.ToInt16(bytes, 2);
            return numArray;
        }

        public short[] UInt32ToRegisters(uint value)
        {
            short[] numArray = new short[2];
            byte[] bytes = BitConverter.GetBytes(value);
            numArray[1] = BitConverter.ToInt16(bytes, 0);
            numArray[0] = BitConverter.ToInt16(bytes, 2);
            return numArray;
        }

        public int GetTxBuffer(byte[] byteArray) => this.TxRx.GetTxBuffer(byteArray);

        public int GetRxBuffer(byte[] byteArray) => this.TxRx.GetRxBuffer(byteArray);

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
                this.components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent() => this.components = (IContainer)new Container();
    }

}

