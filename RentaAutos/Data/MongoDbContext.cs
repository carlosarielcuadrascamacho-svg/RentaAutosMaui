using System;
using MongoDB.Driver;
using MongoDB.Bson;

namespace RentaAutos.Data
{
    // Clase de Contexto: Actúa como el puente central y único entre tu App y MongoDB Atlas.
    // En la arquitectura, esto se inyecta como un Singleton para no abrir 100 conexiones a la vez.
    public class MongoDbContext
    {
        // Interfaz nativa del driver de Mongo que mantiene la sesión de la base de datos viva
        private readonly IMongoDatabase _database;

        // Constructor que se ejecuta una sola vez cuando la app arranca
        public MongoDbContext()
        {
            // ========================================================
            // CONFIGURACIÓN DEL CLÚSTER DE MONGODB ATLAS
            // ========================================================

            // Solución de Ingeniería para Móviles: 
            // En lugar de usar el formato '+srv' (que causa bloqueos de resolución DNS en Android),
            // declaramos explícitamente los 3 nodos (shards) del Replica Set de Atlas. 
            // Esto garantiza que si el nodo 00 cae, la app salte al 01 o 02 automáticamente sin crashear.
            string connectionUri = "Conexion a la base de datos en mongo DB";

            // Parseamos el string larguísimo para que el driver de Mongo entienda las credenciales y el SSL
            var settings = MongoClientSettings.FromConnectionString(connectionUri);

            // Instanciamos el cliente pesado (MongoClient). Este objeto maneja el pool de conexiones en segundo plano.
            var client = new MongoClient(settings);

            // Apuntamos directamente a nuestra base de datos específica dentro del clúster
            _database = client.GetDatabase("RentaCarrosDB");
        }

        // ========================================================
        // EXPOSICIÓN DE LA BASE DE DATOS A LOS CONTROLADORES
        // ========================================================

        // Este método público es la única puerta de entrada. 
        // Los Controladores (Galeria, Registro, Reportes) llamarán a este método 
        // para pedirle colecciones específicas (como "autos" o "rentas") y hacer sus operaciones (CRUD).
        public IMongoDatabase GetDatabase()
        {
            return _database;
        }
    }
}
