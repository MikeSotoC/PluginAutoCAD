using System;
using System.Windows.Forms;
using System.Drawing;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;

namespace CatastroUrbano.UI
{
    /// <summary>
    /// Formulario principal compacto estilo AutoCAD para gestión catastral
    /// </summary>
    public partial class MainForm : Form
    {
        private TabControl mainTabControl;
        private TabPage tabDibujo;
        private TabPage tabAnalisis;
        private TabPage tabClasificacion;
        private TabPage tabTablas;
        
        // Panel de herramientas de dibujo
        private GroupBox grpHerramientasDibujo;
        private Button btnLote;
        private Button btnManzana;
        private Button btnConstruccion;
        private Button btnEditar;
        private Button btnEliminar;
        
        // Panel de análisis
        private GroupBox grpAnalisis;
        private Button btnArea;
        private Button btnPerimetro;
        private Button btnValidar;
        private Button btnSuperposicion;
        
        // Panel de clasificación
        private GroupBox grpClasificacion;
        private ComboBox cmbTipoUso;
        private Button btnClasificar;
        private ListBox lstResultados;
        
        // Panel de tablas
        private GroupBox grpTablas;
        private Button btnGenerarCatastro;
        private Button btnExportarExcel;
        private Button btnReporte;
        
        // Barra de estado
        private StatusStrip statusBar;
        private ToolStripStatusLabel lblEstado;
        private ToolStripProgressBar progressBar;

        public MainForm()
        {
            InitializeComponent();
            this.Text = "Catastro Urbano v1.0";
            this.Size = new System.Drawing.Size(450, 550);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        }

        private void InitDibujoTab()
        {
            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.FlowDirection = FlowDirection.TopDown;
            panel.WrapContents = false;
            panel.Padding = new Padding(10);
            panel.BackColor = System.Drawing.Color.White;
            
            grpHerramientasDibujo = new GroupBox("Herramientas");
            grpHerramientasDibujo.Dock = DockStyle.Top;
            grpHerramientasDibujo.Height = 180;
            grpHerramientasDibujo.FlatStyle = FlatStyle.Flat;
            
            btnLote = CreateButton("📍 Lote", "Dibujar lote", Color.FromArgb(0, 120, 215));
            btnManzana = CreateButton("🏘️ Manzana", "Dibujar manzana", Color.FromArgb(0, 120, 215));
            btnConstruccion = CreateButton("🏢 Construcción", "Dibujar construcción", Color.FromArgb(0, 120, 215));
            btnEditar = CreateButton("✏️ Editar", "Editar elemento", Color.FromArgb(255, 140, 0));
            btnEliminar = CreateButton("🗑️ Eliminar", "Eliminar elemento", Color.FromArgb(220, 53, 69));
            
            AddButtonsToGroup(grpHerramientasDibujo, btnLote, btnManzana, btnConstruccion, btnEditar, btnEliminar);
            panel.Controls.Add(grpHerramientasDibujo);
            
            tabDibujo.Controls.Add(panel);
        }

        private void InitAnalisisTab()
        {
            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.FlowDirection = FlowDirection.TopDown;
            panel.WrapContents = false;
            panel.Padding = new Padding(10);
            panel.BackColor = System.Drawing.Color.White;
            
            grpAnalisis = new GroupBox("Análisis Geométrico");
            grpAnalisis.Dock = DockStyle.Top;
            grpAnalisis.Height = 160;
            grpAnalisis.FlatStyle = FlatStyle.Flat;
            
            btnArea = CreateButton("📐 Área", "Calcular área", Color.FromArgb(40, 167, 69));
            btnPerimetro = CreateButton("📏 Perímetro", "Calcular perímetro", Color.FromArgb(40, 167, 69));
            btnValidar = CreateButton("✓ Validar", "Validar geometría", Color.FromArgb(255, 193, 7));
            btnSuperposicion = CreateButton("🔍 Superposiciones", "Detectar superposiciones", Color.FromArgb(220, 53, 69));
            
            AddButtonsToGroup(grpAnalisis, btnArea, btnPerimetro, btnValidar, btnSuperposicion);
            panel.Controls.Add(grpAnalisis);
            
            tabAnalisis.Controls.Add(panel);
        }

        private void InitClasificacionTab()
        {
            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.FlowDirection = FlowDirection.TopDown;
            panel.WrapContents = false;
            panel.Padding = new Padding(10);
            panel.BackColor = System.Drawing.Color.White;
            
            grpClasificacion = new GroupBox("Clasificación de Uso");
            grpClasificacion.Dock = DockStyle.Top;
            grpClasificacion.Height = 220;
            grpClasificacion.FlatStyle = FlatStyle.Flat;
            
            Label lblTipo = new Label();
            lblTipo.Text = "Tipo de Uso:";
            lblTipo.AutoSize = true;
            lblTipo.Margin = new Padding(5, 5, 5, 2);
            
            cmbTipoUso = new ComboBox();
            cmbTipoUso.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTipoUso.Width = 380;
            cmbTipoUso.Items.AddRange(new object[] { 
                "Residencial", "Comercial", "Industrial", 
                "Mixto", "Equipamiento", "Espacio Público", 
                "Agrícola", "Otros" 
            });
            cmbTipoUso.SelectedIndex = 0;
            cmbTipoUso.Margin = new Padding(5, 2, 5, 10);
            
            btnClasificar = CreateButton("🏷️ Clasificar", "Aplicar clasificación", Color.FromArgb(0, 120, 215));
            btnClasificar.Width = 380;
            
            lstResultados = new ListBox();
            lstResultados.Height = 100;
            lstResultados.Width = 380;
            lstResultados.BorderStyle = BorderStyle.FixedSingle;
            
            grpClasificacion.Controls.Add(lstResultados);
            lstResultados.Location = new System.Drawing.Point(10, 110);
            
            AddControlsToGroup(grpClasificacion, lblTipo, cmbTipoUso, btnClasificar);
            panel.Controls.Add(grpClasificacion);
            
            tabClasificacion.Controls.Add(panel);
        }

        private void InitTablasTab()
        {
            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.FlowDirection = FlowDirection.TopDown;
            panel.WrapContents = false;
            panel.Padding = new Padding(10);
            panel.BackColor = System.Drawing.Color.White;
            
            grpTablas = new GroupBox("Generación de Tablas");
            grpTablas.Dock = DockStyle.Top;
            grpTablas.Height = 150;
            grpTablas.FlatStyle = FlatStyle.Flat;
            
            btnGenerarCatastro = CreateButton("📊 Tabla Catastral", "Generar tabla catastral", Color.FromArgb(0, 120, 215));
            btnExportarExcel = CreateButton("📈 Exportar Excel", "Exportar a Excel", Color.FromArgb(40, 167, 69));
            btnReporte = CreateButton("📄 Reporte", "Generar reporte PDF", Color.FromArgb(108, 117, 125));
            
            AddButtonsToGroup(grpTablas, btnGenerarCatastro, btnExportarExcel, btnReporte);
            panel.Controls.Add(grpTablas);
            
            tabTablas.Controls.Add(panel);
        }

        private Button CreateButton(string text, string tooltip, Color backColor)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Width = 380;
            btn.Height = 35;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = backColor;
            btn.ForeColor = Color.White;
            btn.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
            btn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(15, 0, 0, 0);
            
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(btn, tooltip);
            
            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Lighter(backColor, 0.1F);
            btn.MouseLeave += (s, e) => btn.BackColor = backColor;
            
            return btn;
        }

        private void AddButtonsToGroup(GroupBox group, params Button[] buttons)
        {
            int y = 20;
            foreach (var btn in buttons)
            {
                btn.Location = new System.Drawing.Point(10, y);
                group.Controls.Add(btn);
                y += btn.Height + 8;
            }
            group.Height = y + 10;
        }

        private void AddControlsToGroup(GroupBox group, params Control[] controls)
        {
            int y = 20;
            foreach (var ctrl in controls)
            {
                if (ctrl is Button)
                {
                    ctrl.Location = new System.Drawing.Point(10, y);
                    y += ctrl.Height + 8;
                }
                else
                {
                    ctrl.Location = new System.Drawing.Point(10, y);
                    y += ctrl.Height + 2;
                }
                group.Controls.Add(ctrl);
            }
        }

        // Comandos de AutoCAD para mostrar la UI
        [CommandMethod("CATASTRO_UI")]
        public static void ShowMainForm()
        {
            UIInitializer.Initialize();
            Application.ShowModalDialog(new MainForm());
        }
        
        // Alias corto
        [CommandMethod("CU")]
        public static void ShowMainFormAlias()
        {
            ShowMainForm();
        }
    }
}
