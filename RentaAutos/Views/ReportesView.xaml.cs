using RentaAutos.Controllers;
using RentaAutos.Models;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace RentaAutos.Views;

// Clase de interfaz de usuario (Vista) para el panel de administración y auditoría.
public partial class ReportesView : ContentPage
{
    // Conexión segura al Controlador que orquesta las consultas a la base de datos
    private readonly ReportesController _controller;

    // El motor de MAUI llama a este constructor al abrir la app.
    // Inyecta automáticamente el ReportesController.
    public ReportesView(ReportesController controller)
    {
        InitializeComponent();
        _controller = controller;

        // UX INTELIGENTE: Por defecto, configuramos los filtros del calendario visual
        // para mostrar desde hace un mes exacto, hasta un mes en el futuro.
        // Esto evita que la pantalla cargue vacía la primera vez.
        dpFiltroInicio.Date = DateTime.Today.AddMonths(-1);
        dpFiltroFin.Date = DateTime.Today.AddMonths(+1);
    }

    // ========================================================================
    // CICLO DE VIDA Y EVENTOS DE BOTONES
    // ========================================================================

    // Este evento nativo se ejecuta automáticamente cada vez que el gerente 
    // toca la pestaña de "Reportes" en la aplicación.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Disparamos la carga inicial de datos.
        await CargarHistorialAsync();
    }

    // Evento disparado por el botón azul de "Buscar" en el XAML.
    private async void OnBuscarClicked(object sender, EventArgs e)
    {
        await CargarHistorialAsync();
    }

    // Evento disparado por el botón de "Limpiar" o "Reset" en el XAML.
    private async void OnLimpiarClicked(object sender, EventArgs e)
    {
        // 1. Limpiamos visualmente los campos de texto
        txtFiltroPlaca.Text = string.Empty;

        // 2. Reseteamos los DatePickers a sus valores por defecto
        dpFiltroInicio.Date = DateTime.Today.AddMonths(-1);
        dpFiltroFin.Date = DateTime.Today.AddMonths(+1);

        // 3. ¿QUÉ PIDE?: Le pide al Controlador que traiga el historial absoluto.
        // Al mandarle puros nulos, el Controlador entiende que debe ignorar los filtros
        // y ejecutar un "SELECT *" en MongoDB.
        var rentasLimpias = await _controller.ObtenerHistorialRentasAsync(null, null, null);

        // 4. Actualizamos la lista en pantalla
        ListaHistorial.ItemsSource = rentasLimpias;
    }

    // ========================================================================
    // LÓGICA DE EXTRACCIÓN Y FILTRADO
    // ========================================================================

    // Método central que recolecta lo que el usuario escribió, lo valida
    // y se lo manda al Controlador para pedir los datos a la nube.
    private async Task CargarHistorialAsync()
    {
        // PREPARACIÓN DE LA PLACA
        string placa;
        if (string.IsNullOrWhiteSpace(txtFiltroPlaca.Text))
        {
            // Si el campo está vacío, enviamos null para que el controlador no filtre por placa.
            placa = null;
        }
        else
        {
            // Si hay texto, le quitamos espacios fantasma (Trim) y forzamos mayúsculas (ToUpper)
            // porque las placas en MongoDB están guardadas en mayúsculas.
            placa = txtFiltroPlaca.Text.Trim().ToUpper();
        }

        // PREPARACIÓN DE LAS FECHAS
        // Declaramos las variables como DateTime? (con el signo de interrogación)
        // para indicar que pueden aceptar valores nulos.
        DateTime? fechaInicio = dpFiltroInicio.Date;
        DateTime? fechaFin = dpFiltroFin.Date;

        // Validamos la lógica del tiempo usando .HasValue de forma segura.
        if (fechaInicio.HasValue && fechaFin.HasValue)
        {
            // GUARD CLAUSE (Seguridad): No puedes buscar desde el 20 de Marzo hasta el 10 de Marzo.
            if (fechaFin.Value < fechaInicio.Value)
            {
                await DisplayAlert("Rango Inválido", "La fecha 'Hasta' no puede ser menor a la fecha 'Desde'.", "OK");
                return; // Detenemos la ejecución si el usuario intentó romper la búsqueda
            }
        }

        // ¿A DÓNDE MANDA?: Nuestro ReportesController ya estaba diseñado como un tanque
        // y de hecho estaba esperando recibir nulos (DateTime?), así que se lo pasamos directo.
        var rentas = await _controller.ObtenerHistorialRentasAsync(placa, fechaInicio, fechaFin);

        // ¿HACIA DÓNDE VA?: Asignamos la respuesta filtrada de MongoDB a la propiedad 'ItemsSource'
        // del CollectionView (ListaHistorial) para que dibuje las tarjetas de expedientes.
        ListaHistorial.ItemsSource = rentas;
    }

    // ========================================================================
    // INTERACCIÓN DEL USUARIO (GESTIÓN Y FINALIZACIÓN DE RENTA)
    // ========================================================================

    // Este evento se dispara desde el XAML cuando el gerente toca la tarjeta de un expediente.
    private async void OnRentaSeleccionada(object sender, SelectionChangedEventArgs e)
    {
        // Verificamos de forma segura que lo que se tocó sea realmente un objeto tipo 'Renta'
        if (e.CurrentSelection.FirstOrDefault() is Renta rentaSeleccionada)
        {
            // TRUCO DE UI: Quitamos el color de selección instantáneamente 
            // para que la tarjeta no se quede con el fondo "pegado" o sombreado.
            ListaHistorial.SelectedItem = null;

            // CASO 1: EL AUTO YA SE ENTREGÓ ANTES (Solo lectura)
            // Si la renta ya fue marcada como terminada, mostramos el expediente histórico
            // incluyendo las observaciones con las que regresó el vehículo.
            if (rentaSeleccionada.terminada)
            {
                string detallesHistoricos = $"CLIENTE\n" +
                                            $"Nombre: {rentaSeleccionada.nombre_cliente_ui}\n" +
                                            $"Teléfono: {rentaSeleccionada.telefono_cliente_ui}\n\n" +
                                            $"VEHÍCULO\n" +
                                            $"Auto: {rentaSeleccionada.auto_marca} {rentaSeleccionada.auto_modelo}\n" +
                                            $"Placas: {rentaSeleccionada.auto_placas}\n\n" +
                                            $"TRANSACCIÓN\n" +
                                            $"Total cobrado: ${rentaSeleccionada.costo_total:N2}\n" +
                                            $"Estatus: {rentaSeleccionada.estatus_renta_ui.ToUpper()}\n\n" +
                                            $"OBSERVACIONES DE RECEPCIÓN:\n" +
                                            $"{rentaSeleccionada.observaciones}";

                await DisplayAlert("Expediente Cerrado", detallesHistoricos, "Cerrar");
                return; // Cortamos la ejecución aquí.
            }

            // CASO 2: EL AUTO SIGUE EN LA CALLE (O YA ESTÁ EN EL MOSTRADOR PARA ENTREGARSE)
            // Armamos el resumen de la transacción para el cajero
            string resumen = $"Cliente: {rentaSeleccionada.nombre_cliente_ui}\n" +
                             $"Vehículo: {rentaSeleccionada.auto_marca} {rentaSeleccionada.auto_modelo}\n" +
                             $"Total a cobrar: ${rentaSeleccionada.costo_total:N2}\n\n" +
                             $"¿Deseas registrar la devolución de este vehículo y liberarlo en el sistema?";

            // Le damos al administrador la opción de recibir el carro o solo cerrar el menú
            bool confirmarRecepcion = await DisplayAlert("Gestión de Vehículo", resumen, "Sí, Recibir Auto", "Cancelar");

            if (confirmarRecepcion)
            {
                // Mostramos un Pop-up nativo con caja de texto para cumplir con el requerimiento de Observaciones
                string obs = await DisplayPromptAsync("Auditoría de Recepción",
                    "Escribe el estado del vehículo (Ej. Tanque lleno, sin daños, limpio):",
                    "Guardar", "Omitir", "Sin observaciones");

                // Si el usuario no canceló el Pop-up de texto (obs != null)
                if (obs != null)
                {
                    // Invocamos la transacción doble en nuestro Controlador
                    bool exito = await _controller.FinalizarRentaAsync(rentaSeleccionada.id_renta, rentaSeleccionada.id_auto, obs);

                    if (exito)
                    {
                        await DisplayAlert("Transacción Exitosa", "El vehículo ha sido recibido correctamente y ya se encuentra DISPONIBLE en la galería.", "Entendido");

                        // Refrescamos la lista de la pantalla automáticamente para que la tarjeta
                        // pase de color de Alerta a Verde (Finalizada) en tiempo real.
                        await CargarHistorialAsync();
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo conectar con la base de datos. Intenta de nuevo.", "OK");
                    }
                }
            }
        }
    }
}