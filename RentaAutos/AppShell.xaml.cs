using RentaAutos.Views;

namespace RentaAutos;

// Contenedor principal de navegación de tu aplicación.
// Al heredar de TabbedPage, esta clase es la responsable absoluta 
// de dibujar y gestionar la barra de pestañas inferior (Bottom Tab Bar).
public partial class AppShell : TabbedPage
{
    // ¿QUIÉN LO LLAMA?: El motor de Inyección de Dependencias (DI) configurado en MauiProgram.cs.
    // Al arrancar, MAUI fabrica las tres pantallas (Vistas) en la memoria RAM, 
    // les conecta sus Controladores, y finalmente inyecta las pantallas listas en este constructor.
    public AppShell(GaleriaView galeria, RegistroRentaView registro, ReportesView reportes)
    {
        // Inicializa cualquier estilo o color de fondo definido en AppShell.xaml
        InitializeComponent();

        // ========================================================================
        // CONFIGURACIÓN VISUAL DE LAS PESTAÑAS (TABS)
        // ========================================================================

        // Le asignamos sus títulos e íconos (Textos limpios y directos para buena UX).
        // MAUI buscará automáticamente estos íconos (archivos PNG/SVG) en la carpeta Resources/Images 
        // y los adaptará a la resolución del celular.

        // Pestaña 0 (Izquierda): Catálogo de vehículos
        galeria.Title = "Galería";
        galeria.IconImageSource = "car_icon.png";

        // Pestaña 1 (Centro): Formulario de cotización y guardado
        registro.Title = "Registro";
        registro.IconImageSource = "edit_icon.png";

        // Pestaña 2 (Derecha): Auditoría de la base de datos
        reportes.Title = "Reportes";
        reportes.IconImageSource = "report_icon.png";

        // ========================================================================
        // ENSAMBLAJE FINAL
        // ========================================================================

        // Agregamos las vistas ya configuradas a la colección 'Children' del TabbedPage.
        // El orden en el que hacemos el 'Add' dictará el orden de los botones de izquierda a derecha
        // en la pantalla del celular.
        Children.Add(galeria);
        Children.Add(registro);
        Children.Add(reportes);

        // NOTA ARQUITECTÓNICA: Gracias a que armaste el contenedor de esta forma tan limpia, 
        // en GaleriaView pudimos hacer el truco de 'tabbedPage.Children[1]' para saltar mágicamente 
        // a la pantalla de Registro pasando el auto seleccionado.
    }
}