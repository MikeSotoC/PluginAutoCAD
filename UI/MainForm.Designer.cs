using System.Windows.Forms;
using System.Drawing;

namespace CatastroUrbano.UI
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 550);
            this.Text = "Catastro Urbano v1.0";
            
            // Configurar TabControl
            mainTabControl = new TabControl();
            mainTabControl.Dock = DockStyle.Fill;
            mainTabControl.Appearance = TabAppearance.FlatButtons;
            mainTabControl.ItemSize = new System.Drawing.Size(80, 25);
            mainTabControl.SizeMode = TabSizeMode.Fixed;
            
            // Crear pestañas
            tabDibujo = new TabPage("Dibujo");
            tabAnalisis = new TabPage("Análisis");
            tabClasificacion = new TabPage("Clasificación");
            tabTablas = new TabPage("Tablas");
            
            mainTabControl.Controls.Add(tabDibujo);
            mainTabControl.Controls.Add(tabAnalisis);
            mainTabControl.Controls.Add(tabClasificacion);
            mainTabControl.Controls.Add(tabTablas);
            
            // Inicializar componentes de cada pestaña
            InitDibujoTab();
            InitAnalisisTab();
            InitClasificacionTab();
            InitTablasTab();
            
            // Barra de estado
            statusBar = new StatusStrip();
            lblEstado = new ToolStripStatusLabel("Listo");
            progressBar = new ToolStripProgressBar();
            progressBar.Visible = false;
            
            statusBar.Items.Add(lblEstado);
            statusBar.Items.Add(progressBar);
            
            // Agregar controles al formulario
            this.Controls.Add(mainTabControl);
            this.Controls.Add(statusBar);
        }


        #endregion
    }
}
