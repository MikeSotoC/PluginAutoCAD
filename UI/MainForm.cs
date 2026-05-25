using System;
using System.Drawing;
using System.Windows.Forms;

using Autodesk.AutoCAD.Runtime;

using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CatastroUrbano.UI
{
    /// <summary>
    /// Formulario principal compacto estilo AutoCAD para gestión catastral
    /// Compatible con AutoCAD/Civil 3D 2012 (.NET Framework 4.0)
    /// </summary>
    public partial class MainForm : Form
    {
        private TabControl mainTabControl;

        private TabPage tabDibujo;
        private TabPage tabAnalisis;
        private TabPage tabClasificacion;
        private TabPage tabTablas;

        // ─────────────────────────────────────────────
        // DIBUJO
        // ─────────────────────────────────────────────

        private GroupBox grpHerramientasDibujo;

        private Button btnLote;
        private Button btnManzana;
        private Button btnConstruccion;
        private Button btnEditar;
        private Button btnEliminar;

        // ─────────────────────────────────────────────
        // ANÁLISIS
        // ─────────────────────────────────────────────

        private GroupBox grpAnalisis;

        private Button btnArea;
        private Button btnPerimetro;
        private Button btnValidar;
        private Button btnSuperposicion;

        // ─────────────────────────────────────────────
        // CLASIFICACIÓN
        // ─────────────────────────────────────────────

        private GroupBox grpClasificacion;

        private ComboBox cmbTipoUso;
        private Button btnClasificar;
        private ListBox lstResultados;

        // ─────────────────────────────────────────────
        // TABLAS
        // ─────────────────────────────────────────────

        private GroupBox grpTablas;

        private Button btnGenerarCatastro;
        private Button btnExportarExcel;
        private Button btnReporte;

        // ─────────────────────────────────────────────
        // STATUS BAR
        // ─────────────────────────────────────────────

        private StatusStrip statusBar;
        private ToolStripStatusLabel lblEstado;
        private ToolStripProgressBar progressBar;

        // ─────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────

        public MainForm()
        {
            InitializeComponent();
            InicializarFormulario();
            InicializarTabs();
            InicializarStatusBar();
        }

        // ─────────────────────────────────────────────
        // INICIALIZACIÓN GENERAL
        // ─────────────────────────────────────────────

        private void InicializarFormulario()
        {
            this.Text = "Catastro Urbano v1.0";
            this.Size = new Size(450, 550);

            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            this.MaximizeBox = false;
            this.MinimizeBox = false;

            this.StartPosition = FormStartPosition.CenterScreen;

            this.Font = new Font(
                "Segoe UI",
                9F,
                FontStyle.Regular,
                GraphicsUnit.Point,
                ((byte)(0))
            );

            this.BackColor = Color.White;
        }

        private void InicializarTabs()
        {
            mainTabControl = new TabControl();

            mainTabControl.Dock = DockStyle.Fill;
            mainTabControl.Appearance = TabAppearance.Normal;
            mainTabControl.SizeMode = TabSizeMode.Normal;

            tabDibujo = new TabPage("Dibujo");
            tabAnalisis = new TabPage("Análisis");
            tabClasificacion = new TabPage("Clasificación");
            tabTablas = new TabPage("Tablas");

            mainTabControl.TabPages.Add(tabDibujo);
            mainTabControl.TabPages.Add(tabAnalisis);
            mainTabControl.TabPages.Add(tabClasificacion);
            mainTabControl.TabPages.Add(tabTablas);

            this.Controls.Add(mainTabControl);

            InitDibujoTab();
            InitAnalisisTab();
            InitClasificacionTab();
            InitTablasTab();
        }

        private void InicializarStatusBar()
        {
            statusBar = new StatusStrip();

            lblEstado = new ToolStripStatusLabel();
            lblEstado.Text = "Listo";

            progressBar = new ToolStripProgressBar();
            progressBar.Visible = false;

            statusBar.Items.Add(lblEstado);
            statusBar.Items.Add(progressBar);

            this.Controls.Add(statusBar);
        }

        // ─────────────────────────────────────────────
        // TAB DIBUJO
        // ─────────────────────────────────────────────

        private void InitDibujoTab()
        {
            FlowLayoutPanel panel = CrearPanelBase();

            grpHerramientasDibujo = new GroupBox();
            grpHerramientasDibujo.Text = "Herramientas";
            grpHerramientasDibujo.Dock = DockStyle.Top;
            grpHerramientasDibujo.Height = 220;
            grpHerramientasDibujo.FlatStyle = FlatStyle.Flat;

            btnLote = CreateButton(
                "📍 Lote",
                "Dibujar lote",
                Color.FromArgb(0, 120, 215)
            );

            btnManzana = CreateButton(
                "🏘 Manzana",
                "Dibujar manzana",
                Color.FromArgb(0, 120, 215)
            );

            btnConstruccion = CreateButton(
                "🏢 Construcción",
                "Dibujar construcción",
                Color.FromArgb(0, 120, 215)
            );

            btnEditar = CreateButton(
                "✏ Editar",
                "Editar elemento",
                Color.FromArgb(255, 140, 0)
            );

            btnEliminar = CreateButton(
                "🗑 Eliminar",
                "Eliminar elemento",
                Color.FromArgb(220, 53, 69)
            );

            AddButtonsToGroup(
                grpHerramientasDibujo,
                btnLote,
                btnManzana,
                btnConstruccion,
                btnEditar,
                btnEliminar
            );

            panel.Controls.Add(grpHerramientasDibujo);

            tabDibujo.Controls.Add(panel);
        }

        // ─────────────────────────────────────────────
        // TAB ANÁLISIS
        // ─────────────────────────────────────────────

        private void InitAnalisisTab()
        {
            FlowLayoutPanel panel = CrearPanelBase();

            grpAnalisis = new GroupBox();
            grpAnalisis.Text = "Análisis Geométrico";
            grpAnalisis.Dock = DockStyle.Top;
            grpAnalisis.Height = 190;
            grpAnalisis.FlatStyle = FlatStyle.Flat;

            btnArea = CreateButton(
                "📐 Área",
                "Calcular área",
                Color.FromArgb(40, 167, 69)
            );

            btnPerimetro = CreateButton(
                "📏 Perímetro",
                "Calcular perímetro",
                Color.FromArgb(40, 167, 69)
            );

            btnValidar = CreateButton(
                "✓ Validar",
                "Validar geometría",
                Color.FromArgb(255, 193, 7)
            );

            btnSuperposicion = CreateButton(
                "🔍 Superposiciones",
                "Detectar superposiciones",
                Color.FromArgb(220, 53, 69)
            );

            AddButtonsToGroup(
                grpAnalisis,
                btnArea,
                btnPerimetro,
                btnValidar,
                btnSuperposicion
            );

            panel.Controls.Add(grpAnalisis);

            tabAnalisis.Controls.Add(panel);
        }

        // ─────────────────────────────────────────────
        // TAB CLASIFICACIÓN
        // ─────────────────────────────────────────────

        private void InitClasificacionTab()
        {
            FlowLayoutPanel panel = CrearPanelBase();

            grpClasificacion = new GroupBox();
            grpClasificacion.Text = "Clasificación de Uso";
            grpClasificacion.Dock = DockStyle.Top;
            grpClasificacion.Height = 260;
            grpClasificacion.FlatStyle = FlatStyle.Flat;

            Label lblTipo = new Label();

            lblTipo.Text = "Tipo de Uso:";
            lblTipo.AutoSize = true;
            lblTipo.Location = new Point(10, 25);

            cmbTipoUso = new ComboBox();

            cmbTipoUso.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTipoUso.Width = 380;
            cmbTipoUso.Location = new Point(10, 50);

            cmbTipoUso.Items.AddRange(
                new object[]
                {
                    "Residencial",
                    "Comercial",
                    "Industrial",
                    "Mixto",
                    "Equipamiento",
                    "Espacio Público",
                    "Agrícola",
                    "Otros"
                }
            );

            cmbTipoUso.SelectedIndex = 0;

            btnClasificar = CreateButton(
                "🏷 Clasificar",
                "Aplicar clasificación",
                Color.FromArgb(0, 120, 215)
            );

            btnClasificar.Location = new Point(10, 90);

            lstResultados = new ListBox();

            lstResultados.Width = 380;
            lstResultados.Height = 100;

            lstResultados.Location = new Point(10, 140);

            grpClasificacion.Controls.Add(lblTipo);
            grpClasificacion.Controls.Add(cmbTipoUso);
            grpClasificacion.Controls.Add(btnClasificar);
            grpClasificacion.Controls.Add(lstResultados);

            panel.Controls.Add(grpClasificacion);

            tabClasificacion.Controls.Add(panel);
        }

        // ─────────────────────────────────────────────
        // TAB TABLAS
        // ─────────────────────────────────────────────

        private void InitTablasTab()
        {
            FlowLayoutPanel panel = CrearPanelBase();

            grpTablas = new GroupBox();
            grpTablas.Text = "Generación de Tablas";
            grpTablas.Dock = DockStyle.Top;
            grpTablas.Height = 160;
            grpTablas.FlatStyle = FlatStyle.Flat;

            btnGenerarCatastro = CreateButton(
                "📊 Tabla Catastral",
                "Generar tabla catastral",
                Color.FromArgb(0, 120, 215)
            );

            btnExportarExcel = CreateButton(
                "📈 Exportar Excel",
                "Exportar a Excel",
                Color.FromArgb(40, 167, 69)
            );

            btnReporte = CreateButton(
                "📄 Reporte",
                "Generar reporte PDF",
                Color.FromArgb(108, 117, 125)
            );

            AddButtonsToGroup(
                grpTablas,
                btnGenerarCatastro,
                btnExportarExcel,
                btnReporte
            );

            panel.Controls.Add(grpTablas);

            tabTablas.Controls.Add(panel);
        }

        // ─────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────

        private FlowLayoutPanel CrearPanelBase()
        {
            FlowLayoutPanel panel = new FlowLayoutPanel();

            panel.Dock = DockStyle.Fill;

            panel.FlowDirection = FlowDirection.TopDown;

            panel.WrapContents = false;

            panel.Padding = new Padding(10);

            panel.BackColor = Color.White;

            return panel;
        }

        private Button CreateButton(
            string text,
            string tooltip,
            Color backColor
        )
        {
            Button btn = new Button();

            btn.Text = text;

            btn.Width = 380;
            btn.Height = 35;

            btn.FlatStyle = FlatStyle.Flat;

            btn.FlatAppearance.BorderSize = 0;

            btn.BackColor = backColor;

            btn.ForeColor = Color.White;

            btn.Font = new Font(
                "Segoe UI",
                9.5F,
                FontStyle.Regular
            );

            btn.TextAlign = ContentAlignment.MiddleLeft;

            btn.Padding = new Padding(15, 0, 0, 0);

            ToolTip toolTip = new ToolTip();

            toolTip.SetToolTip(btn, tooltip);

            btn.MouseEnter += delegate
            {
                btn.BackColor = ControlPaint.Light(backColor);
            };

            btn.MouseLeave += delegate
            {
                btn.BackColor = backColor;
            };

            return btn;
        }

        private void AddButtonsToGroup(
            GroupBox group,
            params Button[] buttons
        )
        {
            int y = 25;

            foreach (Button btn in buttons)
            {
                btn.Location = new Point(10, y);

                group.Controls.Add(btn);

                y += btn.Height + 8;
            }

            group.Height = y + 10;
        }

        // ─────────────────────────────────────────────
        // COMANDOS AUTOCAD
        // ─────────────────────────────────────────────

        [CommandMethod("CATASTRO_UI")]
        public static void ShowMainForm()
        {
            AcApp.ShowModalDialog(new MainForm());
        }

        [CommandMethod("CU")]
        public static void ShowMainFormAlias()
        {
            ShowMainForm();
        }
    }
}