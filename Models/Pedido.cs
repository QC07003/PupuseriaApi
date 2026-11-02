using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PupuseriaAPI.Models;

public class Pedido
{
    [Key]
    public int Id { get; set; }
    public long ChatId { get; set; } // <-- Guardará el ID de chat de Telegram del cliente
    public string NumeroOrden { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public decimal Total { get; set; }
    public string Estado { get; set; } = "Pendiente"; // Pendiente, En preparación, Listo

    public List<DetallePedido> Detalles { get; set; } = new();
}

public class DetallePedido
{
    [Key]
    public int Id { get; set; }
    public int PedidoId { get; set; }
    [ForeignKey("PedidoId")]
    public Pedido? Pedido { get; set; }
    
    public string Masa { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Precio { get; set; }
    public decimal Subtotal { get; set; }
}