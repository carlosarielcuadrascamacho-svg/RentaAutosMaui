using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace RentaAutos.Models
{
    // Modelo que representa la colección "clientes" en MongoDB
    public class Cliente
    {
        // Llave primaria del documento (el _id en Mongo)
        [BsonId]
        // Instrucción para que Mongo transforme su ObjectId interno a un texto normal en C#
        [BsonRepresentation(BsonType.ObjectId)]
        public string id_cliente { get; set; }

        // Datos personales básicos del usuario
        public string nombre_completo { get; set; }

        // Campo clave: Se usa en RegistroController para buscar y autocompletar clientes existentes
        public string telefono { get; set; }

        // Información de contacto secundaria
        public string correo { get; set; }

        // Dato legal obligatorio para que el cliente pueda llevarse el auto
        public string numero_licencia { get; set; }

        // Marca de tiempo de auditoría para saber cuándo llegó este cliente por primera vez
        public DateTime fecha_registro { get; set; }
    }
}