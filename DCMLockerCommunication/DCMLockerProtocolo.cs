using System;
using System.Collections.Generic;

namespace DCMLockerCommunication
{
    public abstract class PLocker
    {
        public enum enumCMD
        {
            RqtDoorStatus = 0x30, // Status del CU ==> rsp 0x35
            DoorOpen = 0x31,   // Sin respuesta
            RqtLockAndInfraredStatus = 0x32, // Status de todos los CU ADDR=F0 ==> rsp 0x36
            RspDoorStatus = 0x35,
            RspDoorOpen = 0x36

        }

        public byte CU { get; set; }
        public byte Box { get; set; }

        public byte ADDR
        {
            get
            {
                return (byte)(((CU & 0x0F) << 4) | (Box & 0x0F));
            }
            set
            {
                CU = (byte)((value & 0xF0) >> 4);
                Box = (byte)(value & 0x0F);
            }
        }
        public enumCMD CMD { get; set; }

        public abstract byte[] ToArray();


    }

    public class PLockerTablet : PLocker
    {
        public override string ToString()
        {
            return "TABLET: {CU=" + CU.ToString("x2") + ";BOX=" + Box.ToString("x2") + ";CMD=" + ((byte)CMD).ToString("x2") + ";}";
        }

        public override byte[] ToArray()
        {
            byte[] retorno = new byte[2];
            retorno[0] = ADDR;
            retorno[1] = (byte)CMD;
            return retorno;
        }
    }
    public class PLockerBoard : PLocker
    {
        private void PLockerBoardInit(byte Addr, enumCMD Cmd, byte[] Status)
        {
            ADDR = Addr;
            CMD = Cmd;
            STATUS = Status;
        }

        public PLockerBoard(byte Addr, enumCMD Cmd, byte[] Status) : base()
        {
            PLockerBoardInit(Addr, Cmd, Status);
        }
        public PLockerBoard(byte[] array) : base()
        {
            byte[] s = new byte[4];
            array.CopyTo(s, 2);
            PLockerBoardInit(array[0], (enumCMD)(array[1]), s);
        }
        public PLockerBoard() : base() { STATUS = new byte[4]; }

        public byte[] STATUS { get; set; }

        public override string ToString()
        {
            string s = "";
            foreach (byte b in STATUS)
            {
                s += b.ToString("x2") + " ";
            }
            s.TrimEnd();
            s += ";";
            return "BOARD: {CU=" + CU.ToString("x2") + ";BOX=" + Box.ToString("x2") + ";CMD=" + ((byte)CMD).ToString("x2") + ";STATUS=" + s + "}";
        }
        public override byte[] ToArray()
        {
            byte[] retorno = new byte[2 + STATUS.Length];
            retorno[0] = ADDR;
            retorno[1] = (byte)CMD;
            for (int x = 0; x < STATUS.Length; x++) retorno[2 + x] = STATUS[x];
            return retorno;
        }


    }


    public class PTransporte
    {

        public const byte STX = 0x02;
        public const byte ETX = 0x03;

        public PLocker DATA { get; set; }

        public byte[] ToArray()
        {
            byte sum = 0;
            byte[] d = DATA.ToArray();
            byte[] retorno = new byte[3 + d.Length];
            retorno[0] = STX;
            sum = retorno[0];
            retorno[1] = DATA.ADDR;
            sum += retorno[1];
            retorno[2] = (byte)DATA.CMD;
            sum += retorno[2];

            for (int x = 2; x < d.Length; x++)
            {
                retorno[x + 1] = d[x];
                sum += retorno[x+1];

            }
            retorno[d.Length + 1] = ETX;
            sum += retorno[d.Length + 1];
            retorno[d.Length + 2] = sum;
            return retorno;
        }
        public static PLocker GetPkt(byte[] data, int size)
        {
            PLocker Retorno = null;
            byte sum = 0;
            for(int x = 0;x<size-1;x++)
            {
                sum += data[x];
            }
            // Verificacion de check sum
            if (sum == data[size-1])
            {
                if ((data[2] == (byte)PLocker.enumCMD.RqtLockAndInfraredStatus )
                    || (data[2] == (byte)PLocker.enumCMD.RspDoorOpen)
                    || (data[2] == (byte)PLocker.enumCMD.RspDoorStatus))
                {
                    byte[] st = new byte[4];
                    Array.Copy(data, 3, st, 0, 4);
                    Retorno = new PLockerBoard(data[1], (PLocker.enumCMD)data[2], st);
                }
                else
                {
                    Retorno = new PLockerTablet();
                    Retorno.ADDR = data[1];
                    Retorno.CMD = (PLocker.enumCMD)data[2];
                }
            }
            return Retorno;
        }

    }
}
