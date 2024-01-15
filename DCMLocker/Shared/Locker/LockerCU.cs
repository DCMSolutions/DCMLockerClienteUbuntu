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

    public class Tokenasd
    {
        public int idPK { get; set; }
        public int idLockerRel { get; set; }
        public int TamañoRel { get; set; }
        public int? BoxRel { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int? Token { get; set; }
        public bool Confirmado { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Modo { get; set; }
        public int Contador { get; set; }
        
    }
}
