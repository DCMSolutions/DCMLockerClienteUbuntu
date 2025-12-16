
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
    public class AssetsRapidoRequest
    {
        public string IdEmpleado { get; set; } = string.Empty;
        public string NroSerieLocker { get; set; } = string.Empty;
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

    public class AssetsRapidoResponse
    {
        public string NombreEmpleado { get; set; } = string.Empty;
        public string RolEmpleado { get; set; } = string.Empty;
        public AssetRapido? AssetRapido { get; set; } = new();
        public List<ShortAsset>? AssetsRetiroList { get; set; } = new List<ShortAsset>();
        public List<ShortAsset>? AssetsDevolucionList { get; set; } = new List<ShortAsset>();
    }

    public class AssetRapido
    {
        public string NombreAsset { get; set; } = string.Empty;
        public int IdBox { get; set; }
        public bool IsRetiro { get; set; }
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

    // los eventos por assets:
    public class AssetsEventoLocker
    {
        public string IdAsset { get; set; } = default!;
        public string? IdEmpleado { get; set; }
        public string? NroSerieLocker { get; set; }
        public int? Box { get; set; }

        public AssetsEvento Evento { get; set; } = AssetsEvento.Creacion;
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
