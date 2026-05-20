// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  CategoriaEdificacion.cs
//
//  Categorías de edificación según:
//
//  Resolución Ministerial N.° 277-2025-VIVIENDA
//  Anexo I — Cuadro de Valores Unitarios Oficiales de Edificación
//  Ejercicio Fiscal 2026
//
//  El cuadro define TRES columnas de acabados:
//    (1) Muros y Columnas   → Categorías A – I
//    (2) Techos             → Categorías A – H  (I = sin techo)
//    (3) Puertas y Ventanas → Categorías A – I  (solo referencial)
//
//  Este módulo gestiona ÚNICAMENTE las categorías de
//  MUROS Y COLUMNAS y TECHOS para fines catastrales.
//  NO almacena ni procesa valores monetarios.
//
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System.Collections.Generic;

namespace CatastroUrbano.Core.Models
{
    // ─────────────────────────────────────────────────────────
    //  CATEGORÍAS DE MUROS Y COLUMNAS (Columna 1, Anexo I)
    //  RM 277-2025-VIVIENDA
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Categoría de la partida MUROS Y COLUMNAS según el Anexo I
    /// de la RM 277-2025-VIVIENDA. Aplica a Lima Metropolitana,
    /// Costa, Sierra y Selva (descripción de Lima como referencia).
    /// </summary>
    public enum CategoriaMuros
    {
        /// <summary>
        /// A — Estructuras laminares curvadas de concreto armado que incluyen
        /// en una sola armadura la cimentación y el techo.
        /// (Para este caso no se considera la columna N.° 2 — Techos)
        /// </summary>
        A = 1,

        /// <summary>
        /// B — Columnas, vigas y/o placas de concreto armado y/o metálicas.
        /// </summary>
        B = 2,

        /// <summary>
        /// C — Placas de concreto e = 10 a 15 cm. Albañilería armada, ladrillo
        /// o similar con columnas y vigas de amarre de concreto armado.
        /// </summary>
        C = 3,

        /// <summary>
        /// D — Ladrillo o similar sin elementos de concreto armado.
        /// Drywall o similar (incluye techo).
        /// (Para este caso no se considera la columna N.° 2 — Techos)
        /// </summary>
        D = 4,

        /// <summary>
        /// E — Adobe, tapial o quincha.
        /// </summary>
        E = 5,

        /// <summary>
        /// F — Madera (estoraque, pumaquiro, huayruro, machinga, catahua amarilla,
        /// copaiba, diablo fuerte, tornillo o similares). Drywall o similar.
        /// (Sin techo propio)
        /// </summary>
        F = 6,

        /// <summary>
        /// G — Pircado con mezcla de barro.
        /// </summary>
        G = 7,

        /// <summary>
        /// H — Madera rústica.
        /// </summary>
        H = 8,

        /// <summary>
        /// I — Sin muros / No aplica.
        /// </summary>
        I = 9,

        /// <summary>
        /// No definido (valor por defecto).
        /// </summary>
        NoDefinido = 0
    }

    // ─────────────────────────────────────────────────────────
    //  CATEGORÍAS DE TECHOS (Columna 2, Anexo I)
    //  RM 277-2025-VIVIENDA
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Categoría de la partida TECHOS según el Anexo I
    /// de la RM 277-2025-VIVIENDA.
    /// </summary>
    public enum CategoriaTechos
    {
        /// <summary>
        /// A — Losa o aligerado de concreto armado con luces libres mayores
        /// a 6 m. (medida entre cara de los apoyos) y sobrecarga mayor
        /// a 300 kg/m². Debe cumplir las dos condiciones.
        /// </summary>
        A = 1,

        /// <summary>
        /// B — Aligerados o losas de concreto armado inclinadas.
        /// </summary>
        B = 2,

        /// <summary>
        /// C — Aligerado o losas de concreto armado horizontales.
        /// </summary>
        C = 3,

        /// <summary>
        /// D — Calamina metálica / fibrocemento sobre viguería metálica.
        /// </summary>
        D = 4,

        /// <summary>
        /// E — Madera con material impermeabilizante.
        /// </summary>
        E = 5,

        /// <summary>
        /// F — Calamina metálica / fibrocemento o teja sobre viguería de
        /// madera corriente.
        /// </summary>
        F = 6,

        /// <summary>
        /// G — Madera rústica o caña con torta de barro.
        /// </summary>
        G = 7,

        /// <summary>
        /// H — Sin techo / No aplica (valor cero en el cuadro).
        /// </summary>
        H = 8,

        /// <summary>
        /// No definido (valor por defecto).
        /// </summary>
        NoDefinido = 0
    }

    // ─────────────────────────────────────────────────────────
    //  DESCRIPTOR ESTÁTICO DE CATEGORÍAS
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Proveedor de descripciones textuales de las categorías del Anexo I.
    /// Permite generar etiquetas para rotulación catastral y cuadros técnicos.
    /// </summary>
    public static class DescriptorCategorias
    {
        // ── Muros y Columnas ──────────────────────────────────

        private static readonly Dictionary<CategoriaMuros, string> _descripcionesMuros
            = new()
            {
                [CategoriaMuros.A] =
                    "Estr. laminar curvada de CA (cimentación y techo en una sola armadura)",
                [CategoriaMuros.B] =
                    "Columnas, vigas y/o placas de CA y/o metálicas",
                [CategoriaMuros.C] =
                    "Placas de CA e=10-15 cm / albañilería armada con columnas y vigas de CA",
                [CategoriaMuros.D] =
                    "Ladrillo o similar sin CA / Drywall o similar (incluye techo)",
                [CategoriaMuros.E] =
                    "Adobe, tapial o quincha",
                [CategoriaMuros.F] =
                    "Madera (estoraque, pumaquiro, huayruro, machinga u otros) / Drywall (sin techo)",
                [CategoriaMuros.G] =
                    "Pircado con mezcla de barro",
                [CategoriaMuros.H] =
                    "Madera rústica",
                [CategoriaMuros.I] =
                    "Sin muros / No aplica"
            };

        // ── Techos ────────────────────────────────────────────

        private static readonly Dictionary<CategoriaTechos, string> _descripcionesTechos
            = new()
            {
                [CategoriaTechos.A] =
                    "Losa/aligerado de CA, luces > 6 m. y sobrecarga > 300 kg/m²",
                [CategoriaTechos.B] =
                    "Aligerado o losas de CA inclinadas",
                [CategoriaTechos.C] =
                    "Aligerado o losas de CA horizontales",
                [CategoriaTechos.D] =
                    "Calamina metálica/fibrocemento sobre viguería metálica",
                [CategoriaTechos.E] =
                    "Madera con material impermeabilizante",
                [CategoriaTechos.F] =
                    "Calamina metálica/fibrocemento/teja sobre viguería de madera corriente",
                [CategoriaTechos.G] =
                    "Madera rústica o caña con torta de barro",
                [CategoriaTechos.H] =
                    "Sin techo"
            };

        // ── API pública ───────────────────────────────────────

        public static string ObtenerDescripcionMuros(CategoriaMuros cat) =>
            _descripcionesMuros.TryGetValue(cat, out var d) ? d : "No definido";

        public static string ObtenerDescripcionTechos(CategoriaTechos cat) =>
            _descripcionesTechos.TryGetValue(cat, out var d) ? d : "No definido";

        /// <summary>
        /// Etiqueta corta para cuadros técnicos y rotulación CAD.
        /// Ejemplo: "C/C" para Muros C + Techos C.
        /// </summary>
        public static string EtiquetaCombinada(
            CategoriaMuros muros, CategoriaTechos techos) =>
            $"{muros}/{techos}";

        public static IReadOnlyList<(CategoriaMuros Cat, string Descripcion)>
            TodasCategoriasMuros()
        {
            var lista = new List<(CategoriaMuros, string)>();
            foreach (var kv in _descripcionesMuros)
                lista.Add((kv.Key, kv.Value));
            return lista;
        }

        public static IReadOnlyList<(CategoriaTechos Cat, string Descripcion)>
            TodasCategoriasTechos()
        {
            var lista = new List<(CategoriaTechos, string)>();
            foreach (var kv in _descripcionesTechos)
                lista.Add((kv.Key, kv.Value));
            return lista;
        }
    }

    // ─────────────────────────────────────────────────────────
    //  CATEGORIZACIÓN DE UN POLÍGONO
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Registro de categorización de un polígono catastral según
    /// el Anexo I de la RM 277-2025-VIVIENDA.
    /// Solo muros y techos; sin valores monetarios.
    /// </summary>
    public sealed class CategorizacionEdificacion
    {
        public CategoriaMuros   Muros   { get; set; } = CategoriaMuros.NoDefinido;
        public CategoriaTechos  Techos  { get; set; } = CategoriaTechos.NoDefinido;

        /// <summary>
        /// Etiqueta combinada para el cuadro técnico: "C/C", "B/C", etc.
        /// </summary>
        public string Etiqueta =>
            Muros  == CategoriaMuros.NoDefinido  ? "SIN CAT." :
            Techos == CategoriaTechos.NoDefinido ? $"{Muros}/-" :
            DescriptorCategorias.EtiquetaCombinada(Muros, Techos);

        /// <summary>
        /// Indica si la categoría de muros implica que no se usa la
        /// columna de Techos (casos A y D del Anexo I).
        /// </summary>
        public bool TechosNoAplica =>
            Muros == CategoriaMuros.A || Muros == CategoriaMuros.D;

        public override string ToString() =>
            $"Muros: {Muros} — Techos: {(TechosNoAplica ? "N/A" : Techos.ToString())}";
    }
}
