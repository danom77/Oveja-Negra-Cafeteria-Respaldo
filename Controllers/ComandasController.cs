using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OvejaNegra.Context;
using OvejaNegra.Dtos;
using OvejaNegra.Models;
using OvejaNegra.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OvejaNegra.Controllers
{
    [Authorize(Roles = "Administrador,Cajero,Mesero")]
    public class ComandasController : Controller
    {
        private readonly MiContext _context;

        public ComandasController(MiContext context)
        {
            _context = context;
        }

        // GET: Comandas
        public async Task<IActionResult> Index()
        {
            var estadosActivos = new[]
            {
        EstadoEnum.Pendiente,
        EstadoEnum.EnPreparacion,
        EstadoEnum.ListoParaServir,
        EstadoEnum.Servido,
    };

            var comandas = await _context.Comandas
            .Include(c => c.Usuario)
            .Include(c => c.DetallesComanda)
            .ThenInclude(d => d.Producto)
            .Where(c => estadosActivos.Contains(c.Estado))
            .OrderBy(c => c.Fecha)
            .ToListAsync();

            return View(comandas);
        }
        [HttpPost]
        public async Task<IActionResult> CambiarEstado([FromBody] CambiarEstadoDTO dto)
        {
            var comanda = await _context.Comandas
                .Include(c => c.DetallesComanda)
                .FirstOrDefaultAsync(c => c.Id == dto.ComandaId);

            if (comanda == null)
                return NotFound();

            comanda.Estado = (EstadoEnum)dto.Estado;
            if (comanda.Estado == EstadoEnum.Cobrado)
            {
                bool existeVenta = await _context.Ventas
                    .AnyAsync(v => v.ComandaId == comanda.Id);

                if (!existeVenta)
                {
                    decimal total = comanda.DetallesComanda
                        .Sum(d => d.Cantidad * d.Precio_unitario);

                    var venta = new Venta
                    {
                        Fecha = DateTime.Now,
                        ComandaId = comanda.Id,
                        Total = total,
                        MetodoPago = PagoEnum.Efectivo
                    };

                    _context.Ventas.Add(venta);
                }
            }
            if (comanda.Estado == EstadoEnum.Cancelado)
            {
                var detalles = _context.DetalleComandas
                    .Where(d => d.ComandaId == comanda.Id);

                _context.DetalleComandas.RemoveRange(detalles);

                _context.Comandas.Remove(comanda);
            }

            await _context.SaveChangesAsync();

            return Json(new { ok = true });
        }
        // GET: Comandas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comanda = await _context.Comandas
                .Include(c => c.Usuario)
                .Include(c => c.DetallesComanda)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (comanda == null)
            {
                return NotFound();
            }

            return View(comanda);
        }

        // GET: Comandas/Create
        public IActionResult Create()
        {
            var categorias = _context.Categorias
            .Include(c => c.Productos)
            .ToList();

            var vm = new CrearComandaVM
            {
                Categorias = categorias
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> CrearPedido([FromBody] CrearComandaVM vm)
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int usuarioId = int.Parse(userIdClaim);

            var comanda = new Comanda
            {
                Fecha = DateTime.Now,
                Nro_Mesa = vm.NroMesa,
                UsuarioId = usuarioId,
                Estado = EstadoEnum.Pendiente,
                DetallesComanda = new List<DetalleComanda>()
            };

            foreach (var item in vm.Items)
            {
                var producto = await _context.Productoss.FindAsync(item.Key);

                if (producto == null) continue;

                comanda.DetallesComanda.Add(new DetalleComanda
                {
                    ProductoId = producto.Id,
                    Cantidad = item.Value.Cantidad,
                    Precio_unitario = producto.Precio,
                    Observacion = item.Value.Observacion
                });
            }

            _context.Comandas.Add(comanda);
            await _context.SaveChangesAsync();
            return Json(new { ok = true, redirect = Url.Action("Index", "Comandas") });
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var comanda = await _context.Comandas
                .Include(c => c.DetallesComanda)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comanda == null) return NotFound();

            var categorias = await _context.Categorias
                .Include(c => c.Productos)
                .ToListAsync();

            var vm = new EditarComandaVM
            {
                ComandaId = comanda.Id,
                NroMesa = comanda.Nro_Mesa,
                Categorias = categorias,
                Items = comanda.DetallesComanda?.ToDictionary(
                    d => d.ProductoId,
                    d => new ItemComandaDTO
                    {
                        Cantidad = d.Cantidad,
                        Observacion = d.Observacion
                    }
                ) ?? new Dictionary<int, ItemComandaDTO>()
            };

            return View(vm);
        }
        [HttpPost]
        public async Task<IActionResult> EditarPedido([FromBody] EditarComandaVM vm)
        {
            var comanda = await _context.Comandas
                .Include(c => c.DetallesComanda)
                .FirstOrDefaultAsync(c => c.Id == vm.ComandaId);

            if (comanda == null) return NotFound();
            if (comanda.DetallesComanda != null && comanda.DetallesComanda.Any())
                _context.DetalleComandas.RemoveRange(comanda.DetallesComanda);

            comanda.Nro_Mesa = vm.NroMesa;

            await _context.SaveChangesAsync();

            var nuevosDetalles = new List<DetalleComanda>();

            foreach (var item in vm.Items)
            {
                if (item.Value.Cantidad <= 0) continue;

                var producto = await _context.Productoss.FindAsync(item.Key);
                if (producto == null) continue;

                nuevosDetalles.Add(new DetalleComanda
                {
                    ComandaId = comanda.Id,
                    ProductoId = producto.Id,
                    Cantidad = item.Value.Cantidad,
                    Precio_unitario = producto.Precio,
                    Observacion = item.Value.Observacion
                });
            }
            _context.DetalleComandas.AddRange(nuevosDetalles);
            await _context.SaveChangesAsync();
            return Json(new { ok = true, redirect = Url.Action("Index", "Comandas") });
        }

        // POST: Comandas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Fecha,Nro_Mesa,Estado,UsuarioId")] Comanda comanda)
        {
            if (id != comanda.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(comanda);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ComandaExists(comanda.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Apellido", comanda.UsuarioId);
            return View(comanda);
        }

        // GET: Comandas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comanda = await _context.Comandas
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (comanda == null)
            {
                return NotFound();
            }

            return View(comanda);
        }

        // POST: Comandas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comanda = await _context.Comandas.FindAsync(id);
            if (comanda != null)
            {
                _context.Comandas.Remove(comanda);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ComandaExists(int id)
        {
            return _context.Comandas.Any(e => e.Id == id);
        }

        [HttpPost]
        public async Task<IActionResult> Cobrar([FromBody] CobrarDTO dto)
        {
            try
            {
                var comanda = await _context.Comandas
                    .Include(c => c.DetallesComanda)
                    .FirstOrDefaultAsync(c => c.Id == dto.ComandaId);

                if (comanda == null)
                {
                    return Json(new
                    {
                        ok = false,
                        msg = "Comanda no encontrada"
                    });
                }

                // BUSCAR CLIENTE
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.NITCI == dto.CiNit);

                // CREAR SI NO EXISTE
                if (cliente == null)
                {
                    cliente = new Cliente
                    {
                        Nombre = dto.NombreCliente,
                        NITCI = dto.CiNit
                    };

                    _context.Clientes.Add(cliente);

                    await _context.SaveChangesAsync();
                }

                // TOTAL
                decimal total = comanda.DetallesComanda
                    .Sum(x => x.Cantidad * x.Precio_unitario);

                // CREAR VENTA
                var venta = new Venta
                {
                    Fecha = DateTime.Now,
                    Total = total,

                    MetodoPago = dto.MetodoPago,

                    ComandaId = comanda.Id,

                    ClienteId = cliente.Id
                };

                _context.Ventas.Add(venta);

                // CAMBIAR ESTADO
                comanda.Estado = EstadoEnum.Cobrado;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    ok = true
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    ok = false,
                    msg = ex.Message
                });
            }
        }
    }
}
