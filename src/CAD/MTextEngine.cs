// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  MTextEngine.cs
//  Motor de inserción de MText en polígonos catastrales
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CatastroUrbano.Core.Infrastructure;
using CatastroUrbano.Core.Models;
using CatastroUrbano.Core.Styles;

namespace CatastroUrbano.Core.CAD
{
    // ─────────────────────────────────────────────────────────
    //  CONFIGURACIÓN DE MTEXT
    // ─────────────────────────────────────────────────────────

    public sealed class ConfigMText
    {
        public string   Contenido       { get; set; } = string.Empty;
        public Point3d  PuntoInsercion  { get; set; }
        public string   NombreLayer     { get; set; } = "DESCRIPCION";
        public string   NombreTextStyle { get; set; } = "TEXTO";
        public double   AlturaTexto     { get; set; } = 0.50;
        public double   AnchoCaja       { get; set; } = 0.0;   // 0 = sin límite
        public AttachmentPoint Attachment { get; set; } = AttachmentPoint.MiddleCenter;
    }

    // ─────────────────────────────────────────────────────────
    //  MOTOR DE MTEXT
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Genera e inserta objetos MText dentro de polígonos catastrales.
    /// Aplica textstyle, layer y attachment según reglas institucionales.
    /// NO crea ni modifica styles del DWT.
    /// </summary>
    public sealed class MTextEngine
    {
        private readonly StyleResolver  _styleResolver;
        private readonly ErrorHandler   _log = ErrorHandler.Instancia;

        public MTextEngine(StyleResolver styleResolver)
        {
            _styleResolver = styleResolver
                ?? throw new ArgumentNullException(nameof(styleResolver));
        }

        // ── Texto interior de polígono catastral ──────────────

        /// <summary>
        /// Construye el MText interior estándar para un polígono catastral.
        /// Formato institucional: "CÓDIGO\nÁREA m²"
        /// </summary>
        public MText ConstruirTextoInterior(
            PoligonoCatastral entidad,
            Database db)
        {
            // Contenido institucional: código + área en líneas separadas
            string contenido = FormatearContenidoInterior(entidad);

            // Punto de inserción: centroide geométrico
            var ptInsercion = entidad.CentroGeometrico;

            // TextStyle desde clasificación
            var textStyleId = _styleResolver.ResolverTextStyle(
                ObtenerTextStyleInterior(entidad), db);

            var layerId = _styleResolver.ResolverLayer("DESCRIPCION", db);

            return ConstruirMText(new ConfigMText
            {
                Contenido      = contenido,
                PuntoInsercion = ptInsercion,
                NombreLayer    = "DESCRIPCION",
                NombreTextStyle = ObtenerTextStyleInterior(entidad),
                AlturaTexto    = 0.50,
                Attachment     = AttachmentPoint.MiddleCenter
            }, textStyleId, layerId);
        }

        // ── Texto de número catastral ─────────────────────────

        /// <summary>
        /// Construye MText para número catastral (NUM_CATASTRAL).
        /// </summary>
        public MText ConstruirNumeroCatastral(
            string numeroCatastral,
            Point3d puntoInsercion,
            Database db)
        {
            var textStyleId = _styleResolver.ResolverTextStyle("TEXTO", db);
            var layerId     = _styleResolver.ResolverLayer("NUM_CATASTRAL", db);

            return ConstruirMText(new ConfigMText
            {
                Contenido       = numeroCatastral,
                PuntoInsercion  = puntoInsercion,
                NombreLayer     = "NUM_CATASTRAL",
                NombreTextStyle = "TEXTO",
                AlturaTexto     = 0.30,
                Attachment      = AttachmentPoint.MiddleCenter
            }, textStyleId, layerId);
        }

        // ── Texto para cuadro técnico (fila) ──────────────────

        /// <summary>
        /// Construye un MText para una celda del cuadro técnico.
        /// Respeta el textstyle institucional "TEXTO".
        /// </summary>
        public MText ConstruirTextoCuadro(
            string texto,
            Point3d puntoInsercion,
            double anchoCaja,
            AttachmentPoint attachment,
            double alturaTexto,
            string textStyleName,
            Database db)
        {
            var textStyleId = _styleResolver.ResolverTextStyle(textStyleName, db);
            var layerId     = _styleResolver.ResolverLayer("DESCRIPCION", db);

            return ConstruirMText(new ConfigMText
            {
                Contenido       = texto,
                PuntoInsercion  = puntoInsercion,
                NombreLayer     = "DESCRIPCION",
                NombreTextStyle = textStyleName,
                AlturaTexto     = alturaTexto,
                AnchoCaja       = anchoCaja,
                Attachment      = attachment
            }, textStyleId, layerId);
        }

        // ── Construcción base de MText ────────────────────────

        /// <summary>
        /// Construye un MText CAD a partir de la configuración dada.
        /// Aplica ObjectIds resueltos. NO agrega a la DB.
        /// </summary>
        public MText ConstruirMText(
            ConfigMText config,
            ObjectId textStyleId,
            ObjectId layerId)
        {
            var mtext = new MText();

            // ── Geometría ─────────────────────────────────────
            mtext.Location   = config.PuntoInsercion;
            mtext.Attachment = config.Attachment;
            mtext.Width      = config.AnchoCaja;  // 0 = sin wrapping

            // ── Estilo ────────────────────────────────────────
            if (textStyleId != ObjectId.Null)
                mtext.TextStyleId = textStyleId;

            mtext.TextHeight = config.AlturaTexto;

            // ── Layer ─────────────────────────────────────────
            if (layerId != ObjectId.Null)
                mtext.LayerId = layerId;

            // ── Contenido (sin overrides de formato MTEXT) ────
            // Se usa texto plano. Si el DWT requiere formato RTF
            // puede enriquecerse aquí sin alterar el textstyle.
            mtext.Contents = config.Contenido;

            return mtext;
        }

        // ── Helpers de formato ────────────────────────────────

        /// <summary>
        /// Formatea el contenido interior estándar de un polígono catastral:
        ///   1P CC
        ///   120.49 m²
        /// </summary>
        private static string FormatearContenidoInterior(PoligonoCatastral entidad)
        {
            string codigo = entidad.CodigoCompleto;
            string area   = entidad.AreaFormateada;

            // MText usa \P como salto de párrafo
            return $"{codigo}\\P{area}";
        }

        /// <summary>
        /// Determina el textstyle correcto para el texto interior
        /// según el tipo de polígono.
        /// </summary>
        private static string ObtenerTextStyleInterior(PoligonoCatastral entidad)
        {
            return entidad.TipoPoligono switch
            {
                TipoPoligono.Lote    => "TXT_LOTE",
                TipoPoligono.Manzana => "TXT_MANZANA",
                _                    => "TXT_FABRICA"
            };
        }

        // ── Actualización de texto existente ──────────────────

        /// <summary>
        /// Actualiza el contenido de un MText existente sin alterar
        /// su posición, layer ni textstyle. Útil cuando el área cambia.
        /// </summary>
        public void ActualizarContenido(
            ObjectId mtextId, string nuevoContenido, Transaction tr)
        {
            try
            {
                var mtext = (MText)tr.GetObject(mtextId, OpenMode.ForWrite);
                mtext.Contents = nuevoContenido;
            }
            catch (Exception ex)
            {
                _log.LogError(
                    $"Error actualizando MText [{mtextId}]", ex);
            }
        }
    }
}
