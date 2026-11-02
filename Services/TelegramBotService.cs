//Aquí vivirá toda la lógica de los mensajes y los botones que estructurarán el pedido de pupusas.

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Polling;
using PupuseriaAPI.Data;
using PupuseriaAPI.Models;

namespace PupuseriaAPI.Services;

public class ItemPedido
{
    public string Masa { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Precio { get; set; }
    public decimal Subtotal => Cantidad * Precio;
}

public class EstadoPedido
{
    public string Paso { get; set; } = "Inicio";
    public string NombreCliente { get; set; } = string.Empty;
    public string MasaTemp { get; set; } = string.Empty;
    public string EspecialidadTemp { get; set; } = string.Empty;
    public decimal PrecioTemp { get; set; }
    public List<ItemPedido> Carrito { get; set; } = new();
}

public class TelegramBotService : BackgroundService
{
    private readonly TelegramBotClient _botClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly Dictionary<long, EstadoPedido> _sesiones = new();

    public TelegramBotService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        var token = configuration["TelegramBotToken"] ?? throw new ArgumentNullException("TelegramBotToken");
        _botClient = new TelegramBotClient(token);
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync, 
            receiverOptions: new ReceiverOptions { AllowedUpdates = [] },
            cancellationToken: stoppingToken
        );
        
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is { } message && message.Text is { } messageText)
        {
            var chatId = message.Chat.Id;
            var texto = messageText.ToLower();

            if (texto == "/start" || texto.Contains("hola") || texto.Contains("buenos días") || texto.Contains("buenos dias") || texto.Contains("buenas tardes") || texto.Contains("buenas noches"))
            {
                string nombre = message.From?.FirstName ?? "Cliente";
                _sesiones[chatId] = new EstadoPedido { NombreCliente = nombre };

                var tecladoMasa = new InlineKeyboardMarkup(new[]
                {
                    new [] { InlineKeyboardButton.WithCallbackData("Maíz", "masa_maiz"), InlineKeyboardButton.WithCallbackData("Arroz", "masa_arroz") }
                });

                await botClient.SendMessage(chatId, $"¡Hola {nombre}! Bienvenido a Pupusería La Bendición. ¿De qué tipo de masa deseas tus pupusas?", replyMarkup: tecladoMasa, cancellationToken: cancellationToken);
                return;
            }

            if (_sesiones.TryGetValue(chatId, out var sesion) && sesion.Paso == "EsperandoCantidad")
            {
                if (int.TryParse(messageText, out int cantidad) && cantidad > 0)
                {
                    sesion.Carrito.Add(new ItemPedido {
                        Masa = sesion.MasaTemp,
                        Especialidad = sesion.EspecialidadTemp,
                        Cantidad = cantidad,
                        Precio = sesion.PrecioTemp
                    });

                    sesion.Paso = "PreguntarMas"; 
                    
                    var tecladoMas = new InlineKeyboardMarkup(new[]
                    {
                        new [] { InlineKeyboardButton.WithCallbackData("Sí, agregar más pupusas", "mas_si") },
                        new [] { InlineKeyboardButton.WithCallbackData("No, terminar pedido", "mas_no") }
                    });

                    await botClient.SendMessage(chatId, $"Agregadas {cantidad} pupusas de {sesion.EspecialidadTemp}. ¿Deseas agregar otro tipo?", replyMarkup: tecladoMas, cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendMessage(chatId, "⚠️ Por favor, escribe un número válido mayor a cero (ejemplo: 3).", cancellationToken: cancellationToken);
                }
                return;
            }
            return;
        }

        if (update.CallbackQuery is { } callbackQuery)
        {
            var chatId = callbackQuery.Message!.Chat.Id;
            var seleccion = callbackQuery.Data;

            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

            if (!_sesiones.TryGetValue(chatId, out var sesion)) return;

            if (seleccion != null && seleccion.StartsWith("masa_"))
            {
                sesion.MasaTemp = seleccion == "masa_maiz" ? "Maíz" : "Arroz";
                
                var tecladoIngredientes = new InlineKeyboardMarkup(new[]
                {
                    new [] { InlineKeyboardButton.WithCallbackData("Revueltas ($0.75)", "ing_revueltas") },
                    new [] { InlineKeyboardButton.WithCallbackData("Queso ($1.50)", "ing_queso") },
                    new [] { InlineKeyboardButton.WithCallbackData("Frijol con Queso ($1.00)", "ing_frijol_queso") }
                });

                await botClient.SendMessage(chatId, $"Masa de {sesion.MasaTemp}. Selecciona la especialidad:", replyMarkup: tecladoIngredientes, cancellationToken: cancellationToken);
            }
            else if (seleccion != null && seleccion.StartsWith("ing_"))
            {
                sesion.EspecialidadTemp = seleccion switch { "ing_revueltas" => "Revueltas", "ing_queso" => "Queso", "ing_frijol_queso" => "Frijol con Queso", _ => "Especialidad" };
                sesion.PrecioTemp = seleccion switch { "ing_revueltas" => 0.75m, "ing_queso" => 1.50m, "ing_frijol_queso" => 1.00m, _ => 0.00m };

                sesion.Paso = "EsperandoCantidad";
                await botClient.SendMessage(chatId, $"¿Cuántas pupusas de {sesion.EspecialidadTemp} deseas?", cancellationToken: cancellationToken);
            }
            else if (seleccion == "mas_si")
            {
                var tecladoMasa = new InlineKeyboardMarkup(new[]
                {
                    new [] { InlineKeyboardButton.WithCallbackData("Maíz", "masa_maiz"), InlineKeyboardButton.WithCallbackData("Arroz", "masa_arroz") }
                });
                await botClient.SendMessage(chatId, "¿De qué tipo de masa serán las siguientes?", replyMarkup: tecladoMasa, cancellationToken: cancellationToken);
            }
            else if (seleccion == "mas_no")
            {
                decimal total = 0;
                string resumen = $"📝 *Resumen de tu pedido*\nCliente: {sesion.NombreCliente}\n\n";
                foreach(var item in sesion.Carrito)
                {
                    resumen += $"- {item.Cantidad}x {item.Especialidad} ({item.Masa}) : ${item.Subtotal:F2}\n";
                    total += item.Subtotal;
                }
                resumen += $"\n💵 *Total a pagar: ${total:F2}*\n\n¿El pedido está correcto?";

                var tecladoConfirmacion = new InlineKeyboardMarkup(new[]
                {
                    new [] { InlineKeyboardButton.WithCallbackData("✅ Confirmar Pedido", "conf_si") },
                    new [] { InlineKeyboardButton.WithCallbackData("✏️ Empezar de nuevo", "conf_reiniciar") },
                    new [] { InlineKeyboardButton.WithCallbackData("❌ Cancelar", "conf_no") }
                });

                sesion.Paso = "EsperandoConfirmacion";
                await botClient.SendMessage(chatId, resumen, Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: tecladoConfirmacion, cancellationToken: cancellationToken);
            }
            // --- GUARDAR PEDIDO EN POSTGRESQL LOCAL ---
            else if (seleccion == "conf_si")
            {
                string numeroOrden = new Random().Next(1000, 9999).ToString();
                decimal totalPedido = sesion.Carrito.Sum(x => x.Subtotal);

                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<PupuseriaDbContext>();

                    var nuevoPedido = new Pedido
                    {
                        ChatId = chatId, // <-- Asignamos el chatId del cliente
                        NumeroOrden = numeroOrden,
                        ClienteNombre = sesion.NombreCliente,
                        FechaCreacion = DateTime.UtcNow,
                        Total = totalPedido,
                        Estado = "En Comal",
                        Detalles = sesion.Carrito.Select(i => new DetallePedido
                        {
                            Masa = i.Masa,
                            Especialidad = i.Especialidad,
                            Cantidad = i.Cantidad,
                            Precio = i.Precio,
                            Subtotal = i.Subtotal
                        }).ToList()
                    };

                    dbContext.Pedidos.Add(nuevoPedido);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                await botClient.SendMessage(chatId, $"✅ ¡Pedido #{numeroOrden} confirmado y guardado! En un momento lo pasamos a nuestra pantalla de cocina.", cancellationToken: cancellationToken);
                _sesiones.Remove(chatId);
            }
            else if (seleccion == "conf_reiniciar")
            {
                sesion.Carrito.Clear();
                sesion.Paso = "Inicio";
                var tecladoMasa = new InlineKeyboardMarkup(new[]
                {
                    new [] { InlineKeyboardButton.WithCallbackData("Maíz", "masa_maiz"), InlineKeyboardButton.WithCallbackData("Arroz", "masa_arroz") }
                });
                await botClient.SendMessage(chatId, "¡Carrito limpio! ¿De qué tipo de masa deseas tus pupusas para empezar de nuevo?", replyMarkup: tecladoMasa, cancellationToken: cancellationToken);
            }
            else if (seleccion == "conf_no")
            {
                _sesiones.Remove(chatId);
                await botClient.SendMessage(chatId, "❌ Pedido cancelado. Envía un saludo o escribe /start cuando desees ordenar nuevamente.", cancellationToken: cancellationToken);
            }
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}