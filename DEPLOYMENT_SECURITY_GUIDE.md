# 🚀 Guía de Seguridad para el Despliegue y Sanitización de Credenciales

Esta guía detalla los pasos para **eliminar credenciales hardcodeadas** de tu repositorio antes de subirlo a GitHub o desplegarlo en la nube (Azure, Render, Railway, AWS, Docker, etc.).

---

## 1. Archivos con Credenciales que Debes Sanitizar

### Archivo: `src/InventorySystemCloud.Api/appsettings.json`

Antes de hacer `git push` a un repositorio público, reemplaza los valores sensibles por marcadores de posición (`PLACEHOLDERS`).

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=TU_HOST_MYSQL;port=3306;user=TU_USUARIO;password=TU_PASSWORD;database=Inventory;sslmode=Required"
  },
  "Jwt": {
    "SecretKey": "GENERA_UNA_CLAVE_SECRETA_ALEATORIA_DE_AL_MENOS_32_BYTES",
    "Issuer": "InventorySystemCloudApi",
    "Audience": "InventorySystemCloudApp",
    "ExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 7
  },
  "CloudinarySettings": {
    "CloudName": "TU_CLOUD_NAME",
    "ApiKey": "TU_API_KEY",
    "ApiSecret": "TU_API_SECRET"
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "TU_EMAIL_SMTP@gmail.com",
    "SmtpPassword": "TU_APP_PASSWORD_DE_GOOGLE",
    "SenderEmail": "TU_EMAIL_SMTP@gmail.com",
    "SenderName": "InventorySystem Cloud",
    "IsSimulationMode": false
  },
  "CompanySettings": {
    "CompanyName": "InventorySystem Cloud",
    "LegalName": "Global Retail & Distribution Solutions",
    "TaxIdLabel": "Tax ID / NIT / VAT",
    "TaxIdNumber": "US-987654321",
    "Address": "Av. Principal # 100-20, Suite 500",
    "DefaultCurrency": "USD"
  },
  "InitialAdmin": {
    "Email": "admin@inventorycloud.com",
    "Password": "AdminSecurePassword123!"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## 2. Cómo Inyectar las Credenciales en la Nube (Variables de Entorno)

En ASP.NET Core, cualquier valor de `appsettings.json` se puede sobreescribir desde el panel de variables de entorno de tu servidor usando **doble guion bajo (`__`)**:

| Variable de Entorno | Equivalente en `appsettings.json` | Descripción |
| :--- | :--- | :--- |
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings:DefaultConnection` | Cadena de conexión MySQL (Aiven, PlanetScale, RDS, etc.) |
| `Jwt__SecretKey` | `Jwt:SecretKey` | Llave secreta para firmar los tokens JWT (mínimo 32 caracteres) |
| `CloudinarySettings__ApiKey` | `CloudinarySettings:ApiKey` | API Key de Cloudinary |
| `CloudinarySettings__ApiSecret` | `CloudinarySettings:ApiSecret` | API Secret de Cloudinary |
| `CloudinarySettings__CloudName` | `CloudinarySettings:CloudName` | Cloud Name de Cloudinary |
| `EmailSettings__SmtpUser` | `EmailSettings:SmtpUser` | Correo emisor SMTP |
| `EmailSettings__SmtpPassword` | `EmailSettings:SmtpPassword` | Contraseña de aplicación de Gmail/SendGrid |
| `InitialAdmin__Email` | `InitialAdmin:Email` | Correo del Administrador inicial |
| `InitialAdmin__Password` | `InitialAdmin:Password` | Contraseña del Administrador inicial |
| `DemoCashier__Email` | `DemoCashier:Email` | Correo del Cajero Demo (para portafolio) |
| `DemoCashier__Password` | `DemoCashier:Password` | Contraseña del Cajero Demo |
| `EnableDemoSeeding` | `EnableDemoSeeding` | `true` para portafolios, `false` para clientes reales |

---

## 3. Marcas de Código Implementadas en el Proyecto

1. **`Program.cs`:**
   - Se añadió la verificación `EnableDemoSeeding` para controlar si se siembra o no el cajero demo en producción.
   - Se marcó la política de **CORS** para cambiar `localhost:4200` por el dominio real de tu frontend desplegado.
2. **`DataSeeder.cs`:**
   - La función `SeedDemoCashierAsync` ahora lee de configuración y variables de entorno antes de usar valores por defecto.
3. **`.env.example`:**
   - Creado en la raíz del proyecto para que tengas a mano la plantilla lista para copiar y pegar.
