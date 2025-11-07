using DCMLocker.Shared;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DCMLocker.Server.Controllers
{
    [Route("ClientApp/[controller]")]
    [Route("KioskApp/[controller]")]
    [ApiController]
    public class LogController : ControllerBase
    {
        private static readonly SemaphoreSlim _fileLock = new(1, 1);

        private static readonly JsonSerializerOptions _jsonWriteOpts = new() { WriteIndented = true };
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = false
        };

        // === NEW: Centralize eventos directory & file naming ===
        private static readonly string _eventosDir = Path.Combine(Directory.GetCurrentDirectory(), "eventos");


        private static void EnsureEventosDir() => Directory.CreateDirectory(_eventosDir);

        private static string EventosFilePath(int monthsBack = 0)
        {
            EnsureEventosDir();
            var stamp = DateTime.Now.AddMonths(-monthsBack).ToString("MM-yyyy");
            return Path.Combine(_eventosDir, $"eventos-{stamp}.ans");
        }
        // =======================================================

        [HttpGet]
        public async Task<List<Evento>> GetEventos()
        {
            string fileNameAhora = EventosFilePath(0);
            try
            {
                if (!System.IO.File.Exists(fileNameAhora))
                {
                    await _fileLock.WaitAsync();
                    try { await EnsureFileReadyAsync(fileNameAhora, overwrite: false); }
                    finally { _fileLock.Release(); }
                    return new List<Evento>();
                }

                var eventos = await LeerEventosDesdeArchivo(fileNameAhora);
                eventos.Reverse();
                return eventos;
            }
            catch
            {
                throw new Exception("Hubo un error al buscar los eventos");
            }
        }

        [HttpGet("viejo")]
        public async Task<List<Evento>> GetEventosViejos(int mesesAtras)
        {
            string fileNameVieja = EventosFilePath(mesesAtras);

            try
            {
                if (!System.IO.File.Exists(fileNameVieja))
                {
                    return new List<Evento>();
                }

                var eventos = await LeerEventosDesdeArchivo(fileNameVieja);
                eventos.Reverse();
                return eventos;
            }
            catch
            {
                throw new Exception($"Hubo un error al buscar los eventos del {DateTime.Now.AddMonths(-mesesAtras):MM 'de' yyyy}");
            }
        }

        [HttpPost]
        public async Task<bool> AddEvento([FromBody] Evento evento)
        {
            string fileNameAhora = EventosFilePath(0);

            try
            {
                await _fileLock.WaitAsync();
                try
                {
                    List<Evento> eventos = System.IO.File.Exists(fileNameAhora)
                        ? await LeerEventosDesdeArchivo(fileNameAhora)
                        : new List<Evento>();

                    eventos.Add(evento);

                    string json = JsonSerializer.Serialize(eventos, _jsonWriteOpts);
                    await AtomicWriteAsync(fileNameAhora, json);
                }
                finally
                {
                    _fileLock.Release();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAllEventos()
        {
            try
            {
                string fileNameAhora = EventosFilePath(0);
                await _fileLock.WaitAsync();
                try
                {
                    await EnsureFileReadyAsync(fileNameAhora, overwrite: true);
                }
                finally
                {
                    _fileLock.Release();
                }
                return true;
            }
            catch
            {
                throw new Exception("No se pudieron eliminar los eventos");
            }
        }

        // ======= auxiliares (sin cambios funcionales) =======
        private static async Task AtomicWriteAsync(string path, string json)
        {
            var dir = Path.GetDirectoryName(path) ?? ".";
            var name = Path.GetFileName(path);
            Directory.CreateDirectory(dir);
            var tmp = Path.Combine(dir, $".{name}.{Guid.NewGuid():N}.tmp");

            await System.IO.File.WriteAllTextAsync(tmp, json);

            try
            {
                System.IO.File.Replace(tmp, path, null);
            }
            catch (PlatformNotSupportedException)
            {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                System.IO.File.Move(tmp, path);
            }
            catch (IOException)
            {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                System.IO.File.Move(tmp, path);
            }
        }

        private static async Task EnsureFileReadyAsync(string path, bool overwrite = false)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            if (!overwrite && System.IO.File.Exists(path))
                return;

            var json = JsonSerializer.Serialize(new List<Evento>(), _jsonWriteOpts);
            await AtomicWriteAsync(path, json);
        }

        public async Task<List<Evento>> LeerEventosDesdeArchivo(string fileName)
        {
            try
            {
                using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var eventos = await JsonSerializer.DeserializeAsync<List<Evento>>(fs, _jsonOpts);
                return eventos ?? new();
            }
            catch (JsonException)
            {
                var nuevos = new List<Evento> { new Evento("se corrompio el viejo", "error") };

                try
                {
                    var dir = Path.GetDirectoryName(fileName) ?? ".";
                    var name = Path.GetFileNameWithoutExtension(fileName);
                    var ext = Path.GetExtension(fileName);
                    var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                    var backup = Path.Combine(dir, $"{name}.corrupt-{stamp}{ext}");

                    if (System.IO.File.Exists(fileName))
                        System.IO.File.Move(fileName, backup);

                    var tmp = Path.Combine(dir, $"{name}.{Guid.NewGuid():N}.tmp");
                    await System.IO.File.WriteAllTextAsync(tmp, JsonSerializer.Serialize(nuevos, _jsonWriteOpts));
                    if (System.IO.File.Exists(fileName)) System.IO.File.Delete(fileName);
                    System.IO.File.Move(tmp, fileName);
                }
                catch { }

                return nuevos;
            }
            catch (IOException)
            {
                return new();
            }
        }
    }
}
