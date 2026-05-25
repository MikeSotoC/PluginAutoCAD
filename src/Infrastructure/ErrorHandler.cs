// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  ErrorHandler.cs
//  Gestión centralizada de errores y logging
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

namespace CatastroUrbano.Core.Infrastructure
{
    // ─────────────────────────────────────────────────────────
    //  NIVEL DE SEVERIDAD
    // ─────────────────────────────────────────────────────────

    public enum NivelError
    {
        Info      = 0,
        Advertencia = 1,
        Error     = 2,
        Fatal     = 3
    }

    // ─────────────────────────────────────────────────────────
    //  RESULTADO DE OPERACIÓN
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Encapsula el resultado de cualquier operación del sistema,
    /// permitiendo propagación de errores sin excepciones silenciosas.
    /// </summary>
    public sealed class ResultadoOperacion
    {
        public bool    Exitoso       { get; }
        public string  Mensaje       { get; }
        public string? DetalleError  { get; }
        public NivelError Nivel      { get; }

        private ResultadoOperacion(bool exitoso, string mensaje,
            string? detalle, NivelError nivel)
        {
            Exitoso      = exitoso;
            Mensaje      = mensaje;
            DetalleError = detalle;
            Nivel        = nivel;
        }

        public static ResultadoOperacion Ok(string mensaje = "Operación completada.") =>
            new(true, mensaje, null, NivelError.Info);

        public static ResultadoOperacion Fallo(string mensaje, string? detalle = null) =>
            new(false, mensaje, detalle, NivelError.Error);

        public static ResultadoOperacion Advertencia(string mensaje) =>
            new(true, mensaje, null, NivelError.Advertencia);

        public static ResultadoOperacion Fatal(string mensaje, Exception ex) =>
            new(false, mensaje, ex.ToString(), NivelError.Fatal);

        public override string ToString() =>
            $"[{Nivel}] {Mensaje}" +
            (DetalleError != null ? $"\nDetalle: {DetalleError}" : string.Empty);
    }

    // ─────────────────────────────────────────────────────────
    //  MANEJADOR CENTRAL DE ERRORES
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Servicio singleton para captura, log y presentación
    /// de errores del sistema catastral.
    /// </summary>
    public sealed class ErrorHandler
    {
        private static ErrorHandler? _instancia;
        private static readonly object _lock = new();

        private readonly List<EntradaLog>   _registros = new();
        private          StreamWriter?      _writer;
        private          string             _rutaLog   = string.Empty;

        // ── Singleton ────────────────────────────────────────

        public static ErrorHandler Instancia
        {
            get
            {
                lock (_lock)
                {
                    return _instancia ??= new ErrorHandler();
                }
            }
        }

        private ErrorHandler() { }

        // ── Inicialización ───────────────────────────────────

        /// <summary>
        /// Inicializa el log en disco. Llamar al cargar el plugin.
        /// </summary>
        public void Inicializar(string rutaDirectorio)
        {
            try
            {
                Directory.CreateDirectory(rutaDirectorio);
                _rutaLog = Path.Combine(
                    rutaDirectorio,
                    $"catastro_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                _writer = new StreamWriter(_rutaLog, append: false, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                EscribirCabecera();
            }
            catch
            {
                // El log en disco es opcional; no interrumpir el sistema.
            }
        }

        // ── Registro de eventos ──────────────────────────────

        public void LogInfo(string mensaje,
            [CallerMemberName] string origen = "",
            [CallerFilePath]   string archivo = "")
        {
            Registrar(NivelError.Info, mensaje, null, origen, archivo);
        }

        public void LogAdvertencia(string mensaje,
            [CallerMemberName] string origen = "",
            [CallerFilePath]   string archivo = "")
        {
            Registrar(NivelError.Advertencia, mensaje, null, origen, archivo);
            MostrarEnEditor($"[ADVERTENCIA] {mensaje}");
        }

        public void LogError(string mensaje, Exception? ex = null,
            [CallerMemberName] string origen = "",
            [CallerFilePath]   string archivo = "")
        {
            Registrar(NivelError.Error, mensaje, ex, origen, archivo);
            MostrarEnEditor($"[ERROR] {mensaje}");
        }

        public void LogFatal(string mensaje, Exception ex,
            [CallerMemberName] string origen = "",
            [CallerFilePath]   string archivo = "")
        {
            Registrar(NivelError.Fatal, mensaje, ex, origen, archivo);
            MostrarEnEditor($"[FATAL] {mensaje} — Ver log: {_rutaLog}");
        }

        // ── Ejecución segura de bloques ───────────────────────

        /// <summary>
        /// Ejecuta una acción capturando cualquier excepción.
        /// Garantiza que ningún error del plugin crashee AutoCAD/ZWCAD.
        /// </summary>
        public ResultadoOperacion EjecutarSeguro(
            Action accion,
            string descripcion,
            [CallerMemberName] string origen = "")
        {
            try
            {
                accion();
                return ResultadoOperacion.Ok(descripcion);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception cadEx)
            {
                LogError($"CAD Exception en '{descripcion}': {cadEx.Message}", cadEx, origen);
                return ResultadoOperacion.Fallo(
                    $"Error CAD en {descripcion}: {cadEx.Message}",
                    cadEx.ToString());
            }
            catch (Exception ex)
            {
                LogError($"Excepción en '{descripcion}': {ex.Message}", ex, origen);
                return ResultadoOperacion.Fallo(
                    $"Error en {descripcion}: {ex.Message}",
                    ex.ToString());
            }
        }

        /// <summary>
        /// Versión genérica que retorna un valor.
        /// Retorna el valor por defecto del tipo en caso de error.
        /// </summary>
        public T EjecutarSeguro<T>(
            Func<T> funcion,
            T valorPorDefecto,
            string descripcion,
            [CallerMemberName] string origen = "")
        {
            try
            {
                return funcion();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception cadEx)
            {
                LogError($"CAD Exception en '{descripcion}': {cadEx.Message}", cadEx, origen);
                return valorPorDefecto;
            }
            catch (Exception ex)
            {
                LogError($"Excepción en '{descripcion}': {ex.Message}", ex, origen);
                return valorPorDefecto;
            }
        }

        // ── Acceso a registros ───────────────────────────────

        public IReadOnlyList<EntradaLog> ObtenerRegistros() =>
            _registros.AsReadOnly();

        public string ObtenerRutaLog() => _rutaLog;

        // ── Privados ─────────────────────────────────────────

        private void Registrar(NivelError nivel, string mensaje,
            Exception? ex, string origen, string archivo)
        {
            var nombreArchivo = Path.GetFileNameWithoutExtension(archivo);
            var entrada = new EntradaLog(
                DateTime.Now, nivel, mensaje,
                ex?.ToString(), origen, nombreArchivo);

            _registros.Add(entrada);
            _writer?.WriteLine(entrada.FormatoLog());
        }

        private void MostrarEnEditor(string mensaje)
        {
            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.Editor?.WriteMessage($"\nSISTEMA CATASTRAL: {mensaje}");
            }
            catch { /* Editor puede no estar disponible */ }
        }

        private void EscribirCabecera()
        {
            _writer?.WriteLine("═══════════════════════════════════════════════════");
            _writer?.WriteLine("  SISTEMA CATASTRAL INSTITUCIONAL PERUANO");
            _writer?.WriteLine($"  Sesión iniciada: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            _writer?.WriteLine($"  AutoCAD/ZWCAD .NET API");
            _writer?.WriteLine("═══════════════════════════════════════════════════");
            _writer?.WriteLine();
        }

        public void Finalizar()
        {
            _writer?.Flush();
            _writer?.Close();
            _writer = null;
        }
    }

    // ─────────────────────────────────────────────────────────
    //  MODELO DE ENTRADA DE LOG
    // ─────────────────────────────────────────────────────────

    public sealed record EntradaLog(
        DateTime  Timestamp,
        NivelError Nivel,
        string    Mensaje,
        string?   Excepcion,
        string    Origen,
        string    Archivo)
    {
        public string FormatoLog() =>
            $"[{Timestamp:HH:mm:ss.fff}] [{Nivel,-11}] [{Archivo}.{Origen}] {Mensaje}" +
            (Excepcion != null ? $"\n  EXCEPCIÓN: {Excepcion}" : string.Empty);
    }
}
