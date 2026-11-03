using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PupuseriaAPI.Data;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace PupuseriaAPI.Controllers;

[ApiController]
[Route("api")]
public class PedidosController : ControllerBase
{
    private readonly PupuseriaDbContext _context;
    private readonly ITelegramBotClient _botClient;

    public PedidosController(PupuseriaDbContext context, IConfiguration configuration)
    {
        _context = context;
        var token = configuration["TelegramBotToken"] ?? throw new ArgumentNullException("TelegramBotToken");
        _botClient = new TelegramBotClient(token);
    }

    // GET: api/pedidos (Para el DASHBOARD: devuelve TODOS los pedidos sin filtrar)
    [HttpGet("pedidos")]
    public async Task<IActionResult> GetPedidos()
    {
        var pedidos = await _context.Pedidos
            .Include(p => p.Detalles)
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        var resultado = pedidos.Select(p => new
        {
            id = p.Id,
            numeroOrden = p.NumeroOrden,
            cliente = p.ClienteNombre,
            tipo_entrega = "Telegram",
            estado = p.Estado,
            total = p.Total,
            creado_en = p.FechaCreacion.ToString("o"),
            hora = p.FechaCreacion.ToLocalTime().ToString("hh:mm tt"),
            items = p.Detalles.Select(d => new
            {
                cantidad = d.Cantidad,
                producto = $"Pupusa {d.Especialidad}",
                tipo_masa = d.Masa,
                precio = d.Precio,
                subtotal = d.Subtotal
            })
        });

        return Ok(resultado);
    }

    // GET: api/comandas (Para la COCINA: solo muestra pendientes / en proceso)
    [HttpGet("comandas")]
    public async Task<IActionResult> GetComandas()
    {
        var pedidos = await _context.Pedidos
            .Include(p => p.Detalles)
            .Where(p => p.Estado == "Pendiente" || p.Estado == "En Comal" || p.Estado == "Listo")
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        var comandas = pedidos.Select(p => new
        {
            id = p.Id,
            numeroOrden = p.NumeroOrden,
            cliente = p.ClienteNombre,
            tipo_entrega = "Telegram",
            estado = p.Estado,
            total = p.Total,
            creado_en = p.FechaCreacion.ToString("o"),
            hora = p.FechaCreacion.ToLocalTime().ToString("hh:mm tt"),
            items = p.Detalles.Select(d => new
            {
                cantidad = d.Cantidad,
                producto = $"Pupusa {d.Especialidad}",
                tipo_masa = d.Masa
            })
        });

        return Ok(comandas);
    }

    // PATCH: api/pedidos/{id}/estado
    [HttpPatch("pedidos/{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] EstadoDTO dto)
    {
        var pedido = await _context.Pedidos.FindAsync(id);
        if (pedido == null) return NotFound();

        pedido.Estado = dto.Estado;
        await _context.SaveChangesAsync();

        try
        {
            if (pedido.ChatId != 0)
            {
                string estadoNormalizado = dto.Estado.Trim();

                if (estadoNormalizado.Equals("Listo", StringComparison.OrdinalIgnoreCase))
                {
                    string mensajeListo = $"🔥 ¡Tus pupusas de la orden *#{pedido.NumeroOrden}* ya están *LISTAS EN EL COMAL*! 🫓\n\nPuedes pasar a retirarlas o esperar a tu repartidor.";

                    await _botClient.SendMessage(
                        pedido.ChatId, 
                        mensajeListo,
                        ParseMode.Markdown
                    );
                }
                else if (estadoNormalizado.Equals("Entregado", StringComparison.OrdinalIgnoreCase) || 
                         estadoNormalizado.Equals("Despachado", StringComparison.OrdinalIgnoreCase))
                {
                    string mensajeEntregado = $"🛵 Tu pedido *#{pedido.NumeroOrden}* ha sido *DESPACHADO / ENTREGADO*. ¡Muchas gracias por tu compra en Pupusería La Bendición! ¡Buen provecho!";

                    await _botClient.SendMessage(
                        pedido.ChatId, 
                        mensajeEntregado,
                        ParseMode.Markdown
                    );
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Telegram Error] ChatId {pedido.ChatId}: {ex.Message}");
        }

        return Ok(new { message = "Estado actualizado correctamente", nuevoEstado = pedido.Estado });
    }
}

public class EstadoDTO
{
    public string Estado { get; set; } = string.Empty;
}