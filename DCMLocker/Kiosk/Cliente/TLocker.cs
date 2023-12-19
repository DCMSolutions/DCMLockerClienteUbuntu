using DCMLocker.Shared.Locker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DCMLocker.Kiosk.Cliente
{
    public class TLocker
    {
        private LockerCU[] _locker;
        public LockerCU[] LockerCUs { get { return _locker; } set { _locker = value;  } }
        
        public void IsChange () { OnChange?.Invoke(this, null); } 

        public event EventHandler OnChange;
    }
}
