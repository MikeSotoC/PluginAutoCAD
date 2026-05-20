// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  DimensionEngine.cs
//  Motor de acotado automático para polígonos catastrales
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CatastroUrbano.Core.Analysis;
using CatastroUrbano.Core.Classification;
using CatastroUrbano.Core.Geometry;
using CatastroUrbano.Core.Infrastructure;
using CatastroUrbano.Core.Styles;

namespace CatastroUrbano.Core.CAD
{
    // ─────────────────────────────────────────────────────────
    //  PARÁMETROS DE ACOTADO
    // ─────────────────────────────────────────────────────────

    public sealed class ParametrosAcotado
    {
        /// <summary>
        /// Distancia desde el segmento al texto de cota (offset exterior).
        /// Estándar catastral: 0.50 m.
        /// </summary>
        public double OffsetExterior        { get; set; } = 0.50;

        /// <summary>
        /// Longitud mínima de segmento acotable (m).
        /// </summary>
        public double LongitudMinima        { get; set; } = 0.10;

        /// <summary>
        /// Si true, acotar solo los segmentos paralelos a los ejes X/Y.
        /// Si false, acotar todos los segmentos.
        /// </summary>
        public bool   SoloSegmentosOrtonormales { get; set; } = false;

        /// <summary>
        /// Tolerancia angular para considerar un segmento horizontal/vertical.
        /// En radianes. 5° ≈ 0.087 rad.
        /// </summary>
        public double ToleranciaAngular     { get; set; } = 0.087;
    }

    // ─────────────────────────────────────────────────────────
    //  MOTOR DE ACOTADO
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Genera cotas alineadas automáticamente sobre los segmentos
    /// de polilíneas catastrales. Usa el dimstyle institucional
    /// del DWT sin ninguna modificación de variables globales.
    /// </summary>
    public sealed class DimensionEngine
    {
        private readonly StyleResolver    _styleResolver;
        private readonly LayerClassifier  _layerClassifier;
        private readonly ErrorHandler     _log = ErrorHandler.Instancia;

        public DimensionEngine(
            StyleResolver   styleResolver,
            LayerClassifier layerClassifier)
        {
            _styleResolver   = styleResolver
                ?? throw new ArgumentNullException(nameof(styleResolver));
            _layerClassifier = layerClassifier
                ?? throw new ArgumentNullException(nameof(layerClassifier));
        }

        // ── Acotado de un polígono completo ───────────────────

        /// <summary>
        /// Genera todas las cotas alineadas para un polígono catastral.
        /// Retorna lista de RotatedDimension listos para insertar en DB.
        /// NO modifica dimstyles ni variables globales del DWT.
        /// </summary>
        public List<RotatedDimension> GenerarCotasPoligono(
            ResultadoAnalisis analisis,
            Database db,
            ParametrosAcotado? parametros = null)
        {
            parametros ??= new ParametrosAcotado();

            var clasificacion = _layerClassifier.Clasificar(analisis.LayerOrigen);
            var contexto      = _styleResolver.ResolverContexto(clasificacion, db);
            var cotas         = new List<RotatedDimension>();

            if (contexto.DimStyleId == ObjectId.Null)
            {
                _log.LogAdvertencia(
                    $"DimStyle '{clasificacion.DimStyle}' no resuelto. " +
                    "Cotas no generadas.");
                return cotas;
            }

            // Filtrar segmentos acotables
            var segmentosAcotar = analisis.Segmentos
                .Where(s => s.EsAcotable(parametros.LongitudMinima))
                .ToList();

            if (parametros.SoloSegmentosOrtonormales)
            {
                segmentosAcotar = segmentosAcotar
                    .Where(s => EsOrtonormal(s.Angulo, parametros.ToleranciaAngular))
                    .ToList();
            }

            foreach (var seg in segmentosAcotar)
            {
                var cota = ConstruirCotaAlineada(
                    seg, contexto, parametros.OffsetExterior);

                if (cota != null) cotas.Add(cota);
            }

            _log.LogInfo(
                $"Cotas generadas: {cotas.Count} para layer '{analisis.LayerOrigen}' " +
                $"(DimStyle: {clasificacion.DimStyle}).");

            return cotas;
        }

        // ── Construcción de cota alineada ─────────────────────

        /// <summary>
        /// Construye una RotatedDimension (cota alineada al segmento).
        /// No usa Alignment porque RotatedDimension con ángulo del segmento
        /// es más estable en AutoCAD/ZWCAD.
        /// </summary>
        private RotatedDimension? ConstruirCotaAlineada(
            SegmentoLineal  segmento,
            ContextoEstilo  contexto,
            double          offsetExterior)
        {
            try
            {
                var p0  = segmento.PtoInicio;
                var p1  = segmento.PtoFin;
                var ang = segmento.Angulo;

                // Punto de línea de cota: desplazado perpendicularmente
                var ptCota = GeometryHelper.OffsetPuntoNormal(p0, p1, offsetExterior);

                var dim = new RotatedDimension
                {
                    // ── Puntos de definición ─────────────────
                    XLine1Point      = new Point3d(p0.X, p0.Y, 0),
                    XLine2Point      = new Point3d(p1.X, p1.Y, 0),
                    DimLinePoint     = new Point3d(ptCota.X, ptCota.Y, 0),

                    // ── Ángulo de rotación (alineado al segmento) ─
                    Rotation         = ang,

                    // ── Estilo institucional desde DWT ────────
                    DimensionStyleId = contexto.DimStyleId,

                    // ── Layer de cotas ────────────────────────
                    LayerId          = contexto.LayerCotasId,

                    // ── Sin overrides ─────────────────────────
                    // Toda la configuración viene del DimStyle del DWT
                };

                // Aplicar textstyle si está disponible
                // NOTA: No se modifica DIMTXT, DIMGAP, DIMASZ.
                //       Todo lo hereda del DimStyle.
                if (contexto.TextStyleId != ObjectId.Null)
                    dim.TextStyleId = contexto.TextStyleId;

                return dim;
            }
            catch (Exception ex)
            {
                _log.LogError(
                    $"Error construyendo cota para segmento " +
                    $"[{segmento.PtoInicio:F2} → {segmento.PtoFin:F2}]", ex);
                return null;
            }
        }

        // ── Acotado de lote ───────────────────────────────────

        /// <summary>
        /// Genera cotas específicas para el perímetro del lote (TG_LOTE).
        /// Usa COTA_LOTE y distancia exterior mayor por convención.
        /// </summary>
        public List<RotatedDimension> GenerarCotasLote(
            ResultadoAnalisis analisis, Database db)
        {
            return GenerarCotasPoligono(analisis, db, new ParametrosAcotado
            {
                OffsetExterior = 0.80,   // Lote tiene mayor offset
                LongitudMinima = 0.20
            });
        }

        /// <summary>
        /// Genera cotas para fábrica (PISO_NN, AREA_LIBRE, etc.)
        /// con offset estándar catastral.
        /// </summary>
        public List<RotatedDimension> GenerarCotasFabrica(
            ResultadoAnalisis analisis, Database db)
        {
            return GenerarCotasPoligono(analisis, db, new ParametrosAcotado
            {
                OffsetExterior = 0.50,
                LongitudMinima = 0.10
            });
        }

        // ── Helpers ───────────────────────────────────────────

        /// <summary>
        /// Determina si un ángulo (radianes) es aproximadamente
        /// horizontal (0°) o vertical (90°).
        /// </summary>
        private static bool EsOrtonormal(double angulo, double tolerancia)
        {
            double norm = Math.Abs(angulo % (Math.PI / 2.0));
            return norm <= tolerancia || norm >= (Math.PI / 2.0 - tolerancia);
        }
    }
}
