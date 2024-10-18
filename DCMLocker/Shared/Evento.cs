using DCMLocker.Shared.Locker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCMLocker.Shared
{
    public class Evento
    {
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; }
        public string Identificador { get; set; }
        //posibles: sistema, cerraduras, token, fallas

        public Evento(string descripcion, string identificador)
        {
            Fecha = DateTime.Now;
            Descripcion = descripcion;
            Identificador = identificador;
        }
    }
}
