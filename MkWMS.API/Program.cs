using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MkWMS.API.Services;
using MkWMS.Data.Context;
using System.Text;
using System.Text.Json.Serialization;
// Добавленные пространства имен для работы генератора токенов:
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// --- СЕРВИСЫ ---
builder.Services.AddHttpContextAccessor();

// Регистрация Интерцептора
builder.Services.AddScoped<MkWMS.API.Interceptors.AuditInterceptor>();

// Регистрация DB Context
builder.Services.AddDbContext<MkWMSDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<MkWMS.API.Interceptors.AuditInterceptor>();
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(interceptor);
});

// Остальные сервисы
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserRoleService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IDocumentPostingService, DocumentPostingService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IPrintService, PrintService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateDocumentDtoValidator>();

// JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not set")))
        };
    });

// Политики для ролей
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Администратор", "Admin"));
    options.AddPolicy("ManagerPolicy", policy => policy.RequireRole("Руководитель", "Manager"));
    options.AddPolicy("OperatorPolicy", policy => policy.RequireRole("Кладовщик", "Operator"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization", // Исправлено на стандартное имя заголовка
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Введите JWT токен. Пример: Bearer eyJhbGciOiJIUzI1NiIs..."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddScoped<DataSeeder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();

    // ──────────────────────────────────────────────────────────────────
    // ВРЕМЕННЫЙ КУСОК: ГЕНЕРАЦИЯ ТОКЕНА ДЛЯ ОБХОДА АВТОРИЗАЦИИ
    // ──────────────────────────────────────────────────────────────────
    try
    {
        var jwtKey = app.Configuration["Jwt:Key"];
        var jwtIssuer = app.Configuration["Jwt:Issuer"];
        var jwtAudience = app.Configuration["Jwt:Audience"];

        if (!string.IsNullOrEmpty(jwtKey))
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),         // Будет спарсено в GetCurrentUserId() как 1
                new Claim(ClaimTypes.Name, "AdminBypass"),
                new Claim(ClaimTypes.Role, "Admin"),               // Для AdminPolicy
                new Claim(ClaimTypes.Role, "Администратор")       // На всякий случай для русской локализации роли
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddDays(14),                 // Токен живет 2 недели
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=================================================================================");
            Console.WriteLine("👉 ТВОЙ ВРЕМЕННЫЙ ТОКЕН ДЛЯ SWAGGER (СКОПИРУЙ ПОЛНОСТЬЮ СЛЕДУЮЩУЮ СТРОКУ):");
            Console.WriteLine($"Bearer {tokenString}");
            Console.WriteLine("=================================================================================\n");
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Не удалось сгенерировать бэкдор-токен: {ex.Message}");
    }
    // ──────────────────────────────────────────────────────────────────
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();