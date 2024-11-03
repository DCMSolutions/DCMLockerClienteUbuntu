using DCMLocker.Shared;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DCMLocker.Server.Controllers
{
    [Route("ClientApp/[controller]")]
    [Route("KioskApp/[controller]")]
    [ApiController]
    public class LogController : ControllerBase
    {
        private string fileNameAhora = Path.Combine("/home/pi", $"eventos-{DateTime.Now:MM-yyyy}.ans");

        [HttpGet]
        public List<Evento> GetEventos()
        {
            try
            {
                if (!System.IO.File.Exists(fileNameAhora))
                {
                    CrearVacia();
                    return new List<Evento>();
                }
                else
                {
                    string content = System.IO.File.ReadAllText(fileNameAhora);
                    List<Evento>? consultas = JsonSerializer.Deserialize<List<Evento>>(content);
                    consultas.Reverse();
                    return consultas;
                }
            }
            catch
            {
                throw new Exception("Hubo un error al buscar los eventos");
            }
        }

        [HttpGet("viejo")]
        public List<Evento> GetEventosViejos(int mesesAtras)
        {
            string fileNameVieja = Path.Combine("/home/pi", $"eventos-{DateTime.Now.AddMonths(-mesesAtras):MM-yyyy}.ans");

            try
            {
                if (!System.IO.File.Exists(fileNameVieja))
                {
                    return new List<Evento>();
                }
                else
                {
                    string content = System.IO.File.ReadAllText(fileNameVieja);
                    List<Evento>? consultas = JsonSerializer.Deserialize<List<Evento>>(content);
                    return consultas;
                }
            }
            catch
            {
                throw new Exception($"Hubo un error al buscar los eventos del {DateTime.Now.AddMonths(-mesesAtras):MM de yyyy}");
            }
        }

        [HttpPost]
        public bool AddEvento([FromBody] Evento evento)
        {
            try
            {
                if (!System.IO.File.Exists(fileNameAhora))
                {
                    CrearConEvento(evento);
                    return true;
                }
                else
                {
                    string content = System.IO.File.ReadAllText(fileNameAhora);
                    List<Evento>? eventos = JsonSerializer.Deserialize<List<Evento>>(content);
                    eventos.Add(evento);
                    Guardar(eventos);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool DeleteAllEventos()
        {
            try
            {
                CrearVacia();
                return true;
            }
            catch
            {
                throw new Exception("No se pudieron eliminar los eventos");
            }
        }

        //funciones auxiliares
        void CrearVacia()
        {
            List<Evento> nuevaLista = new();
            string json = JsonSerializer.Serialize(nuevaLista, new JsonSerializerOptions { WriteIndented = true });
            using StreamWriter sw = System.IO.File.CreateText(fileNameAhora);
            sw.Write(json);
        }
        void CrearConEvento(Evento evento)
        {
            List<Evento> nuevaLista = new() { evento };
            string json = JsonSerializer.Serialize(nuevaLista, new JsonSerializerOptions { WriteIndented = true });
            using StreamWriter sw = System.IO.File.CreateText(fileNameAhora);
            sw.Write(json);
        }
        void Guardar(List<Evento> eventos)
        {
            string json = JsonSerializer.Serialize(eventos, new JsonSerializerOptions { WriteIndented = true });
            using (StreamWriter sw = new StreamWriter(fileNameAhora, false)) // false to overwrite
            {
                sw.Write(json);
            }
        }
    }
}
