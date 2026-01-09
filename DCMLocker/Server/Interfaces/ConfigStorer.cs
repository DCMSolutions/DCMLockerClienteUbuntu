using DCMLocker.Shared.Locker;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DCMLocker.Server.Interfaces
{
    public interface IConfig2Store
    {
        Task<Config2> GetAsync(CancellationToken ct = default);
        Task<(Config2 Before, Config2 After)> SaveAsync(Config2 incoming, CancellationToken ct = default);
    }

    public sealed class Config2Store : IConfig2Store
    {
        private static readonly string ConfigPath = "/home/config.json";

        private static readonly JsonSerializerOptions FileJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null // deja PascalCase en archivo
        };

        private static readonly JsonSerializerOptions DeserializeOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);
        private readonly ILogger<Config2Store> _logger;

        public Config2Store(ILogger<Config2Store> logger)
        {
            _logger = logger;
        }

        public async Task<Config2> GetAsync(CancellationToken ct = default)
        {
            await _mutex.WaitAsync(ct);
            try
            {
                var (cfg, _) = await LoadEnsureAndMaybeFixAsync(ct);
                return cfg;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<(Config2 Before, Config2 After)> SaveAsync(Config2 incoming, CancellationToken ct = default)
        {
            if (incoming is null) throw new ArgumentNullException(nameof(incoming));

            await _mutex.WaitAsync(ct);
            try
            {
                var (before, _) = await LoadEnsureAndMaybeFixAsync(ct);

                // Mínimo: no permitir dejar Modo vacío
                if (string.IsNullOrWhiteSpace(incoming.Modo))
                    incoming.Modo = before.Modo;

                // Completar nulls/faltantes con defaults (similar a tu EnsureAllFields anterior)
                var normalized = FillNullsWithDefaults(incoming, new Config2());

                // Reasegurar Modo
                if (string.IsNullOrWhiteSpace(normalized.Modo))
                    normalized.Modo = before.Modo ?? new Config2().Modo;

                await WriteConfigAtomicAsync(normalized, ct);
                return (before, normalized);
            }
            finally
            {
                _mutex.Release();
            }
        }

        private async Task<(Config2 Config, bool FileChanged)> LoadEnsureAndMaybeFixAsync(CancellationToken ct)
        {
            var defaults = new Config2();

            if (!File.Exists(ConfigPath))
            {
                await WriteConfigAtomicAsync(defaults, ct);
                return (defaults, true);
            }

            string json;
            try
            {
                json = await File.ReadAllTextAsync(ConfigPath, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo leer {ConfigPath}. Se recrea con defaults.", ConfigPath);
                await WriteConfigAtomicAsync(defaults, ct);
                return (defaults, true);
            }

            Config2 cfg;
            bool changed;

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    throw new JsonException("Root JSON no es un objeto.");

                (cfg, changed) = BuildConfigFromRoot(doc.RootElement, defaults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Config corrupto o ilegible. Se recrea con defaults.");
                cfg = defaults;
                changed = true;
            }

            // Normalización adicional
            if (string.IsNullOrWhiteSpace(cfg.Modo))
            {
                cfg.Modo = defaults.Modo;
                changed = true;
            }

            if (changed)
                await WriteConfigAtomicAsync(cfg, ct);

            return (cfg, changed);
        }

        private static (Config2 Config, bool Changed) BuildConfigFromRoot(JsonElement root, Config2 defaults)
        {
            bool changed = false;
            var cfg = new Config2();

            var props = typeof(Config2).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var p in props)
            {
                if (!p.CanRead || !p.CanWrite) continue;

                var pascal = p.Name;
                var camel = ToCamel(p.Name);

                JsonElement el;
                bool found =
                    root.TryGetProperty(pascal, out el) ||
                    (root.TryGetProperty(camel, out el) && (changed = true)); // camel->pascal implica reescritura

                object valueToSet;

                if (!found)
                {
                    valueToSet = p.GetValue(defaults);
                    changed = true;
                }
                else
                {
                    try
                    {
                        // Deserialize del fragmento del JSON a tipo de la propiedad
                        var raw = el.GetRawText();
                        var v = JsonSerializer.Deserialize(raw, p.PropertyType, DeserializeOptions);

                        if (v == null && p.PropertyType.IsValueType && Nullable.GetUnderlyingType(p.PropertyType) == null)
                        {
                            // value type no-nullable: si vino null, usar default
                            valueToSet = p.GetValue(defaults);
                            changed = true;
                        }
                        else if (v == null)
                        {
                            // referencia/nullable: si vino null, usar default (como tu EnsureAllFields anterior)
                            valueToSet = p.GetValue(defaults);
                            changed = true;
                        }
                        else
                        {
                            valueToSet = v;
                        }
                    }
                    catch
                    {
                        // Si el tipo no coincide / valor inválido, fallback a defaults
                        valueToSet = p.GetValue(defaults);
                        changed = true;
                    }
                }

                p.SetValue(cfg, valueToSet);
            }

            // Si querés forzar que strings vacíos vuelvan a defaults, lo normalizamos acá:
            foreach (var p in props)
            {
                if (!p.CanRead || !p.CanWrite) continue;
                if (p.PropertyType != typeof(string)) continue;

                var s = (string)p.GetValue(cfg);
                if (string.IsNullOrWhiteSpace(s))
                {
                    p.SetValue(cfg, (string)p.GetValue(defaults));
                    changed = true;
                }
            }

            return (cfg, changed);
        }

        private static Config2 FillNullsWithDefaults(Config2 incoming, Config2 defaults)
        {
            // Copiamos propiedad por propiedad: null -> default
            var cfg = new Config2();
            var props = typeof(Config2).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var p in props)
            {
                if (!p.CanRead || !p.CanWrite) continue;

                var val = p.GetValue(incoming);

                if (p.PropertyType == typeof(string))
                {
                    var s = val as string;
                    if (string.IsNullOrWhiteSpace(s))
                        p.SetValue(cfg, (string)p.GetValue(defaults));
                    else
                        p.SetValue(cfg, s);
                }
                else
                {
                    if (val == null)
                        p.SetValue(cfg, p.GetValue(defaults));
                    else
                        p.SetValue(cfg, val);
                }
            }

            return cfg;
        }

        private static string ToCamel(string s)
            => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);

        private static async Task WriteConfigAtomicAsync(Config2 cfg, CancellationToken ct)
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var tmp = ConfigPath + ".tmp";
            var json = JsonSerializer.Serialize(cfg, FileJsonOptions);

            await File.WriteAllTextAsync(tmp, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), ct);
            File.Move(tmp, ConfigPath, overwrite: true);
        }
    }
}
