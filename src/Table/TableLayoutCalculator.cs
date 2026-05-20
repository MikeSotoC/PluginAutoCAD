// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  TableLayoutCalculator.cs
//  Cálculo del layout dinámico del cuadro técnico institucional
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using CatastroUrbano.Core.Infrastructure;
using CatastroUrbano.Core.Models;

namespace CatastroUrbano.Core.Table
{
    // ─────────────────────────────────────────────────────────
    //  CALCULADORA DE LAYOUT
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Transforma una lista de FilaCuadro en un LayoutCuadroCalculado
    /// con posiciones Y absolutas para cada fila.
    /// Maneja crecimiento dinámico según número de partidas.
    /// </summary>
    public sealed class TableLayoutCalculator
    {
        private readonly ErrorHandler _log = ErrorHandler.Instancia;

        // ── Cálculo principal ─────────────────────────────────

        /// <summary>
        /// Calcula las posiciones Y de cada fila a partir del origen
        /// (esquina superior izquierda del cuadro).
        /// Las Y son absolutas en el espacio de dibujo CAD (decrecen hacia abajo).
        /// </summary>
        public LayoutCuadroCalculado Calcular(
            List<FilaCuadro>     filas,
            ConfiguracionCuadro  config)
        {
            if (filas == null || !filas.Any())
                throw new ArgumentException("La lista de filas no puede estar vacía.");

            var layout = new LayoutCuadroCalculado
            {
                Configuracion = config,
                Filas         = filas
            };

            double yActual     = config.OrigenY;   // Parte desde arriba
            double alturaTotal = 0.0;

            foreach (var fila in filas)
            {
                layout.PosicionesY.Add(yActual);
                yActual     -= fila.AlturaFila;
                alturaTotal += fila.AlturaFila;
            }

            layout.AlturaTotal = alturaTotal;
            return layout;
        }

        // ── Posición de texto dentro de una fila ─────────────

        /// <summary>
        /// Calcula el punto de inserción del texto izquierdo de una fila.
        /// Centrado verticalmente dentro de la celda.
        /// </summary>
        public (double X, double Y) PuntoTextoIzquierdo(
            LayoutCuadroCalculado layout,
            int indiceFila)
        {
            ValidarIndice(indiceFila, layout.Filas.Count);

            var fila   = layout.Filas[indiceFila];
            var ySupFila = layout.PosicionesY[indiceFila];
            var cfg    = layout.Configuracion;

            double x = cfg.OrigenX + cfg.MargenTextoH;
            double y = ySupFila - (fila.AlturaFila / 2.0);

            return (x, y);
        }

        /// <summary>
        /// Calcula el punto de inserción del texto derecho de una fila.
        /// Centrado verticalmente; horizontalmente en la columna derecha.
        /// </summary>
        public (double X, double Y) PuntoTextoDerecho(
            LayoutCuadroCalculado layout,
            int indiceFila)
        {
            ValidarIndice(indiceFila, layout.Filas.Count);

            var fila    = layout.Filas[indiceFila];
            var ySupFila = layout.PosicionesY[indiceFila];
            var cfg     = layout.Configuracion;

            double xDivisor  = cfg.OrigenX + cfg.AnchoTotal * fila.ProporcionIzquierda;
            double xDerecho  = xDivisor + (cfg.AnchoTotal * (1.0 - fila.ProporcionIzquierda)) / 2.0;
            double y         = ySupFila - (fila.AlturaFila / 2.0);

            return (xDerecho, y);
        }

        /// <summary>
        /// Para filas de título centradas (ProporcionIzquierda = 1.0),
        /// retorna el punto central del ancho total.
        /// </summary>
        public (double X, double Y) PuntoTextoCentrado(
            LayoutCuadroCalculado layout,
            int indiceFila)
        {
            ValidarIndice(indiceFila, layout.Filas.Count);

            var fila    = layout.Filas[indiceFila];
            var ySupFila = layout.PosicionesY[indiceFila];
            var cfg     = layout.Configuracion;

            double x = cfg.OrigenX + cfg.AnchoTotal / 2.0;
            double y = ySupFila - (fila.AlturaFila / 2.0);

            return (x, y);
        }

        // ── X del divisor vertical ────────────────────────────

        /// <summary>
        /// X absoluta de la línea divisora vertical de una fila.
        /// </summary>
        public double XDivisorVertical(
            LayoutCuadroCalculado layout,
            int indiceFila)
        {
            ValidarIndice(indiceFila, layout.Filas.Count);
            var cfg  = layout.Configuracion;
            var fila = layout.Filas[indiceFila];
            return cfg.OrigenX + cfg.AnchoTotal * fila.ProporcionIzquierda;
        }

        // ── Límites horizontales de una fila ──────────────────

        /// <summary>
        /// Retorna el rango Y (superior, inferior) de una fila.
        /// </summary>
        public (double YSup, double YInf) RangoVerticalFila(
            LayoutCuadroCalculado layout,
            int indiceFila)
        {
            ValidarIndice(indiceFila, layout.Filas.Count);
            var ySup = layout.PosicionesY[indiceFila];
            var yInf = ySup - layout.Filas[indiceFila].AlturaFila;
            return (ySup, yInf);
        }

        // ── Ancho de columna derecha ──────────────────────────

        public double AnchoCeldaDerecha(
            LayoutCuadroCalculado layout,
            int indiceFila)
        {
            var fila = layout.Filas[indiceFila];
            return layout.AnchoTotal * (1.0 - fila.ProporcionIzquierda);
        }

        public double AnchoCeldaIzquierda(
            LayoutCuadroCalculado layout,
            int indiceFila)
        {
            var fila = layout.Filas[indiceFila];
            return layout.AnchoTotal * fila.ProporcionIzquierda;
        }

        // ── Diagnóstico ───────────────────────────────────────

        public void ImprimirDiagnostico(LayoutCuadroCalculado layout)
        {
            _log.LogInfo($"Layout cuadro: {layout.Filas.Count} filas | " +
                         $"Ancho={layout.AnchoTotal:F2} | " +
                         $"Alto total={layout.AlturaTotal:F2} | " +
                         $"Origen=({layout.Configuracion.OrigenX:F2}," +
                         $"{layout.Configuracion.OrigenY:F2})");

            for (int i = 0; i < layout.Filas.Count; i++)
            {
                var f = layout.Filas[i];
                _log.LogInfo(
                    $"  [{i:D2}] Y={layout.PosicionesY[i]:F3} " +
                    $"H={f.AlturaFila:F3} " +
                    $"Tipo={f.Tipo,-22} " +
                    $"Izq='{f.TextoIzquierda}' " +
                    $"Der='{f.TextoDerecha}'");
            }
        }

        // ── Privado ───────────────────────────────────────────

        private static void ValidarIndice(int indice, int total)
        {
            if (indice < 0 || indice >= total)
                throw new ArgumentOutOfRangeException(nameof(indice),
                    $"Índice de fila {indice} fuera de rango [0, {total - 1}].");
        }
    }
}
