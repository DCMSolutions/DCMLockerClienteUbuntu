using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DCMLocker.Shared
{
    public class SystemNetwork
    {
        public string NetworkInterfaceType { get; set; }
        public string NetworkOperationalStatus { get; set; }

        public string IP { get; set; }
        public string NetMask { get; set; }
        public string Gateway { get; set; }
    }

    public class SystemSSID
    {
        public string SSID { get; set; }
        public string Pass { get; set; }
    }
    public class SystemEthernet
    {
        [Required(ErrorMessage = "La dirección IP es requerida.")]
        [RegularExpression(@"^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", ErrorMessage = "El formato de dirección IP no es válido.")]
        public string IP { get; set; }

        [Required(ErrorMessage = "La máscara de red es requerida.")]
        [RegularExpression(@"^(255\.255\.255\.255|255\.255\.255\.254|255\.255\.255\.252|255\.255\.255\.248|255\.255\.255\.240|255\.255\.255\.224|255\.255\.255\.192|255\.255\.255\.128|255\.255\.255\.0|255\.255\.254\.0|255\.255\.252\.0|255\.255\.248\.0|255\.255\.240\.0|255\.255\.224\.0|255\.255\.192\.0|255\.255\.128\.0|255\.255\.0\.0|255\.254\.0\.0|255\.252\.0\.0|255\.248\.0\.0|255\.240\.0\.0|255\.224\.0\.0|255\.192\.0\.0|255\.128\.0\.0|255\.0\.0\.0|254\.0\.0\.0|252\.0\.0\.0|248\.0\.0\.0|240\.0\.0\.0|224\.0\.0\.0|192\.0\.0\.0|128\.0\.0\.0|0\.0\.0\.0)$", ErrorMessage = "El formato de la máscara de red no es válido.")]
        public string Mask { get; set; }


        public string CalculateGateway()
        {
            if (string.IsNullOrEmpty(IP) || string.IsNullOrEmpty(Mask))
            {
                throw new InvalidOperationException("Both IP and Mask must be set to calculate the gateway.");
            }

            IPAddress ipAddress = IPAddress.Parse(IP);
            IPAddress maskAddress = IPAddress.Parse(Mask);

            if (ipAddress.AddressFamily != maskAddress.AddressFamily)
            {
                throw new InvalidOperationException("IP and Mask address families must match.");
            }

            byte[] ipBytes = ipAddress.GetAddressBytes();
            byte[] maskBytes = maskAddress.GetAddressBytes();

            if (ipBytes.Length != maskBytes.Length)
            {
                throw new InvalidOperationException("IP and Mask must have the same length.");
            }

            byte[] networkBytes = new byte[ipBytes.Length];

            for (int i = 0; i < ipBytes.Length; i++)
            {
                networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
            }

            // Incrementa el último byte de la dirección de red en 1 para obtener la puerta de enlace
            networkBytes[networkBytes.Length - 1] += 1;

            return new IPAddress(networkBytes).ToString();
        }

    }
}
