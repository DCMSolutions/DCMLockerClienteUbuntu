using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCMLocker.Shared.Locker
{
    public class LockerUserPerfil
    {
        public string user { get; set; }
        public bool IsLocked { get; set; }
        public bool Enable { get; set; }
        public int[] Boxses { get; set; }
    }
    public class BoxAccessToken
    {
        public int CU { get; set; }
        public int Box { get; set; }
        public string User { get; set; }
        public string Token { get; set; }
    }

    public class BoxAddr
    {
        public int CU { get; set; }
        public int Box { get; set; }
    }

    public class LockerBox
    {
        public int Box { get; set; }
        public bool Reservado { get; set; }
        public bool Door { get; set; }
        public bool Sensor { get; set; }
        public int Id { get; set; }
    }

    public class LockerCU
    {
       
        public int CU { get; set; }

        public LockerBox[] Box { get; set; }

    }
}
