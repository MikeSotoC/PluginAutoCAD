// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  LayerClassifier.cs
//  Clasificación automática de polígonos por layer institucional
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using CatastroUrbano.Core.Models;

namespace CatastroUrbano.Core.Classification
{
    // ─────────────────────────────────────────────────────────
    //  RESULTADO DE CLASIFICACIÓN
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Resultado completo de la clasificación automática
    /// de un layer institucional.
    /// </summary>
    public sealed class ResultadoClasificacion
    {
        public bool         Reconocido      { get; set; }
        public NivelPiso    Piso            { get; set; }
        public TipoPoligono TipoPoligono    { get; set; }
        public string       LayerCotas      { get; set; } = string.Empty;
        public string       DimStyle        { get; set; } = string.Empty;
        public string       TextStyle       { get; set; } = string.Empty;
        public string       LayerTexto      { get; set; } = "DESCRIPCION";
        public string       LayerOriginal   { get; set; } = string.Empty;

        public override string ToString() =>
            $"Layer={LayerOriginal} | Piso={Piso} | Tipo={TipoPoligono} | " +
            $"DimStyle={DimStyle} | TextStyle={TextStyle}";
    }

    // ─────────────────────────────────────────────────────────
    //  CLASIFICADOR PRINCIPAL
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Motor de clasificación automática para layers del DWT institucional.
    /// Determina: nivel de piso, tipo de polígono, dimstyle y textstyle
    /// correctos sin intervención del operador.
    /// </summary>
    public sealed class LayerClassifier
    {
        // ── Constantes de layers institucionales ─────────────

        private static class Layers
        {
            public const string Manzana         = "TG_MANZANA";
            public const string Lote            = "TG_LOTE";
            public const string EjeVia          = "TG_EJE_VIA";
            public const string Piso01          = "PISO_01";
            public const string Piso02          = "PISO_02";
            public const string Piso03          = "PISO_03";
            public const string AreaLibre       = "AREA_LIBRE";
            public const string NoCategorizable = "NO_CATEGORIZABLE";
            public const string Ducto01         = "DUCTO_01";
            public const string Ducto02         = "DUCTO_02";
            public const string Ingreso         = "INGRESO";
            public const string Formato         = "FORMATO";
            public const string Descripcion     = "DESCRIPCION";
            public const string CotaLote        = "COTA_LOTE";
            public const string CotaManzana     = "COTA_MANZANA";
            public const string CotaFabrica     = "COTA_FABRICA";
            public const string NumCatastral    = "NUM_CATASTRAL";
            public const string NumHU           = "NUM_HU";
            public const string Triangulacion   = "TRIANGULACION_COTA";
        }

        private static class DimStyles
        {
            public const string Standard        = "Standard";
            public const string CotaFabrica     = "COTA_FABRICA";
            public const string CotaLote        = "COTA_LOTE";
            public const string CotaManzana     = "COTA_MANZANA";
        }

        private static class TextStyles
        {
            public const string TxtFabrica      = "TXT_FABRICA";
            public const string TxtLote         = "TXT_LOTE";
            public const string TxtManzana      = "TXT_MANZANA";
            public const string Texto           = "TEXTO";
            public const string Arial           = "ARIAL";
            public const string Standard        = "Standard";
        }

        // ── Tabla de clasificación ────────────────────────────

        private readonly Dictionary<string, ResultadoClasificacion> _tablaClasificacion;

        public LayerClassifier()
        {
            _tablaClasificacion = ConstruirTablaClasificacion();
        }

        // ── API principal ─────────────────────────────────────

        /// <summary>
        /// Clasifica un layer institucional y retorna toda la información
        /// necesaria para generar cotas y textos correctamente.
        /// </summary>
        public ResultadoClasificacion Clasificar(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return CrearNoReconocido(layerName);

            var keyNormalizado = layerName.Trim().ToUpperInvariant();

            if (_tablaClasificacion.TryGetValue(keyNormalizado, out var resultado))
                return resultado;

            // Detección por prefijo numérico para pisos no contemplados
            // (PISO_04, PISO_05, etc.)
            var resultadoPiso = ClasificarPorPatronPiso(keyNormalizado);
            if (resultadoPiso != null) return resultadoPiso;

            return CrearNoReconocido(layerName);
        }

        /// <summary>
        /// Verifica si un layer puede contener polilíneas catastrales
        /// (excluye layers de cotas, textos, formato, etc.).
        /// </summary>
        public bool EsLayerCatastrable(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName)) return false;
            var key = layerName.Trim().ToUpperInvariant();

            // Layers de infraestructura: no catastrables
            var excluidos = new HashSet<string>
            {
                Layers.Formato, Layers.Descripcion,
                Layers.CotaLote, Layers.CotaManzana, Layers.CotaFabrica,
                Layers.NumCatastral, Layers.NumHU, Layers.Triangulacion,
                Layers.EjeVia
            };

            return !excluidos.Contains(key);
        }

        /// <summary>
        /// Retorna el layer de cotas correcto según la clasificación.
        /// </summary>
        public string ObtenerLayerCotas(string layerPoligono)
        {
            var r = Clasificar(layerPoligono);
            return r.LayerCotas;
        }

        /// <summary>
        /// Retorna el dimstyle correcto según la clasificación.
        /// </summary>
        public string ObtenerDimStyle(string layerPoligono)
        {
            var r = Clasificar(layerPoligono);
            return r.DimStyle;
        }

        // ── Construcción de tabla ─────────────────────────────

        private Dictionary<string, ResultadoClasificacion> ConstruirTablaClasificacion()
        {
            return new Dictionary<string, ResultadoClasificacion>(
                StringComparer.OrdinalIgnoreCase)
            {
                // ── Lote ──────────────────────────────────────
                [Layers.Lote] = new ResultadoClasificacion
                {
                    Reconocido   = true,
                    LayerOriginal = Layers.Lote,
                    Piso         = NivelPiso.Lote,
                    TipoPoligono = TipoPoligono.Lote,
                    LayerCotas   = Layers.CotaLote,
                    DimStyle     = DimStyles.CotaLote,
                    TextStyle    = TextStyles.TxtLote
                },

                // ── Manzana ───────────────────────────────────
                [Layers.Manzana] = new ResultadoClasificacion
                {
                    Reconocido   = true,
                    LayerOriginal = Layers.Manzana,
                    Piso         = NivelPiso.Manzana,
                    TipoPoligono = TipoPoligono.Manzana,
                    LayerCotas   = Layers.CotaManzana,
                    DimStyle     = DimStyles.CotaManzana,
                    TextStyle    = TextStyles.TxtManzana
                },

                // ── Pisos de fábrica ──────────────────────────
                [Layers.Piso01] = new ResultadoClasificacion
                {
                    Reconocido   = true,
                    LayerOriginal = Layers.Piso01,
                    Piso         = NivelPiso.PrimerPiso,
                    TipoPoligono = TipoPoligono.FabricaEdificacion,
                    LayerCotas   = Layers.CotaFabrica,
                    DimStyle     = DimStyles.CotaFabrica,
                    TextStyle    = TextStyles.TxtFabrica
                },

                [Layers.Piso02] = new ResultadoClasificacion
                {
                    Reconocido   = true,
                    LayerOriginal = Layers.Piso02,
                    Piso         = NivelPiso.SegundoPiso,
                    TipoPoligono = TipoPoligono.FabricaEdificacion,
                    LayerCotas   = Layers.CotaFabrica,
                    DimStyle     = DimStyles.CotaFabrica,
                    TextStyle    = TextStyles.TxtFabrica
                },

                [Layers.Piso03] = new ResultadoClasificacion
                {
                    Reconocido   = true,
                    LayerOriginal = Layers.Piso03,
                    Piso         = NivelPiso.TercerPiso,
                    TipoPoligono = TipoPoligono.FabricaEdificacion,
                    LayerCotas   = Layers.CotaFabrica,
                    DimStyle     = DimStyles.CotaFabrica,
                    TextStyle    = TextStyles.TxtFabrica
                },

                // ── Área libre ────────────────────────────────
                [Layers.AreaLibre] = new ResultadoClasificacion
                {
                    Reconocido   = true,
                    LayerOriginal = Layers.AreaLibre,
                    Piso         = NivelPiso.AreaLibre,
                    TipoPoligono = TipoPoligono.AreaLibre,
                    LayerCotas   = Layers.CotaFabrica,
                    DimStyle     = DimStyles.CotaFabrica,
                    TextStyle    = TextStyles.TxtFabrica
                },

                // ── No categorizable ──────────────────────────
                [Layers.NoCategorizable] = new ResultadoClasificacion
                {
                    Reconocido   = true,
                    LayerOriginal = Layers.NoCategorizable,
                    Piso         = NivelPiso.NoCategorizable,
                    TipoPoligono = TipoPoligono.NoCategorizable,
                    LayerCotas   = Layers.CotaFabrica,
                    DimStyle     = DimStyles.CotaFabrica,
                    TextStyle    = TextStyles.TxtFabrica
                },

                // ── Ductos ────────────────────────────────────
                [Layers.Ducto01] = new ResultadoClasificacion
                {
                    Reconocido   = true,
                    LayerOriginal = Layers.Ducto01,
                    Piso         = NivelPiso.Ducto,
                    TipoPoligono = TipoPoligono.Ducto,
                    LayerCotas   = Layers.CotaFabrica,
                    DimStyle     = DimStyles.CotaFabrica,
                    TextStyle    = TextStyles.TxtFabrica
                },

                [Layers.Ducto02] = new ResultadoClasificacion
                {
                    Reconocido   = true,
                    LayerOriginal = Layers.Ducto02,
                    Piso         = NivelPiso.Ducto,
                    TipoPoligono = TipoPoligono.Ducto,
                    LayerCotas   = Layers.CotaFabrica,
                    DimStyle     = DimStyles.CotaFabrica,
                    TextStyle    = TextStyles.TxtFabrica
                },

                // ── Ingreso ───────────────────────────────────
                [Layers.Ingreso] = new ResultadoClasificacion
                {
                    Reconocido   = true,
                    LayerOriginal = Layers.Ingreso,
                    Piso         = NivelPiso.PrimerPiso,
                    TipoPoligono = TipoPoligono.Ingreso,
                    LayerCotas   = Layers.CotaFabrica,
                    DimStyle     = DimStyles.CotaFabrica,
                    TextStyle    = TextStyles.TxtFabrica
                }
            };
        }

        // ── Detección por patrón PISO_NN ─────────────────────

        private ResultadoClasificacion? ClasificarPorPatronPiso(string layerNormalizado)
        {
            if (!layerNormalizado.StartsWith("PISO_", StringComparison.Ordinal))
                return null;

            var sufijo = layerNormalizado[5..]; // después de "PISO_"
            if (!int.TryParse(sufijo, out var numPiso) || numPiso < 1 || numPiso > 20)
                return null;

            var nivel = numPiso switch
            {
                1 => NivelPiso.PrimerPiso,
                2 => NivelPiso.SegundoPiso,
                3 => NivelPiso.TercerPiso,
                4 => NivelPiso.CuartoPiso,
                5 => NivelPiso.QuintoPiso,
                _ => NivelPiso.NoDefinido
            };

            return new ResultadoClasificacion
            {
                Reconocido   = true,
                LayerOriginal = layerNormalizado,
                Piso         = nivel,
                TipoPoligono = TipoPoligono.FabricaEdificacion,
                LayerCotas   = "COTA_FABRICA",
                DimStyle     = "COTA_FABRICA",
                TextStyle    = "TXT_FABRICA"
            };
        }

        private static ResultadoClasificacion CrearNoReconocido(string layer) =>
            new()
            {
                Reconocido    = false,
                LayerOriginal = layer,
                Piso          = NivelPiso.NoDefinido,
                TipoPoligono  = TipoPoligono.Indefinido,
                LayerCotas    = "COTA_FABRICA",
                DimStyle      = "COTA_FABRICA",
                TextStyle     = "TXT_FABRICA"
            };
    }
}
