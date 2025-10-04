
using System.Collections.Generic;

namespace DCMLocker.Shared
{
    public class AssetsRequest
    {
        public string IdEmpleado { get; set; } = string.Empty;
        public string NroSerieLocker { get; set; } = string.Empty;
        public AssetsAccion Accion { get; set; } = AssetsAccion.None;
    }

    public enum AssetsAccion
    {
        Retiro,
        Devolucion,
        Mantenimiento,
        None
    }

    public class AssetsResponse
    {
        public string NombreEmpleado { get; set; } = string.Empty;
        public List<ShortAsset> AssetsList { get; set; } = new List<ShortAsset>();
    }

    public class ShortAsset
    {
        public string IdAsset { get; set; } = string.Empty;
        public string NombreAsset { get; set; } = string.Empty;
        public int IdBox { get; set; }
    }
}
