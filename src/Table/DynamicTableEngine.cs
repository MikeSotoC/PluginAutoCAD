// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  DynamicTableEngine.cs
//  Motor de construcción dinámica del cuadro técnico institucional
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using CatastroUrbano.Core.Analysis;
using CatastroUrbano.Core.Infrastructure;
using CatastroUrbano.Core.Models;

namespace CatastroUrbano.Core.Table
{
    // ─────────────────────────────────────────────────────────
    //  MOTOR DINÁMICO DEL CUADRO
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Construye la lista de FilaCuadro para una EdificacionCatastral.
    /// Calcula dinámicamente: filas por piso, obras complementarias,
    /// totales parciales y total general.
    /// </summary>
    public sealed class DynamicTableEngine
    {
        private readonly AreaCalculator _calculator;
        private readonly ErrorHandler   _log = ErrorHandler.Instancia;

        public DynamicTableEngine(AreaCalculator calculator)
        {
            _calculator = calculator
                ?? throw new ArgumentNullException(nameof(calculator));
        }

        // ── Construcción del cuadro por edificación ────────────

        /// <summary>
        /// Genera la lista completa de filas para el cuadro técnico
        /// de una EdificacionCatastral, respetando el formato institucional.
        /// </summary>
        public List<FilaCuadro> ConstruirFilas(EdificacionCatastral edificacion)
        {
            if (edificacion == null)
                throw new ArgumentNullException(nameof(edificacion));

            var filas = new List<FilaCuadro>();

            // ── Fila 1: Título principal ──────────────────────
            filas.Add(FilaCuadro.CrearTitulo(edificacion.Codigo));

            // ── Fila 2: Subtítulo área ────────────────────────
            filas.Add(FilaCuadro.CrearSubtituloArea(edificacion.Descripcion));

            // ── Fila 3: Subtítulo unidad + categoría ──────────
            filas.Add(FilaCuadro.CrearSubtituloUnidad(
                edificacion.DescripcionUnidad,
                edificacion.CategoriaGeneral));

            // ── Filas de fábrica por piso ─────────────────────
            var pisosOrdenados = edificacion.PorPiso.ToList();

            foreach (var grupoPiso in pisosOrdenados)
            {
                var poligonosOrdenados = grupoPiso
                    .OrderBy(p => p.CodigoCompleto)
                    .ToList();

                foreach (var poligono in poligonosOrdenados)
                {
                    filas.Add(FilaCuadro.CrearPartida(
                        poligono.CodigoCompleto,
                        poligono.Area));
                }
            }

            // ── Filas de área libre (si existe) ──────────────
            var areasLibres = edificacion.Poligonos
                .Where(p => p.TipoPoligono == TipoPoligono.AreaLibre)
                .OrderBy(p => p.CodigoCompleto)
                .ToList();

            foreach (var al in areasLibres)
            {
                filas.Add(FilaCuadro.CrearPartida(
                    $"AL {al.CategoriaTextoLibre}".Trim(),
                    al.Area));
            }

            // ── Filas de no categorizable (si existe) ─────────
            var noCateg = edificacion.Poligonos
                .Where(p => p.TipoPoligono == TipoPoligono.NoCategorizable)
                .OrderBy(p => p.CodigoCompleto)
                .ToList();

            foreach (var nc in noCateg)
            {
                filas.Add(FilaCuadro.CrearPartida(
                    $"NC {nc.CategoriaTextoLibre}".Trim(),
                    nc.Area));
            }

            // ── Filas de ductos (si existe) ───────────────────
            var ductos = edificacion.Poligonos
                .Where(p => p.TipoPoligono == TipoPoligono.Ducto)
                .OrderBy(p => p.CodigoCompleto)
                .ToList();

            foreach (var dc in ductos)
            {
                filas.Add(FilaCuadro.CrearPartida(
                    $"DC {dc.CategoriaTextoLibre}".Trim(),
                    dc.Area));
            }

            // ── Filas de obras complementarias (si existen) ───
            if (edificacion.ObrasComplementarias.Any())
            {
                filas.Add(FilaCuadro.CrearSeparador());

                foreach (var oc in edificacion.ObrasComplementarias)
                {
                    filas.Add(FilaCuadro.CrearObraComplementaria(
                        oc.TextoCuadro,
                        oc.EsMedidaLineal ? 0 : oc.Area));
                }
            }

            // ── Fila de total general ─────────────────────────
            var resumen = _calculator.CalcularPorEdificacion(edificacion);
            filas.Add(FilaCuadro.CrearTotal(resumen.AreaTotalPredioM2));

            _log.LogInfo(
                $"Cuadro dinámico construido: {filas.Count} filas | " +
                $"Edificación '{edificacion.Codigo}' | " +
                $"Total: {resumen.AreaTotalPredioM2:N2} m²");

            return filas;
        }

        // ── Cuadro de resumen de sesión ───────────────────────

        /// <summary>
        /// Genera un cuadro de resumen global para la sesión catastral.
        /// Lista todas las edificaciones con sus totales.
        /// </summary>
        public List<FilaCuadro> ConstruirResumenSesion(SesionCatastral sesion)
        {
            var filas = new List<FilaCuadro>();

            filas.Add(FilaCuadro.CrearTitulo("CUADRO DE ÁREAS"));
            filas.Add(FilaCuadro.CrearSubtituloArea("RESUMEN PREDIAL"));

            foreach (var edificacion in sesion.Edificaciones)
            {
                var resumen = _calculator.CalcularPorEdificacion(edificacion);

                filas.Add(new FilaCuadro
                {
                    TextoIzquierda          = edificacion.Codigo,
                    TextoDerecha            = resumen.TotalFormateado,
                    Tipo                    = TipoFila.SubtituloUnidad,
                    AlturaFila              = 0.60,
                    ProporcionIzquierda     = 0.60,
                    TextoIzquierdaMayusculas = true
                });
            }

            double totalGeneral = sesion.Edificaciones
                .Sum(e => e.AreaGrandTotal);

            filas.Add(FilaCuadro.CrearTotal(totalGeneral));
            return filas;
        }

        // ── Recálculo de totales (para cuadros ya existentes) ──

        /// <summary>
        /// Recalcula la fila de total de un cuadro existente
        /// actualizando solo la última fila de tipo TotalGeneral.
        /// </summary>
        public void ActualizarFilaTotal(
            List<FilaCuadro>    filas,
            EdificacionCatastral edificacion)
        {
            var filaTotal = filas.LastOrDefault(
                f => f.Tipo == TipoFila.TotalGeneral);

            if (filaTotal == null) return;

            var resumen = _calculator.CalcularPorEdificacion(edificacion);
            filaTotal.TextoDerecha = $"{resumen.AreaTotalPredioM2:N2} m²";

            _log.LogInfo(
                $"Total del cuadro actualizado: {filaTotal.TextoDerecha}");
        }

        // ── Validación de coherencia del cuadro ──────────────

        /// <summary>
        /// Verifica que el cuadro tenga al menos las filas mínimas
        /// institucionales: título + subtítulo + 1 partida + total.
        /// </summary>
        public bool ValidarEstructura(List<FilaCuadro> filas)
        {
            bool tieneTitulo   = filas.Any(f => f.Tipo == TipoFila.TituloPrincipal);
            bool tienePartida  = filas.Any(f => f.Tipo == TipoFila.DatoPartida);
            bool tieneTotal    = filas.Any(f => f.Tipo == TipoFila.TotalGeneral);

            if (!tieneTitulo || !tienePartida || !tieneTotal)
            {
                _log.LogAdvertencia(
                    "Cuadro técnico con estructura incompleta: " +
                    $"Título={tieneTitulo}, Partida={tienePartida}, Total={tieneTotal}");
                return false;
            }

            return true;
        }
    }
}
