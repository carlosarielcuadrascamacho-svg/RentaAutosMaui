using MongoDB.Driver;
using RentaAutos.Models;
using RentaAutos.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RentaAutos.Controllers
{
    // Controlador de Lectura, Filtrado y Actualización para la pantalla de Reportes.
    public class ReportesController
    {
        // Conexión segura a la capa de Acceso a Datos (Data Access Layer)
        private readonly MongoDbContext _context;

        // El motor de Inyección de Dependencias de MAUI llama a esta clase 
        // cuando la app arranca y necesita cargar la pestaña de reportes.
        public ReportesController(MongoDbContext context)
        {
            _context = context;
        }

        // ========================================================================
        // 1. OBTENER HISTORIAL DE RENTAS CON FILTROS DINÁMI
        // ========================================================================
        // La pantalla ReportesView.xaml.cs le habla a este método cada vez que el usuario
        // entra a la pantalla, o cuando presiona los botones de "Buscar" o "Limpiar".
        public async Task<List<Renta>> ObtenerHistorialRentasAsync(string placa, DateTime? fechaInicio, DateTime? fechaFin)
        {
            // Llama a MongoDbContext.cs para conectarse a la colección 'rentas'.
            var coleccionRentas = _context.GetDatabase().GetCollection<Renta>("rentas");

            // Se usa un Builder de MongoDB para armar una consulta dinámica.
            // Empieza vacío (Filter.Empty), lo que equivale a un "Tráeme todo".
            var builder = Builders<Renta>.Filter;
            var filtro = builder.Empty;

            // PASO 1: APLICAR FILTROS (Si el usuario los mandó)
            if (!string.IsNullOrWhiteSpace(placa))
            {
                // Agregamos la regla al filtro y convertimos a mayúsculas.
                filtro &= builder.Eq(r => r.auto_placas, placa.ToUpper());
            }

            // Se filtra por fechas, solo si la vista mandó ambas fechas válidas.
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                // Se busca por rango (Gte = Mayor o igual, Lte = Menor o igual)
                filtro &= builder.Gte(r => r.fecha_inicio, fechaInicio.Value) &
                          builder.Lte(r => r.fecha_inicio, fechaFin.Value);
            }

            // PASO 2: EJECUTAR LA CONSULTA
            // Los datos bajan de la nube y se guardan en la variable 'rentas', ordenados por fecha.
            var rentas = await coleccionRentas.Find(filtro)
                                              .SortByDescending(r => r.fecha_registro_renta)
                                              .ToListAsync();

            // PASO 3: EL "JOIN" MANUAL (Enriquecer los datos)
            var coleccionClientes = _context.GetDatabase().GetCollection<Cliente>("clientes");

            foreach (var renta in rentas)
            {
                var cliente = await coleccionClientes.Find(c => c.id_cliente == renta.id_cliente).FirstOrDefaultAsync();

                if (cliente != null)
                {
                    // Propiedades reactivas para que la Vista las dibuje sin alterar la base de datos
                    renta.nombre_cliente_ui = cliente.nombre_completo;
                    renta.telefono_cliente_ui = cliente.telefono;
                }
            }

            return rentas;
        }

        // ========================================================================
        // 2. FINALIZAR RENTA Y LIBERAR AUTO
        // ========================================================================
        // La pantalla ReportesView.xaml.cs llama a este método cuando el gerente 
        // presiona el botón "Recibir Auto" tras validar que el cliente entregó las llaves.
        public async Task<bool> FinalizarRentaAsync(string idRenta, string idAuto, string observaciones)
        {
            try
            {
                // Obtenemos las dos colecciones que vamos a afectar simultáneamente
                var coleccionRentas = _context.GetDatabase().GetCollection<Renta>("rentas");
                var coleccionAutos = _context.GetDatabase().GetCollection<Auto>("autos");

                // PASO 1: CERRAR EL EXPEDIENTE (Contrato)
                var filtroRenta = Builders<Renta>.Filter.Eq(r => r.id_renta, idRenta);

                // Preparamos los cambios: Encendemos el "Kill Switch" (terminada = true) 
                // y guardamos la auditoría de cómo regresó el carro (observaciones).
                var updateRenta = Builders<Renta>.Update
                    .Set(r => r.terminada, true)
                    .Set(r => r.observaciones, observaciones);

                // Mandamos la actualización a MongoDB
                await coleccionRentas.UpdateOneAsync(filtroRenta, updateRenta);

                // PASO 2: LIBERAR EL VEHÍCULO (Devolverlo al inventario)
                var filtroAuto = Builders<Auto>.Filter.Eq(a => a.id_auto, idAuto);

                // Le devolvemos su estatus original para que vuelva a aparecer con el círculo Verde en la Galería
                var updateAuto = Builders<Auto>.Update.Set(a => a.estatus_auto, "disponible");

                // Mandamos la actualización a MongoDB
                await coleccionAutos.UpdateOneAsync(filtroAuto, updateAuto);

                // Si ambas actualizaciones pasaron sin crashear, todo fue un éxito
                return true;
            }
            catch (Exception ex)
            {
                // Manejo de errores defensivo: Si la conexión falla, no cerramos la app
                Console.WriteLine($"Error al finalizar la renta y liberar el auto: {ex.Message}");
                return false;
            }
        }
    }
}