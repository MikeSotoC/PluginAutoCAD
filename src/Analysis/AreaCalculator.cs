// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  AreaCalculator.cs
//  Motor de cálculo y validación de áreas catastrales
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using CatastroUrbano.Core.Models;
using CatastroUrbano.Core.Infrastructure;

namespace CatastroUrbano.Core.Analysis
{
    // ─────────────────────────────────────────────────────────
    //  RESUMEN DE ÁREAS POR EDIFICACIÓN
    // ─────────────────────────────────────────────────────────

    public sealed class ResumenAreas
    {
        public double AreaFabricaTotalM2        { get; set; }
        public double AreaLibreTotalM2          { get; set; }
        public double AreaObrasComplementariasM2{ get; set; }
        public double AreaNoCategorizable       { get; set; }
        public double AreaDuctos                { get; set; }
        public double AreaTotalPredioM2         { get; set; }

        public Dictionary<NivelPiso, double> AreasPorPiso { get; } = new();

        // ── Representaciones formateadas ──────────────────────
        public string FabricaFormateada  => $"{AreaFabricaTotalM2:N2} m²";
        public string LibreFormateada    => $"{AreaLibreTotalM2:N2} m²";
        public string TotalFormateado    => $"{AreaTotalPredioM2:N2} m²";

        public string AreaPisoFormateada(NivelPiso piso) =>
            AreasPorPiso.TryGetValue(piso, out var a) ? $"{a:N2} m²" : "0.00 m²";
    }

    // ─────────────────────────────────────────────────────────
    //  CALCULADORA DE ÁREAS
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Agrega, valida y formatea áreas catastrales de acuerdo
    /// a las reglas del catastro urbano peruano.
    /// </summary>
    public sealed class AreaCalculator
    {
        private readonly ErrorHandler _log = ErrorHandler.Instancia;

        // ── Tolerancias de cuadre ─────────────────────────────
        // Diferencia máxima aceptable entre área de lote y suma de interiores
        private const double ToleranciaDesfase = 0.10; // 10 cm²

        // ── Cálculo por edificación ────────────────────────────

        /// <summary>
        /// Calcula el resumen de áreas completo de una edificación.
        /// Incluye desglose por piso y totales.
        /// </summary>
        public ResumenAreas CalcularPorEdificacion(EdificacionCatastral edificacion)
        {
            var resumen = new ResumenAreas();

            // ── Áreas de fábrica por piso ─────────────────────
            foreach (var grupo in edificacion.PorPiso)
            {
                double areaPiso = grupo.Sum(p => p.Area);
                resumen.AreasPorPiso[grupo.Key] = areaPiso;
                resumen.AreaFabricaTotalM2 += areaPiso;
            }

            // ── Área libre ────────────────────────────────────
            resumen.AreaLibreTotalM2 = edificacion.Poligonos
                .Where(p => p.TipoPoligono == TipoPoligono.AreaLibre)
                .Sum(p => p.Area);

            // ── No categorizable ──────────────────────────────
            resumen.AreaNoCategorizable = edificacion.Poligonos
                .Where(p => p.TipoPoligono == TipoPoligono.NoCategorizable)
                .Sum(p => p.Area);

            // ── Ductos ────────────────────────────────────────
            resumen.AreaDuctos = edificacion.Poligonos
                .Where(p => p.TipoPoligono == TipoPoligono.Ducto)
                .Sum(p => p.Area);

            // ── Obras complementarias ─────────────────────────
            resumen.AreaObrasComplementariasM2 = edificacion.ObrasComplementarias
                .Where(o => !o.EsMedidaLineal)
                .Sum(o => o.Area);

            // ── Total predio ──────────────────────────────────
            resumen.AreaTotalPredioM2 =
                resumen.AreaFabricaTotalM2 +
                resumen.AreaLibreTotalM2   +
                resumen.AreaObrasComplementariasM2;

            return resumen;
        }

        // ── Cálculo de total de sesión ────────────────────────

        /// <summary>
        /// Calcula el área total de todas las edificaciones en sesión.
        /// </summary>
        public double CalcularTotalSesion(SesionCatastral sesion) =>
            sesion.Edificaciones.Sum(e => e.AreaGrandTotal);

        // ── Validación de cuadre ──────────────────────────────

        /// <summary>
        /// Compara el área del polígono de lote con la suma de los
        /// polígonos interiores. Reporta discrepancia si supera tolerancia.
        /// </summary>
        public ResultadoValidacionArea ValidarCuadre(
            double areaLote,
            double sumaInteriores)
        {
            double diferencia = Math.Abs(areaLote - sumaInteriores);
            bool cuadra       = diferencia <= ToleranciaDesfase;

            return new ResultadoValidacionArea
            {
                AreaLote          = areaLote,
                SumaInteriores    = sumaInteriores,
                Diferencia        = diferencia,
                Cuadra            = cuadra,
                PorcentajeError   = areaLote > 0
                                    ? (diferencia / areaLote) * 100.0
                                    : 0.0
            };
        }

        // ── Acumulación incremental ───────────────────────────

        /// <summary>
        /// Actualiza el área de una edificación al agregar una
        /// nueva entidad catastral. Útil en flujo interactivo.
        /// </summary>
        public void ActualizarAreaEdificacion(
            EdificacionCatastral edificacion,
            PoligonoCatastral nuevaEntidad)
        {
            // La edificación calcula dinámicamente desde sus polígonos.
            // Solo necesitamos confirmar que el área está calculada.
            if (!nuevaEntidad.AreaCalculada || nuevaEntidad.Area <= 0)
            {
                _log.LogAdvertencia(
                    $"Entidad '{nuevaEntidad.CodigoCompleto}' sin área calculada. " +
                    "No se agregará al total.");
                return;
            }

            edificacion.Poligonos.Add(nuevaEntidad);

            _log.LogInfo(
                $"Entidad '{nuevaEntidad.CodigoCompleto}' " +
                $"({nuevaEntidad.AreaFormateada}) agregada a '{edificacion.Codigo}'. " +
                $"Total actual: {edificacion.AreaGrandTotal:N2} m²");
        }

        // ── Generación de etiquetas para cuadro ──────────────

        /// <summary>
        /// Genera las filas de área desglosadas por piso
        /// para inserción directa en el cuadro técnico.
        /// </summary>
        public IReadOnlyList<(string Codigo, double Area)>
            GenerarFilasDesglosePiso(EdificacionCatastral edificacion)
        {
            var filas = new List<(string, double)>();

            foreach (var grupo in edificacion.PorPiso)
            {
                foreach (var poligono in grupo.OrderBy(p => p.CodigoCompleto))
                {
                    filas.Add((poligono.CodigoCompleto, poligono.Area));
                }
            }

            return filas;
        }
    }

    // ─────────────────────────────────────────────────────────
    //  RESULTADO DE VALIDACIÓN DE CUADRE
    // ─────────────────────────────────────────────────────────

    public sealed class ResultadoValidacionArea
    {
        public double AreaLote          { get; set; }
        public double SumaInteriores    { get; set; }
        public double Diferencia        { get; set; }
        public bool   Cuadra            { get; set; }
        public double PorcentajeError   { get; set; }

        public string MensajeValidacion =>
            Cuadra
                ? $"✓ Cuadre correcto. Diferencia: {Diferencia:F4} m²"
                : $"⚠ Descuadre detectado: {Diferencia:F4} m² " +
                  $"({PorcentajeError:F2}%). Verificar geometría.";

        public override string ToString() =>
            $"Lote={AreaLote:N2}m² | Interior={SumaInteriores:N2}m² | " +
            $"Δ={Diferencia:F4}m² | {(Cuadra ? "OK" : "DESCUADRE")}";
    }
}
