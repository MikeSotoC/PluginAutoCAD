// ============================================================
//  SISTEMA CATASTRAL INSTITUCIONAL PERUANO
//  UCCommand.cs
//  Comando principal UC_CATEGORIZAR — Orquestador del sistema
//  Compatible: AutoCAD / ZWCAD  (.NET API)
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CatastroUrbano.Core.Analysis;
using CatastroUrbano.Core.CAD;
using CatastroUrbano.Core.Classification;
using CatastroUrbano.Core.Infrastructure;
using CatastroUrbano.Core.Models;
using CatastroUrbano.Core.Styles;
using CatastroUrbano.Core.Table;

// Registro del plugin en AutoCAD/ZWCAD
[assembly: ExtensionApplication(typeof(CatastroUrbano.Commands.CatastroPlugin))]
[assembly: CommandClass(typeof(CatastroUrbano.Commands.UCCommand))]

namespace CatastroUrbano.Commands
{
    // ─────────────────────────────────────────────────────────
    //  INICIALIZACIÓN DEL PLUGIN
    // ─────────────────────────────────────────────────────────

    public sealed class CatastroPlugin : IExtensionApplication
    {
        public void Initialize()
        {
            // Inicializar log en carpeta de usuario
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CatastroUrbano", "Logs");

            ErrorHandler.Instancia.Inicializar(logDir);
            ErrorHandler.Instancia.LogInfo(
                "Plugin Catastro Urbano Institucional Peruano inicializado.");

            Application.DocumentManager.MdiActiveDocument?
                .Editor.WriteMessage(
                    "\n═══════════════════════════════════════════════\n" +
                    "  SISTEMA CATASTRAL INSTITUCIONAL PERUANO\n" +
                    "  Comandos: UC_CATEGORIZAR | UC_VALIDAR_DWT\n" +
                    "  | UC_NUEVA_EDIFICACION | UC_OBRAS_COMP\n" +
                    "═══════════════════════════════════════════════\n");
        }

        public void Terminate()
        {
            ErrorHandler.Instancia.LogInfo("Plugin finalizado.");
            ErrorHandler.Instancia.Finalizar();
            SesionCatastral.ResetearSesion();
        }
    }

    // ─────────────────────────────────────────────────────────
    //  COMANDO PRINCIPAL: UC_CATEGORIZAR
    // ─────────────────────────────────────────────────────────

    public sealed class UCCommand
    {
        // ── Servicios ─────────────────────────────────────────

        private readonly StyleResolver         _styleResolver    = new();
        private readonly LayerClassifier       _layerClassifier  = new();
        private readonly PolylineAnalyzer      _polyAnalyzer     = new();
        private readonly AreaCalculator        _areaCalc         = new();
        private readonly CadTransactionManager _txManager        = new();
        private readonly ErrorHandler          _log              = ErrorHandler.Instancia;

        // ── Comando UC_CATEGORIZAR ────────────────────────────

        [CommandMethod("UC_CATEGORIZAR", CommandFlags.Modal)]
        public void EjecutarCategorizar()
        {
            var ctx = ContextoCAD.ObtenerActivo();
            ctx.Escribir("=== CATASTRO URBANO: CATEGORIZACIÓN DE POLÍGONOS ===");

            // ── Paso 1: Validar DWT ────────────────────────────
            var faltantes = _styleResolver.ValidarDWT(ctx.Database);
            if (faltantes.Any())
            {
                ctx.EscribirAdvertencia(
                    "DWT con recursos faltantes. El sistema usará fallbacks:");
                foreach (var f in faltantes)
                    ctx.Editor.WriteMessage($"\n  ⚠ {f}");
            }

            // ── Paso 2: Obtener sesión activa ──────────────────
            var sesion = SesionCatastral.Instancia;
            if (!sesion.Edificaciones.Any())
            {
                ctx.EscribirAdvertencia(
                    "No hay edificaciones activas. " +
                    "Use UC_NUEVA_EDIFICACION primero.");
                return;
            }

            var edificacionActual = sesion.Edificaciones.Last();
            ctx.Escribir(
                $"Edificación activa: {edificacionActual.Codigo}");

            // ── Paso 3: Selección de polilíneas ───────────────
            ctx.Escribir(
                "Seleccione polilíneas cerradas a categorizar (Enter para terminar):");

            var seleccion = SolicitarSeleccionPolilineas(ctx.Editor);
            if (seleccion == null || !seleccion.Any())
            {
                ctx.Escribir("Selección vacía. Operación cancelada.");
                return;
            }

            ctx.Escribir($"{seleccion.Count} entidad(es) seleccionada(s).");

            // ── Paso 4: Pedir categorización normativa ─────────
            var (usoTexto, catMuros, catTechos) =
                SolicitarCategorizacionNormativa(ctx.Editor);

            if (string.IsNullOrWhiteSpace(usoTexto))
            {
                ctx.Escribir("Uso no ingresado. Operación cancelada.");
                return;
            }

            ctx.Escribir(
                $"Uso: {usoTexto} | Muros: {catMuros} | Techos: {catTechos}");

            string categoria = usoTexto; // alias para compatibilidad

            // ── Paso 5: Procesar en transacción ───────────────
            int procesadas = 0;
            int errores    = 0;

            var lineEngine  = new CadLineEngine(_styleResolver);
            var mtextEngine = new MTextEngine(_styleResolver);
            var dimEngine   = new DimensionEngine(_styleResolver, _layerClassifier);

            var resultado = _txManager.Ejecutar(
                ctx.Database,
                tr =>
                {
                    foreach (var id in seleccion)
                    {
                        bool ok = ProcesarPoligono(
                            id, categoria, catMuros, catTechos,
                            edificacionActual,
                            tr, ctx.Database,
                            lineEngine, mtextEngine, dimEngine);

                        if (ok) procesadas++;
                        else    errores++;
                    }

                    // ── Paso 6: Generar cuadro técnico ────────
                    if (procesadas > 0)
                    {
                        GenerarCuadroTecnico(
                            edificacionActual, ctx, tr);
                    }
                },
                "UC_CATEGORIZAR");

            // ── Resultado final ────────────────────────────────
            if (resultado.Exitoso)
            {
                ctx.EscribirOk(
                    $"Proceso completado: {procesadas} polígono(s) categorizados." +
                    (errores > 0 ? $" ({errores} con errores — ver log)" : string.Empty));
            }
            else
            {
                ctx.EscribirError($"Error en transacción: {resultado.Mensaje}");
            }
        }

        // ─────────────────────────────────────────────────────
        //  COMANDO: UC_NUEVA_EDIFICACION
        // ─────────────────────────────────────────────────────

        [CommandMethod("UC_NUEVA_EDIFICACION", CommandFlags.Modal)]
        public void CrearNuevaEdificacion()
        {
            var ctx = ContextoCAD.ObtenerActivo();
            ctx.Escribir("=== NUEVA EDIFICACIÓN ===");

            var sesion = SesionCatastral.Instancia;
            int numero = sesion.Edificaciones.Count + 1;

            // Código correlativo: EDIFICACION 01, 02, etc.
            string codigo = $"EDIFICACION {numero:D2}";
            string descArea  = $"AREA DE EDIFICA {numero:D2}";
            string descUnidad = $"UNIDAD {numero}";

            // Pedir categoría general
            var optStr = new PromptStringOptions(
                $"\nCategoría general de '{codigo}' " +
                $"[CASA HABITACION/DPTO/COMERCIO]: ")
            {
                DefaultValue    = "CASA HABITACION",
                AllowSpaces     = true,
                UseDefaultValue = true
            };

            var res = ctx.Editor.GetString(optStr);
            string categoriaGeneral = res.Status == PromptStatus.OK
                ? res.StringResult.ToUpperInvariant()
                : "CASA HABITACION";

            var nueva = new EdificacionCatastral
            {
                Codigo            = codigo,
                Descripcion       = descArea,
                DescripcionUnidad = descUnidad,
                CategoriaGeneral  = categoriaGeneral
            };

            sesion.Edificaciones.Add(nueva);
            ctx.EscribirOk(
                $"Edificación '{codigo}' creada con categoría '{categoriaGeneral}'.");
            ctx.Escribir(
                "Use UC_CATEGORIZAR para agregar polígonos.");
        }

        // ─────────────────────────────────────────────────────
        //  COMANDO: UC_OBRAS_COMP
        // ─────────────────────────────────────────────────────

        [CommandMethod("UC_OBRAS_COMP", CommandFlags.Modal)]
        public void RegistrarObrasComplementarias()
        {
            var ctx    = ContextoCAD.ObtenerActivo();
            var sesion = SesionCatastral.Instancia;

            if (!sesion.Edificaciones.Any())
            {
                ctx.EscribirAdvertencia(
                    "No hay edificaciones activas. Use UC_NUEVA_EDIFICACION.");
                return;
            }

            var edif = sesion.Edificaciones.Last();
            ctx.Escribir(
                $"=== OBRAS COMPLEMENTARIAS: {edif.Codigo} ===");
            ctx.Escribir(
                "Catálogo según RM 277-2025-VIVIENDA, Anexo III");
            ctx.Escribir(
                "Grupos disponibles:");

            // Mostrar grupos del catálogo
            foreach (var (grupo, desc, count) in CatalogoObras.Grupos())
                ctx.Editor.WriteMessage(
                    $"\n  Grupo {(int)grupo:D2}: {desc} ({count} ítems)");

            // Solicitar número de ítem directamente
            var optItem = new PromptIntegerOptions(
                "\nIngrese número de ítem (1–96, 0 = ver ítems de un grupo, -1 = cancelar): ")
            {
                DefaultValue  = 0,
                LowerLimit    = -1,
                UpperLimit    = 96
            };

            int numeroItem = 0;
            var resItem = ctx.Editor.GetInteger(optItem);
            if (resItem.Status != PromptStatus.OK || resItem.Value == -1) return;
            numeroItem = resItem.Value;

            // Si ingresó 0, mostrar ítems de un grupo específico
            if (numeroItem == 0)
            {
                var optGrupo = new PromptIntegerOptions(
                    "\nIngrese número de grupo (1–30): ")
                {
                    DefaultValue = 1,
                    LowerLimit   = 1,
                    UpperLimit   = 30
                };
                var resGrupo = ctx.Editor.GetInteger(optGrupo);
                if (resGrupo.Status != PromptStatus.OK) return;

                var grupo = (GrupoObraComplementaria)resGrupo.Value;
                var itemsGrupo = CatalogoObras.PorGrupo(grupo);

                ctx.Editor.WriteMessage($"\n─── Ítems del grupo {resGrupo.Value} ───");
                foreach (var item in itemsGrupo)
                    ctx.Editor.WriteMessage(
                        $"\n  [{item.NumeroItem:D2}] {item.DescripcionComponente} ({item.UnidadMedida})");

                // Volver a pedir el ítem
                resItem = ctx.Editor.GetInteger(optItem);
                if (resItem.Status != PromptStatus.OK || resItem.Value <= 0) return;
                numeroItem = resItem.Value;
            }

            // Buscar ítem en el catálogo
            var itemSeleccionado = CatalogoObras.BuscarPorNumero(numeroItem);
            if (itemSeleccionado == null)
            {
                ctx.EscribirError(
                    $"Ítem {numeroItem} no existe en el Anexo III. " +
                    "Verifique el número.");
                return;
            }

            ctx.Escribir(
                $"Ítem {numeroItem:D2}: {itemSeleccionado.DescripcionComponente} " +
                $"[{itemSeleccionado.UnidadMedida}]");

            // Solicitar cantidad medida en campo
            string unidadLabel = itemSeleccionado.UnidadMedida switch
            {
                "m²"  => "metros cuadrados (m²)",
                "m³"  => "metros cúbicos (m³)",
                "ml"  => "metros lineales (ml)",
                "und" => "unidades (und)",
                "pza" => "piezas (pza)",
                _     => itemSeleccionado.UnidadMedida
            };

            var optCant = new PromptDoubleOptions(
                $"\nIngrese cantidad en {unidadLabel}: ")
            {
                DefaultValue  = 0.0,
                AllowNegative = false,
                AllowNone     = false
            };
            var resCant = ctx.Editor.GetDouble(optCant);
            if (resCant.Status != PromptStatus.OK) return;

            var obra = CatalogoObras.Crear(numeroItem, resCant.Value);
            edif.ObrasComplementarias.Add(obra);

            ctx.EscribirOk(
                $"Obra complementaria registrada: " +
                $"Ítem {numeroItem:D2} — {itemSeleccionado.DescripcionAbreviada} | " +
                $"{obra.ValorFormateado}");
            ctx.Escribir(
                "Use UC_CATEGORIZAR nuevamente para regenerar el cuadro técnico.");
        }

        // ─────────────────────────────────────────────────────
        //  COMANDO: UC_VALIDAR_DWT
        // ─────────────────────────────────────────────────────

        [CommandMethod("UC_VALIDAR_DWT", CommandFlags.Modal)]
        public void ValidarDWT()
        {
            var ctx = ContextoCAD.ObtenerActivo();
            ctx.Escribir("=== VALIDACIÓN DEL DWT INSTITUCIONAL ===");

            var faltantes = _styleResolver.ValidarDWT(ctx.Database);

            if (!faltantes.Any())
            {
                ctx.EscribirOk(
                    "DWT correcto. Todos los recursos institucionales " +
                    "están presentes.");
                return;
            }

            ctx.EscribirAdvertencia(
                $"{faltantes.Count} recurso(s) faltante(s):");
            foreach (var f in faltantes)
                ctx.Editor.WriteMessage($"\n  ✗ {f}");

            ctx.Escribir(
                "\nEl sistema usará recursos alternativos (fallbacks) " +
                "donde sea posible. Se recomienda cargar el DWT correcto.");
        }

        // ─────────────────────────────────────────────────────
        //  LÓGICA INTERNA
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Procesa un polígono individual dentro de la transacción activa.
        /// </summary>
        private bool ProcesarPoligono(
            ObjectId              id,
            string                usoTexto,
            CategoriaMuros        catMuros,
            CategoriaTechos       catTechos,
            EdificacionCatastral  edificacion,
            Transaction           tr,
            Database              db,
            CadLineEngine         lineEngine,
            MTextEngine           mtextEngine,
            DimensionEngine       dimEngine)
        {
            try
            {
                var entidad = (Entity)tr.GetObject(id, OpenMode.ForRead);

                // ── Analizar geometría ─────────────────────────
                var analisis = _polyAnalyzer.Analizar(entidad, tr);
                if (!analisis.Valida)
                {
                    _log.LogAdvertencia(
                        $"Polígono [{id}] inválido: {analisis.MotivoInvalidez}");
                    return false;
                }

                // ── Clasificar por layer ───────────────────────
                var clasificacion = _layerClassifier.Clasificar(analisis.LayerOrigen);

                // ── Construir entidad catastral ────────────────
                var poligono = _polyAnalyzer.ConstruirEntidad(analisis, id);
                poligono.UsoTextoLibre = usoTexto;
                poligono.Piso          = clasificacion.Piso;
                poligono.TipoPoligono  = clasificacion.TipoPoligono;

                // ── Categorización normativa RM 277-2025-VIVIENDA ──
                poligono.Categorizacion.Muros  = catMuros;
                poligono.Categorizacion.Techos = catTechos;

                // Código completo: "1P CC [C/C]"
                poligono.CodigoUnidad =
                    $"{poligono.EtiquetaPiso} {usoTexto} [{poligono.Categorizacion.Etiqueta}]"
                    .Trim();

                // ── Insertar texto interior ────────────────────
                var mtext = mtextEngine.ConstruirTextoInterior(poligono, db);
                _txManager.AgregarAlModelSpace(mtext, tr, db);
                poligono.TextoInsertado = true;

                // ── Generar cotas ─────────────────────────────
                List<RotatedDimension> cotas;

                if (poligono.TipoPoligono == TipoPoligono.Lote)
                    cotas = dimEngine.GenerarCotasLote(analisis, db);
                else
                    cotas = dimEngine.GenerarCotasFabrica(analisis, db);

                _txManager.AgregarAlModelSpaceLote(cotas, tr, db);
                poligono.CotasGeneradas = true;

                // ── Registrar en edificación ───────────────────
                _areaCalc.ActualizarAreaEdificacion(edificacion, poligono);

                _log.LogInfo(
                    $"Polígono procesado: {poligono.CodigoCompleto} | " +
                    $"{poligono.AreaFormateada} | Layer: {analisis.LayerOrigen}");

                return true;
            }
            catch (System.Exception ex)
            {
                _log.LogError($"Error procesando polígono [{id}]", ex);
                return false;
            }
        }

        /// <summary>
        /// Genera el cuadro técnico y lo inserta en el dibujo.
        /// Solicita punto de inserción al usuario.
        /// </summary>
        private void GenerarCuadroTecnico(
            EdificacionCatastral edificacion,
            ContextoCAD          ctx,
            Transaction          tr)
        {
            ctx.Escribir("Especifique punto de inserción del cuadro técnico:");

            var optPt = new PromptPointOptions("\nPunto de inserción: ")
            {
                AllowNone = true
            };

            var resPt = ctx.Editor.GetPoint(optPt);
            if (resPt.Status != PromptStatus.OK)
            {
                ctx.EscribirAdvertencia(
                    "Punto de inserción no especificado. " +
                    "Cuadro no insertado.");
                return;
            }

            // ── Construir layout del cuadro ────────────────────
            var config = new ConfiguracionCuadro
            {
                OrigenX    = resPt.Value.X,
                OrigenY    = resPt.Value.Y,
                AnchoTotal = 8.0
            };

            var tableEngine = new DynamicTableEngine(_areaCalc);
            var filas       = tableEngine.ConstruirFilas(edificacion);

            if (!tableEngine.ValidarEstructura(filas))
            {
                ctx.EscribirAdvertencia(
                    "Estructura del cuadro incompleta. Verifique los datos.");
            }

            var layoutCalc = new TableLayoutCalculator();
            var layout     = layoutCalc.Calcular(filas, config);

            // ── Dibujar cuadro ─────────────────────────────────
            var lineEngine  = new CadLineEngine(_styleResolver);
            var mtextEngine = new MTextEngine(_styleResolver);

            var drawer = new CadTableDrawer(
                lineEngine, mtextEngine, _styleResolver, layoutCalc);

            var dibujado = drawer.DibujarCuadro(layout, ctx.Database);

            // ── Insertar entidades ─────────────────────────────
            var todasLasEntidades = dibujado.TodasLasEntidades();
            _txManager.AgregarAlModelSpaceLote(todasLasEntidades, tr, ctx.Database);

            ctx.EscribirOk(
                $"Cuadro técnico insertado: {dibujado.TotalEntidades} entidades. " +
                $"Total área: {edificacion.AreaGrandTotal:N2} m².");
        }

        /// <summary>
        /// Solicita al usuario la selección de polilíneas cerradas.
        /// Filtra automáticamente entidades que no son Polyline.
        /// </summary>
        private List<ObjectId>? SolicitarSeleccionPolilineas(Editor editor)
        {
            var filtro = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE,POLYLINE")
            });

            var opciones = new PromptSelectionOptions
            {
                MessageForAdding  = "\nSeleccione polilíneas catastrales: ",
                MessageForRemoval = "\nQuite entidades de la selección: ",
                AllowDuplicates   = false
            };

            var resultado = editor.GetSelection(opciones, filtro);

            if (resultado.Status != PromptStatus.OK)
                return null;

            return new List<ObjectId>(resultado.Value.GetObjectIds());
        }

        /// <summary>
        /// Solicita al operador: uso del ambiente + categoría normativa de
        /// Muros y Columnas + Techos según RM 277-2025-VIVIENDA Anexo I.
        /// </summary>
        private (string Uso, CategoriaMuros Muros, CategoriaTechos Techos)
            SolicitarCategorizacionNormativa(Editor editor)
        {
            // ── Paso A: Uso del ambiente (texto libre) ─────────
            var optUso = new PromptStringOptions(
                "\n[RM 277-2025] Uso del ambiente (ej: CC, DEP, COMERCIO): ")
            {
                AllowSpaces     = true,
                UseDefaultValue = false
            };
            var resUso = editor.GetString(optUso);
            if (resUso.Status != PromptStatus.OK)
                return (string.Empty, CategoriaMuros.NoDefinido, CategoriaTechos.NoDefinido);

            string uso = resUso.StringResult.Trim().ToUpperInvariant();

            // ── Paso B: Categoría Muros y Columnas ────────────
            editor.WriteMessage(
                "\n─── CATEGORÍA MUROS Y COLUMNAS (Anexo I, Col. 1) ───");
            foreach (var (cat, desc) in DescriptorCategorias.TodasCategoriasMuros())
            {
                if (cat == CategoriaMuros.NoDefinido) continue;
                editor.WriteMessage($"\n  [{cat}] {desc}");
            }

            var optMuros = new PromptStringOptions(
                "\nIngrese categoría de Muros [A/B/C/D/E/F/G/H/I]: ")
            {
                UseDefaultValue = true,
                DefaultValue    = "C"
            };
            var resMuros = editor.GetString(optMuros);
            if (resMuros.Status != PromptStatus.OK)
                return (uso, CategoriaMuros.NoDefinido, CategoriaTechos.NoDefinido);

            var catMuros = ParsearCategoriaMuros(
                resMuros.StringResult.Trim().ToUpperInvariant());

            // ── Paso C: Categoría Techos (si aplica) ──────────
            // Las categorías A y D del cuadro no usan la columna de Techos.
            CategoriaTechos catTechos = CategoriaTechos.NoDefinido;

            bool techosAplica = catMuros != CategoriaMuros.A &&
                                catMuros != CategoriaMuros.D &&
                                catMuros != CategoriaMuros.NoDefinido;

            if (techosAplica)
            {
                editor.WriteMessage(
                    "\n─── CATEGORÍA TECHOS (Anexo I, Col. 2) ───");
                foreach (var (cat, desc) in DescriptorCategorias.TodasCategoriasTechos())
                {
                    if (cat == CategoriaTechos.NoDefinido) continue;
                    editor.WriteMessage($"\n  [{cat}] {desc}");
                }

                var optTechos = new PromptStringOptions(
                    "\nIngrese categoría de Techos [A/B/C/D/E/F/G/H]: ")
                {
                    UseDefaultValue = true,
                    DefaultValue    = "C"
                };
                var resTechos = editor.GetString(optTechos);
                if (resTechos.Status == PromptStatus.OK)
                    catTechos = ParsearCategoriaTechos(
                        resTechos.StringResult.Trim().ToUpperInvariant());
            }
            else
            {
                editor.WriteMessage(
                    $"\n  (Categoría de Muros {catMuros}: columna Techos no aplica " +
                    "según RM 277-2025-VIVIENDA)");
            }

            return (uso, catMuros, catTechos);
        }

        private static CategoriaMuros ParsearCategoriaMuros(string letra) => letra switch
        {
            "A" => CategoriaMuros.A,
            "B" => CategoriaMuros.B,
            "C" => CategoriaMuros.C,
            "D" => CategoriaMuros.D,
            "E" => CategoriaMuros.E,
            "F" => CategoriaMuros.F,
            "G" => CategoriaMuros.G,
            "H" => CategoriaMuros.H,
            "I" => CategoriaMuros.I,
            _   => CategoriaMuros.NoDefinido
        };

        private static CategoriaTechos ParsearCategoriaTechos(string letra) => letra switch
        {
            "A" => CategoriaTechos.A,
            "B" => CategoriaTechos.B,
            "C" => CategoriaTechos.C,
            "D" => CategoriaTechos.D,
            "E" => CategoriaTechos.E,
            "F" => CategoriaTechos.F,
            "G" => CategoriaTechos.G,
            "H" => CategoriaTechos.H,
            _   => CategoriaTechos.NoDefinido
        };
    }
}
