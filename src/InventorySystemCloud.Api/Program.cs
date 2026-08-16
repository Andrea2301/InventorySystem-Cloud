using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using InventorySystemCloud.Api.Middleware;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Application.Services;
using InventorySystemCloud.Application.Settings;
using InventorySystemCloud.Infrastructure.Data;
using InventorySystemCloud.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var jwt = builder.Configuration.GetSection("Jwt");
var secretKey = jwt["SecretKey"];
var issuer = jwt["Issuer"];
var audience = jwt["Audience"];
if (string.IsNullOrWhiteSpace(secretKey) || Encoding.UTF8.GetByteCount(secretKey) < 32 ||
    string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) ||
    !int.TryParse(jwt["ExpirationMinutes"], out var expirationMinutes) || expirationMinutes is < 5 or > 60)
    throw new InvalidOperationException("La configuración JWT es inválida. Configure una clave de al menos 32 bytes y ExpirationMinutes entre 5 y 60.");

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddPolicy("FrontendDevelopment", policy =>
    policy.WithOrigins("http://localhost:4200", "https://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.InvalidModelStateResponseFactory = _ => new BadRequestObjectResult(new { success = false, message = "La solicitud no es válida." }));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "InventorySystemCloud API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", In = ParameterLocation.Header, Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
    builder.Services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
else
    builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("InventoryDbDev"));
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ISaleService, SaleService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = issuer, ValidateAudience = true, ValidAudience = audience,
        ValidateLifetime = true, RequireExpirationTime = true, ClockSkew = TimeSpan.Zero,
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var publicId = context.Principal?.FindFirstValue("sub");
            var stamp = context.Principal?.FindFirstValue("security_stamp");
            if (!Guid.TryParse(publicId, out var id) || string.IsNullOrEmpty(stamp)) { context.Fail("Token inválido."); return; }
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleOrDefaultAsync(u => u.PublicId == id);
            if (user == null || !user.IsActive || user.SecurityStamp != stamp) context.Fail("Token inválido.");
        }
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
app.UseCors("FrontendDevelopment");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
