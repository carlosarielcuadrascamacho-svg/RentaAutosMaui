using RentaAutos.Controllers;
using System;
using System.Linq;

namespace RentaAutos.Views
{
    // Clase de interfaz de usuario (Vista) para el catálogo de carros.
    public partial class GaleriaView : ContentPage
    {
        // Conexión segura al Controlador que orquesta la inteligencia de negocio.
        private readonly GaleriaController _controller;

        // El motor de MAUI (en MauiProgram.cs) llama a este constructor al abrir la app.
        // Inyecta automáticamente el GaleriaController que necesita esta vista.
        public GaleriaView(GaleriaController controller)
        {
            // Inicializa los componentes visuales dibujados en el archivo XAML.
            InitializeComponent();
            _controller = controller;
        }

        // ========================================================
        // 1. CARGA DE DATOS (CICLO DE VIDA DE LA PANTALLA)
        // ========================================================

        // Este evento nativo de MAUI se dispara automáticamente justo antes 
        // de que la pantalla se vuelva visible para el usuario (cada vez que tocan la pestaña).
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // La Vista le pide al Controlador que cruce los datos de autos y rentas.
                // ¿A DÓNDE PIDE?: Va a Controllers -> GaleriaController.cs.
                var listaDeAutos = await _controller.ObtenerAutosParaGaleriaAsync();

                // ¿A DÓNDE MANDA?: Le inyecta la lista resultante a la propiedad 'ItemsSource' 
                // del 'CollectionView' (llamado ListaAutos) en el archivo XAML.
                // Con esto, la pantalla dibuja las tarjetas, las fotos y los colores al instante.
                ListaAutos.ItemsSource = listaDeAutos;
            }
            catch (Exception ex)
            {
                // Manejo de errores defensivo: Si no hay internet o MongoDB falla, se avisa al usuario sin crashear.
                await DisplayAlert("Error", "No se pudo conectar a MongoDB: " + ex.Message, "OK");
            }
        }

        // ========================================================
        // 2. INTERACCIÓN Y NAVEGACIÓN (PASO DE DATOS ENTRE TABS)
        // ========================================================

        // Este evento se dispara desde el XAML cuando el usuario toca la tarjeta de un carro.
        private async void OnAutoSeleccionado(object sender, SelectionChangedEventArgs e)
        {
            // Revisa si el elemento que se tocó es realmente un objeto de tipo 'Auto'.
            // FirstOrDefault() asegura que no explote si el usuario toca un espacio vacío.
            if (e.CurrentSelection.FirstOrDefault() is RentaAutos.Models.Auto autoSeleccionado)
            {
                // Inmediatamente quitamos el color de "seleccionado" de la tarjeta.
                // Esto permite que el usuario pueda volver a tocar el mismo carro más tarde.
                ListaAutos.SelectedItem = null;

                // REGLA DE NEGOCIO: Avisar disponibilidad.
                // Si el Controlador calculó que el carro está rentado hoy (Color Rojo),
                // le mostramos una alerta al usuario antes de mandarlo a la pantalla de Registro.
                if (autoSeleccionado.EstaRentadoHoy)
                {
                    await DisplayAlert("Ocupado el día de hoy",
                        "Este auto se encuentra rentado actualmente. Serás redirigido al Registro donde podrás apartarlo para fechas futuras.",
                        "Entendido");
                }

                // ========================================================
                // NAVEGACIÓN INTELIGENTE (CROSS-TAB COMMUNICATION)
                // ========================================================

                // Las pestañas en MAUI no están conectadas directamente entre sí.
                // Buscamos el contenedor "padre" de la app (el TabbedPage) para usarlo como puente de comunicación.
                if (Application.Current.MainPage is TabbedPage tabbedPage)
                {
                    // Buscamos a la pestaña número 2 (Índice 1), que sabemos que es la pantalla de Registro.
                    if (tabbedPage.Children[1] is RegistroRentaView registroView)
                    {
                        // ¿A DÓNDE MANDA?: Le entregamos físicamente el objeto 'autoSeleccionado' 
                        // a un método público dentro de RegistroRentaView.xaml.cs.
                        registroView.RecibirAutoSeleccionado(autoSeleccionado);

                        // Obligamos a la aplicación a cambiar de pestaña automáticamente,
                        // llevando al usuario directo a la pantalla de cobro.
                        tabbedPage.CurrentPage = registroView;
                    }
                }
            }
        }
    }
}