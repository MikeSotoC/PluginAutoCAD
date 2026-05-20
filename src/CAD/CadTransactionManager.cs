// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  CadTransactionManager.cs
//  Gestión segura de transacciones sobre la base de datos CAD
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using CatastroUrbano.Core.Infrastructure;

namespace CatastroUrbano.Core.CAD
{
    // ─────────────────────────────────────────────────────────
    //  CONTEXTO CAD ACTIVO
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Encapsula el documento, base de datos y editor activos.
    /// Se obtiene una sola vez al inicio de cada comando.
    /// </summary>
    public sealed class ContextoCAD
    {
        public Document  Documento  { get; }
        public Database  Database   { get; }
        public Editor    Editor     { get; }

        public ContextoCAD(Document doc)
        {
            Documento = doc ?? throw new ArgumentNullException(nameof(doc));
            Database  = doc.Database;
            Editor    = doc.Editor;
        }

        /// <summary>
        /// Obtiene el contexto del documento activo en el momento
        /// de la llamada (thread-safe para MDI).
        /// </summary>
        public static ContextoCAD ObtenerActivo()
        {
            var doc = Application.DocumentManager.MdiActiveDocument
                ?? throw new InvalidOperationException(
                    "No hay documento activo en AutoCAD/ZWCAD.");
            return new ContextoCAD(doc);
        }

        /// <summary>
        /// Escribe un mensaje en la línea de comandos del CAD.
        /// </summary>
        public void Escribir(string mensaje) =>
            Editor.WriteMessage($"\n{mensaje}");

        public void EscribirOk(string mensaje) =>
            Editor.WriteMessage($"\n✓ {mensaje}");

        public void EscribirError(string mensaje) =>
            Editor.WriteMessage($"\n✗ ERROR: {mensaje}");

        public void EscribirAdvertencia(string mensaje) =>
            Editor.WriteMessage($"\n⚠ AVISO: {mensaje}");
    }

    // ─────────────────────────────────────────────────────────
    //  GESTOR DE TRANSACCIONES
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Centraliza la apertura, commit y rollback de transacciones
    /// CAD. Garantiza que ningún error deje la DB en estado corrupto.
    /// </summary>
    public sealed class CadTransactionManager
    {
        private readonly ErrorHandler _log = ErrorHandler.Instancia;

        // ── Ejecución de operación en transacción ─────────────

        /// <summary>
        /// Ejecuta una acción dentro de una transacción CAD segura.
        /// Hace commit si la acción completa sin excepciones.
        /// Hace abort y reporta el error en caso contrario.
        /// </summary>
        public ResultadoOperacion Ejecutar(
            Database db,
            Action<Transaction> accion,
            string descripcion = "Operación CAD")
        {
            using var tr = db.TransactionManager.StartTransaction();
            try
            {
                accion(tr);
                tr.Commit();
                _log.LogInfo($"TX Commit: {descripcion}");
                return ResultadoOperacion.Ok(descripcion);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception cadEx)
            {
                tr.Abort();
                _log.LogError($"CAD Exception en TX '{descripcion}'", cadEx);
                return ResultadoOperacion.Fallo(
                    $"Error CAD en '{descripcion}': {cadEx.Message}");
            }
            catch (Exception ex)
            {
                tr.Abort();
                _log.LogError($"Exception en TX '{descripcion}'", ex);
                return ResultadoOperacion.Fallo(
                    $"Error en '{descripcion}': {ex.Message}");
            }
        }

        /// <summary>
        /// Versión genérica que retorna un valor desde la transacción.
        /// </summary>
        public (ResultadoOperacion Resultado, T? Valor) Ejecutar<T>(
            Database db,
            Func<Transaction, T> funcion,
            string descripcion = "Consulta CAD")
            where T : class
        {
            using var tr = db.TransactionManager.StartTransaction();
            try
            {
                var valor = funcion(tr);
                tr.Commit();
                return (ResultadoOperacion.Ok(descripcion), valor);
            }
            catch (Exception ex)
            {
                tr.Abort();
                _log.LogError($"Exception en TX '{descripcion}'", ex);
                return (ResultadoOperacion.Fallo(ex.Message), null);
            }
        }

        // ── Helpers para agregar entidades al Model Space ─────

        /// <summary>
        /// Agrega una entidad al BlockTableRecord del ModelSpace
        /// dentro de una transacción activa.
        /// </summary>
        public ObjectId AgregarAlModelSpace(
            Entity entidad, Transaction tr, Database db)
        {
            var btrId = db.CurrentSpaceId;
            var btr   = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForWrite);
            var id    = btr.AppendEntity(entidad);
            tr.AddNewlyCreatedDBObject(entidad, true);
            return id;
        }

        /// <summary>
        /// Agrega múltiples entidades al ModelSpace en una transacción.
        /// </summary>
        public List<ObjectId> AgregarAlModelSpaceLote(
            IEnumerable<Entity> entidades, Transaction tr, Database db)
        {
            var ids = new List<ObjectId>();
            var btrId = db.CurrentSpaceId;
            var btr   = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForWrite);

            foreach (var ent in entidades)
            {
                var id = btr.AppendEntity(ent);
                tr.AddNewlyCreatedDBObject(ent, true);
                ids.Add(id);
            }

            return ids;
        }

        // ── Apertura de entidad existente ─────────────────────

        /// <summary>
        /// Abre una entidad en modo escritura dentro de una transacción.
        /// </summary>
        public T AbrirEntidad<T>(ObjectId id, Transaction tr,
            OpenMode modo = OpenMode.ForWrite)
            where T : DBObject
        {
            return (T)tr.GetObject(id, modo);
        }

        // ── Transacción de solo lectura optimizada ────────────

        /// <summary>
        /// Ejecuta una consulta en transacción de solo lectura.
        /// Más eficiente que StartTransaction para consultas puras.
        /// </summary>
        public T ConsultarSeguro<T>(
            Database db,
            Func<Transaction, T> consulta,
            T valorPorDefecto,
            string descripcion = "Consulta")
        {
            using var tr = db.TransactionManager.StartOpenCloseTransaction();
            try
            {
                var resultado = consulta(tr);
                tr.Commit();
                return resultado;
            }
            catch (Exception ex)
            {
                _log.LogError($"Error en consulta '{descripcion}'", ex);
                tr.Abort();
                return valorPorDefecto;
            }
        }

        // ── Bloqueo de documento ──────────────────────────────

        /// <summary>
        /// Adquiere un lock de documento para operaciones
        /// desde contextos fuera del command handler.
        /// Usar cuando se opera desde eventos o threads externos.
        /// </summary>
        public void EjecutarConLock(
            Document doc,
            Action<Transaction> accion,
            string descripcion = "Operación con lock")
        {
            using var docLock = doc.LockDocument();
            Ejecutar(doc.Database, accion, descripcion);
        }
    }
}
