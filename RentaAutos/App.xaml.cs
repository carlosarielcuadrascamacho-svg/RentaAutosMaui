using Microsoft.Extensions.DependencyInjection;

namespace RentaAutos
{
    // La clase raíz y maestra de toda tu aplicación. 
    // Es lo primero que "despierta" a nivel de interfaz visual después de que el archivo MauiProgram.cs configura los servicios.
    public partial class App : Application
    {
        // Variable de solo lectura para guardar la estructura principal de navegación de tu app (tu AppShell o TabbedPage).
        private readonly AppShell _appShell;

        // ¿QUIÉN LO LLAMA?: El motor interno de .NET MAUI justo al abrir la app en el celular.
        // Gracias a la Inyección de Dependencias, MAUI crea el 'AppShell' automáticamente 
        // en la memoria y te lo "inyecta" pasándolo por los paréntesis de este constructor.
        public App(AppShell appShell)
        {
            // Inicializa los diccionarios de recursos globales definidos en el archivo App.xaml 
            // (como colores primarios, estilos globales y fuentes personalizadas).
            InitializeComponent();

            // Guardamos el "esqueleto" de navegación de la app en nuestra variable global.
            _appShell = appShell;
        }

        // ========================================================================
        // GESTIÓN DE VENTANAS
        // ========================================================================

        // ¿QUÉ HACE ESTE MÉTOD0?: A diferencia de las apps móviles antiguas, MAUI es multi-plataforma.
        // Esto significa que tu app puede correr en Windows o Mac, donde puedes abrir "Múltiples Ventanas".
        // Este método se encarga de fabricar la primera ventana de la aplicación.
        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Toma nuestro contenedor maestro (_appShell, que adentro tiene tus pestañas de 
            // Galería, Registro y Reportes) y lo envuelve adentro de una "Window" (Ventana)
            // del sistema operativo para poder proyectarlo en la pantalla del usuario.
            return new Window(_appShell);
        }
    }
}