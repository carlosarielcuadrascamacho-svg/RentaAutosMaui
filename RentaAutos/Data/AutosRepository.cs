using MongoDB.Driver;
using RentaAutos.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RentaAutos.Data
{
    // Implementación del Patrón Repositorio para la entidad Auto.
    // Encapsula toda la lógica de consultas a MongoDB en un solo lugar.
    public class AutosRepository
    {
        // Interfaz nativa del driver de Mongo que representa nuestra colección específica.
        // Es 'readonly' para garantizar que no se apunte a otra colección por error durante la ejecución.
        private readonly IMongoCollection<Auto> _autosCollection;

        // Inyección de Dependencias: El constructor recibe la conexión activa del Contexto central.
        // Esto evita crear un nuevo cliente de Mongo (que consume mucha RAM) cada vez que hacemos una consulta.
        public AutosRepository(MongoDbContext context)
        {
            // Extraemos la instancia de la base de datos "RentaCarrosDB"
            var database = context.GetDatabase();

            // Apuntamos específicamente a la colección "autos" y le decimos a Mongo
            // que mapee automáticamente los documentos BSON a nuestra clase 'Auto' de C#.
            _autosCollection = database.GetCollection<Auto>("autos");
        }

        // ========================================================
        // MÉTODOS DE ACCESO A DATOS (CRUD)
        // ========================================================

        // Método asíncrono (Task) para consultar el catálogo.
        // Ser asíncrono garantiza que la interfaz de usuario en el celular (UI Thread)
        // no se congele (se quede pasmada) mientras espera a que Mongo responda por internet.
        public async Task<List<Auto>> ObtenerTodosLosAutosAsync()
        {
            // Expresión Lambda: '_ => true' actúa como un filtro universal (equivale a un "SELECT *").
            // ToListAsync() ejecuta la búsqueda en la nube y materializa los resultados en una lista de C#.
            return await _autosCollection.Find(_ => true).ToListAsync();
        }
    }
}