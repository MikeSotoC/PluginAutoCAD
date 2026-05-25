// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  CadLineEngine.cs
//  Motor de dibujo de geometría base: líneas y polilíneas
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CatastroUrbano.Core.Infrastructure;
using CatastroUrbano.Core.Styles;

namespace CatastroUrbano.Core.CAD
{
    // ─────────────────────────────────────────────────────────
    //  PARÁMETROS DE LÍNEA
    // ─────────────────────────────────────────────────────────

    public sealed class ParamsLinea
    {
        public Point3d  Inicio      { get; set; }
        public Point3d  Fin         { get; set; }
        public ObjectId LayerId     { get; set; } = ObjectId.Null;
        public string   NombreLayer { get; set; } = "FORMATO";
    }

    // ─────────────────────────────────────────────────────────
    //  MOTOR DE LÍNEAS CAD
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Fábrica de primitivas CAD: Line, Polyline.
    /// Todas las entidades se construyen SIN agregar a la DB.
    /// La inserción la realiza CadTransactionManager.
    /// </summary>
    public sealed class CadLineEngine
    {
        private readonly StyleResolver  _styleResolver;
        private readonly ErrorHandler   _log = ErrorHandler.Instancia;

        public CadLineEngine(StyleResolver styleResolver)
        {
            _styleResolver = styleResolver
                ?? throw new ArgumentNullException(nameof(styleResolver));
        }

        // ── Línea simple ──────────────────────────────────────

        /// <summary>
        /// Construye una Line entre dos puntos en el layer especificado.
        /// </summary>
        public Line ConstruirLinea(
            Point3d inicio, Point3d fin,
            ObjectId layerId)
        {
            var linea = new Line(inicio, fin);
            if (layerId != ObjectId.Null)
                linea.LayerId = layerId;
            return linea;
        }

        /// <summary>
        /// Construye una Line resolviendo el layer por nombre.
        /// </summary>
        public Line ConstruirLinea(
            Point3d inicio, Point3d fin,
            string nombreLayer, Database db)
        {
            var layerId = _styleResolver.ResolverLayer(nombreLayer, db);
            return ConstruirLinea(inicio, fin, layerId);
        }

        // ── Rectángulo ────────────────────────────────────────

        /// <summary>
        /// Construye una Polyline rectangular cerrada.
        /// Origen = esquina superior izquierda.
        /// </summary>
        public Polyline ConstruirRectangulo(
            double xOrigen, double yOrigen,
            double ancho,   double alto,
            ObjectId layerId)
        {
            var pline = new Polyline();
            pline.AddVertexAt(0, new Point2d(xOrigen,         yOrigen),         0, 0, 0);
            pline.AddVertexAt(1, new Point2d(xOrigen + ancho, yOrigen),         0, 0, 0);
            pline.AddVertexAt(2, new Point2d(xOrigen + ancho, yOrigen - alto),  0, 0, 0);
            pline.AddVertexAt(3, new Point2d(xOrigen,         yOrigen - alto),  0, 0, 0);
            pline.Closed = true;

            if (layerId != ObjectId.Null)
                pline.LayerId = layerId;

            return pline;
        }

        public Polyline ConstruirRectangulo(
            double xOrigen, double yOrigen,
            double ancho,   double alto,
            string nombreLayer, Database db)
        {
            var layerId = _styleResolver.ResolverLayer(nombreLayer, db);
            return ConstruirRectangulo(xOrigen, yOrigen, ancho, alto, layerId);
        }

        // ── Segmento horizontal ───────────────────────────────

        /// <summary>
        /// Construye una línea horizontal en Y dado, entre X1 y X2.
        /// </summary>
        public Line ConstruirLineaHorizontal(
            double x1, double x2, double y,
            ObjectId layerId)
        {
            return ConstruirLinea(
                new Point3d(x1, y, 0),
                new Point3d(x2, y, 0),
                layerId);
        }

        public Line ConstruirLineaHorizontal(
            double x1, double x2, double y,
            string nombreLayer, Database db)
        {
            var layerId = _styleResolver.ResolverLayer(nombreLayer, db);
            return ConstruirLineaHorizontal(x1, x2, y, layerId);
        }

        // ── Segmento vertical ─────────────────────────────────

        /// <summary>
        /// Construye una línea vertical en X dado, entre Y1 y Y2.
        /// </summary>
        public Line ConstruirLineaVertical(
            double x, double y1, double y2,
            ObjectId layerId)
        {
            return ConstruirLinea(
                new Point3d(x, y1, 0),
                new Point3d(x, y2, 0),
                layerId);
        }

        public Line ConstruirLineaVertical(
            double x, double y1, double y2,
            string nombreLayer, Database db)
        {
            var layerId = _styleResolver.ResolverLayer(nombreLayer, db);
            return ConstruirLineaVertical(x, y1, y2, layerId);
        }

        // ── Borde de cuadro (4 líneas) ────────────────────────

        /// <summary>
        /// Construye las 4 líneas del borde exterior de un cuadro.
        /// Retorna lista en orden: Top, Bottom, Left, Right.
        /// </summary>
        public List<Line> ConstruirBordeCuadro(
            double xIzq, double xDer,
            double ySup, double yInf,
            ObjectId layerId)
        {
            return new List<Line>
            {
                ConstruirLineaHorizontal(xIzq, xDer, ySup,  layerId), // Top
                ConstruirLineaHorizontal(xIzq, xDer, yInf,  layerId), // Bottom
                ConstruirLineaVertical  (xIzq, ySup, yInf,  layerId), // Left
                ConstruirLineaVertical  (xDer, ySup, yInf,  layerId)  // Right
            };
        }

        public List<Line> ConstruirBordeCuadro(
            double xIzq, double xDer,
            double ySup, double yInf,
            string nombreLayer, Database db)
        {
            var layerId = _styleResolver.ResolverLayer(nombreLayer, db);
            return ConstruirBordeCuadro(xIzq, xDer, ySup, yInf, layerId);
        }

        // ── Polilínea por lista de puntos ─────────────────────

        /// <summary>
        /// Construye una Polyline a partir de una lista de Point2d.
        /// </summary>
        public Polyline ConstruirPolyline(
            IList<Point2d> puntos,
            bool cerrada,
            ObjectId layerId)
        {
            var pline = new Polyline();
            for (int i = 0; i < puntos.Count; i++)
                pline.AddVertexAt(i, puntos[i], 0, 0, 0);

            pline.Closed = cerrada;

            if (layerId != ObjectId.Null)
                pline.LayerId = layerId;

            return pline;
        }
    }
}
