using System.Windows.Forms;

namespace CatastroUrbano.UI
{
    /// <summary>
    /// Clase de inicialización para formularios WinForms
    /// Required para usar controles WinForms en aplicaciones .NET Framework
    /// </summary>
    public static class UIInitializer
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void Initialize()
        {
            // Habilitar estilos visuales para mejor apariencia
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(true);
        }
    }
}
