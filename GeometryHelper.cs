// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  GeometryHelper.cs
//  Utilidades de geometría CAD reutilizables
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace CatastroUrbano.Core.Geometry
{
    /// <summary>
    /// Biblioteca de utilidades geométricas para operaciones
    /// catastrales: centros, segmentos, normalización de polilíneas.
    /// </summary>
    public static class GeometryHelper
    {
        // ── Tolerancias institucionales ───────────────────────

        private const double Toleranciacierre    = 0.001;  // metros
        private const double ToleranciaCollinear = 1e-6;

        // ─────────────────────────────────────────────────────
        //  CENTRO GEOMÉTRICO (CENTROIDE)
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Calcula el centroide de un polígono definido por sus vértices
        /// usando la fórmula de centroide de polígono (no el bounding box).
        /// </summary>
        public static Point3d CalcularCentroide(IList<Point3d> vertices)
        {
            if (vertices == null || vertices.Count < 3)
                throw new ArgumentException(
                    "Se requieren al menos 3 vértices para calcular el centroide.");

            double areaSignada = 0.0;
            double cx = 0.0;
            double cy = 0.0;
            int n = vertices.Count;

            for (int i = 0; i < n; i++)
            {
                int    j    = (i + 1) % n;
                double cruz = vertices[i].X * vertices[j].Y
                            - vertices[j].X * vertices[i].Y;
                areaSignada += cruz;
                cx          += (vertices[i].X + vertices[j].X) * cruz;
                cy          += (vertices[i].Y + vertices[j].Y) * cruz;
            }

            areaSignada *= 0.5;

            if (Math.Abs(areaSignada) < 1e-10)
            {
                // Polígono degenerado: usar centroide simple
                double mx = vertices.Average(v => v.X);
                double my = vertices.Average(v => v.Y);
                return new Point3d(mx, my, vertices[0].Z);
            }

            double factor = 1.0 / (6.0 * areaSignada);
            return new Point3d(cx * factor, cy * factor, vertices[0].Z);
        }

        // ─────────────────────────────────────────────────────
        //  ÁREA DE POLÍGONO (SHOELACE)
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Calcula el área de un polígono plano por la fórmula de Shoelace.
        /// Devuelve siempre valor positivo.
        /// </summary>
        public static double CalcularArea(IList<Point3d> vertices)
        {
            if (vertices == null || vertices.Count < 3) return 0.0;

            double suma = 0.0;
            int n = vertices.Count;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                suma += vertices[i].X * vertices[j].Y;
                suma -= vertices[j].X * vertices[i].Y;
            }

            return Math.Abs(suma) * 0.5;
        }

        // ─────────────────────────────────────────────────────
        //  EXTRACCIÓN DE VÉRTICES DESDE POLYLINE
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Extrae todos los vértices de una Polyline (2D) en formato Point3d.
        /// Trabaja con Polyline (ligera, la más común en catastro).
        /// </summary>
        public static List<Point3d> ExtraerVertices(Polyline pline)
        {
            var vertices = new List<Point3d>(pline.NumberOfVertices);
            for (int i = 0; i < pline.NumberOfVertices; i++)
                vertices.Add(pline.GetPoint3dAt(i));
            return vertices;
        }

        /// <summary>
        /// Extrae vértices de una Polyline2d (formato antiguo).
        /// </summary>
        public static List<Point3d> ExtraerVertices(
            Polyline2d pline2d, Transaction tr)
        {
            var vertices = new List<Point3d>();
            foreach (ObjectId vId in pline2d)
            {
                var v = (Vertex2d)tr.GetObject(vId, OpenMode.ForRead);
                vertices.Add(new Point3d(v.Position.X, v.Position.Y, 0));
            }
            return vertices;
        }

        // ─────────────────────────────────────────────────────
        //  VALIDACIÓN DE CIERRE
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Verifica si una Polyline está cerrada o si su primer y último
        /// vértice coinciden dentro de la tolerancia institucional.
        /// </summary>
        public static bool EstaFisicamenteCerrada(Polyline pline)
        {
            if (pline.Closed) return true;
            if (pline.NumberOfVertices < 3) return false;

            var p0 = pline.GetPoint3dAt(0);
            var pN = pline.GetPoint3dAt(pline.NumberOfVertices - 1);
            return p0.DistanceTo(pN) <= Toleranciacierre;
        }

        // ─────────────────────────────────────────────────────
        //  BOUNDING BOX
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Calcula el bounding box 2D de un conjunto de vértices.
        /// </summary>
        public static (Point3d MinPt, Point3d MaxPt) CalcularBoundingBox(
            IList<Point3d> vertices)
        {
            if (vertices == null || !vertices.Any())
                throw new ArgumentException("Lista de vértices vacía.");

            double minX = vertices.Min(v => v.X);
            double minY = vertices.Min(v => v.Y);
            double maxX = vertices.Max(v => v.X);
            double maxY = vertices.Max(v => v.Y);
            double z    = vertices[0].Z;

            return (new Point3d(minX, minY, z), new Point3d(maxX, maxY, z));
        }

        // ─────────────────────────────────────────────────────
        //  SEGMENTOS DE UNA POLYLINE
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Extrae los segmentos lineales de una polilínea cerrada.
        /// Cada segmento se representa como (PtoInicio, PtoFin, Longitud).
        /// Los segmentos de arco se convierten en su cuerda.
        /// </summary>
        public static List<SegmentoLineal> ObtenerSegmentos(Polyline pline)
        {
            var segmentos = new List<SegmentoLineal>();
            int n = pline.NumberOfVertices;

            for (int i = 0; i < n; i++)
            {
                // Para polilínea cerrada, el último segmento vuelve al inicio
                int siguiente = (i + 1) % n;
                if (!pline.Closed && siguiente == 0) break;

                var p0 = pline.GetPoint3dAt(i);
                var p1 = pline.GetPoint3dAt(siguiente);

                // Segmento tipo: 0 = línea, 1 = arco (bulge != 0)
                double bulge = pline.GetBulgeAt(i);

                if (Math.Abs(bulge) < ToleranciaCollinear)
                {
                    // Segmento recto
                    segmentos.Add(new SegmentoLineal(p0, p1, false, bulge));
                }
                else
                {
                    // Segmento de arco: usamos la cuerda para acotado
                    segmentos.Add(new SegmentoLineal(p0, p1, true, bulge));
                }
            }

            return segmentos;
        }

        // ─────────────────────────────────────────────────────
        //  PUNTO MEDIO DE SEGMENTO
        // ─────────────────────────────────────────────────────

        public static Point3d PuntoMedio(Point3d p0, Point3d p1) =>
            new((p0.X + p1.X) / 2.0,
                (p0.Y + p1.Y) / 2.0,
                (p0.Z + p1.Z) / 2.0);

        // ─────────────────────────────────────────────────────
        //  OFFSET DE PUNTO EN DIRECCIÓN NORMAL
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Desplaza un punto en la dirección perpendicular al segmento,
        /// hacia afuera del polígono. Usado para insertar cotas.
        /// </summary>
        public static Point3d OffsetPuntoNormal(
            Point3d p0, Point3d p1, double distancia)
        {
            double dx = p1.X - p0.X;
            double dy = p1.Y - p0.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);

            if (len < 1e-10) return PuntoMedio(p0, p1);

            // Normal perpendicular
            double nx = -dy / len;
            double ny =  dx / len;

            var mid = PuntoMedio(p0, p1);
            return new Point3d(mid.X + nx * distancia,
                               mid.Y + ny * distancia, p0.Z);
        }

        // ─────────────────────────────────────────────────────
        //  LONGITUD DE SEGMENTO EN XY
        // ─────────────────────────────────────────────────────

        public static double LongitudXY(Point3d p0, Point3d p1)
        {
            double dx = p1.X - p0.X;
            double dy = p1.Y - p0.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // ─────────────────────────────────────────────────────
        //  ÁNGULO DE SEGMENTO (para rotación de texto)
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Retorna el ángulo en radianes del segmento respecto al eje X.
        /// Normalizado en [0, π] para textos de cota legibles.
        /// </summary>
        public static double AnguloSegmento(Point3d p0, Point3d p1)
        {
            double angulo = Math.Atan2(p1.Y - p0.Y, p1.X - p0.X);
            // Normalizar para que el texto nunca quede boca abajo
            if (angulo < 0) angulo += Math.PI;
            if (angulo >= Math.PI) angulo -= Math.PI;
            return angulo;
        }

        // ─────────────────────────────────────────────────────
        //  PUNTO DE INSERCIÓN DE TEXTO INTERIOR
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Calcula el punto de inserción de texto interior centrado
        /// geométricamente en un polígono. Usa el centroide.
        /// </summary>
        public static Point3d ObtenerPuntoTextoInterior(Polyline pline)
        {
            var vertices = ExtraerVertices(pline);
            return CalcularCentroide(vertices);
        }
    }

    // ─────────────────────────────────────────────────────────
    //  MODELO DE SEGMENTO LINEAL
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Representa un segmento de polilínea con sus propiedades
    /// geométricas para el motor de acotado.
    /// </summary>
    public sealed class SegmentoLineal
    {
        public Point3d PtoInicio    { get; }
        public Point3d PtoFin       { get; }
        public bool    EsArco       { get; }
        public double  Bulge        { get; }

        public double  Longitud     => GeometryHelper.LongitudXY(PtoInicio, PtoFin);
        public Point3d PuntoMedio   => GeometryHelper.PuntoMedio(PtoInicio, PtoFin);
        public double  Angulo       => GeometryHelper.AnguloSegmento(PtoInicio, PtoFin);

        public SegmentoLineal(Point3d p0, Point3d p1, bool esArco, double bulge)
        {
            PtoInicio = p0;
            PtoFin    = p1;
            EsArco    = esArco;
            Bulge     = bulge;
        }

        /// <summary>
        /// Filtra segmentos que no vale la pena acotar (muy cortos).
        /// Umbral 0.10 m estándar catastral.
        /// </summary>
        public bool EsAcotable(double longitudMinima = 0.10) =>
            Longitud >= longitudMinima;

        public override string ToString() =>
            $"Seg [{PtoInicio:F3} → {PtoFin:F3}] L={Longitud:F3} " +
            (EsArco ? "ARC" : "LIN");
    }
}
