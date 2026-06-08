using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.RulesetToEditorconfig;
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
        private readonly IConverter _converter;

        public ComandasController(MiContext context, IConverter converter)
        {
            _context = context;
            _converter = converter;
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
                .Include(c => c.Productos.Where(p => p.Disponible))
                .Where(c => c.Productos.Any(p => p.Disponible))
                .ToList();

            var estadosActivos = new[]
            {
        EstadoEnum.Pendiente,
        EstadoEnum.EnPreparacion,
        EstadoEnum.ListoParaServir,
        EstadoEnum.Servido
    };

            var mesasOcupadas = _context.Comandas
                .Where(c => estadosActivos.Contains(c.Estado))
                .Select(c => c.Nro_Mesa)
                .Distinct()
                .ToList();

            var mesasDisponibles = Enumerable.Range(1, 10)
                .Where(m => !mesasOcupadas.Contains(m))
                .ToList();

            var vm = new CrearComandaVM
            {
                Categorias = categorias,
                MesasDisponibles = mesasDisponibles
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

            var existeMesaOcupada = await _context.Comandas.AnyAsync(c =>
    c.Nro_Mesa == vm.NroMesa &&
    (
        c.Estado == EstadoEnum.Pendiente ||
        c.Estado == EstadoEnum.EnPreparacion ||
        c.Estado == EstadoEnum.ListoParaServir ||
        c.Estado == EstadoEnum.Servido
    ));

            if (existeMesaOcupada)
            {
                return Json(new
                {
                    ok = false,
                    msg = "La mesa ya tiene una comanda activa."
                });
            }


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
                .Include(c => c.Productos.Where(p => p.Disponible))
                .Where(c => c.Productos.Any(p => p.Disponible))
                .ToListAsync();

            var estadosActivos = new[]
            {
        EstadoEnum.Pendiente,
        EstadoEnum.EnPreparacion,
        EstadoEnum.ListoParaServir,
        EstadoEnum.Servido
    };
            var mesasOcupadas = await _context.Comandas
                .Where(c => estadosActivos.Contains(c.Estado) && c.Id != comanda.Id)
                .Select(c => c.Nro_Mesa)
                .Distinct()
                .ToListAsync();

            var mesasDisponibles = Enumerable.Range(1, 10)
                .Where(m => !mesasOcupadas.Contains(m))
                .ToList();

            var vm = new EditarComandaVM
            {
                ComandaId = comanda.Id,
                NroMesa = comanda.Nro_Mesa,
                Categorias = categorias,
                MesasDisponibles = mesasDisponibles,   
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
                    .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(c => c.Id == dto.ComandaId);

                if (comanda == null)
                    return Json(new { ok = false, msg = "Comanda no encontrada" });

                if (string.IsNullOrWhiteSpace(dto.NombreCliente))
                {
                    return Json(new
                    {
                        ok = false,
                        msg = "Debe ingresar el nombre del cliente"
                    });
                }

                if (!Enum.IsDefined(typeof(PagoEnum), dto.MetodoPago))
                {
                    return Json(new
                    {
                        ok = false,
                        msg = "Debe seleccionar un método de pago"
                    });
                }


                bool imprimirFactura = !string.IsNullOrEmpty(dto.CiNit) && dto.CiNit != "00000000";

                Cliente cliente;

                if (imprimirFactura)
                {
                    cliente = await _context.Clientes
                        .FirstOrDefaultAsync(c => c.NITCI == dto.CiNit);

                    if (cliente == null)
                    {
                        cliente = new Cliente
                        {
                            Nombre = string.IsNullOrEmpty(dto.NombreCliente) 
                            ? "Cliente" 
                            : dto.NombreCliente,
                            NITCI = dto.CiNit
                        };
                        _context.Clientes.Add(cliente);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    cliente = new Cliente
                    {
                        Nombre = "Ocasional",
                        NITCI = null
                    };
                    _context.Clientes.Add(cliente);
                    await _context.SaveChangesAsync();
                }

                decimal total = comanda.DetallesComanda
                    .Sum(x => x.Cantidad * x.Precio_unitario);

                var venta = new Venta
                {
                    Fecha = DateTime.Now,
                    Total = total,
                    MetodoPago = dto.MetodoPago,
                    ComandaId = comanda.Id,
                    ClienteId = cliente.Id
                };

                _context.Ventas.Add(venta);
                comanda.Estado = EstadoEnum.Cobrado;
                await _context.SaveChangesAsync();

                string base64 = null;

                if (imprimirFactura)
                {
                    var metodoPagoTexto = dto.MetodoPago == PagoEnum.Efectivo ? "Efectivo" :
                                          dto.MetodoPago == PagoEnum.QR ? "QR" : "Tarjeta";

                    var filas = string.Join("", comanda.DetallesComanda.Select(d =>
                        $@"<tr>
                    <td>{d.Producto?.Nombre}</td>
                    <td style='text-align:center'>{d.Cantidad}</td>
                    <td style='text-align:right'>{d.Precio_unitario:N2}</td>
                    <td style='text-align:right'>{d.Cantidad * d.Precio_unitario:N2}</td>
                </tr>"));

                    var html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: 'Courier New', monospace; font-size: 13px; padding: 20px; max-width: 320px; margin: auto; color: #111; }}
                    .header {{ text-align: center; border-bottom: 1px dashed #aaa; padding-bottom: 10px; margin-bottom: 14px; }}
                    .header h2 {{ font-size: 18px; letter-spacing: 2px; margin-bottom: 4px; }}
                    .header p {{ font-size: 11px; color: #555; margin: 2px 0; }}
                    .info {{ font-size: 12px; line-height: 1.9; margin-bottom: 12px; }}
                    .info b {{ font-weight: bold; }}
                    table {{ width: 100%; border-collapse: collapse; font-size: 12px; margin-bottom: 12px; }}
                    thead tr {{ border-top: 1px dashed #aaa; border-bottom: 1px dashed #aaa; }}
                    th {{ padding: 5px 2px; font-size: 11px; text-align: left; }}
                    th:nth-child(2) {{ text-align: center; }}
                    th:nth-child(3), th:nth-child(4) {{ text-align: right; }}
                    td {{ padding: 4px 2px; }}
                    .total-section {{ border-top: 1px dashed #aaa; padding-top: 10px; }}
                    .total-final {{ display: flex; justify-content: space-between; font-size: 16px; font-weight: bold; border-top: 1px dashed #aaa; padding-top: 6px; margin-top: 6px; }}
                    .metodo {{ font-size: 11px; color: #555; text-align: right; margin-top: 4px; }}
                    .footer {{ text-align: center; border-top: 1px dashed #aaa; margin-top: 16px; padding-top: 10px; font-size: 11px; color: #777; line-height: 1.8; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <h2>OVEJA NEGRA</h2>
                    <p>Restaurante &amp; Bar</p>
                    <p>─────────────────────</p>
                    <p><strong>FACTURA #{venta.Id:D6}</strong></p>
                    <p>{venta.Fecha:dd/MM/yyyy HH:mm}</p>
                </div>
                <div class='info'>
                    <div><b>Cliente:</b> {cliente.Nombre}</div>
                    <div><b>CI/NIT:</b> {cliente.NITCI}</div>
                    <div><b>Mesa:</b> {comanda.Nro_Mesa}</div>
                    <div><b>Pago:</b> {metodoPagoTexto}</div>
                    <div><b>Comanda:</b> #{comanda.Id:D3}</div>
                </div>
                <table>
                    <thead>
                        <tr>
                            <th>Producto</th>
                            <th>Cant</th>
                            <th>P.U.</th>
                            <th>Sub.</th>
                        </tr>
                    </thead>
                    <tbody>{filas}</tbody>
                </table>
                <div class='total-section'>
                    <div class='total-final'>
                        <span>TOTAL</span>
                        <span>{total:N2} Bs.</span>
                    </div>
                    <div class='metodo'>Metodo de pago: {metodoPagoTexto}</div>
                </div>
                <div class='footer'>
                    <p>Gracias por su visita!</p>
                    <p>Vuelva pronto</p>
                </div>
            </body>
            </html>";

                    var pdf = _converter.Convert(new HtmlToPdfDocument()
                    {
                        GlobalSettings = new GlobalSettings
                        {
                            PaperSize = PaperKind.A6,
                            Orientation = Orientation.Portrait,
                            Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
                        },
                        Objects = { new ObjectSettings { HtmlContent = html } }
                    });

                    base64 = Convert.ToBase64String(pdf);
                }

                return Json(new { ok = true, pdf = base64 });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message });
            }
        }
    }
}
