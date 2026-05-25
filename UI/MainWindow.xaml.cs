using System.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CatastroUrbano.UI
{
    /// <summary>
    /// Ventana principal WPF para gestión catastral
    /// Compatible con AutoCAD/Civil 3D 2012+ (.NET Framework 4.8)
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InicializarComboBox();
        }

        private void InicializarComboBox()
        {
            // El ComboBox ya está inicializado en XAML con los items
            // Solo aseguramos que el índice seleccionado sea 0
            cmbTipoUso.SelectedIndex = 0;
        }

        // ─────────────────────────────────────────────
        // EVENT HANDLERS - TAB DIBUJO
        // ─────────────────────────────────────────────

        private void BtnLote_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("Dibujando lote...");
            // TODO: Implementar lógica de dibujo de lote
        }

        private void BtnManzana_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("Dibujando manzana...");
            // TODO: Implementar lógica de dibujo de manzana
        }

        private void BtnConstruccion_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("Dibujando construcción...");
            // TODO: Implementar lógica de dibujo de construcción
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("Editando elemento...");
            // TODO: Implementar lógica de edición
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("Eliminando elemento...");
            // TODO: Implementar lógica de eliminación
        }

        // ─────────────────────────────────────────────
        // EVENT HANDLERS - TAB ANÁLISIS
        // ─────────────────────────────────────────────

        private void BtnArea_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("Calculando área...");
            // TODO: Implementar cálculo de área
        }

        private void BtnPerimetro_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("Calculando perímetro...");
            // TODO: Implementar cálculo de perímetro
        }

        private void BtnValidar_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("Validando geometría...");
            // TODO: Implementar validación de geometría
        }

        private void BtnSuperposicion_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("Detectando superposiciones...");
            // TODO: Implementar detección de superposiciones
        }

        // ─────────────────────────────────────────────
        // EVENT HANDLERS - TAB CLASIFICACIÓN
        // ─────────────────────────────────────────────

        private void BtnClasificar_Click(object sender, RoutedEventArgs e)
        {
            string tipoUso = ObtenerTipoUsoSeleccionado();
            ActualizarEstado($"Clasificando como: {tipoUso}");
            
            lstResultados.Items.Add($"Clasificado: {tipoUso} - {System.DateTime.Now:HH:mm:ss}");
        }

        private string ObtenerTipoUsoSeleccionado()
        {
            if (cmbTipoUso.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString() ?? "Desconocido";
            }
            return "Desconocido";
        }

        // ─────────────────────────────────────────────
        // EVENT HANDLERS - TAB TABLAS
        // ─────────────────────────────────────────────

        private void BtnGenerarCatastro_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("Generando tabla catastral...");
            // TODO: Implementar generación de tabla catastral
        }

        private void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("Exportando a Excel...");
            // TODO: Implementar exportación a Excel
        }

        private void BtnReporte_Click(object sender, RoutedEventArgs e)
        {
            ActualizarEstado("Generando reporte PDF...");
            // TODO: Implementar generación de reporte PDF
        }

        // ─────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────

        private void ActualizarEstado(string mensaje)
        {
            lblEstado.Text = mensaje;
        }

        private void MostrarProgressBar(bool visible)
        {
            progressBar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        // ─────────────────────────────────────────────
        // COMANDOS AUTOCAD
        // ─────────────────────────────────────────────

        [CommandMethod("CATASTRO_UI_WPF")]
        public static void ShowMainWindow()
        {
            var window = new MainWindow();
            
            // Usar ShowModalWindow para ventanas WPF en AutoCAD
            AcApp.ShowModalWindow(Application.Current.MainWindow, window, true);
        }

        [CommandMethod("CU_WPF")]
        public static void ShowMainWindowAlias()
        {
            ShowMainWindow();
        }
    }
}
