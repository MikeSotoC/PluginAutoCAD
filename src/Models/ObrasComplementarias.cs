// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  ObrasComplementarias.cs
//
//  Catálogo de obras complementarias e instalaciones fijas
//  y permanentes según:
//
//  Resolución Ministerial N.° 277-2025-VIVIENDA
//  Anexo III — Valores Unitarios a costo directo de algunas
//  Obras Complementarias e Instalaciones Fijas y Permanentes
//  Ejercicio Fiscal 2026
//
//  IMPORTANTE: Este módulo gestiona ÚNICAMENTE la identificación
//  y clasificación de las obras. NO almacena ni procesa valores
//  monetarios. Los ítems son referenciales al número oficial
//  del Anexo III para correlación normativa.
//
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace CatastroUrbano.Core.Models
{
    // ─────────────────────────────────────────────────────────
    //  GRUPOS DE OBRAS (ANEXO III — RM 277-2025-VIVIENDA)
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Grupo funcional de la obra complementaria según el Anexo III
    /// de la RM 277-2025-VIVIENDA. Permite agrupar ítems en el cuadro.
    /// </summary>
    public enum GrupoObraComplementaria
    {
        MurosPerimetricosYCercos            = 1,
        PortonesYPuertas                    = 2,
        TanquesElevados                     = 3,
        CisternasYPozosSumideros            = 4,
        PiscinasYEspejosDe Agua             = 5,
        LosasDeportivasYEstacionamientos    = 6,
        HornosChimeneasIncineradores        = 7,
        TorresDeVigilancia                  = 8,
        Bovedas                             = 9,
        BalanzasIndustriales                = 10,
        PostesDeAlumbrado                   = 11,
        BasesDeSoporteDeMaquinas            = 12,
        CajasDeRegistroDeConcreto           = 13,
        BuzonesDeConcreto                   = 14,
        Parapetos                           = 15,
        RampasDradasYEscaleras              = 16,
        MurosDeContencion                   = 17,
        EscalerasMetalicas                  = 18,
        Pastorales                          = 19,
        ProyectoresLuminaria                = 20,
        TuberiasDeConcreto                  = 21,
        CanalesYZanjas                      = 22,
        PostesDeConcreto                    = 23,
        Cubiertas                           = 24,
        PasamanoMetalico                    = 25,
        CercosMetalicos                     = 26,
        ColumnasYEstructurasDeFierro        = 27,
        Sardineles                          = 28,
        PistasYPavimentos                   = 29,
        TrampasDGrasa                       = 30,
        Otro                                = 99
    }

    // ─────────────────────────────────────────────────────────
    //  ÍTEM INDIVIDUAL DEL CATÁLOGO
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Ítem del Anexo III de la RM 277-2025-VIVIENDA.
    /// Contiene el número oficial, descripción y unidad de medida.
    /// NO contiene valor monetario.
    /// </summary>
    public sealed record ItemObraComplementaria(
        int    NumeroItem,
        GrupoObraComplementaria Grupo,
        string DescripcionGrupo,
        string DescripcionComponente,
        string UnidadMedida)
    {
        /// <summary>
        /// Etiqueta corta para el cuadro técnico CAD.
        /// Formato: "Ítem 01 — Descripción abreviada"
        /// </summary>
        public string EtiquetaCuadro =>
            $"Ítem {NumeroItem:D2} — {DescripcionAbreviada}";

        /// <summary>
        /// Descripción abreviada (máx. 40 caracteres) para celdas del cuadro.
        /// </summary>
        public string DescripcionAbreviada =>
            DescripcionComponente.Length > 40
                ? DescripcionComponente[..37] + "..."
                : DescripcionComponente;
    }

    // ─────────────────────────────────────────────────────────
    //  MODELO DE OBRA REGISTRADA EN SESIÓN
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Registro de una obra complementaria declarada en la sesión.
    /// Vincula el ítem del Anexo III con la cantidad medida en campo.
    /// </summary>
    public sealed class ObraComplementaria
    {
        public Guid   Id              { get; }
        public int    NumeroItem      { get; set; }   // Correlativo Anexo III
        public string DescripcionGrupo    { get; set; } = string.Empty;
        public string DescripcionComponente { get; set; } = string.Empty;
        public string UnidadMedida    { get; set; } = "m²";
        public double Cantidad        { get; set; }   // Área, volumen o longitud medida
        public string Observacion     { get; set; } = string.Empty;

        // Alias semántico para compatibilidad con motores de cuadro
        public double Area => UnidadMedida is "m²" or "m2" ? Cantidad : 0;
        public bool   EsMedidaLineal => UnidadMedida == "ml";

        public ObraComplementaria() => Id = Guid.NewGuid();

        /// <summary>Texto para celda izquierda del cuadro técnico.</summary>
        public string TextoCuadro =>
            string.IsNullOrWhiteSpace(DescripcionComponente)
                ? $"Ítem {NumeroItem:D2}"
                : $"Ítem {NumeroItem:D2} — {DescripcionAbreviada}";

        public string DescripcionAbreviada =>
            DescripcionComponente.Length > 38
                ? DescripcionComponente[..35] + "..."
                : DescripcionComponente;

        /// <summary>Valor formateado para celda derecha del cuadro.</summary>
        public string ValorFormateado =>
            Cantidad > 0 ? $"{Cantidad:N2} {UnidadMedida}" : "-";

        public override string ToString() =>
            $"[OC-{NumeroItem:D2}] {DescripcionComponente} | {ValorFormateado}";
    }

    // ─────────────────────────────────────────────────────────
    //  CATÁLOGO OFICIAL — ANEXO III RM 277-2025-VIVIENDA
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Catálogo estático de los 96 ítems del Anexo III de la
    /// RM 277-2025-VIVIENDA. Solo identifica y describe; no guarda precios.
    /// </summary>
    public static class CatalogoObras
    {
        private static readonly IReadOnlyList<ItemObraComplementaria> _items;

        static CatalogoObras()
        {
            _items = ConstruirCatalogo();
        }

        // ── API pública ───────────────────────────────────────

        public static IReadOnlyList<ItemObraComplementaria> Todos => _items;

        public static ItemObraComplementaria? BuscarPorNumero(int numero) =>
            _items.FirstOrDefault(i => i.NumeroItem == numero);

        public static IReadOnlyList<ItemObraComplementaria> PorGrupo(
            GrupoObraComplementaria grupo) =>
            _items.Where(i => i.Grupo == grupo).ToList();

        public static IReadOnlyList<(GrupoObraComplementaria Grupo,
            string Descripcion, int CantidadItems)> Grupos() =>
            _items
                .GroupBy(i => i.Grupo)
                .Select(g => (g.Key, g.First().DescripcionGrupo, g.Count()))
                .OrderBy(x => (int)x.Key)
                .ToList();

        /// <summary>
        /// Crea un registro de ObraComplementaria a partir del número de ítem.
        /// </summary>
        public static ObraComplementaria Crear(int numeroItem, double cantidad)
        {
            var item = BuscarPorNumero(numeroItem)
                ?? throw new ArgumentException(
                    $"Ítem {numeroItem} no existe en el Anexo III.");

            return new ObraComplementaria
            {
                NumeroItem           = item.NumeroItem,
                DescripcionGrupo     = item.DescripcionGrupo,
                DescripcionComponente = item.DescripcionComponente,
                UnidadMedida         = item.UnidadMedida,
                Cantidad             = cantidad
            };
        }

        // ── Construcción del catálogo completo ────────────────

        private static List<ItemObraComplementaria> ConstruirCatalogo() => new()
        {
            // ── Grupo 1: Muros perimétricos o cercos (1–9) ────
            new(1,  GrupoObraComplementaria.MurosPerimetricosYCercos,
                "Muros perimétricos o cercos",
                "Muro de concreto armado que incluye armadura y cimentación, e: hasta 0.25 m. h: hasta 2.40 m.",
                "m²"),
            new(2,  GrupoObraComplementaria.MurosPerimetricosYCercos,
                "Muros perimétricos o cercos",
                "Muro traslúcido de concreto armado (tipo UNI) y/o metálico que incluye cimentación. h: 2.40 m.",
                "m²"),
            new(3,  GrupoObraComplementaria.MurosPerimetricosYCercos,
                "Muros perimétricos o cercos",
                "Muro de ladrillo de arcilla o similar tarrajeado, amarre en soga, con columnas de CA y/o metálicas, h > 2.40 m.",
                "m²"),
            new(4,  GrupoObraComplementaria.MurosPerimetricosYCercos,
                "Muros perimétricos o cercos",
                "Muro de ladrillo de arcilla o similar tarrajeado, amarre en soga, con columnas de CA y/o metálicas, h ≤ 2.40 m.",
                "m²"),
            new(5,  GrupoObraComplementaria.MurosPerimetricosYCercos,
                "Muros perimétricos o cercos",
                "Muro de ladrillo de arcilla o similar, amarre en soga, con columnas de CA, solaqueados, h ≤ 2.40 m.",
                "m²"),
            new(6,  GrupoObraComplementaria.MurosPerimetricosYCercos,
                "Muros perimétricos o cercos",
                "Cerco de fierro/aluminio.",
                "m²"),
            new(7,  GrupoObraComplementaria.MurosPerimetricosYCercos,
                "Muros perimétricos o cercos",
                "Muro de ladrillo de arcilla o similar amarrado en soga que incluye cimentación.",
                "m²"),
            new(8,  GrupoObraComplementaria.MurosPerimetricosYCercos,
                "Muros perimétricos o cercos",
                "Muro de adobe, tapial o quincha tarrajeado.",
                "m²"),
            new(9,  GrupoObraComplementaria.MurosPerimetricosYCercos,
                "Muros perimétricos o cercos",
                "Muro de ladrillo o similar tarrajeado, amarre de cabeza con columnas de CA, h ≤ 2.40 m.",
                "m²"),

            // ── Grupo 2: Portones y puertas (10–16) ───────────
            new(10, GrupoObraComplementaria.PortonesYPuertas,
                "Portones y puertas",
                "Puerta de fierro, aluminio o similar, h 2.20 m., ancho ≤ 2.00 m.",
                "m²"),
            new(11, GrupoObraComplementaria.PortonesYPuertas,
                "Portones y puertas",
                "Puerta de fierro con plancha metálica, h 2.20 m., ancho > 2.00 m.",
                "m²"),
            new(12, GrupoObraComplementaria.PortonesYPuertas,
                "Portones y puertas",
                "Portón de fierro con plancha metálica, h > 3.00 m. hasta 4.00 m.",
                "m²"),
            new(13, GrupoObraComplementaria.PortonesYPuertas,
                "Portones y puertas",
                "Puerta de madera o similar, h 2.20 m., ancho ≤ 2.00 m.",
                "m²"),
            new(14, GrupoObraComplementaria.PortonesYPuertas,
                "Portones y puertas",
                "Puerta de madera o similar, h 2.20 m., ancho > 2.00 m.",
                "m²"),
            new(15, GrupoObraComplementaria.PortonesYPuertas,
                "Portones y puertas",
                "Portón de fierro con plancha metálica, h ≤ 3.00 m.",
                "m²"),
            new(16, GrupoObraComplementaria.PortonesYPuertas,
                "Portones y puertas",
                "Portón de fierro con plancha metálica, h > 4.00 m.",
                "m²"),

            // ── Grupo 3: Tanques elevados (17–21) ─────────────
            new(17, GrupoObraComplementaria.TanquesElevados,
                "Tanques elevados",
                "Tanque de concreto armado, capacidad ≤ 5.00 m³.",
                "m³"),
            new(18, GrupoObraComplementaria.TanquesElevados,
                "Tanques elevados",
                "Tanque elevado de plástico/fibra de vidrio/polietileno o similar, cap. > 1.00 m³.",
                "m³"),
            new(19, GrupoObraComplementaria.TanquesElevados,
                "Tanques elevados",
                "Tanque de concreto armado, capacidad > 5.00 m³.",
                "m³"),
            new(20, GrupoObraComplementaria.TanquesElevados,
                "Tanques elevados",
                "Tanque de concreto armado, capacidad > 15.00 m³. (Opcional)",
                "m³"),
            new(21, GrupoObraComplementaria.TanquesElevados,
                "Tanques elevados",
                "Tanque elevado de plástico/fibra de vidrio/polietileno o similar, cap. ≤ 1.00 m³.",
                "m³"),

            // ── Grupo 4: Cisternas (22–28) ─────────────────────
            new(22, GrupoObraComplementaria.CisternasYPozosSumideros,
                "Cisternas, pozos sumideros, tanques sépticos",
                "Tanque cisterna de plástico, fibra de vidrio, polietileno o similar, cap. > 1.00 m³.",
                "m³"),
            new(23, GrupoObraComplementaria.CisternasYPozosSumideros,
                "Cisternas, pozos sumideros, tanques sépticos",
                "Cisterna de concreto armado, capacidad ≤ 5.00 m³.",
                "m³"),
            new(24, GrupoObraComplementaria.CisternasYPozosSumideros,
                "Cisternas, pozos sumideros, tanques sépticos",
                "Cisterna de concreto armado, capacidad ≤ 10.00 m³.",
                "m³"),
            new(25, GrupoObraComplementaria.CisternasYPozosSumideros,
                "Cisternas, pozos sumideros, tanques sépticos",
                "Cisterna de concreto armado, capacidad ≤ 20.00 m³.",
                "m³"),
            new(26, GrupoObraComplementaria.CisternasYPozosSumideros,
                "Cisternas, pozos sumideros, tanques sépticos",
                "Cisterna, pozo de ladrillo tarrajeado, ≤ 5.00 m³.",
                "m³"),
            new(27, GrupoObraComplementaria.CisternasYPozosSumideros,
                "Cisternas, pozos sumideros, tanques sépticos",
                "Cisterna de concreto armado, capacidad > 20.00 m³.",
                "m³"),
            new(28, GrupoObraComplementaria.CisternasYPozosSumideros,
                "Cisternas, pozos sumideros, tanques sépticos",
                "Tanque de plástico, fibra de vidrio, polietileno o similar, cap. ≤ 1.00 m³.",
                "m³"),

            // ── Grupo 5: Piscinas y espejos de agua (29–32) ───
            new(29, GrupoObraComplementaria.PiscinasYEspejosDe Agua,
                "Piscinas, espejos de agua",
                "Piscina/espejo de agua, concreto armado con mayólica, cap. ≤ 5.00 m³.",
                "m³"),
            new(30, GrupoObraComplementaria.PiscinasYEspejosDe Agua,
                "Piscinas, espejos de agua",
                "Piscina/espejo de agua, concreto armado con mayólica, cap. ≤ 10.00 m³.",
                "m³"),
            new(31, GrupoObraComplementaria.PiscinasYEspejosDe Agua,
                "Piscinas, espejos de agua",
                "Piscina/espejo de agua, concreto armado con mayólica, cap. > 10.00 m³.",
                "m³"),
            new(32, GrupoObraComplementaria.PiscinasYEspejosDe Agua,
                "Piscinas, espejos de agua",
                "Piscina de ladrillo KK con pintura.",
                "m³"),

            // ── Grupo 6: Losas deportivas / estacionamientos (33–36)
            new(33, GrupoObraComplementaria.LosasDeportivasYEstacionamientos,
                "Losas deportivas, estacionamientos, patios de maniobras, veredas",
                "Losa de concreto armado espesor 4\".",
                "m²"),
            new(34, GrupoObraComplementaria.LosasDeportivasYEstacionamientos,
                "Losas deportivas, estacionamientos, patios de maniobras, veredas",
                "Asfalto espesor 2\".",
                "m²"),
            new(35, GrupoObraComplementaria.LosasDeportivasYEstacionamientos,
                "Losas deportivas, estacionamientos, patios de maniobras, veredas",
                "Losa de concreto simple espesor ≤ 4\".",
                "m²"),
            new(36, GrupoObraComplementaria.LosasDeportivasYEstacionamientos,
                "Losas deportivas, estacionamientos, patios de maniobras, veredas",
                "Concreto para veredas espesor 4\".",
                "m²"),

            // ── Grupo 7: Hornos, chimeneas, incineradores (37–39)
            new(37, GrupoObraComplementaria.HornosChimeneasIncineradores,
                "Hornos, chimeneas, incineradores",
                "Horno de concreto armado con enchape de ladrillo refractario.",
                "m³"),
            new(38, GrupoObraComplementaria.HornosChimeneasIncineradores,
                "Hornos, chimeneas, incineradores",
                "Horno de ladrillo con enchape de ladrillo refractario.",
                "m³"),
            new(39, GrupoObraComplementaria.HornosChimeneasIncineradores,
                "Hornos, chimeneas, incineradores",
                "Horno de adobe.",
                "m³"),

            // ── Grupo 8: Torres de vigilancia (40–41) ──────────
            new(40, GrupoObraComplementaria.TorresDeVigilancia,
                "Torres de vigilancia",
                "Estructura de concreto armado que incluye torre de vigilancia.",
                "und"),
            new(41, GrupoObraComplementaria.TorresDeVigilancia,
                "Torres de vigilancia",
                "Estructura de concreto armado, no incluye torre de vigilancia.",
                "und"),

            // ── Grupo 9: Bóvedas (42) ──────────────────────────
            new(42, GrupoObraComplementaria.Bovedas,
                "Bóvedas",
                "Bóveda de concreto armado reforzado.",
                "m³"),

            // ── Grupo 10: Balanzas industriales (43) ───────────
            new(43, GrupoObraComplementaria.BalanzasIndustriales,
                "Balanzas industriales",
                "Balanza industrial de concreto armado (obra civil).",
                "m³"),

            // ── Grupo 11: Postes de alumbrado (44) ─────────────
            new(44, GrupoObraComplementaria.PostesDeAlumbrado,
                "Postes de alumbrado",
                "Poste de concreto/fierro que incluye un reflector.",
                "und"),

            // ── Grupo 12: Bases de soporte de máquinas (45) ────
            new(45, GrupoObraComplementaria.BasesDeSoporteDeMaquinas,
                "Bases de soporte de máquinas",
                "Dados de concreto armado.",
                "m³"),

            // ── Grupo 13: Cajas de registro de concreto (46–48) ─
            new(46, GrupoObraComplementaria.CajasDeRegistroDeConcreto,
                "Cajas de registro de concreto",
                "Caja de registro de concreto de 24\"x24\".",
                "und"),
            new(47, GrupoObraComplementaria.CajasDeRegistroDeConcreto,
                "Cajas de registro de concreto",
                "Caja de registro de concreto de 12\"x24\".",
                "und"),
            new(48, GrupoObraComplementaria.CajasDeRegistroDeConcreto,
                "Cajas de registro de concreto",
                "Caja de registro de concreto de 10\"x20\".",
                "und"),

            // ── Grupo 14: Buzones de concreto (49) ─────────────
            new(49, GrupoObraComplementaria.BuzonesDeConcreto,
                "Buzón de concreto",
                "Buzón de concreto estándar.",
                "und"),

            // ── Grupo 15: Parapetos (50–53) ─────────────────────
            new(50, GrupoObraComplementaria.Parapetos,
                "Parapeto",
                "Parapeto ladrillo KK, de cabeza, acabado tarrajeado, h = 0.80 m – 1.00 m.",
                "m²"),
            new(51, GrupoObraComplementaria.Parapetos,
                "Parapeto",
                "Parapeto ladrillo KK, de soga, acabado tarrajeado, h = 0.80 m – 1.00 m.",
                "m²"),
            new(52, GrupoObraComplementaria.Parapetos,
                "Parapeto",
                "Parapeto ladrillo KK, de cabeza, acabado caravista, h = 0.80 m – 1.00 m.",
                "m²"),
            new(53, GrupoObraComplementaria.Parapetos,
                "Parapeto",
                "Parapeto ladrillo KK, de soga, acabado caravista, h = 0.80 m – 1.00 m.",
                "m²"),

            // ── Grupo 16: Rampas, gradas y escaleras de concreto (54–57)
            new(54, GrupoObraComplementaria.RampasDradasYEscaleras,
                "Rampas, gradas y escaleras de concreto",
                "Escalera de concreto armado c/acabados.",
                "m³"),
            new(55, GrupoObraComplementaria.RampasDradasYEscaleras,
                "Rampas, gradas y escaleras de concreto",
                "Escalera de concreto armado s/acabados.",
                "m³"),
            new(56, GrupoObraComplementaria.RampasDradasYEscaleras,
                "Rampas, gradas y escaleras de concreto",
                "Rampa o grada de concreto c/encofrado.",
                "m³"),
            new(57, GrupoObraComplementaria.RampasDradasYEscaleras,
                "Rampas, gradas y escaleras de concreto",
                "Rampa de concreto s/encofrado.",
                "m³"),

            // ── Grupo 17: Muros de contención (58–63) ──────────
            new(58, GrupoObraComplementaria.MurosDeContencion,
                "Muro de contención de concreto armado",
                "Muro de contención CA, h = 1.40 m., e = 20 cm.",
                "m³"),
            new(59, GrupoObraComplementaria.MurosDeContencion,
                "Muro de contención de concreto armado",
                "Muro de contención CA, h = 2.50 m., e = 20 cm.",
                "m³"),
            new(60, GrupoObraComplementaria.MurosDeContencion,
                "Muro de contención de concreto armado",
                "Muro de contención CA, h = 4.00 m., e = 20 cm.",
                "m³"),
            new(61, GrupoObraComplementaria.MurosDeContencion,
                "Muro de contención de concreto armado",
                "Muro de contención CA, h = 1.40 m., e = 15 cm.",
                "m³"),
            new(62, GrupoObraComplementaria.MurosDeContencion,
                "Muro de contención de concreto armado",
                "Muro de contención CA, h = 2.50 m., e = 15 cm.",
                "m³"),
            new(63, GrupoObraComplementaria.MurosDeContencion,
                "Muro de contención de concreto armado",
                "Muro de contención CA, h = 4.00 m., e = 15 cm.",
                "m³"),

            // ── Grupo 18: Escaleras metálicas (64–66) ──────────
            new(64, GrupoObraComplementaria.EscalerasMetalicas,
                "Escalera metálica",
                "Escalera metálica caracol h = 6.00 m. (1.er piso al 3.er piso).",
                "und"),
            new(65, GrupoObraComplementaria.EscalerasMetalicas,
                "Escalera metálica",
                "Escalera metálica caracol h = 3.00 m. (1.er piso al 2.do piso).",
                "und"),
            new(66, GrupoObraComplementaria.EscalerasMetalicas,
                "Escalera metálica",
                "Escalera metálica caracol h = 3.00 m. (entre pisos).",
                "und"),

            // ── Grupo 19: Pastorales (67) ───────────────────────
            new(67, GrupoObraComplementaria.Pastorales,
                "Pastoral",
                "Pastoral h = 2.20 m.",
                "und"),

            // ── Grupo 20: Proyectores luminaria (68–69) ─────────
            new(68, GrupoObraComplementaria.ProyectoresLuminaria,
                "Proyectores luminaria",
                "Proyector luminaria 250 W, vapor de sodio, instalación y cableado.",
                "und"),
            new(69, GrupoObraComplementaria.ProyectoresLuminaria,
                "Proyectores luminaria",
                "Proyector luminaria 150 W, vapor de mercurio, instalación y cableado.",
                "und"),

            // ── Grupo 21: Tuberías de concreto (70–71) ──────────
            new(70, GrupoObraComplementaria.TuberiasDeConcreto,
                "Tuberías de concreto",
                "Tubería de concreto armado D = 1.20 m.",
                "ml"),
            new(71, GrupoObraComplementaria.TuberiasDeConcreto,
                "Tuberías de concreto",
                "Tubería de concreto D = 18\" (45 cm).",
                "ml"),

            // ── Grupo 22: Canales y zanjas (72–73) ──────────────
            new(72, GrupoObraComplementaria.CanalesYZanjas,
                "Canaleta de concreto armado",
                "Canaleta de concreto sin rejillas.",
                "ml"),
            new(73, GrupoObraComplementaria.CanalesYZanjas,
                "Zanjas de concreto",
                "Zanja de concreto armado (talleres).",
                "ml"),

            // ── Grupo 23: Postes de concreto armado (74–80) ─────
            new(74, GrupoObraComplementaria.PostesDeConcreto,
                "Postes de concreto armado",
                "Poste de concreto, un reflector, instalado y cableado, h = 12.00 m.",
                "pza"),
            new(75, GrupoObraComplementaria.PostesDeConcreto,
                "Postes de concreto armado",
                "Poste de concreto, un reflector, instalado y cableado, h = 11.00 m.",
                "pza"),
            new(76, GrupoObraComplementaria.PostesDeConcreto,
                "Postes de concreto armado",
                "Poste de concreto, un reflector, instalado y cableado, h = 10.00 m.",
                "pza"),
            new(77, GrupoObraComplementaria.PostesDeConcreto,
                "Postes de concreto armado",
                "Poste de concreto, un reflector, instalado y cableado, h = 9.00 m.",
                "pza"),
            new(78, GrupoObraComplementaria.PostesDeConcreto,
                "Postes de concreto armado",
                "Poste de concreto, un reflector, instalado y cableado, h = 8.00 m.",
                "pza"),
            new(79, GrupoObraComplementaria.PostesDeConcreto,
                "Postes de concreto armado",
                "Poste de concreto, un reflector, instalado y cableado, h = 7.00 m.",
                "pza"),
            new(80, GrupoObraComplementaria.PostesDeConcreto,
                "Postes de concreto armado",
                "Poste de concreto, un reflector, instalado y cableado, h = 3.00 m.",
                "pza"),

            // ── Grupo 24: Cubiertas (81–84) ─────────────────────
            new(81, GrupoObraComplementaria.Cubiertas,
                "Cubiertas",
                "Cubierta de tejas de arcilla o similar.",
                "m²"),
            new(82, GrupoObraComplementaria.Cubiertas,
                "Cubiertas",
                "Cubierta de ladrillo pastelero asentado c/mezcla 1:5.",
                "m²"),
            new(83, GrupoObraComplementaria.Cubiertas,
                "Cubiertas",
                "Cubierta de ladrillo pastelero asentado c/barro.",
                "m²"),
            new(84, GrupoObraComplementaria.Cubiertas,
                "Cubiertas",
                "Cubierta con torta de barro 2\".",
                "m²"),

            // ── Grupo 25: Pasamano metálico (85–87) ─────────────
            new(85, GrupoObraComplementaria.PasamanoMetalico,
                "Pasamano metálico",
                "Pasamano metálico tubo circular galvanizado Ø 3\".",
                "ml"),
            new(86, GrupoObraComplementaria.PasamanoMetalico,
                "Pasamano metálico",
                "Pasamano metálico tubo circular galvanizado Ø 2\".",
                "ml"),
            new(87, GrupoObraComplementaria.PasamanoMetalico,
                "Pasamano metálico",
                "Pasamano metálico tubo circular galvanizado Ø 1\".",
                "ml"),

            // ── Grupo 26: Cercos metálicos (88–90) ──────────────
            new(88, GrupoObraComplementaria.CercosMetalicos,
                "Cercos metálicos",
                "Cerco metálico: tubo circular Ø 2\", Ang. 1\", malla 2\"×2\", alam. #8.",
                "m²"),
            new(89, GrupoObraComplementaria.CercosMetalicos,
                "Cercos metálicos",
                "Cerco metálico: tubo circular Ø 2\", Ang. 1\", malla 2\"×2\", alam. #10.",
                "m²"),
            new(90, GrupoObraComplementaria.CercosMetalicos,
                "Cercos metálicos",
                "Cerco metálico: tubo circular Ø 2\", Ang. 1\", malla 2\"×2\", alam. #12.",
                "m²"),

            // ── Grupo 27: Columnas / estructuras de fierro (91–92)
            new(91, GrupoObraComplementaria.ColumnasYEstructurasDeFierro,
                "Columnas, estructuras o similares de fierro",
                "Poste/estructura de fierro h = 4.00 m.",
                "pza"),
            new(92, GrupoObraComplementaria.ColumnasYEstructurasDeFierro,
                "Columnas, estructuras o similares de fierro",
                "Poste/estructura de fierro h = 2.50 m.",
                "pza"),

            // ── Grupo 28: Sardinel (93–94) ───────────────────────
            new(93, GrupoObraComplementaria.Sardineles,
                "Sardinel",
                "Sardinel de concreto e = 0.15 m, peraltado, sin pintura, h peralte = 0.35 m.",
                "ml"),
            new(94, GrupoObraComplementaria.Sardineles,
                "Sardinel",
                "Sardinel de concreto e = 0.15 m, peraltado, con pintura, h peralte = 0.35 m.",
                "ml"),

            // ── Grupo 29: Pista o pavimento de concreto (95) ────
            new(95, GrupoObraComplementaria.PistasYPavimentos,
                "Pista o pavimento de concreto",
                "Pista o losa de concreto de 6\".",
                "m²"),

            // ── Grupo 30: Trampa de concreto para grasa (96) ────
            new(96, GrupoObraComplementaria.TrampasDGrasa,
                "Trampa de concreto para grasa",
                "Trampa de concreto armado para grasa.",
                "m³"),
        };
    }
}
