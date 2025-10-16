
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public AssetsEstado Estado { get; set; } = AssetsEstado.Funcional;
        public int IdBox { get; set; }
    }

    public enum AssetsEstado
    {
        Funcional,
        Defectuoso,
        Mantenimiento,
        Perdido,
        EOL,
        Archivado
    }

    //el historial que es solo los eventos por assets:
    public class AssetsHistorial
    {
        public int Id { get; set; }
        public string IdAsset { get; set; } = default!;
        public string? IdEmpleado { get; set; }
        public string? NroSerieLocker { get; set; }

        public AssetsEvento Evento { get; set; } = AssetsEvento.Creacion;
        public AssetsEstado? EstadoPrevio { get; set; }
        public AssetsEstado? EstadoNuevo { get; set; }


        public DateTime? FechaEvento { get; set; } = DateTime.Now;
    }

    public enum AssetsEvento
    {
        Creacion,
        CheckIn,
        CheckOut,
        AsignacionAEmpleado,
        AsignacionAGrupo,
        ActualizacionEstado
    }
}
