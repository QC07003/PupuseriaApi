using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PupuseriaAPI.Data;
using Telegram.Bot;

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

    // GET: api/comandas
    [HttpGet("comandas")]
    public async Task<IActionResult> GetComandas()
    {
        var pedidos = await _context.Pedidos
            .Include(p => p.Detalles)
            .Where(p => p.Estado == "En Comal" || p.Estado == "Listo")
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        var comandas = pedidos.Select(p => new
        {
            id = p.Id,
            numeroOrden = p.NumeroOrden, // <-- Número de orden real (ej. 6657)
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

        // Notificación automática al usuario por Telegram
        try
        {
            if (pedido.ChatId != 0)
            {
                if (dto.Estado == "Listo")
                {
                    await _botClient.SendMessage(
                        pedido.ChatId, 
                        $"🔥 ¡Tus pupusas de la orden *#{pedido.NumeroOrden}* ya están **LISTAS EN EL COMAL**! 🫓\nPuedes pasar a retirarlas o esperar a tu repartidor.",
                        Telegram.Bot.Types.Enums.ParseMode.Markdown
                    );
                }
                else if (dto.Estado == "Entregado")
                {
                    await _botClient.SendMessage(
                        pedido.ChatId, 
                        $"🛵 Tu pedido *#{pedido.NumeroOrden}* ha sido **DESPACHADO / ENTREGADO**. ¡Muchas gracias por tu compra en Pupusería La Bendición! ¡Buen provecho!",
                        Telegram.Bot.Types.Enums.ParseMode.Markdown
                    );
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al notificar por Telegram: {ex.Message}");
        }

        return Ok(new { message = "Estado actualizado correctamente", nuevoEstado = pedido.Estado });
    }
}

public class EstadoDTO
{
    public string Estado { get; set; } = string.Empty;
}