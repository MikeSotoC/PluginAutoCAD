// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  CatastroEntityModel.cs
//  Modelos de dominio central del sistema
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

namespace CatastroUrbano.Core.Models
{
    // ─────────────────────────────────────────────────────────
    //  ENUMERACIONES DE DOMINIO
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Clasificación de piso detectada automáticamente por layer.
    /// </summary>
    public enum NivelPiso
    {
        NoDefinido      = 0,
        PrimerPiso      = 1,
        SegundoPiso     = 2,
        TercerPiso      = 3,
        CuartoPiso      = 4,
        QuintoPiso      = 5,
        AreaLibre       = 10,
        NoCategorizable = 20,
        Ducto           = 30,
        Lote            = 50,
        Manzana         = 60
    }

    /// <summary>
    /// Tipo funcional del polígono catastral.
    /// </summary>
    public enum TipoPoligono
    {
        Indefinido          = 0,
        FabricaEdificacion  = 1,
        AreaLibre           = 2,
        NoCategorizable     = 3,
        Ducto               = 4,
        Ingreso             = 5,
        Lote                = 6,
        Manzana             = 7
    }

    /// <summary>
    /// Uso declarado del ambiente catastral (texto libre del operador).
    /// Ejemplos institucionales: CC, DEP, COMERCIO, EST, OFICINA, etc.
    /// La categorización estructural (muros/techos) se registra en
    /// CategorizacionEdificacion según la RM 277-2025-VIVIENDA.
    /// </summary>
    public enum UsoAmbiente
    {
        NoDefinido          = 0,
        CasaHabitacion      = 1,
        Departamento        = 2,
        Comercio            = 3,
        Oficina             = 4,
        Deposito            = 5,
        Estacionamiento     = 6,
        Cochera             = 7,
        Servicio            = 8,
        AreaComun           = 9,
        Escalera            = 10,
        Hall                = 11,
        Otro                = 99
    }

    // ─────────────────────────────────────────────────────────
    //  ENTIDAD PRINCIPAL: POLÍGONO CATASTRAL
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Representa una unidad catastral individual detectada
    /// desde una polilínea cerrada en el DWG institucional.
    /// </summary>
    public sealed class PoligonoCatastral
    {
        // ── Identificación ──────────────────────────────────
        public Guid   Id           { get; }
        public string LayerOrigen  { get; set; } = string.Empty;

        // ── Clasificación automática (layer) ─────────────────
        public NivelPiso    Piso           { get; set; } = NivelPiso.NoDefinido;
        public TipoPoligono TipoPoligono   { get; set; } = TipoPoligono.Indefinido;

        // ── Uso declarado (texto libre del operador) ──────────
        // Ejemplos: "CC", "DEP", "COMERCIO", "COCHERA", etc.
        public UsoAmbiente  Uso            { get; set; } = UsoAmbiente.NoDefinido;
        public string       UsoTextoLibre  { get; set; } = string.Empty;
        public string       CodigoUnidad   { get; set; } = string.Empty;  // Ej: "1P CC"

        // ── Categorización normativa RM 277-2025-VIVIENDA ─────
        // Columna 1 (Muros y Columnas) + Columna 2 (Techos) del Anexo I
        public CategorizacionEdificacion Categorizacion { get; set; }
            = new CategorizacionEdificacion();

        // Alias de compatibilidad hacia abajo con código existente
        public string CategoriaTextoLibre
        {
            get => UsoTextoLibre;
            set => UsoTextoLibre = value;
        }

        // ── Geometría ────────────────────────────────────────
        public double        Area           { get; set; }
        public Point3d       CentroGeometrico { get; set; }
        public Point3dCollection Vertices   { get; set; } = new Point3dCollection();
        public BoundingBox3d BoundingBox    { get; set; }

        // ── Estado ───────────────────────────────────────────
        public bool EsPolilineaCerrada  { get; set; }
        public bool AreaCalculada       { get; set; }
        public bool TextoInsertado      { get; set; }
        public bool CotasGeneradas      { get; set; }

        // ── Referencias CAD (ObjectId como long para compatibilidad) ──
        public long ObjectIdPolilinea   { get; set; }
        public long ObjectIdTexto       { get; set; }

        // ── Constructor ──────────────────────────────────────
        public PoligonoCatastral()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Etiqueta de piso para presentación institucional.
        /// </summary>
        public string EtiquetaPiso => Piso switch
        {
            NivelPiso.PrimerPiso      => "1P",
            NivelPiso.SegundoPiso     => "2P",
            NivelPiso.TercerPiso      => "3P",
            NivelPiso.CuartoPiso      => "4P",
            NivelPiso.QuintoPiso      => "5P",
            NivelPiso.AreaLibre       => "AL",
            NivelPiso.NoCategorizable => "NC",
            NivelPiso.Ducto           => "DC",
            _                         => "??"
        };

        /// <summary>
        /// Código completo de unidad para el cuadro técnico.
        /// Formato: "1P CC [C/C]" — piso + uso + categoría muros/techos.
        /// </summary>
        public string CodigoCompleto
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(CodigoUnidad))
                    return CodigoUnidad;

                var base_ = $"{EtiquetaPiso} {UsoTextoLibre}".Trim();

                // Agregar categoría normativa si está definida
                if (Categorizacion.Muros != CategoriaMuros.NoDefinido)
                    return $"{base_} [{Categorizacion.Etiqueta}]";

                return base_;
            }
        }

        /// <summary>
        /// Área formateada para cuadros técnicos peruanos.
        /// </summary>
        public string AreaFormateada => $"{Area:N2} m²";

        public override string ToString() =>
            $"[{EtiquetaPiso}] {UsoTextoLibre} [{Categorizacion.Etiqueta}] " +
            $"| {AreaFormateada} | Layer: {LayerOrigen}";
    }

    // ─────────────────────────────────────────────────────────
    //  AGRUPACIÓN POR EDIFICACIÓN
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Agrupa polígonos catastrales de una edificación completa.
    /// Corresponde a un bloque "EDIFICACIÓN 01" en el cuadro técnico.
    /// </summary>
    public sealed class EdificacionCatastral
    {
        public string Codigo            { get; set; } = "EDIFICACION 01";
        public string Descripcion       { get; set; } = "AREA DE EDIFICA 01";
        public string DescripcionUnidad { get; set; } = "UNIDAD 1";
        public string CategoriaGeneral  { get; set; } = "CASA HABITACION";

        public List<PoligonoCatastral>   Poligonos             { get; } = new();
        public List<ObraComplementaria>  ObrasComplementarias  { get; } = new();

        /// <summary>
        /// Suma total de áreas de fábrica (pisos).
        /// </summary>
        public double AreaTotalFabrica =>
            Poligonos
                .Where(p => p.TipoPoligono == TipoPoligono.FabricaEdificacion)
                .Sum(p => p.Area);

        /// <summary>
        /// Suma de área libre.
        /// </summary>
        public double AreaLibreTotal =>
            Poligonos
                .Where(p => p.TipoPoligono == TipoPoligono.AreaLibre)
                .Sum(p => p.Area);

        /// <summary>
        /// Área total incluyendo obras complementarias.
        /// </summary>
        public double AreaGrandTotal =>
            AreaTotalFabrica
            + AreaLibreTotal
            + ObrasComplementarias.Sum(o => o.Area);

        /// <summary>
        /// Polígonos agrupados por piso para el cuadro técnico.
        /// </summary>
        public IEnumerable<IGrouping<NivelPiso, PoligonoCatastral>> PorPiso =>
            Poligonos
                .Where(p => p.TipoPoligono == TipoPoligono.FabricaEdificacion)
                .GroupBy(p => p.Piso)
                .OrderBy(g => (int)g.Key);
    }

    // ─────────────────────────────────────────────────────────
    //  SESIÓN DE TRABAJO CATASTRAL
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Contenedor de sesión: acumula todas las entidades
    /// procesadas durante una ejecución del comando UC_CATEGORIZAR.
    /// </summary>
    public sealed class SesionCatastral
    {
        private static SesionCatastral? _instancia;
        private static readonly object _lock = new();

        public static SesionCatastral Instancia
        {
            get
            {
                lock (_lock)
                {
                    return _instancia ??= new SesionCatastral();
                }
            }
        }

        private SesionCatastral() { }

        public List<EdificacionCatastral> Edificaciones { get; } = new();
        public List<PoligonoCatastral>    Pendientes    { get; } = new();

        public DateTime FechaInicio     { get; } = DateTime.Now;
        public string   NombreOperador  { get; set; } = string.Empty;
        public string   CodigoPredio    { get; set; } = string.Empty;

        public double AreaTotalGeneral =>
            Edificaciones.Sum(e => e.AreaGrandTotal);

        public void AgregarPoligono(PoligonoCatastral p) =>
            Pendientes.Add(p);

        public void LimpiarSesion()
        {
            Edificaciones.Clear();
            Pendientes.Clear();
        }

        public static void ResetearSesion()
        {
            lock (_lock) { _instancia = null; }
        }
    }
}
