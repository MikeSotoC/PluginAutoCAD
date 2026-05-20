// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  ObrasCompCommand.cs
//
//  Comando: UC_OBRASCOMP
//
//  Flujo completo de inserción de leader catastral para
//  obras complementarias e instalaciones fijas y permanentes
//  según RM 277-2025-VIVIENDA Anexo III.
//
//  FLUJO DEL OPERADOR:
//
//  1. Escribe UC_OBRASCOMP en la línea de comandos
//  2. Ve el listado de grupos del Anexo III
//  3. Ingresa el número de ítem (1–96) o 0 para ver ítems de un grupo
//  4. El sistema confirma: descripción + unidad
//  5. [Opcional] Escribe una descripción libre adicional
//     (ej: "Portón garaje" o "2.40 × 1.20 m" o ambos)
//  6. Clic sobre el elemento catastral → ORIGEN de la flecha
//  7. Clic en la posición del texto    → DESTINO / guía
//  8. El sistema inserta el leader (4 entidades CAD)
//  9. Pregunta si insertar otro leader del mismo ítem (S/N)
//  10. Al terminar, registra la obra en la edificación activa
//
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CatastroUrbano.Core.CAD;
using CatastroUrbano.Core.Infrastructure;
using CatastroUrbano.Core.Models;
using CatastroUrbano.Core.Styles;

[assembly: CommandClass(typeof(CatastroUrbano.Commands.ObrasCompCommand))]

namespace CatastroUrbano.Commands
{
    public sealed class ObrasCompCommand
    {
        // ── Servicios ─────────────────────────────────────────
        private readonly StyleResolver         _styleResolver = new();
        private readonly CadTransactionManager _txManager     = new();
        private readonly ErrorHandler          _log           = ErrorHandler.Instancia;

        // ─────────────────────────────────────────────────────
        //  COMANDO UC_OBRASCOMP
        // ─────────────────────────────────────────────────────

        [CommandMethod("UC_OBRASCOMP", CommandFlags.Modal)]
        public void EjecutarObrasComp()
        {
            var ctx = ContextoCAD.ObtenerActivo();

            ImprimirEncabezado(ctx.Editor);

            // ── Paso 1: Seleccionar ítem del Anexo III ─────────
            var item = SolicitarItem(ctx.Editor);
            if (item == null)
            {
                ctx.Escribir("Operación cancelada.");
                return;
            }

            ctx.Editor.WriteMessage(
                $"\n  ✓ Ítem {item.NumeroItem:D2}: {item.DescripcionComponente} " +
                $"[{item.UnidadMedida}]");

            // ── Paso 2: Descripción libre + dimensiones (opcional)
            string descripcionLibre = SolicitarDescripcionLibre(
                ctx.Editor, item);

            // ── Paso 3: Armar líneas del MText ────────────────
            // Línea 1: descripción oficial del Anexo III (sin número de ítem)
            string linea1 = item.DescripcionAbreviada;

            // Línea 2: dimensiones / descripción libre del operador (vacía si omitió)
            string linea2 = descripcionLibre;

            // ── Paso 4: Vista previa del texto en comandos ─────
            ctx.Editor.WriteMessage(
                $"\n  Etiqueta que se insertará:");
            ctx.Editor.WriteMessage($"\n    {linea1}");
            if (!string.IsNullOrWhiteSpace(linea2))
                ctx.Editor.WriteMessage($"\n    {linea2}");
            else
                ctx.Editor.WriteMessage($"\n    (sin dimensiones — solo descripción)");

            // ── Paso 5: Inserción interactiva de leaders ───────
            int   leadersInsertados = 0;
            bool  continuar         = true;

            while (continuar)
            {
                ctx.Editor.WriteMessage(
                    $"\n[Leader {leadersInsertados + 1}] " +
                    "Especifique los dos puntos del leader:");

                // Punto de origen (flecha sobre el elemento)
                var ptOrigen = SolicitarPunto(
                    ctx.Editor,
                    "\n  Clic en el elemento a señalar [origen flecha]: ");

                if (ptOrigen == null)
                {
                    ctx.Escribir("Punto de origen no especificado.");
                    break;
                }

                // Punto de destino (posición del texto)
                var ptDestino = SolicitarPunto(
                    ctx.Editor,
                    "\n  Clic en la posición del texto [destino]: ");

                if (ptDestino == null)
                {
                    ctx.Escribir("Punto de destino no especificado.");
                    break;
                }

                // Insertar leader en transacción
                var opResult = InsertarLeader(
                    ptOrigen.Value, ptDestino.Value,
                    linea1, linea2, ctx);

                if (opResult.Exitoso)
                {
                    leadersInsertados++;
                    ctx.Editor.WriteMessage(
                        $"\n  ✓ Leader {leadersInsertados} insertado.");
                }
                else
                {
                    ctx.EscribirError(opResult.Mensaje);
                }

                // ¿Otro leader del mismo ítem?
                continuar = PreguntarOtroLeader(ctx.Editor, item.NumeroItem);
            }

            // ── Paso 6: Registrar en edificación activa ────────
            if (leadersInsertados > 0)
            {
                RegistrarEnSesion(item, ctx);

                ctx.EscribirOk(
                    $"UC_OBRASCOMP finalizado. " +
                    $"{leadersInsertados} leader(s) insertado(s) para " +
                    $"Ítem {item.NumeroItem:D2}.");
            }
            else
            {
                ctx.Escribir("No se insertó ningún leader.");
            }
        }

        // ─────────────────────────────────────────────────────
        //  INSERCIÓN DE LEADER EN TRANSACCIÓN
        // ─────────────────────────────────────────────────────

        private ResultadoOperacion InsertarLeader(
            Point3d     ptOrigen,
            Point3d     ptDestino,
            string      linea1,
            string      linea2,
            ContextoCAD ctx)
        {
            var mtextEngine  = new MTextEngine(_styleResolver);
            var leaderEngine = new LeaderEngine(_styleResolver, mtextEngine);

            var paramsLeader = new ParamsLeader
            {
                PuntoOrigen          = ptOrigen,
                PuntoDestino         = ptDestino,
                TextoLinea1          = linea1,
                TextoLinea2          = linea2,
                TamanoFlecha         = 0.18,
                LongitudHorizontal   = 2.00,
                MargenTextoVertical  = 0.07,
                AlturaTexto          = 0.18,
                NombreLayerLeader    = "DESCRIPCION",
                NombreLayerTexto     = "DESCRIPCION",
                NombreTextStyle      = "TEXTO"
            };

            int cantEntidades = 0;

            var resultado = _txManager.Ejecutar(
                ctx.Database,
                tr =>
                {
                    var leader = leaderEngine.Construir(paramsLeader, ctx.Database);
                    _txManager.AgregarAlModelSpaceLote(
                        leader.TodasLasEntidades(), tr, ctx.Database);
                    cantEntidades = leader.CantidadEntidades;
                },
                "UC_OBRASCOMP — insertar leader");

            return resultado.Exitoso
                ? ResultadoOperacion.Ok($"{cantEntidades} entidades")
                : resultado;
        }

        // ─────────────────────────────────────────────────────
        //  REGISTRO EN SESIÓN CATASTRAL
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Registra el ítem en la edificación activa de la sesión.
        /// Solicita la cantidad al operador en este momento
        /// (ya que fue una inserción de leader libre, sin cantidad previa).
        /// </summary>
        private void RegistrarEnSesion(
            ItemObraComplementaria item,
            ContextoCAD            ctx)
        {
            var sesion = SesionCatastral.Instancia;
            if (!sesion.Edificaciones.Any())
            {
                ctx.EscribirAdvertencia(
                    "Sin edificación activa. Use UC_NUEVA_EDIFICACION. " +
                    "La obra quedó graficada pero no registrada en el cuadro.");
                return;
            }

            // Solicitar cantidad para el registro en cuadro técnico
            string labelUnidad = ObtenerLabelUnidad(item.UnidadMedida);
            var optCant = new PromptDoubleOptions(
                $"\nRegistrar en cuadro: cantidad en {labelUnidad} " +
                "(Enter para omitir registro): ")
            {
                DefaultValue  = 0.0,
                AllowNegative = false,
                AllowZero     = true,
                AllowNone     = true
            };

            var resCant = ctx.Editor.GetDouble(optCant);

            if (resCant.Status == PromptStatus.OK && resCant.Value > 0)
            {
                var obra = CatalogoObras.Crear(item.NumeroItem, resCant.Value);
                sesion.Edificaciones.Last().ObrasComplementarias.Add(obra);
                ctx.EscribirOk(
                    $"Obra registrada en '{sesion.Edificaciones.Last().Codigo}': " +
                    $"{obra.ValorFormateado}.");
            }
            else
            {
                ctx.Escribir(
                    "Obra no registrada en cuadro técnico " +
                    "(solo fue graficada). Use UC_OBRAS_COMP para registrarla.");
            }
        }

        // ─────────────────────────────────────────────────────
        //  INTERACCIÓN: CATÁLOGO
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Muestra el catálogo por grupos y solicita el número de ítem.
        /// El operador puede ingresar 0 para expandir un grupo.
        /// Retorna null si cancela con ESC o -1.
        /// </summary>
        private static ItemObraComplementaria? SolicitarItem(Editor editor)
        {
            // ── Listado de grupos ─────────────────────────────
            editor.WriteMessage(
                "\n\n  GRUPOS DEL ANEXO III:\n");

            foreach (var (grupo, desc, _) in CatalogoObras.Grupos())
            {
                var items     = CatalogoObras.PorGrupo(grupo);
                int primero   = items.First().NumeroItem;
                int ultimo    = items.Last().NumeroItem;
                string rango  = primero == ultimo
                    ? $"ítem {primero:D2}"
                    : $"ítems {primero:D2}–{ultimo:D2}";

                editor.WriteMessage(
                    $"  Grp {(int)grupo:D2}: {desc,-46} ({rango})\n");
            }

            editor.WriteMessage(
                "\n  → Ingrese 0 para ver ítems detallados de un grupo.");

            // ── Bucle de selección ────────────────────────────
            var optItem = new PromptIntegerOptions(
                "\nNúmero de ítem [1–96]  (0 = ver grupo  |  -1 = cancelar): ")
            {
                LowerLimit   = -1,
                UpperLimit   = 96,
                DefaultValue = 0,
                AllowNone    = false
            };

            while (true)
            {
                var res = editor.GetInteger(optItem);

                if (res.Status != PromptStatus.OK || res.Value == -1)
                    return null;

                if (res.Value == 0)
                {
                    ExpandirGrupo(editor);
                    continue;
                }

                var item = CatalogoObras.BuscarPorNumero(res.Value);
                if (item == null)
                {
                    editor.WriteMessage(
                        $"\n  ✗ Ítem {res.Value} no existe en el Anexo III. " +
                        "Ingrese un número válido (1–96) o 0 para ver el catálogo.");
                    continue;
                }

                return item;
            }
        }

        /// <summary>
        /// Muestra los ítems numerados de un grupo específico.
        /// </summary>
        private static void ExpandirGrupo(Editor editor)
        {
            var optGrupo = new PromptIntegerOptions(
                "\n  Número de grupo a expandir [1–30]: ")
            {
                LowerLimit   = 1,
                UpperLimit   = 30,
                DefaultValue = 1,
                AllowNone    = false
            };

            var resG = editor.GetInteger(optGrupo);
            if (resG.Status != PromptStatus.OK) return;

            var grupo = (GrupoObraComplementaria)resG.Value;
            var items = CatalogoObras.PorGrupo(grupo);

            if (!items.Any())
            {
                editor.WriteMessage($"\n  Sin ítems para grupo {resG.Value}.");
                return;
            }

            editor.WriteMessage(
                $"\n  ── Grupo {resG.Value}: {items.First().DescripcionGrupo} ──");

            foreach (var it in items)
                editor.WriteMessage(
                    $"\n    [{it.NumeroItem:D2}] {it.DescripcionComponente}  ({it.UnidadMedida})");

            editor.WriteMessage("\n");
        }

        // ─────────────────────────────────────────────────────
        //  INTERACCIÓN: DESCRIPCIÓN LIBRE
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Solicita al operador la descripción libre opcional (línea 2).
        /// Puede incluir nombre del elemento, dimensiones, observaciones.
        /// Enter sin texto → se omite la línea 2.
        /// </summary>
        private static string SolicitarDescripcionLibre(
            Editor editor, ItemObraComplementaria item)
        {
            editor.WriteMessage(
                $"\n  Dimensiones u observación adicional (opcional)." +
                $"\n  Ej: '2.40 × 1.00 m'  /  'h=2.20 m'  /  '3 hojas'" +
                $"\n  Enter para omitir:");

            var opt = new PromptStringOptions(
                "\n  Dimensiones [Enter = omitir]: ")
            {
                AllowSpaces     = true,
                UseDefaultValue = true,
                DefaultValue    = string.Empty
            };

            var res = editor.GetString(opt);

            if (res.Status != PromptStatus.OK)
                return string.Empty;

            return res.StringResult.Trim();
        }

        // ─────────────────────────────────────────────────────
        //  INTERACCIÓN: PUNTOS
        // ─────────────────────────────────────────────────────

        private static Point3d? SolicitarPunto(Editor editor, string mensaje)
        {
            var opt = new PromptPointOptions(mensaje)
            {
                AllowNone = true
            };

            var res = editor.GetPoint(opt);
            return res.Status == PromptStatus.OK ? res.Value : (Point3d?)null;
        }

        // ─────────────────────────────────────────────────────
        //  INTERACCIÓN: OTRO LEADER
        // ─────────────────────────────────────────────────────

        private static bool PreguntarOtroLeader(Editor editor, int numItem)
        {
            var opt = new PromptKeywordOptions(
                $"\n¿Insertar otro leader del Ítem {numItem:D2}? [Si/No] <No>: ")
            {
                AllowNone = true
            };
            opt.Keywords.Add("Si");
            opt.Keywords.Add("No");
            opt.Keywords.Default = "No";

            var res = editor.GetKeywords(opt);

            if (res.Status == PromptStatus.None)   return false;  // Enter → No
            if (res.Status != PromptStatus.OK)     return false;

            return res.StringResult.Equals("Si",
                StringComparison.OrdinalIgnoreCase);
        }

        // ─────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────

        private static void ImprimirEncabezado(Editor editor)
        {
            editor.WriteMessage(
                "\n╔═══════════════════════════════════════════════════════╗");
            editor.WriteMessage(
                "\n║  UC_OBRASCOMP — Obras Complementarias e Inst. Fijas   ║");
            editor.WriteMessage(
                "\n║  RM 277-2025-VIVIENDA  Anexo III  (Ejercicio 2026)    ║");
            editor.WriteMessage(
                "\n╚═══════════════════════════════════════════════════════╝");
        }

        private static string ObtenerLabelUnidad(string unidad) => unidad switch
        {
            "m²"  => "metros cuadrados (m²)",
            "m³"  => "metros cúbicos (m³)",
            "ml"  => "metros lineales (ml)",
            "und" => "unidades (und)",
            "pza" => "piezas (pza)",
            _     => unidad
        };
    }
}
