using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RentaAutos.Models
{
    public class Auto
    {
        // Llave primaria del documento en la colección de MongoDB
        [BsonId]
        // Le dice a Mongo que convierta automáticamente su ObjectId interno a un string normal de C#
        [BsonRepresentation(BsonType.ObjectId)]
        public string id_auto { get; set; }

        // Datos físicos y de identificación del vehículo
        public string placas { get; set; }
        public string marca { get; set; }
        public string modelo { get; set; }
        public int año { get; set; }

        // Tarifa base que se usará en la pantalla de Registro para multiplicar por los días
        public decimal precio_por_dia { get; set; }

        // Estado administrativo del vehículo (ej. disponible, mantenimiento)
        public string estatus_auto { get; set; }

        // Enlace directo a la fotografía para cargarla en la galería y en los reportes
        public string imagen_url { get; set; }

        // Trazabilidad y auditoría de cuándo se registró o modificó este auto
        public DateTime fecha_registro { get; set; }
        public DateTime fecha_actualizacion { get; set; }

        // ========================================================
        // PROPIEDADES REACTIVAS PARA LA INTERFAZ (No tocan la BD)
        // ========================================================

        // BsonIgnore evita que Mongo guarde este dato basura en la nube.
        // Esta variable (true/false) la enciende el GaleriaController tras revisar el calendario.
        [BsonIgnore]
        public bool EstaRentadoHoy { get; set; }

        // Las siguientes propiedades usan el operador flecha (=>) para calcular su valor al instante.
        // Si EstaRentadoHoy es true, pinta el fondo de la tarjeta de un rojo muy oscuro, si no, azul marino.
        [BsonIgnore]
        public string ColorFondo => EstaRentadoHoy ? "#450a0a" : "#0F172A";

        // Pinta el contorno de la tarjeta de rojo brillante si alguien lo tiene rentado hoy
        [BsonIgnore]
        public string ColorBorde => EstaRentadoHoy ? "#ef4444" : "#1E293B";

        // Cambia el texto de la etiqueta visual en la galería
        [BsonIgnore]
        public string TextoEstado => EstaRentadoHoy ? "OCUPADO HOY" : "DISPONIBLE";

        // Cambia el color de las letras del estado (Rojo alerta vs Verde éxito)
        [BsonIgnore]
        public string ColorEstado => EstaRentadoHoy ? "#ef4444" : "#10b981";
    }
}