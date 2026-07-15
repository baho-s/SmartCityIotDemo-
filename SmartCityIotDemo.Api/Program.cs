using Microsoft.EntityFrameworkCore;
using SmartCityIotDemo.Api.Data;
using SmartCityIotDemo.Api.Hubs;
using SmartCityIotDemo.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. SERVICES TO THE CONTAINER (Servis Tanýmlamalarý)
// ==========================================

builder.Services.AddControllers();

// Swagger / OpenAPI (Ýlk koddan korundu)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Veri Tabaný Baðlantýsý (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Real-time Ýletiþim (SignalR)
builder.Services.AddSignalR();

// Arka Plan Görevi (MQTT Telemetry Listener)
builder.Services.AddHostedService<MqttTelemetryWorker>();

// React Frontend Entegrasyonu (CORS Politikasý)
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ==========================================
// 2. HTTP REQUEST PIPELINE (Middleware Katmanlarý)
// ==========================================

// Geliþtirme Ortamý Araçlarý
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Güvenlik ve Að Yönlendirmeleri
app.UseHttpsRedirection();

// CORS Politikasý (Eþleþmenin doðru çalýþmasý için UseRouting ile Map arasýnda olmalýdýr)
app.UseCors("ReactApp");

app.UseAuthorization();

// Endpoint Mappings
app.MapControllers();

// SignalR Hub Endpoint Tanýmlamasý
app.MapHub<TelemetryHub>("/hubs/telemetry");

app.Run();