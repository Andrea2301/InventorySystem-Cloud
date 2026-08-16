using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Purchases;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Shared;
using Microsoft.EntityFrameworkCore;

namespace InventorySystemCloud.Application.Services
{
    public class PurchaseService : IPurchaseService
    {
        private readonly IAppDbContext _context;
        private readonly IAuditService _auditService;

        public PurchaseService(IAppDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<ApiResponse<PurchaseResponseDto>> CreatePurchaseAsync(CreatePurchaseDto request, Guid userPublicId)
        {
            if (request.Items == null || request.Items.Count == 0)
                return ApiResponse<PurchaseResponseDto>.FailureResponse("La orden de compra debe contener al menos un producto.", statusCode: 400);

            // 1. Validate User
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PublicId == userPublicId);
            if (user == null || !user.IsActive)
                return ApiResponse<PurchaseResponseDto>.FailureResponse("Usuario no autorizado o inactivo.", statusCode: 401);

            // 2. Validate Supplier
            var supplier = await _context.Suppliers.FindAsync(request.SupplierId);
            if (supplier == null)
                return ApiResponse<PurchaseResponseDto>.FailureResponse("El proveedor especificado no existe.", statusCode: 404);

            if (!supplier.IsActive)
                return ApiResponse<PurchaseResponseDto>.FailureResponse("El proveedor especificado se encuentra inactivo.", statusCode: 400);

            // 3. Validate & Process Products / Stock
            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var purchaseDetails = new List<PurchaseDetail>();
            decimal calculatedTotal = 0;

            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                    return ApiResponse<PurchaseResponseDto>.FailureResponse($"La cantidad para el producto ID {item.ProductId} debe ser mayor a cero.", statusCode: 400);

                if (item.UnitPrice < 0)
                    return ApiResponse<PurchaseResponseDto>.FailureResponse($"El costo unitario para el producto ID {item.ProductId} no puede ser negativo.", statusCode: 400);

                if (!products.TryGetValue(item.ProductId, out var product))
                    return ApiResponse<PurchaseResponseDto>.FailureResponse($"El producto con ID {item.ProductId} no fue encontrado.", statusCode: 404);

                if (!product.IsActive)
                    return ApiResponse<PurchaseResponseDto>.FailureResponse($"El producto '{product.Name}' no está activo en el catálogo.", statusCode: 400);

                // Increment stock
                product.Quantity += item.Quantity;

                var lineTotal = item.UnitPrice * item.Quantity;
                calculatedTotal += lineTotal;

                purchaseDetails.Add(new PurchaseDetail
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = lineTotal
                });
            }

            // 4. Create Purchase
            var purchase = new Purchase
            {
                SupplierId = supplier.Id,
                CreatedByUserId = user.Id,
                PurchaseDate = DateTime.UtcNow,
                TotalAmount = calculatedTotal,
                InvoiceNumber = string.IsNullOrWhiteSpace(request.InvoiceNumber) ? null : request.InvoiceNumber.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "COP" : request.Currency.Trim(),
                PurchaseDetails = purchaseDetails
            };

            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            // 5. Audit Trail
            await _auditService.LogActionAsync(
                user.Id,
                "CREATE_PURCHASE",
                $"Compra #{purchase.Id} registrada para proveedor {supplier.CompanyName} por total de {purchase.TotalAmount:C2} {purchase.Currency}");

            var responseDto = MapToResponseDto(purchase, supplier, user);
            return ApiResponse<PurchaseResponseDto>.SuccessResponse(responseDto, "Compra registrada y stock actualizado exitosamente.", statusCode: 201);
        }

        public async Task<ApiResponse<IEnumerable<PurchaseResponseDto>>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null, int? supplierId = null)
        {
            var query = _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.CreatedBy)
                .Include(p => p.PurchaseDetails)
                    .ThenInclude(pd => pd.Product)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(p => p.PurchaseDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.PurchaseDate <= endDate.Value);

            if (supplierId.HasValue)
                query = query.Where(p => p.SupplierId == supplierId.Value);

            var purchases = await query
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            var result = purchases.Select(p => MapToResponseDto(p, p.Supplier, p.CreatedBy));
            return ApiResponse<IEnumerable<PurchaseResponseDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse<PurchaseResponseDto>> GetByIdAsync(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.CreatedBy)
                .Include(p => p.PurchaseDetails)
                    .ThenInclude(pd => pd.Product)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null)
                return ApiResponse<PurchaseResponseDto>.FailureResponse("Compra no encontrada.", statusCode: 404);

            return ApiResponse<PurchaseResponseDto>.SuccessResponse(MapToResponseDto(purchase, purchase.Supplier, purchase.CreatedBy));
        }

        public async Task<ApiResponse<PurchaseReportDto>> GetDailyReportAsync(DateTime? date = null)
        {
            var targetDate = (date ?? DateTime.UtcNow).Date;
            var nextDate = targetDate.AddDays(1);

            var purchases = await _context.Purchases
                .Include(p => p.PurchaseDetails)
                .Where(p => p.PurchaseDate >= targetDate && p.PurchaseDate < nextDate)
                .ToListAsync();

            var totalPurchasesCount = purchases.Count;
            var totalSpent = purchases.Sum(p => p.TotalAmount);
            var averagePurchaseCost = totalPurchasesCount > 0 ? totalSpent / totalPurchasesCount : 0;
            var totalItemsPurchased = purchases.SelectMany(p => p.PurchaseDetails).Sum(pd => pd.Quantity);

            var report = new PurchaseReportDto
            {
                Date = targetDate,
                TotalPurchasesCount = totalPurchasesCount,
                TotalSpent = totalSpent,
                AveragePurchaseCost = Math.Round(averagePurchaseCost, 2),
                TotalItemsPurchased = totalItemsPurchased
            };

            return ApiResponse<PurchaseReportDto>.SuccessResponse(report);
        }

        private static PurchaseResponseDto MapToResponseDto(Purchase p, Supplier? supplier, User? user)
        {
            return new PurchaseResponseDto
            {
                Id = p.Id,
                SupplierId = p.SupplierId,
                SupplierName = supplier?.CompanyName ?? "Proveedor General",
                SupplierEmail = supplier?.Email ?? "N/A",
                CreatedByUserId = p.CreatedByUserId,
                CreatedByEmail = user?.Email,
                PurchaseDate = p.PurchaseDate,
                TotalAmount = p.TotalAmount,
                InvoiceNumber = p.InvoiceNumber,
                Notes = p.Notes,
                Currency = p.Currency,
                Items = p.PurchaseDetails.Select(pd => new PurchaseDetailResponseDto
                {
                    Id = pd.Id,
                    ProductId = pd.ProductId,
                    ProductName = pd.Product?.Name ?? $"Producto #{pd.ProductId}",
                    Quantity = pd.Quantity,
                    UnitPrice = pd.UnitPrice,
                    TotalPrice = pd.TotalPrice
                }).ToList()
            };
        }
    }
}
