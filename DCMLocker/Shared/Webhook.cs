using DCMLocker.Shared.Locker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DCMLocker.Shared
{
    public class Webhook
    {
        public DateTime FechaCreacion { get; set; }
        public string Evento { get; set; }
        public string NroSerieLocker { get; set; }
        public string Data { get; set; }

        public Webhook(string evento, string nroSerieLocker, object data)
        {
            FechaCreacion = DateTime.Now;
            Evento = evento;
            NroSerieLocker = nroSerieLocker;
            Data = data != null ? JsonSerializer.Serialize(data) : null;
        }
    }
}
