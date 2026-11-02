using Microsoft.EntityFrameworkCore;
using PupuseriaAPI.Models;

namespace PupuseriaAPI.Data;

public class PupuseriaDbContext : DbContext
{
    public PupuseriaDbContext(DbContextOptions<PupuseriaDbContext> options) : base(options) { }

    public DbSet<Pedido> Pedidos { get; set; }
    public DbSet<DetallePedido> DetallesPedido { get; set; }
}