using Syncfusion.Maui.Toolkit.Calendar;
using RentaAutos.Controllers;
using RentaAutos.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace RentaAutos.Views;

// Clase de interfaz de usuario (Vista) encargada del proceso de cotización y cobro.
public partial class RegistroRentaView : ContentPage
{

    // Guarda el carro que el usuario eligió en la pantalla anterior
    private Auto _autoActual;

    // Conexión segura al Controlador que orquesta la escritura en la base de datos
    private readonly RegistroController _controller;

    // Una lista ultra rápida (HashSet) para guardar los días que el carro está ocupado.
    // Usamos HashSet en lugar de List porque las búsquedas adentro de un HashSet son casi instantáneas.
    private HashSet<DateTime> _fechasOcupadas = new HashSet<DateTime>();

    // El motor de MAUI (en MauiProgram.cs) llama a este constructor al abrir la app.
    // Inyecta automáticamente el RegistroController.
    public RegistroRentaView(RegistroController controller)
    {
        InitializeComponent();
        _controller = controller;

        // Sobrescribimos el diseño del calendario desde C# para aplicar el tema Luxury
        ConfigurarEstiloCalendario();

        // Por defecto, pre-seleccionamos el día de hoy y mañana para que el usuario 
        // entienda visualmente cómo funciona la selección de rangos.
        CalendarioRenta.SelectedDateRange = new CalendarDateRange(DateTime.Today, DateTime.Today.AddDays(1));
    }

    // Aplica el sistema de diseño "Luxury Dark Carbon" al calendario mediante código.
    // Esto se hace aquí para tener control total sobre los colores del Toolkit gratuito de Syncfusion.
    private void ConfigurarEstiloCalendario()
    {
        // 1. Estilo del Header (Mes y Año)
        CalendarioRenta.HeaderView = new CalendarHeaderView
        {
            Background = Color.FromArgb("#060A12"), // Fondo ultra oscuro
            TextStyle = new CalendarTextStyle
            {
                TextColor = Color.FromArgb("#C9A84C"), // Texto dorado premium
                FontSize = 16,
                FontAttributes = FontAttributes.Bold
            }
        };

        // 2. Estilo de la cuadrícula de días (Mes)
        CalendarioRenta.MonthView = new CalendarMonthView
        {
            // Forzamos nuestro fondo oscuro para evitar el cuadro blanco nativo que trae por defecto
            Background = Color.FromArgb("#080C14"),

            // Días normales y disponibles (Texto claro para que resalte)
            TextStyle = new CalendarTextStyle
            {
                TextColor = Color.FromArgb("#EDF2FF"),
                FontSize = 14
            },

            // Días deshabilitados (pasado o fechas que nuestra base de datos diga que están ocupadas)
            DisabledDatesTextStyle = new CalendarTextStyle
            {
                TextColor = Color.FromArgb("#1E2A3A"), // Gris muy oscuro, casi invisible
                FontSize = 14
            }
        };

        // 3. Estilo de la Selección (El rango de días que elige el usuario)
        CalendarioRenta.SelectionBackground = Color.FromArgb("#40C9A84C"); // Dorado con transparencia para el "relleno"
        CalendarioRenta.StartRangeSelectionBackground = Color.FromArgb("#C9A84C"); // Dorado sólido para el inicio
        CalendarioRenta.EndRangeSelectionBackground = Color.FromArgb("#C9A84C");   // Dorado sólido para el fin
    }

    // ========================================================================
    // LÓGICA DE NEGOCIO Y EVENTOS
    // ========================================================================

    // La pantalla GaleriaView.xaml.cs llama a recibir auto, justo cuando el usuario hace clic en una tarjeta.
    // Recibe el auto seleccionado y prepara toda la interfaz visual.
    public async void RecibirAutoSeleccionado(Auto auto)
    {
        // Guardamos el auto en la memoria de esta pantalla
        _autoActual = auto;

        // Bindeo manual: Inyectamos los datos del auto en los Labels e Imágenes de la tarjeta superior
        imgAuto.Source = auto.imagen_url;
        lblMarcaModelo.Text = $"{auto.marca} {auto.modelo}";
        lblPlacas.Text = $"Placas: {auto.placas}";
        lblDetallesExtra.Text = $"Año: {auto.año}";
        lblPrecio.Text = $"${auto.precio_por_dia:N2} / día";

        // Limpiamos cualquier selección vieja del calendario para evitar bugs visuales
        CalendarioRenta.SelectedDateRange = null;

        // Le pide al Controlador que vaya a MongoDB y traiga todos los días prohibidos de este carro específico
        var listaFechas = await _controller.ObtenerFechasOcupadasPorAutoAsync(auto.id_auto);

        // Convertimos la lista normal a un HashSet para máxima velocidad de lectura
        _fechasOcupadas = new HashSet<DateTime>(listaFechas);

        // MAGIA DEL CALENDARIO: Inyectamos una regla (Predicado) al calendario.
        // El calendario va a iterar día por día. Si el día que está dibujando existe 
        // en nuestro HashSet de ocupados, devuelve 'false' y lo bloquea visual y funcionalmente.
        CalendarioRenta.SelectableDayPredicate = (date) =>
        {
            if (_fechasOcupadas.Contains(date.Date))
            {
                return false; // Día bloqueado (No se puede tocar)
            }
            return true; // Día disponible
        };

        // Calculamos la cotización inicial (que será cero porque limpiamos el calendario)
        CalcularCotizacionAutonoma();
    }

    // Evento disparado automáticamente por Syncfusion cada vez que el usuario toca el calendario.
    private void OnCalendarSelectionChanged(object sender, CalendarSelectionChangedEventArgs e)
    {
        CalcularCotizacionAutonoma();
    }

    // Algoritmo principal que calcula los días totales y el costo en tiempo real.
    private async void CalcularCotizacionAutonoma()
    {
        // Si no hay carro cargado, no hacemos matemáticas
        if (_autoActual == null) return;

        var rangoFechas = CalendarioRenta.SelectedDateRange;

        // Verificamos que el usuario haya seleccionado un rango completo (Inicio y Fin)
        if (rangoFechas != null && rangoFechas.StartDate.HasValue && rangoFechas.EndDate.HasValue)
        {
            DateTime inicio = rangoFechas.StartDate.Value.Date;
            DateTime fin = rangoFechas.EndDate.Value.Date;

            // VALIDACIÓN 
            // El usuario podría seleccionar el día 1 y el día 5, intentando "brincarse" 
            // los días 2, 3 y 4 que ya estaban ocupados. Aquí evitamos eso.
            DateTime diaRevision = inicio;
            while (diaRevision <= fin)
            {
                if (_fechasOcupadas.Contains(diaRevision))
                {
                    // Si el ciclo encuentra un obstáculo en medio de la selección, cancela todo.
                    CalendarioRenta.SelectedDateRange = null;
                    lblDiasCalculados.Text = "0 días";
                    lblCostoTotal.Text = "$0.00";

                    await DisplayAlert("Rango Inválido", "El rango seleccionado cruza con días en los que el vehículo ya está reservado. Elige un bloque continuo libre.", "Entendido");
                    return;
                }
                diaRevision = diaRevision.AddDays(1);
            }

            // Cálculo matemático de los días reales de renta
            int dias = (fin - inicio).Days;

            if (dias > 0)
            {
                // Multiplicamos días por la tarifa diaria del vehículo actual
                decimal total = dias * _autoActual.precio_por_dia;

                // Actualizamos la "caja registradora" en la pantalla
                lblDiasCalculados.Text = $"{dias} días";
                lblCostoTotal.Text = $"${total:N2}";
                return;
            }
        }

        // Estado por defecto si la selección está incompleta o si el usuario seleccionó un solo día
        lblDiasCalculados.Text = "0 días";
        lblCostoTotal.Text = "$0.00";
    }

    // ========================================================================
    // VALIDACIONES DE FORMULARIO DE CLIENTE
    // ========================================================================

    // Valida que el teléfono tenga el formato correcto al presionar "Enter" en el teclado del celular.
    private async void OnTelefonoCompleted(object sender, EventArgs e)
    {
        string telefono = txtTelefono.Text?.Trim() ?? "";

        if (telefono.Length != 10)
        {
            borderTelefono.Stroke = Colors.Red; // Feedback visual de error
            await DisplayAlert("Teléfono inválido", "Debes ingresar exactamente 10 dígitos numéricos.", "Entendido");
            txtTelefono.Focus();
            return;
        }
        else
        {
            borderTelefono.Stroke = Colors.Green; // Feedback visual de éxito
        }
    }

    // Disparado cuando el usuario quita el dedo/foco de la caja de texto del teléfono.
    // Realiza una búsqueda autónoma en la base de datos para autocompletar el perfil.
    private async void OnTelefonoUnfocused(object sender, FocusEventArgs e)
    {
        string telefono = txtTelefono.Text?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(telefono))
        {
            // Validamos que sean 10 caracteres y que todos sean números puros
            if (telefono.Length == 10 && telefono.All(char.IsDigit))
            {
                borderTelefono.Stroke = Colors.Green;

                // Pasamos el foco al siguiente campo automáticamente (Buena UX)
                txtNombreCliente.Focus();

                // Le pide al Controlador que busque si este cliente ya nos ha rentado antes
                var clienteExiste = await _controller.BuscarClientePorTelefonoAsync(telefono);

                if (clienteExiste != null)
                {
                    // Si existe, autocompletamos los campos para agilizar el flujo de renta
                    txtNombreCliente.Text = clienteExiste.nombre_completo;
                    txtCorreo.Text = clienteExiste.correo;
                    txtLicencia.Text = clienteExiste.numero_licencia;

                    await DisplayAlert("Cliente Encontrado", $"Bienvenido de nuevo a nuestra red ejecutiva, {clienteExiste.nombre_completo}.", "Aceptar");
                }
            }
            else
            {
                await DisplayAlert("Teléfono Inválido", "El teléfono debe contener exactamente 10 números.", "Entendido");
                borderTelefono.Stroke = Colors.Red;
                txtTelefono.Focus();
            }
        }
    }

    // Flujo de validación final y almacenamiento en base de datos.
    // Se ejecuta al presionar el botón azul grande de Confirmar.
    private async void OnConfirmarRentaClicked(object sender, EventArgs e)
    {
        // 1. Validar existencia del Vehículo (Programación Defensiva)
        if (_autoActual == null)
        {
            await DisplayAlert("Operación Detenida", "Selecciona un vehículo de la flota antes de continuar.", "Entendido");
            return;
        }

        // Recolectar datos limpiando espacios en blanco fantasma (Trim)
        string telefono = (txtTelefono.Text ?? "").Trim();
        string nombre = (txtNombreCliente.Text ?? "").Trim();
        string correo = (txtCorreo.Text ?? "").Trim();
        string licencia = (txtLicencia.Text ?? "").Trim();

        // 2. Bloque de validaciones del Cliente (Impedimos que guarden basura en la base de datos)
        if (string.IsNullOrWhiteSpace(telefono) || telefono.Length != 10 || !telefono.All(char.IsDigit))
        {
            await DisplayAlert("Datos Incompletos", "El teléfono debe contener EXACTAMENTE 10 dígitos numéricos.", "Entendido");
            txtTelefono.Focus();
            return;
        }

        // El nombre no puede tener números
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length < 3 || nombre.Any(char.IsDigit))
        {
            await DisplayAlert("Datos Incompletos", "Ingresa un nombre válido (sin números y mínimo 3 caracteres).", "Entendido");
            txtNombreCliente.Focus();
            return;
        }

        // Validación de correo mediante Expresión Regular
        string patronCorreo = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (string.IsNullOrWhiteSpace(correo) || !Regex.IsMatch(correo, patronCorreo))
        {
            await DisplayAlert("Datos Incompletos", "Formato de correo inválido. Ejemplo: contacto@empresa.com", "Entendido");
            txtCorreo.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(licencia) || licencia.Length < 5)
        {
            await DisplayAlert("Datos Incompletos", "Ingresa un número de licencia válido.", "Entendido");
            txtLicencia.Focus();
            return;
        }

        // 3. Bloque de validaciones del Calendario/Cotización
        var rangoFechas = CalendarioRenta.SelectedDateRange;

        if (rangoFechas == null || !rangoFechas.StartDate.HasValue || !rangoFechas.EndDate.HasValue)
        {
            await DisplayAlert("Fechas Requeridas", "Selecciona fecha de inicio y fin en el calendario.", "Entendido");
            return;
        }

        DateTime fechaInicio = rangoFechas.StartDate.Value.Date;
        DateTime fechaFin = rangoFechas.EndDate.Value.Date;

        // Impedimos que el empleado haga rentas en el pasado
        if (fechaInicio < DateTime.Today)
        {
            await DisplayAlert("Error Temporal", "La fecha de inicio no puede estar en el pasado.", "Entendido");
            return;
        }

        if (fechaFin <= fechaInicio)
        {
            await DisplayAlert("Rango Inválido", "La fecha de fin debe ser posterior a la fecha de inicio.", "Entendido");
            return;
        }

        int diasRenta = (fechaFin - fechaInicio).Days;

        if (diasRenta <= 0 || _autoActual.precio_por_dia <= 0)
        {
            await DisplayAlert("Cálculo Fallido", "Hubo un error calculando los días o la tarifa del vehículo.", "Entendido");
            return;
        }

        // 4. Ejecución: Preparar objeto y mandar a guardar a MongoDB
        decimal totalCalculado = diasRenta * _autoActual.precio_por_dia;

        // Empaquetamos los datos en el modelo de la BD
        var clienteCapturado = new Cliente
        {
            telefono = telefono,
            nombre_completo = nombre,
            correo = correo,
            numero_licencia = licencia
        };

        // Invocamos al controlador para ejecutar la transacción. 
        // Le pasamos toda la responsabilidad de guardar en la nube.
        bool exito = await _controller.GuardarNuevaRentaAsync(_autoActual, clienteCapturado, fechaInicio, fechaFin, diasRenta, totalCalculado);

        // 5. Retroalimentación final al usuario
        if (exito)
        {
            await DisplayAlert("Operación Exitosa", $"La renta del {_autoActual.marca} se ha emitido y guardado exitosamente en el sistema.", "Finalizar");

            // Limpiamos todo el formulario para dejarlo listo para el siguiente cliente
            txtTelefono.Text = string.Empty;
            txtNombreCliente.Text = string.Empty;
            txtCorreo.Text = string.Empty;
            txtLicencia.Text = string.Empty;
            borderTelefono.Stroke = Colors.Transparent;
            CalendarioRenta.SelectedDateRange = null;
        }
        else
        {
            await DisplayAlert("Error de Sistema", "Hubo un problema de red al intentar guardar el expediente. Inténtalo de nuevo.", "Entendido");
        }
    }
}