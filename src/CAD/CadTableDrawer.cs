// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  CadTableDrawer.cs
//  Dibujante del cuadro técnico institucional en geometría CAD
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CatastroUrbano.Core.Infrastructure;
using CatastroUrbano.Core.Models;
using CatastroUrbano.Core.Styles;

namespace CatastroUrbano.Core.Table
{
    // ─────────────────────────────────────────────────────────
    //  DIBUJANTE DE CUADRO
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Traduce un LayoutCuadroCalculado a entidades CAD reales:
    /// Line + MText. NO usa AutoCAD Table API.
    /// Todas las entidades se retornan en listas para inserción
    /// batch por CadTransactionManager.
    /// </summary>
    public sealed class CadTableDrawer
    {
        private readonly CAD.CadLineEngine  _lineEngine;
        private readonly CAD.MTextEngine    _mtextEngine;
        private readonly StyleResolver      _styleResolver;
        private readonly TableLayoutCalculator _layoutCalc;
        private readonly ErrorHandler       _log = ErrorHandler.Instancia;

        public CadTableDrawer(
            CAD.CadLineEngine       lineEngine,
            CAD.MTextEngine         mtextEngine,
            StyleResolver           styleResolver,
            TableLayoutCalculator   layoutCalc)
        {
            _lineEngine    = lineEngine;
            _mtextEngine   = mtextEngine;
            _styleResolver = styleResolver;
            _layoutCalc    = layoutCalc;
        }

        // ─────────────────────────────────────────────────────
        //  DIBUJO COMPLETO DEL CUADRO
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Genera todas las entidades CAD del cuadro técnico:
        /// - Borde exterior
        /// - Líneas horizontales entre filas
        /// - Líneas verticales divisoras
        /// - MText por celda
        ///
        /// Retorna listas separadas de Line y MText para inserción.
        /// </summary>
        public ResultadoDibujo DibujarCuadro(
            LayoutCuadroCalculado layout,
            Database db)
        {
            var resultado = new ResultadoDibujo();

            var layerFormatoId  = _styleResolver.ResolverLayer("FORMATO",      db);
            var layerTextoId    = _styleResolver.ResolverLayer("DESCRIPCION",  db);

            // ── 1. Borde exterior del cuadro ──────────────────
            var bordes = DibujarBordeExterior(layout, layerFormatoId);
            resultado.Lineas.AddRange(bordes);

            // ── 2. Filas internas ─────────────────────────────
            for (int i = 0; i < layout.Filas.Count; i++)
            {
                var fila = layout.Filas[i];
                var ySup = layout.PosicionesY[i];
                var yInf = ySup - fila.AlturaFila;

                // ── 2a. Línea horizontal inferior de la fila ──
                if (fila.DibujarBordeInferior && i < layout.Filas.Count - 1)
                {
                    var linHoriz = _lineEngine.ConstruirLineaHorizontal(
                        layout.XIzquierda, layout.XDerecha, yInf,
                        layerFormatoId);
                    resultado.Lineas.Add(linHoriz);
                }

                // ── 2b. Línea divisora vertical (columnas) ────
                if (fila.DividirColumnas &&
                    fila.Tipo != TipoFila.TituloPrincipal &&
                    fila.Tipo != TipoFila.Separador)
                {
                    double xDiv = _layoutCalc.XDivisorVertical(layout, i);
                    var linVert = _lineEngine.ConstruirLineaVertical(
                        xDiv, ySup, yInf, layerFormatoId);
                    resultado.Lineas.Add(linVert);
                }

                // ── 2c. Textos ────────────────────────────────
                if (fila.Tipo != TipoFila.Separador)
                {
                    DibujarTextosFila(layout, i, fila, db,
                        layerTextoId, resultado);
                }
            }

            _log.LogInfo(
                $"Cuadro dibujado: {resultado.Lineas.Count} líneas, " +
                $"{resultado.MTexts.Count} textos.");

            return resultado;
        }

        // ─────────────────────────────────────────────────────
        //  BORDE EXTERIOR
        // ─────────────────────────────────────────────────────

        private List<Line> DibujarBordeExterior(
            LayoutCuadroCalculado layout,
            ObjectId layerId)
        {
            return _lineEngine.ConstruirBordeCuadro(
                layout.XIzquierda,
                layout.XDerecha,
                layout.YSuperior,
                layout.YInferior,
                layerId);
        }

        // ─────────────────────────────────────────────────────
        //  TEXTOS POR FILA
        // ─────────────────────────────────────────────────────

        private void DibujarTextosFila(
            LayoutCuadroCalculado layout,
            int       indiceFila,
            FilaCuadro fila,
            Database   db,
            ObjectId   layerTextoId,
            ResultadoDibujo resultado)
        {
            var cfg = layout.Configuracion;

            // ── Columna izquierda ─────────────────────────────
            if (!string.IsNullOrWhiteSpace(fila.TextoIzquierda))
            {
                string textoIzq = fila.TextoIzquierdaMayusculas
                    ? fila.TextoIzquierda.ToUpperInvariant()
                    : fila.TextoIzquierda;

                AttachmentPoint attachment;
                (double x, double y) ptIzq;

                if (fila.CentrarAmbas)
                {
                    attachment = AttachmentPoint.MiddleCenter;
                    ptIzq = _layoutCalc.PuntoTextoCentrado(layout, indiceFila);
                }
                else
                {
                    attachment = AttachmentPoint.MiddleLeft;
                    ptIzq = _layoutCalc.PuntoTextoIzquierdo(layout, indiceFila);
                }

                double anchoCeldaIzq = fila.CentrarAmbas
                    ? cfg.AnchoTotal
                    : _layoutCalc.AnchoCeldaIzquierda(layout, indiceFila)
                      - cfg.MargenTextoH * 2;

                var mtIzq = _mtextEngine.ConstruirTextoCuadro(
                    textoIzq,
                    new Point3d(ptIzq.x, ptIzq.y, 0),
                    anchoCeldaIzq,
                    attachment,
                    fila.AlturaTexto,
                    fila.TextStyleIzquierda,
                    db);

                if (layerTextoId != ObjectId.Null)
                    mtIzq.LayerId = layerTextoId;

                resultado.MTexts.Add(mtIzq);
            }

            // ── Columna derecha (solo si hay divisor) ─────────
            if (fila.DividirColumnas &&
                !fila.CentrarAmbas   &&
                !string.IsNullOrWhiteSpace(fila.TextoDerecha))
            {
                string textoDer = fila.TextoDerechaMayusculas
                    ? fila.TextoDerecha.ToUpperInvariant()
                    : fila.TextoDerecha;

                var ptDer = _layoutCalc.PuntoTextoDerecho(layout, indiceFila);

                double anchoCeldaDer =
                    _layoutCalc.AnchoCeldaDerecha(layout, indiceFila)
                    - cfg.MargenTextoH * 2;

                var mtDer = _mtextEngine.ConstruirTextoCuadro(
                    textoDer,
                    new Point3d(ptDer.X, ptDer.Y, 0),
                    anchoCeldaDer,
                    AttachmentPoint.MiddleCenter,
                    fila.AlturaTexto,
                    fila.TextStyleDerecha,
                    db);

                if (layerTextoId != ObjectId.Null)
                    mtDer.LayerId = layerTextoId;

                resultado.MTexts.Add(mtDer);
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    //  RESULTADO DEL DIBUJO
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Contiene todas las entidades CAD generadas por CadTableDrawer,
    /// listas para inserción batch en la transacción.
    /// </summary>
    public sealed class ResultadoDibujo
    {
        public List<Line>  Lineas  { get; } = new();
        public List<MText> MTexts  { get; } = new();

        public int TotalEntidades => Lineas.Count + MTexts.Count;

        /// <summary>
        /// Itera todas las entidades en orden: primero líneas, luego textos.
        /// Conveniente para la inserción batch.
        /// </summary>
        public IEnumerable<Entity> TodasLasEntidades()
        {
            foreach (var l in Lineas)  yield return l;
            foreach (var m in MTexts)  yield return m;
        }
    }
}
