// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  LeaderEngine.cs
//
//  Motor de dibujo de leaders catastrales institucionales.
//
//  ANATOMÍA DEL LEADER CATASTRAL PERUANO:
//
//   Punto origen (flecha)
//       ●
//        \              ← Línea de llamada inclinada
//         \
//          └───────────  ← Línea horizontal (encima del texto)
//          ÍTEM 10 — Puerta de fierro...    ← MText debajo
//          Descripción libre opcional
//
//  La dirección de la línea horizontal es ADAPTATIVA:
//  Si el destino está a la derecha → horizontal va a la derecha.
//  Si el destino está a la izquierda → horizontal va a la izquierda.
//  El texto SIEMPRE cuelga debajo del extremo de la horizontal.
//
//  COMPONENTES GENERADOS (todo geometría CAD pura):
//    1. Solid        → cabeza de flecha rellena en el origen
//    2. Line         → línea de llamada inclinada (origen → quiebre)
//    3. Line         → línea horizontal (quiebre → extremo)
//    4. MText        → etiqueta colgando debajo del extremo
//
//  NO usa MLeader API. NO crea ni modifica nada en el DWT.
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
    //  PARÁMETROS DE ENTRADA DEL LEADER
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Define todos los datos necesarios para construir un leader
    /// catastral institucional. El llamador solo provee puntos y texto;
    /// el motor calcula el quiebre y la dirección.
    /// </summary>
    public sealed class ParamsLeader
    {
        // ── Geometría definida por el operador ────────────────

        /// <summary>
        /// Punto de ORIGEN: donde apunta la flecha.
        /// El operador hace clic sobre el elemento catastral.
        /// </summary>
        public Point3d PuntoOrigen   { get; set; }

        /// <summary>
        /// Punto de DESTINO: guía la dirección y posición del texto.
        /// El operador hace clic donde quiere que quede la etiqueta.
        /// </summary>
        public Point3d PuntoDestino  { get; set; }

        // ── Contenido textual ─────────────────────────────────

        /// <summary>
        /// Línea 1 del MText: ítem del Anexo III.
        /// Ejemplo: "Ítem 10 — Puerta de fierro, aluminio o similar"
        /// </summary>
        public string TextoLinea1    { get; set; } = string.Empty;

        /// <summary>
        /// Línea 2 del MText: descripción libre + dimensiones del operador.
        /// Ejemplo: "Portón principal / 2.40 × 1.00 m"
        /// Puede ser string.Empty si el operador no la completó.
        /// </summary>
        public string TextoLinea2    { get; set; } = string.Empty;

        // ── Dimensiones gráficas ──────────────────────────────

        /// <summary>Lado del triángulo relleno de la flecha (unidades de dibujo).</summary>
        public double TamanoFlecha         { get; set; } = 0.20;

        /// <summary>Longitud de la línea horizontal a partir del quiebre.</summary>
        public double LongitudHorizontal   { get; set; } = 2.00;

        /// <summary>
        /// Separación vertical entre la línea horizontal y el texto.
        /// El texto cuelga esta distancia por debajo de la línea.
        /// </summary>
        public double MargenTextoVertical  { get; set; } = 0.08;

        /// <summary>Altura del texto MText de la etiqueta.</summary>
        public double AlturaTexto          { get; set; } = 0.18;

        // ── Layers y estilos (deben existir en el DWT) ────────
        public string NombreLayerLeader    { get; set; } = "DESCRIPCION";
        public string NombreLayerTexto     { get; set; } = "DESCRIPCION";
        public string NombreTextStyle      { get; set; } = "TEXTO";
    }

    // ─────────────────────────────────────────────────────────
    //  RESULTADO DEL LEADER CONSTRUIDO
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Contiene todas las entidades CAD del leader listas para
    /// inserción batch por CadTransactionManager.
    /// </summary>
    public sealed class ResultadoLeader
    {
        /// <summary>Triángulo relleno en el punto de origen.</summary>
        public Solid? Flecha            { get; set; }

        /// <summary>Línea inclinada de origen al quiebre.</summary>
        public Line?  LineaLlamada      { get; set; }

        /// <summary>Línea horizontal del quiebre al extremo del texto.</summary>
        public Line?  LineaHorizontal   { get; set; }

        /// <summary>MText con el contenido de la obra complementaria.</summary>
        public MText? Etiqueta          { get; set; }

        public IEnumerable<Entity> TodasLasEntidades()
        {
            if (Flecha          != null) yield return Flecha;
            if (LineaLlamada    != null) yield return LineaLlamada;
            if (LineaHorizontal != null) yield return LineaHorizontal;
            if (Etiqueta        != null) yield return Etiqueta;
        }

        public int CantidadEntidades =>
            (Flecha          != null ? 1 : 0) +
            (LineaLlamada    != null ? 1 : 0) +
            (LineaHorizontal != null ? 1 : 0) +
            (Etiqueta        != null ? 1 : 0);
    }

    // ─────────────────────────────────────────────────────────
    //  MOTOR DE LEADERS CATASTRALES
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Construye leaders institucionales catastrales peruanos.
    /// Toda la geometría es CAD pura (Solid, Line, MText).
    /// La dirección de la horizontal es adaptativa.
    /// El texto cuelga siempre debajo del extremo de la horizontal.
    /// </summary>
    public sealed class LeaderEngine
    {
        private readonly StyleResolver _styleResolver;
        private readonly MTextEngine   _mtextEngine;
        private readonly ErrorHandler  _log = ErrorHandler.Instancia;

        public LeaderEngine(StyleResolver styleResolver, MTextEngine mtextEngine)
        {
            _styleResolver = styleResolver
                ?? throw new ArgumentNullException(nameof(styleResolver));
            _mtextEngine   = mtextEngine
                ?? throw new ArgumentNullException(nameof(mtextEngine));
        }

        // ─────────────────────────────────────────────────────
        //  CONSTRUCCIÓN PRINCIPAL
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Genera las cuatro entidades del leader catastral a partir de
        /// PuntoOrigen (flecha) y PuntoDestino (guía de texto).
        /// </summary>
        public ResultadoLeader Construir(ParamsLeader p, Database db)
        {
            var resultado = new ResultadoLeader();

            try
            {
                // ── Resolver ObjectIds del DWT ──────────────────
                var layerLeaderId = _styleResolver.ResolverLayer(
                    p.NombreLayerLeader, db);
                var layerTextoId  = _styleResolver.ResolverLayer(
                    p.NombreLayerTexto, db);
                var textStyleId   = _styleResolver.ResolverTextStyle(
                    p.NombreTextStyle, db);

                // ── Calcular geometría del leader ───────────────

                // Dirección adaptativa: derecha si destino.X >= origen.X
                bool vaADerecha = p.PuntoDestino.X >= p.PuntoOrigen.X;

                // Punto de quiebre: mismo Y que destino, mismo X que origen
                var ptQuiebre = new Point3d(
                    p.PuntoOrigen.X,
                    p.PuntoDestino.Y,
                    0);

                // Extremo de la línea horizontal
                double xExtremo = vaADerecha
                    ? ptQuiebre.X + p.LongitudHorizontal
                    : ptQuiebre.X - p.LongitudHorizontal;

                var ptExtremo = new Point3d(xExtremo, ptQuiebre.Y, 0);

                // Punto de arranque de la línea de llamada
                // (desplazado desde el origen para no solapar la flecha)
                var ptArranque = DesplazarDesdeOrigen(
                    p.PuntoOrigen, ptQuiebre, p.TamanoFlecha);

                // ── 1. Cabeza de flecha ─────────────────────────
                resultado.Flecha = ConstruirFlecha(
                    p.PuntoOrigen, ptQuiebre,
                    p.TamanoFlecha, layerLeaderId);

                // ── 2. Línea de llamada inclinada ───────────────
                resultado.LineaLlamada = new Line(ptArranque, ptQuiebre)
                {
                    LayerId = layerLeaderId
                };

                // ── 3. Línea horizontal ─────────────────────────
                resultado.LineaHorizontal = new Line(ptQuiebre, ptExtremo)
                {
                    LayerId = layerLeaderId
                };

                // ── 4. MText colgando debajo del extremo ────────
                resultado.Etiqueta = ConstruirMTextColgante(
                    p, ptExtremo, vaADerecha,
                    textStyleId, layerTextoId);

                _log.LogInfo(
                    $"Leader construido: '{p.TextoLinea1}' | " +
                    $"Origen={p.PuntoOrigen:F2} | " +
                    $"Quiebre={ptQuiebre:F2} | " +
                    $"Extremo={ptExtremo:F2} | " +
                    $"Dirección={(vaADerecha ? "→" : "←")}");
            }
            catch (Exception ex)
            {
                _log.LogError("Error construyendo leader catastral", ex);
            }

            return resultado;
        }

        // ─────────────────────────────────────────────────────
        //  CABEZA DE FLECHA (SOLID RELLENO)
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Triángulo relleno apuntando en la dirección origen → quiebre.
        /// Usa Solid de 4 puntos (p3 == p4 para triángulo cerrado).
        /// </summary>
        private static Solid ConstruirFlecha(
            Point3d origen, Point3d quiebre,
            double  tamano, ObjectId layerId)
        {
            var vecDir = quiebre - origen;
            double len = vecDir.Length;

            // Dirección normalizada hacia el quiebre
            var dir  = len > 1e-6 ? vecDir / len : new Vector3d(1, 0, 0);

            // Perpendicular en plano XY
            var perp = new Vector3d(-dir.Y, dir.X, 0);

            // Tres vértices:
            //   - Punta exacta en el punto de origen (el elemento señalado)
            //   - Base izquierda y derecha desplazadas en la dirección de la llamada
            var punta    = new Point3d(origen.X,  origen.Y,  0);
            var baseIzq  = new Point3d(
                (origen + dir * tamano - perp * (tamano * 0.38)).X,
                (origen + dir * tamano - perp * (tamano * 0.38)).Y, 0);
            var baseDer  = new Point3d(
                (origen + dir * tamano + perp * (tamano * 0.38)).X,
                (origen + dir * tamano + perp * (tamano * 0.38)).Y, 0);

            return new Solid(punta, baseIzq, baseDer, baseDer)
            {
                LayerId = layerId
            };
        }

        // ─────────────────────────────────────────────────────
        //  DESPLAZAMIENTO DESDE EL ORIGEN
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Calcula el punto de arranque de la línea de llamada:
        /// desplazado desde el origen una distancia igual al tamaño
        /// de la flecha para evitar solapamiento visual.
        /// </summary>
        private static Point3d DesplazarDesdeOrigen(
            Point3d origen, Point3d quiebre, double tamano)
        {
            var vec = quiebre - origen;
            double len = vec.Length;
            if (len < 1e-6) return origen;

            double desplazamiento = Math.Min(tamano, len * 0.85);
            return origen + (vec / len) * desplazamiento;
        }

        // ─────────────────────────────────────────────────────
        //  MTEXT COLGANTE
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Construye el MText de la etiqueta posicionado debajo del
        /// extremo de la línea horizontal.
        ///
        /// Alineación:
        ///   - Si va a la derecha → TopLeft (texto se expande hacia la derecha)
        ///   - Si va a la izquierda → TopRight (texto se expande hacia la izquierda)
        ///
        /// El texto cuelga una distancia MargenTextoVertical por debajo
        /// del extremo de la horizontal.
        /// </summary>
        private MText ConstruirMTextColgante(
            ParamsLeader p,
            Point3d      ptExtremo,
            bool         vaADerecha,
            ObjectId     textStyleId,
            ObjectId     layerTextoId)
        {
            // Punto de inserción: debajo del extremo
            var ptInsercion = new Point3d(
                ptExtremo.X,
                ptExtremo.Y - p.MargenTextoVertical,
                0);

            // Contenido MText
            string contenido;
            if (string.IsNullOrWhiteSpace(p.TextoLinea2))
                contenido = p.TextoLinea1;
            else
                contenido = $"{p.TextoLinea1}\\P{p.TextoLinea2}";  // \P = párrafo MText

            // Alineación adaptativa
            var attachment = vaADerecha
                ? AttachmentPoint.TopLeft
                : AttachmentPoint.TopRight;

            return _mtextEngine.ConstruirMText(
                new ConfigMText
                {
                    Contenido       = contenido,
                    PuntoInsercion  = ptInsercion,
                    NombreLayer     = p.NombreLayerTexto,
                    NombreTextStyle = p.NombreTextStyle,
                    AlturaTexto     = p.AlturaTexto,
                    AnchoCaja       = 0.0,     // sin wrapping automático
                    Attachment      = attachment
                },
                textStyleId,
                layerTextoId);
        }
    }
}
