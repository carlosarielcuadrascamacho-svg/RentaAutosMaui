using MongoDB.Driver;
using RentaAutos.Models;
using RentaAutos.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RentaAutos.Controllers
{
    public class RegistroController
    {
        private readonly MongoDbContext _context;
        private readonly IMongoDatabase _database;

        public RegistroController(MongoDbContext context)
        {
            _context = context;
            _database = _context.GetDatabase();
        }

        // ============================================================
        // 1. BÚSQUEDA DE CLIENTE POR TELÉFONO
        // ============================================================
        public async Task<Cliente?> BuscarClientePorTelefonoAsync(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
                return null;

            var coleccionClientes = _database.GetCollection<Cliente>("clientes");

            return await coleccionClientes
                .Find(c => c.telefono == telefono)
                .FirstOrDefaultAsync();
        }

        // ============================================================
        // 2. CREACIÓN TRANSACCIONAL DE RENTA
        // ============================================================
        public async Task<bool> GuardarNuevaRentaAsync(
            Auto auto,
            Cliente datosCliente,
            DateTime fechaInicio,
            DateTime fechaFin,
            int dias,
            decimal total)
        {
            if (auto == null || datosCliente == null)
                return false;

            if (fechaFin < fechaInicio)
                return false;

            var coleccionClientes = _database.GetCollection<Cliente>("clientes");
            var coleccionRentas = _database.GetCollection<Renta>("rentas");
            var coleccionAutos = _database.GetCollection<Auto>("autos");

            using var session = await _database.Client.StartSessionAsync();
            session.StartTransaction();

            try
            {
                // =====================================================
                // 1️⃣ VALIDAR DISPONIBILIDAD REAL DEL AUTO
                // =====================================================
                var filtroConflicto = Builders<Renta>.Filter.Where(r =>
                    r.id_auto == auto.id_auto &&
                    r.terminada == false &&
                    (
                        fechaInicio <= r.fecha_fin &&
                        fechaFin >= r.fecha_inicio
                    ));

                var rentaExistente = await coleccionRentas
                    .Find(session, filtroConflicto)
                    .FirstOrDefaultAsync();

                if (rentaExistente != null)
                {
                    await session.AbortTransactionAsync();
                    return false; // Conflicto de fechas
                }

                // =====================================================
                // 2️⃣ GESTIÓN DEL CLIENTE
                // =====================================================
                var clienteExistente = await coleccionClientes
                    .Find(session, c => c.telefono == datosCliente.telefono)
                    .FirstOrDefaultAsync();

                string idClienteFinal;

                if (clienteExistente != null)
                {
                    idClienteFinal = clienteExistente.id_cliente;
                }
                else
                {
                    datosCliente.fecha_registro = DateTime.UtcNow;
                    await coleccionClientes.InsertOneAsync(session, datosCliente);
                    idClienteFinal = datosCliente.id_cliente;
                }

                // =====================================================
                // 3️⃣ CONSTRUCCIÓN DEL CONTRATO
                // =====================================================
                var nuevaRenta = new Renta
                {
                    id_auto = auto.id_auto,
                    id_cliente = idClienteFinal,

                    // Snapshot del auto
                    auto_placas = auto.placas,
                    auto_marca = auto.marca,
                    auto_modelo = auto.modelo,
                    auto_precio_por_dia = auto.precio_por_dia,

                    fecha_inicio = fechaInicio,
                    fecha_fin = fechaFin,
                    dias_renta = dias,
                    costo_total = total,
                    fecha_registro_renta = DateTime.UtcNow,

                    estatus_renta = "programada",
                    observaciones = "Sin observaciones",
                    terminada = false
                };

                await coleccionRentas.InsertOneAsync(session, nuevaRenta);

                // =====================================================
                // 4️⃣ ACTUALIZAR INVENTARIO
                // =====================================================
                var filtroAuto = Builders<Auto>.Filter.Eq(a => a.id_auto, auto.id_auto);
                var updateAuto = Builders<Auto>.Update.Set(a => a.estatus_auto, "rentado");

                await coleccionAutos.UpdateOneAsync(session, filtroAuto, updateAuto);

                // =====================================================
                // ✅ CONFIRMAR TRANSACCIÓN
                // =====================================================
                await session.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await session.AbortTransactionAsync();
                Console.WriteLine($"Error transaccional al guardar renta: {ex.Message}");
                return false;
            }
        }

        // ============================================================
        // 3. OBTENER FECHAS OCUPADAS (CALENDARIO)
        // ============================================================
        public async Task<List<DateTime>> ObtenerFechasOcupadasPorAutoAsync(string idAuto)
        {
            var fechasOcupadas = new HashSet<DateTime>();

            try
            {
                var coleccionRentas = _database.GetCollection<Renta>("rentas");

                var filtro = Builders<Renta>.Filter.Where(r =>
                    r.id_auto == idAuto &&
                    r.terminada == false);

                var rentasActivas = await coleccionRentas
                    .Find(filtro)
                    .ToListAsync();

                foreach (var renta in rentasActivas)
                {
                    for (var fecha = renta.fecha_inicio.Date;
                         fecha <= renta.fecha_fin.Date;
                         fecha = fecha.AddDays(1))
                    {
                        fechasOcupadas.Add(fecha);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener fechas ocupadas: {ex.Message}");
            }

            return new List<DateTime>(fechasOcupadas);
        }
    }
}