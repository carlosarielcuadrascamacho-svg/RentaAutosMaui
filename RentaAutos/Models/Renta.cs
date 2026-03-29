using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace RentaAutos.Models
{
    // Clase maestra que representa un contrato de renta y une al Cliente con el Auto
    public class Renta
    {
        // Llave primaria autogenerada por MongoDB
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id_renta { get; set; }

        // Relaciones (Llaves Foráneas lógicas) para saber qué cliente rentó qué auto
        public string id_auto { get; set; }
        public string id_cliente { get; set; }

        // ========================================================
        // DATOS HISTÓRICOS DEL AUTO (Inmutables)
        // ========================================================
        public string auto_placas { get; set; }
        public string auto_marca { get; set; }
        public string auto_modelo { get; set; }
        public decimal auto_precio_por_dia { get; set; }

        // ========================================================
        // DETALLES DE LA TRANSACCIÓN
        // ========================================================
        public DateTime fecha_inicio { get; set; }
        public DateTime fecha_fin { get; set; }
        public int dias_renta { get; set; }
        public decimal costo_total { get; set; }
        public DateTime fecha_registro_renta { get; set; }

        // ========================================================
        // CAMPOS OBLIGATORIOS (Ahora con soporte para Finalización)
        // ========================================================
        public string estatus_renta { get; set; }

        // ¡REVIVIMOS ESTE CAMPO!: Aquí se guardará si el carro llegó chocado, 
        // sucio, sin gasolina, o en perfectas condiciones al momento de devolverlo.
        public string observaciones { get; set; }

        // NUEVO: El "Kill Switch" o interruptor manual. 
        // Cuando el admin recibe el carro, esto se vuelve 'true'.
        public bool terminada { get; set; } = false;

        // ========================================================
        // PROPIEDADES DINÁMICAS (Solo existen en RAM)
        // ========================================================
        [BsonIgnore]
        public string nombre_cliente_ui { get; set; } = "Cliente Desconocido";

        [BsonIgnore]
        public string telefono_cliente_ui { get; set; } = "N/A";

        // ========================================================
        // INTELIGENCIA DE NEGOCIO EN TIEMPO REAL (Actualizada)
        // ========================================================
        [BsonIgnore]
        public string estatus_renta_ui
        {
            get
            {
                // 1. EL BOTÓN MANUAL MANDA: Si el administrador ya recibió las llaves 
                // y marcó la renta como terminada, no importa qué día sea, el contrato se cierra.
                if (terminada) return "FINALIZADA (DEVUELTO)";

                var hoy = DateTime.Today;

                // 2. Si nadie ha presionado el botón, evaluamos el tiempo
                if (hoy < fecha_inicio.Date) return "PROGRAMADA";

                if (hoy >= fecha_inicio.Date && hoy <= fecha_fin.Date) return "EN CURSO (OCUPADO)";

                // 3. NUEVO ESTADO DE ALERTA: Si la fecha de entrega ya pasó, 
                // pero el admin NO ha marcado la casilla 'terminada', significa que 
                // el cliente no ha regresado el carro. ¡Es una alerta para el negocio!
                return "PENDIENTE DE RECEPCIÓN";
            }
        }

        [BsonIgnore]
        public Color color_estatus_ui
        {
            get
            {
                // Verde éxito: El auto ya está sano y salvo en la agencia.
                if (estatus_renta_ui == "FINALIZADA (DEVUELTO)") return Color.FromArgb("#10B981");

                // Azul claro: El cliente lo apartó para el futuro.
                if (estatus_renta_ui == "PROGRAMADA") return Color.FromArgb("#38BDF8");

                // Amarillo: El auto está rodando en la calle dentro de su tiempo legal.
                if (estatus_renta_ui == "EN CURSO (OCUPADO)") return Color.FromArgb("#FACC15");

                // Rojo Alerta: El cliente debió entregarlo ayer y no ha llegado.
                return Color.FromArgb("#EF4444");
            }
        }
    }
}