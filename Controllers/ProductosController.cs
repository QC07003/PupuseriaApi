using Microsoft.AspNetCore.Mvc;

namespace PupuseriaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetProductos()
        {
            var productos = new[]
            {
                new { id = 1, nombre = "Pupusa Revuelta", descripcion = "Chicharrón, frijol y queso", precio = 1.00 },
                new { id = 2, nombre = "Pupusa Queso con Loroco", descripcion = "Queso especial con loroco", precio = 1.00 },
                new { id = 3, nombre = "Pupusa Jalapeño", descripcion = "Queso con jalapeño", precio = 1.25 },
                new { id = 4, nombre = "Fresco de Ensalada", descripcion = "Bebida natural", precio = 1.00 },
                new { id = 5, nombre = "Chocolate de Olla", descripcion = "Bebida caliente", precio = 1.25 }
            };

            return Ok(productos);
        }
    }
}