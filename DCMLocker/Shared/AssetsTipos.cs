
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

}
