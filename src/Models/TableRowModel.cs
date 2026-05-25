// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  TableRowModel.cs
//  Modelos de datos para filas del cuadro técnico dinámico
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;

namespace CatastroUrbano.Core.Models
{
    // ─────────────────────────────────────────────────────────
    //  TIPO DE FILA
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Define el rol gráfico y semántico de cada fila del cuadro.
    /// Controla altura, estilo de texto y bordes.
    /// </summary>
    public enum TipoFila
    {
        TituloPrincipal     = 0,   // EDIFICACIÓN 01  — fila doble, centrada
        SubtituloArea       = 1,   // AREA DE EDIFICA 01
        SubtituloUnidad     = 2,   // UNIDAD 1        | CASA HABITACION
        CabeceraPiso        = 3,   // separador de piso (no siempre visible)
        DatoPartida         = 4,   // 1P CC           | 120.49 m²
        ObraComplementaria  = 5,   // Portón / Cobertizo / etc.
        TotalParcial        = 6,   // Subtotal por piso
        TotalGeneral        = 7,   // TOTAL           | 250.60 m²
        Separador           = 8    // línea horizontal sin texto
    }

    // ─────────────────────────────────────────────────────────
    //  FILA BASE DEL CUADRO
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Unidad mínima de datos para una fila del cuadro técnico.
    /// El motor de dibujo traduce esta estructura en geometría CAD.
    /// </summary>
    public sealed class FilaCuadro
    {
        // ── Contenido ────────────────────────────────────────
        public string  TextoIzquierda   { get; set; } = string.Empty;
        public string  TextoDerecha     { get; set; } = string.Empty;
        public TipoFila Tipo            { get; set; } = TipoFila.DatoPartida;

        // ── Layout ───────────────────────────────────────────

        /// <summary>Altura de la fila en unidades de dibujo.</summary>
        public double AlturaFila        { get; set; } = 0.60;

        /// <summary>
        /// Porcentaje del ancho total asignado a la columna izquierda.
        /// Valor entre 0.0 y 1.0. La columna derecha ocupa el resto.
        /// </summary>
        public double ProporcionIzquierda { get; set; } = 0.65;

        // ── Estilo de texto ──────────────────────────────────
        public string TextStyleIzquierda  { get; set; } = "TEXTO";
        public string TextStyleDerecha    { get; set; } = "TEXTO";
        public double AlturaTexto         { get; set; } = 0.18;

        // ── Bordes ───────────────────────────────────────────
        /// <summary>Dibuja línea divisoria vertical entre columnas.</summary>
        public bool DividirColumnas     { get; set; } = true;

        /// <summary>Dibuja línea horizontal inferior de la fila.</summary>
        public bool DibujarBordeInferior { get; set; } = true;

        // ── Centrado ─────────────────────────────────────────
        /// <summary>Centra horizontalmente ambas columnas (para títulos).</summary>
        public bool CentrarAmbas        { get; set; } = false;

        // ── Negrita simulada (texto uppercase) ───────────────
        public bool TextoIzquierdaMayusculas { get; set; } = false;
        public bool TextoDerechaMayusculas   { get; set; } = false;

        // ── Constructor de fábrica ────────────────────────────

        public static FilaCuadro CrearTitulo(string texto) => new()
        {
            TextoIzquierda          = texto,
            Tipo                    = TipoFila.TituloPrincipal,
            AlturaFila              = 0.80,
            ProporcionIzquierda     = 1.0,
            DividirColumnas         = false,
            CentrarAmbas            = true,
            TextoIzquierdaMayusculas = true,
            AlturaTexto             = 0.22
        };

        public static FilaCuadro CrearSubtituloArea(string textoArea) => new()
        {
            TextoIzquierda          = textoArea,
            TextoDerecha            = "-",
            Tipo                    = TipoFila.SubtituloArea,
            AlturaFila              = 0.60,
            ProporcionIzquierda     = 0.70,
            TextoIzquierdaMayusculas = true
        };

        public static FilaCuadro CrearSubtituloUnidad(string unidad, string categoria) => new()
        {
            TextoIzquierda          = unidad,
            TextoDerecha            = categoria,
            Tipo                    = TipoFila.SubtituloUnidad,
            AlturaFila              = 0.60,
            ProporcionIzquierda     = 0.55,
            TextoIzquierdaMayusculas = true,
            TextoDerechaMayusculas  = true
        };

        public static FilaCuadro CrearPartida(string codigo, double area) => new()
        {
            TextoIzquierda  = codigo,
            TextoDerecha    = $"{area:N2} m²",
            Tipo            = TipoFila.DatoPartida,
            AlturaFila      = 0.55
        };

        public static FilaCuadro CrearObraComplementaria(string descripcion, double area) => new()
        {
            TextoIzquierda  = descripcion,
            TextoDerecha    = area > 0 ? $"{area:N2} m²" : "-",
            Tipo            = TipoFila.ObraComplementaria,
            AlturaFila      = 0.55
        };

        public static FilaCuadro CrearTotal(double areaTotal) => new()
        {
            TextoIzquierda          = "TOTAL",
            TextoDerecha            = $"{areaTotal:N2} m²",
            Tipo                    = TipoFila.TotalGeneral,
            AlturaFila              = 0.65,
            TextoIzquierdaMayusculas = true,
            AlturaTexto             = 0.20
        };

        public static FilaCuadro CrearSeparador() => new()
        {
            Tipo            = TipoFila.Separador,
            AlturaFila      = 0.30,
            DividirColumnas = false
        };
    }

    // ─────────────────────────────────────────────────────────
    //  CONFIGURACIÓN GLOBAL DEL CUADRO
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Parámetros de layout del cuadro técnico institucional.
    /// Define dimensiones, márgenes y estilos globales.
    /// </summary>
    public sealed class ConfiguracionCuadro
    {
        // ── Dimensiones ──────────────────────────────────────
        public double AnchoTotal          { get; set; } = 8.0;   // ancho cuadro
        public double MargenTextoH        { get; set; } = 0.15;  // margen izq/der del texto
        public double MargenTextoV        { get; set; } = 0.12;  // margen sup/inf del texto

        // ── Origen de inserción ──────────────────────────────
        // Esquina superior izquierda del cuadro
        public double OrigenX             { get; set; } = 0.0;
        public double OrigenY             { get; set; } = 0.0;

        // ── Layer de dibujo ──────────────────────────────────
        public string LayerBordes         { get; set; } = "FORMATO";
        public string LayerTextos         { get; set; } = "DESCRIPCION";

        // ── TextStyle por defecto ────────────────────────────
        public string TextStyleDefault    { get; set; } = "TEXTO";
        public double AlturaTextoDefault  { get; set; } = 0.18;

        // ── Grosor de líneas ─────────────────────────────────
        // Se usa lineweight del layer; no se aplican overrides
        public bool   UsarLineweightLayer { get; set; } = true;

        // ── Instancia por defecto ─────────────────────────────
        public static ConfiguracionCuadro PorDefecto => new();
    }

    // ─────────────────────────────────────────────────────────
    //  LAYOUT CALCULADO DEL CUADRO
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Resultado del cálculo de layout: posiciones exactas
    /// de cada fila para el motor de dibujo CadTableDrawer.
    /// </summary>
    public sealed class LayoutCuadroCalculado
    {
        public ConfiguracionCuadro Configuracion    { get; set; } = new();
        public List<FilaCuadro>    Filas            { get; set; } = new();

        /// <summary>Posición Y (superior) de cada fila, en el mismo orden que Filas.</summary>
        public List<double>        PosicionesY      { get; set; } = new();

        public double AlturaTotal                   { get; set; }
        public double AnchoTotal => Configuracion.AnchoTotal;

        // ── Límites del cuadro ───────────────────────────────
        public double XIzquierda => Configuracion.OrigenX;
        public double XDerecha   => Configuracion.OrigenX + AnchoTotal;
        public double YSuperior  => Configuracion.OrigenY;
        public double YInferior  => Configuracion.OrigenY - AlturaTotal;
    }
}
