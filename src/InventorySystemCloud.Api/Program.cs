using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using InventorySystemCloud.Api.Authorization;
using InventorySystemCloud.Api.Middleware;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Application.Services;
using InventorySystemCloud.Application.Settings;
using InventorySystemCloud.Infrastructure.Data;
using InventorySystemCloud.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;

// Configure QuestPDF license (Community License)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);
var jwt = builder.Configuration.GetSection("Jwt");
var secretKey = jwt["SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey) || Encoding.UTF8.GetByteCount(secretKey) < 32)
{
    secretKey = "InventorySystemCloud_Demo_Super_Secret_Key_At_Least_32_Bytes_Long_2026!";
    builder.Configuration["Jwt:SecretKey"] = secretKey;
}

var issuer = jwt["Issuer"];
if (string.IsNullOrWhiteSpace(issuer))
{
    issuer = "InventorySystemCloudApi";
    builder.Configuration["Jwt:Issuer"] = issuer;
}

var audience = jwt["Audience"];
if (string.IsNullOrWhiteSpace(audience))
{
    audience = "InventorySystemCloudApp";
    builder.Configuration["Jwt:Audience"] = audience;
}

if (!int.TryParse(jwt["ExpirationMinutes"], out var expirationMinutes) || expirationMinutes is < 5 or > 60)
{
    expirationMinutes = 60;
    builder.Configuration["Jwt:ExpirationMinutes"] = "60";
}

// Inyección de dependencias

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<CompanySettings>(builder.Configuration.GetSection("CompanySettings"));
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddPolicy("FrontendDevelopment", policy =>
{
    var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    var isDemo = builder.Configuration.GetValue<bool>("IsDemoMode", true);

    if (isDemo || configuredOrigins == null || configuredOrigins.Length == 0 || configuredOrigins.Contains("*"))
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    }
    else
    {
        policy.WithOrigins(configuredOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    }
}));

builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.InvalidModelStateResponseFactory = _ => new BadRequestObjectResult(new { success = false, message = "The request is invalid." }));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "InventorySystemCloud API (Versión Demo)",
        Version = "v1",
        Description = "API de demostración pública para evaluar InventorySystem Cloud.\n\n" +
                      "**Credenciales de Acceso Demo:**\n" +
                      "- **Administrador:** `admin@demo.com` / `AdminDemo123*!` (Control total: Usuarios, Permisos, Auditoría, Reportes, Compras, Ventas)\n" +
                      "- **Cajero:** `cajero@demo.com` / `CashierDemo123*!` (Punto de Venta POS, Facturas PDF, Clientes, Catálogo)\n\n" +
                      "*Puedes consultar `/demo/credentials` o restablecer los datos en cualquier momento con `POST /demo/reset`.*"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", In = ParameterLocation.Header, Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
    builder.Services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
else
    builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("InventoryDbDev"));

builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IImageStorageService, CloudinaryImageStorageService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IPdfInvoiceGenerator, PdfInvoiceGenerator>();
builder.Services.AddScoped<IEmailGenerator, EmailGenerator>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    // Preserve JWT claim names such as `sub`; token validation below reads that exact claim.
    options.MapInboundClaims = false;
    options.RequireHttpsMetadata = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        ClockSkew = TimeSpan.Zero,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
        RoleClaimType = "role"  // Use short claim name instead of the default ClaimTypes.Role URI
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var publicId = context.Principal?.FindFirstValue("sub");
            var stamp = context.Principal?.FindFirstValue("security_stamp");
            if (!Guid.TryParse(publicId, out var id) || string.IsNullOrEmpty(stamp)) { context.Fail("Invalid token."); return; }
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleOrDefaultAsync(u => u.PublicId == id);
            if (user == null || !user.IsActive || user.SecurityStamp != stamp) context.Fail("Invalid token.");
        }
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

// Apply pending migrations and seed initial data automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<AppDbContext>();

    if (!string.IsNullOrEmpty(connectionString))
    {
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error to apply automatic migrations to the database: {Message}", ex.Message);
        }
    }

 
    await DataSeeder.SeedInitialAdminAsync(db, app.Configuration, logger);

    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableDemoSeeding", true))
    {
        await DataSeeder.SeedDemoCashierAsync(db, app.Configuration, logger);
        await DemoDataSeeder.SeedAllDemoDataAsync(db, logger);
    }
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InventorySystemCloud API v1 (Demo)");
    c.DocumentTitle = "InventorySystemCloud - Demo API";
    c.RoutePrefix = string.Empty; // This makes Swagger the home page (root)
});

app.UseHttpsRedirection();



app.UseCors("FrontendDevelopment");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();