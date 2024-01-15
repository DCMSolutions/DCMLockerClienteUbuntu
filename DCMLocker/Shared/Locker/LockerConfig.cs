using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCMLocker.Shared.Locker
{
    public class TokenAccessBox
    {
        public string User { get; set; }
        public string Token { get; set; }
        public string Tag { get; set; }
        public int Box { get; set; }
        public bool IsBoxTemporal { get; set; }
        public DateTime DTExpiration { get; set; }
    }

    public class Tamaño
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "El alto es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El alto debe ser mayor que 0.")]
        public int Alto { get; set; }
        [Required(ErrorMessage = "El ancho es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El ancho debe ser mayor que 0.")]
        public int Ancho { get; set; }
        [Required(ErrorMessage = "La profundidad es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "La profundidad debe ser mayor que 0.")]
        public int Profundidad { get; set; }

    }

    public class BoxState
    {
        public int Box { get; set; }
        public bool IsFixed { get; set; }
        public bool IsAssigned { get; set; }
    }

    public class LockerUserBox
    {
        public string User { get; set; }
        public int Box { get; set; }
    }

    public class LockerEmail
    {
        public string From { get; set; }
        public string Asunto { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public bool EnableSSL { get; set; }
    }

    public class LockerConfig
    {
        public string LockerID { get; set; }
        public bool IsConfirmarEmail { get; set; }
        public LockerEmail SmtpServer { get; set; }
        public int LockerType { get; set; }
        public int LockerMode { get; set; }
        public Uri UrlServer { get; set; }
    }

    public class TLockerMap
    {
       
        public enum EnumLockerType { NORMAL = 0, COOL = 1, TEMP = 2 }
        public int BoxAddr { get; set; }
        /// <summary>
        /// Indica si el Box esta habilitado para usarse
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// Indica si el Box puede ser entregado a usuarios temporales
        /// </summary>
        public bool IsUserFixed { get; set; }
        /// <summary>
        /// Indica si el sensor esta instalado
        /// </summary>
        public bool IsSensorPresent { get; set; }
        /// <summary>
        ///  Indica si el Box es Normal, Refrigerado o Con control de temperatura
        /// </summary>
        public EnumLockerType LockerType { get; set; }
        /// <summary>
        /// Temperatura Maxima
        /// </summary>
        public int TempMax { get; set; }
        /// <summary>
        /// Temperatura minima
        /// </summary>
        public int TempMin { get; set; }
        /// <summary>
        /// Alarma que se activa si se realiza la apertura del box 
        /// </summary>
        public int AlamrNro { get; set; }
        public string State { get; set; }
        public string Size { get; set; }
        public int Id { get; set; }
    }
}
