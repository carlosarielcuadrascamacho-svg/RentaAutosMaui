using MongoDB.Driver;
using RentaAutos.Models;
using RentaAutos.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RentaAutos.Controllers
{
    // Controlador principal para la vista de la Galería. 
    // Orquesta la lógica de negocio para mostrar el catálogo y su disponibilidad.
    public class GaleriaController
    {
        // Inyección de dependencia del contexto de la base de datos.
        // Se mantiene de solo lectura (readonly) para proteger la conexión.
        private readonly MongoDbContext _context;

        // El contenedor de dependencias de MAUI en MauiProgram.cs llama a esta clase 
        // cuando la app arranca y necesita crear la pantalla de Galería.
        // Pide la conexión activa a MongoDbContext.cs
        public GaleriaController(MongoDbContext context)
        {
            _context = context;
        }

        // La pantalla GaleriaView.xaml.cs le habla a este metodo para obtener los carros.
        // Método asíncrono que devuelve la lista de autos ya procesada con su estatus real.
        public async Task<List<Auto>> ObtenerAutosParaGaleriaAsync()
        {
            // Llama a MongoDbContext.cs para obtener 
            // la conexión a la base de datos "RentaCarrosDB" en MongoDB.
            var coleccionAutos = _context.GetDatabase().GetCollection<Auto>("autos");
            var coleccionRentas = _context.GetDatabase().GetCollection<Renta>("rentas");

            // Le Pide a MongoDB descargar la colección completa de autos.
            // Los datos crudos bajan de la nube y se guardan en la variable 'autos'.
            var autos = await coleccionAutos.Find(_ => true).ToListAsync();

            // Congelamos la fecha de hoy a las 00:00:00 horas para hacer comparaciones exactas.
            var hoy = DateTime.Today;

            // Iteramos sobre cada auto para calcular su disponibilidad dinámica.
            foreach (var auto in autos)
            {
                // Construimos una consulta LINQ para buscar si este auto en particular
                // tiene un contrato de renta donde el día de hoy caiga exactamente en medio
                // del inicio y el fin de la renta.
                var filtroRenta = Builders<Renta>.Filter.Where(r =>
                    r.id_auto == auto.id_auto &&     // Coincidencia exacta del vehículo
                    r.fecha_inicio <= hoy &&         // La renta ya empezó o empieza hoy
                    r.fecha_fin >= hoy);             // La renta todavía no termina

                // Hace una mini-consulta a MongoDB buscando rentas activas de este carro.
                // Ejecutamos la consulta. FirstOrDefaultAsync() trae el primer registro que coincida,
                // o null si el auto está libre hoy.
                var rentaHoy = await coleccionRentas.Find(filtroRenta).FirstOrDefaultAsync();

                // Modifica la propiedad reactiva en Models -> Auto.cs
                // Si la consulta trajo un documento, significa que el auto está en la calle.
                if (rentaHoy != null)
                {
                    // Al encender esta bandera, el Modelo (Auto.cs) automáticamente cambiará
                    // el ColorFondo a rojo y el TextoEstado a "OCUPADO HOY" para la Vista (XAML).
                    auto.EstaRentadoHoy = true;
                }
                else
                {
                    // El auto está en la agencia, libre para rentarse.
                    auto.EstaRentadoHoy = false;
                }
            }

            // Devuelve esta lista ya procesada hacia GaleriaView.xaml.cs.
            // Esa vista tomará esta lista y se la inyectará a la propiedad 'ItemsSource'
            // del CollectionView llamado 'ListaAutos' para que se dibujen las tarjetas en la pantalla.
            return autos;
        }
    }
}