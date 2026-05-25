// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  StyleResolver.cs
//  Resolución y validación de estilos del DWT institucional
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using CatastroUrbano.Core.Infrastructure;
using CatastroUrbano.Core.Classification;

namespace CatastroUrbano.Core.Styles
{
    // ─────────────────────────────────────────────────────────
    //  CONTEXTO DE ESTILO RESUELTO
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Bundle de estilos resueltos y validados contra el DWT real.
    /// Garantiza que no se use ningún estilo inexistente.
    /// </summary>
    public sealed class ContextoEstilo
    {
        public ObjectId DimStyleId      { get; set; } = ObjectId.Null;
        public ObjectId TextStyleId     { get; set; } = ObjectId.Null;
        public ObjectId LayerCotasId    { get; set; } = ObjectId.Null;
        public ObjectId LayerTextoId    { get; set; } = ObjectId.Null;
        public ObjectId LayerFormatoId  { get; set; } = ObjectId.Null;

        public string NombreDimStyle    { get; set; } = string.Empty;
        public string NombreTextStyle   { get; set; } = string.Empty;
        public string NombreLayerCotas  { get; set; } = string.Empty;

        public bool EsValido =>
            DimStyleId    != ObjectId.Null &&
            TextStyleId   != ObjectId.Null &&
            LayerCotasId  != ObjectId.Null;
    }

    // ─────────────────────────────────────────────────────────
    //  RESOLVEDOR DE ESTILOS
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Resuelve ObjectIds de styles, layers y dimstyles desde el DWT
    /// institucional YA CARGADO en el documento activo.
    /// NO crea nada. Solo lee y valida.
    /// </summary>
    public sealed class StyleResolver
    {
        private readonly ErrorHandler _log = ErrorHandler.Instancia;

        // Cache de ObjectIds para evitar búsquedas repetidas en DB
        private readonly Dictionary<string, ObjectId> _cacheDimStyles  = new();
        private readonly Dictionary<string, ObjectId> _cacheTextStyles = new();
        private readonly Dictionary<string, ObjectId> _cacheLayers     = new();

        // ── API principal ─────────────────────────────────────

        /// <summary>
        /// Resuelve el contexto de estilo completo para un layer dado.
        /// Usa la clasificación del LayerClassifier para determinar
        /// qué dimstyle/textstyle aplicar.
        /// </summary>
        public ContextoEstilo ResolverContexto(
            ResultadoClasificacion clasificacion,
            Database db)
        {
            var ctx = new ContextoEstilo
            {
                NombreDimStyle   = clasificacion.DimStyle,
                NombreTextStyle  = clasificacion.TextStyle,
                NombreLayerCotas = clasificacion.LayerCotas
            };

            ctx.DimStyleId     = ResolverDimStyle(clasificacion.DimStyle, db);
            ctx.TextStyleId    = ResolverTextStyle(clasificacion.TextStyle, db);
            ctx.LayerCotasId   = ResolverLayer(clasificacion.LayerCotas, db);
            ctx.LayerTextoId   = ResolverLayer(clasificacion.LayerTexto, db);
            ctx.LayerFormatoId = ResolverLayer("FORMATO", db);

            if (!ctx.EsValido)
                _log.LogAdvertencia(
                    $"Contexto de estilo incompleto para layer '{clasificacion.LayerOriginal}'. " +
                    $"DimStyle={ctx.DimStyleId.IsNull}, TextStyle={ctx.TextStyleId.IsNull}, " +
                    $"LayerCotas={ctx.LayerCotasId.IsNull}");

            return ctx;
        }

        // ── Resolución de DimStyle ─────────────────────────────

        /// <summary>
        /// Obtiene el ObjectId de un DimStyle existente en el DWT.
        /// Nunca crea ni modifica. Retorna Null si no existe.
        /// </summary>
        public ObjectId ResolverDimStyle(string nombre, Database db)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return ObjectId.Null;
            if (_cacheDimStyles.TryGetValue(nombre, out var cached)) return cached;

            using var tr = db.TransactionManager.StartOpenCloseTransaction();
            try
            {
                var tabla = (DimStyleTable)tr.GetObject(
                    db.DimStyleTableId, OpenMode.ForRead);

                if (tabla.Has(nombre))
                {
                    var id = tabla[nombre];
                    _cacheDimStyles[nombre] = id;
                    tr.Commit();
                    return id;
                }

                _log.LogAdvertencia(
                    $"DimStyle '{nombre}' no encontrado en el DWT. " +
                    $"Se usará el dimstyle activo.");

                // Fallback: usar dimstyle activo del documento
                var fallbackId = db.Dimstyle;
                _cacheDimStyles[nombre] = fallbackId;
                tr.Commit();
                return fallbackId;
            }
            catch (Exception ex)
            {
                _log.LogError($"Error resolviendo DimStyle '{nombre}'", ex);
                tr.Abort();
                return ObjectId.Null;
            }
        }

        // ── Resolución de TextStyle ────────────────────────────

        /// <summary>
        /// Obtiene el ObjectId de un TextStyle existente en el DWT.
        /// Nunca crea ni modifica. Fallback a textstyle activo.
        /// </summary>
        public ObjectId ResolverTextStyle(string nombre, Database db)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return ObjectId.Null;
            if (_cacheTextStyles.TryGetValue(nombre, out var cached)) return cached;

            using var tr = db.TransactionManager.StartOpenCloseTransaction();
            try
            {
                var tabla = (TextStyleTable)tr.GetObject(
                    db.TextStyleTableId, OpenMode.ForRead);

                if (tabla.Has(nombre))
                {
                    var id = tabla[nombre];
                    _cacheTextStyles[nombre] = id;
                    tr.Commit();
                    return id;
                }

                _log.LogAdvertencia(
                    $"TextStyle '{nombre}' no encontrado en el DWT. " +
                    $"Se usará el textstyle activo.");

                var fallbackId = db.Textstyle;
                _cacheTextStyles[nombre] = fallbackId;
                tr.Commit();
                return fallbackId;
            }
            catch (Exception ex)
            {
                _log.LogError($"Error resolviendo TextStyle '{nombre}'", ex);
                tr.Abort();
                return ObjectId.Null;
            }
        }

        // ── Resolución de Layer ───────────────────────────────

        /// <summary>
        /// Obtiene el ObjectId de un layer existente en el DWT.
        /// Nunca crea ni modifica. Retorna el layer activo como fallback.
        /// </summary>
        public ObjectId ResolverLayer(string nombre, Database db)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return ObjectId.Null;
            if (_cacheLayers.TryGetValue(nombre, out var cached)) return cached;

            using var tr = db.TransactionManager.StartOpenCloseTransaction();
            try
            {
                var tabla = (LayerTable)tr.GetObject(
                    db.LayerTableId, OpenMode.ForRead);

                if (tabla.Has(nombre))
                {
                    var id = tabla[nombre];
                    _cacheLayers[nombre] = id;
                    tr.Commit();
                    return id;
                }

                _log.LogAdvertencia(
                    $"Layer '{nombre}' no encontrado en el DWT. " +
                    $"Se usará el layer activo '{db.Clayer}'.");

                var fallbackId = db.Clayer;
                _cacheLayers[nombre] = fallbackId;
                tr.Commit();
                return fallbackId;
            }
            catch (Exception ex)
            {
                _log.LogError($"Error resolviendo Layer '{nombre}'", ex);
                tr.Abort();
                return ObjectId.Null;
            }
        }

        // ── Validación del DWT ────────────────────────────────

        /// <summary>
        /// Valida que el DWT institucional tenga todos los recursos
        /// necesarios. Retorna lista de recursos faltantes.
        /// </summary>
        public IReadOnlyList<string> ValidarDWT(Database db)
        {
            var faltantes = new List<string>();

            var dimStylesRequeridos = new[]
            {
                "COTA_FABRICA", "COTA_LOTE", "COTA_MANZANA"
            };

            var textStylesRequeridos = new[]
            {
                "TXT_FABRICA", "TXT_LOTE", "TXT_MANZANA", "TEXTO"
            };

            var layersRequeridos = new[]
            {
                "TG_MANZANA", "TG_LOTE", "PISO_01", "PISO_02", "PISO_03",
                "AREA_LIBRE", "NO_CATEGORIZABLE", "DUCTO_01", "DUCTO_02",
                "INGRESO", "FORMATO", "DESCRIPCION", "COTA_LOTE",
                "COTA_MANZANA", "COTA_FABRICA", "NUM_CATASTRAL",
                "NUM_HU", "TRIANGULACION_COTA"
            };

            using var tr = db.TransactionManager.StartOpenCloseTransaction();
            try
            {
                var dimTable  = (DimStyleTable)tr.GetObject(
                    db.DimStyleTableId, OpenMode.ForRead);
                var txtTable  = (TextStyleTable)tr.GetObject(
                    db.TextStyleTableId, OpenMode.ForRead);
                var layTable  = (LayerTable)tr.GetObject(
                    db.LayerTableId, OpenMode.ForRead);

                foreach (var ds in dimStylesRequeridos)
                    if (!dimTable.Has(ds))
                        faltantes.Add($"[DIMSTYLE] {ds}");

                foreach (var ts in textStylesRequeridos)
                    if (!txtTable.Has(ts))
                        faltantes.Add($"[TEXTSTYLE] {ts}");

                foreach (var ly in layersRequeridos)
                    if (!layTable.Has(ly))
                        faltantes.Add($"[LAYER] {ly}");

                tr.Commit();
            }
            catch (Exception ex)
            {
                _log.LogError("Error validando DWT institucional", ex);
                tr.Abort();
            }

            return faltantes;
        }

        /// <summary>
        /// Limpia la caché de ObjectIds (útil si se carga un nuevo documento).
        /// </summary>
        public void LimpiarCache()
        {
            _cacheDimStyles.Clear();
            _cacheTextStyles.Clear();
            _cacheLayers.Clear();
        }
    }
}
