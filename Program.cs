using Microsoft.EntityFrameworkCore;
using PupuseriaAPI.Data;
using PupuseriaAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTodo", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddDbContext<PupuseriaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SupabaseConnection")));

builder.Services.AddHostedService<TelegramBotService>();

var app = builder.Build();

app.UseCors("PermitirTodo");
app.MapControllers();

app.Run();