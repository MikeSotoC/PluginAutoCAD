// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  PolylineAnalyzer.cs
//  Análisis, validación y extracción de datos de polilíneas
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CatastroUrbano.Core.Geometry;
using CatastroUrbano.Core.Infrastructure;
using CatastroUrbano.Core.Models;

namespace CatastroUrbano.Core.Analysis
{
    // ─────────────────────────────────────────────────────────
    //  RESULTADO DE ANÁLISIS
    // ─────────────────────────────────────────────────────────

    public sealed class ResultadoAnalisis
    {
        public bool   Valida             { get; set; }
        public string MotivoInvalidez    { get; set; } = string.Empty;

        public double         Area            { get; set; }
        public Point3d        Centroide       { get; set; }
        public List<Point3d>  Vertices        { get; set; } = new();
        public List<SegmentoLineal> Segmentos { get; set; } = new();

        public Point3d PtMin { get; set; }
        public Point3d PtMax { get; set; }

        public double AnchoX   => PtMax.X - PtMin.X;
        public double AlturaY  => PtMax.Y - PtMin.Y;

        public string LayerOrigen   { get; set; } = string.Empty;
        public long   ObjectIdRaw   { get; set; }
    }

    // ─────────────────────────────────────────────────────────
    //  ANALIZADOR PRINCIPAL
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Analiza polilíneas cerradas del DWG para extraer todos los
    /// datos geométricos requeridos por el sistema catastral.
    /// </summary>
    public sealed class PolylineAnalyzer
    {
        private readonly ErrorHandler _log = ErrorHandler.Instancia;

        private const double LongitudMinimaSegmento = 0.05;   // m
        private const double AreaMinimaValida        = 0.01;   // m²
        private const double AreaMaximaValida        = 100_000; // m²

        // ── API principal ─────────────────────────────────────

        /// <summary>
        /// Analiza una entidad CAD e intenta extraer datos catastrales.
        /// Soporta Polyline (2D ligera) y Polyline2d (formato antiguo).
        /// </summary>
        public ResultadoAnalisis Analizar(Entity entidad, Transaction tr)
        {
            return entidad switch
            {
                Polyline   pline   => AnalizarPolyline(pline),
                Polyline2d pline2d => AnalizarPolyline2d(pline2d, tr),
                _                  => ResultadoInvalido(
                    $"Tipo no soportado: {entidad.GetType().Name}. " +
                    "Solo se procesan Polyline y Polyline2d.")
            };
        }

        // ── Análisis de Polyline (ligera) ──────────────────────

        private ResultadoAnalisis AnalizarPolyline(Polyline pline)
        {
            var resultado = new ResultadoAnalisis
            {
                LayerOrigen = pline.Layer,
                ObjectIdRaw = pline.ObjectId.OldIdPtr.ToInt64()
            };

            // ── Validar cierre ────────────────────────────────
            if (!GeometryHelper.EstaFisicamenteCerrada(pline))
            {
                return ResultadoInvalido(
                    "La polilínea no está cerrada. " +
                    "Usar CLOSE o verificar que el primer y último " +
                    "vértice coincidan dentro de 1mm.", pline.Layer);
            }

            // ── Validar número de vértices ────────────────────
            if (pline.NumberOfVertices < 3)
            {
                return ResultadoInvalido(
                    "La polilínea tiene menos de 3 vértices.", pline.Layer);
            }

            // ── Extraer vértices ──────────────────────────────
            resultado.Vertices = GeometryHelper.ExtraerVertices(pline);

            // ── Calcular área ─────────────────────────────────
            // Prioridad: área interna de la Polyline para máxima precisión
            resultado.Area = ObtenerAreaPolyline(pline);

            if (resultado.Area < AreaMinimaValida)
            {
                return ResultadoInvalido(
                    $"Área calculada ({resultado.Area:F4} m²) " +
                    $"por debajo del mínimo válido ({AreaMinimaValida} m²).",
                    pline.Layer);
            }

            if (resultado.Area > AreaMaximaValida)
            {
                return ResultadoInvalido(
                    $"Área calculada ({resultado.Area:F2} m²) " +
                    $"supera el máximo catastral ({AreaMaximaValida} m²). " +
                    "Verificar escala del dibujo.", pline.Layer);
            }

            // ── Calcular centroide ────────────────────────────
            resultado.Centroide = GeometryHelper.CalcularCentroide(resultado.Vertices);

            // ── Extraer segmentos ─────────────────────────────
            resultado.Segmentos = GeometryHelper
                .ObtenerSegmentos(pline)
                .Where(s => s.EsAcotable(LongitudMinimaSegmento))
                .ToList();

            // ── Bounding Box ──────────────────────────────────
            var (minPt, maxPt) = GeometryHelper.CalcularBoundingBox(resultado.Vertices);
            resultado.PtMin = minPt;
            resultado.PtMax = maxPt;

            resultado.Valida = true;
            return resultado;
        }

        // ── Análisis de Polyline2d (formato antiguo) ──────────

        private ResultadoAnalisis AnalizarPolyline2d(Polyline2d pline2d, Transaction tr)
        {
            var resultado = new ResultadoAnalisis
            {
                LayerOrigen = pline2d.Layer,
                ObjectIdRaw = pline2d.ObjectId.OldIdPtr.ToInt64()
            };

            if (!pline2d.Closed)
                return ResultadoInvalido(
                    "Polyline2d no está cerrada.", pline2d.Layer);

            resultado.Vertices = GeometryHelper.ExtraerVertices(pline2d, tr);

            if (resultado.Vertices.Count < 3)
                return ResultadoInvalido(
                    "Polyline2d con menos de 3 vértices.", pline2d.Layer);

            resultado.Area      = GeometryHelper.CalcularArea(resultado.Vertices);
            resultado.Centroide = GeometryHelper.CalcularCentroide(resultado.Vertices);

            var (minPt, maxPt) = GeometryHelper.CalcularBoundingBox(resultado.Vertices);
            resultado.PtMin = minPt;
            resultado.PtMax = maxPt;

            // Para Polyline2d construimos segmentos manualmente
            resultado.Segmentos = ConstruirSegmentos2d(resultado.Vertices)
                .Where(s => s.EsAcotable(LongitudMinimaSegmento))
                .ToList();

            resultado.Valida = true;
            return resultado;
        }

        // ── Análisis por lotes ────────────────────────────────

        /// <summary>
        /// Analiza múltiples ObjectIds y retorna solo los válidos.
        /// Registra advertencias para los inválidos.
        /// </summary>
        public List<(ObjectId Id, ResultadoAnalisis Resultado)> AnalizarLote(
            IEnumerable<ObjectId> ids, Transaction tr)
        {
            var resultados = new List<(ObjectId, ResultadoAnalisis)>();

            foreach (var id in ids)
            {
                try
                {
                    var entidad = (Entity)tr.GetObject(id, OpenMode.ForRead);
                    var r = Analizar(entidad, tr);

                    if (r.Valida)
                    {
                        resultados.Add((id, r));
                    }
                    else
                    {
                        _log.LogAdvertencia(
                            $"Polilínea descartada [{id}]: {r.MotivoInvalidez}");
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError($"Error analizando entidad [{id}]", ex);
                }
            }

            _log.LogInfo(
                $"Lote analizado: {resultados.Count} válidas de {ids.Count()} seleccionadas.");

            return resultados;
        }

        // ── Construcción de PoligonoCatastral desde análisis ──

        /// <summary>
        /// Construye un PoligonoCatastral poblado con los datos
        /// del análisis geométrico.
        /// </summary>
        public PoligonoCatastral ConstruirEntidad(
            ResultadoAnalisis analisis, ObjectId objectId)
        {
            var entidad = new PoligonoCatastral
            {
                LayerOrigen       = analisis.LayerOrigen,
                Area              = analisis.Area,
                CentroGeometrico  = analisis.Centroide,
                EsPolilineaCerrada = true,
                AreaCalculada     = true,
                ObjectIdPolilinea = objectId.OldIdPtr.ToInt64()
            };

            foreach (var v in analisis.Vertices)
                entidad.Vertices.Add(v);

            return entidad;
        }

        // ── Helpers privados ──────────────────────────────────

        /// <summary>
        /// Obtiene el área directamente desde la API de Polyline.
        /// Más precisa que Shoelace para polilíneas con arcos.
        /// </summary>
        private static double ObtenerAreaPolyline(Polyline pline)
        {
            try
            {
                // La API de AutoCAD/ZWCAD calcula el área internamente
                // incluyendo bulges (arcos)
                return Math.Abs(pline.Area);
            }
            catch
            {
                // Fallback a Shoelace
                var vertices = GeometryHelper.ExtraerVertices(pline);
                return GeometryHelper.CalcularArea(vertices);
            }
        }

        private static List<SegmentoLineal> ConstruirSegmentos2d(
            List<Point3d> vertices)
        {
            var segs = new List<SegmentoLineal>(vertices.Count);
            for (int i = 0; i < vertices.Count; i++)
            {
                int j = (i + 1) % vertices.Count;
                segs.Add(new SegmentoLineal(vertices[i], vertices[j], false, 0));
            }
            return segs;
        }

        private static ResultadoAnalisis ResultadoInvalido(
            string motivo, string layer = "")
        {
            return new ResultadoAnalisis
            {
                Valida          = false,
                MotivoInvalidez = motivo,
                LayerOrigen     = layer
            };
        }
    }
}
