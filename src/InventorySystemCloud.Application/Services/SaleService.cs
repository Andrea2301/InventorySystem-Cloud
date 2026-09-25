using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Sales;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Shared;
using Microsoft.EntityFrameworkCore;

namespace InventorySystemCloud.Application.Services
{
    public class SaleService : ISaleService
    {
        private readonly IAppDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IPdfInvoiceGenerator _pdfInvoiceGenerator;
        private readonly IEmailService _emailService;

        public SaleService(
            IAppDbContext context, 
            IAuditService auditService,
            IPdfInvoiceGenerator pdfInvoiceGenerator,
            IEmailService emailService)
        {
            _context = context;
            _auditService = auditService;
            _pdfInvoiceGenerator = pdfInvoiceGenerator;
            _emailService = emailService;
        }

        public async Task<ApiResponse<SaleResponseDto>> CreateSaleAsync(CreateSaleDto request, Guid userPublicId)
        {
            if (request.Items == null || request.Items.Count == 0)
                return ApiResponse<SaleResponseDto>.FailureResponse("The sale must contain at least one product.", statusCode: 400);

            // 1. Validate User
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PublicId == userPublicId);
            if (user == null || !user.IsActive)
                return ApiResponse<SaleResponseDto>.FailureResponse("Unauthorized or inactive user.", statusCode: 401);

            // 2. Validate Client
            var client = await _context.Clients.FindAsync(request.ClientId);
            if (client == null)
                return ApiResponse<SaleResponseDto>.FailureResponse("The specified client does not exist.", statusCode: 404);

            if (!client.IsActive)
                return ApiResponse<SaleResponseDto>.FailureResponse("The specified client is inactive.", statusCode: 400);

            // 3. Validate & Process Products / Stock
            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var saleDetails = new List<SaleDetail>();
            decimal calculatedTotal = 0;

            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                    return ApiResponse<SaleResponseDto>.FailureResponse($"The quantity for product ID {item.ProductId} must be greater than zero.", statusCode: 400);

                if (!products.TryGetValue(item.ProductId, out var product))
                    return ApiResponse<SaleResponseDto>.FailureResponse($"The product with ID {item.ProductId} was not found.", statusCode: 404);

                if (!product.IsActive)
                    return ApiResponse<SaleResponseDto>.FailureResponse($"The product '{product.Name}' is not active for sale.", statusCode: 400);

                if (product.Quantity < item.Quantity)
                    return ApiResponse<SaleResponseDto>.FailureResponse($"Insufficient stock for '{product.Name}'. Available stock: {product.Quantity}, Requested: {item.Quantity}.", statusCode: 400);

                // Deduct stock
                product.Quantity -= item.Quantity;

                var lineTotal = product.Price * item.Quantity;
                calculatedTotal += lineTotal;

                saleDetails.Add(new SaleDetail
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = lineTotal
                });
            }

            // 4. Validate Payment
            if (request.AmountPaid < calculatedTotal)
            {
                return ApiResponse<SaleResponseDto>.FailureResponse(
                    $"The amount paid ({request.AmountPaid:N2}) is less than the total of the sale ({calculatedTotal:N2}).", statusCode: 400);
            }

            var changeDue = request.AmountPaid - calculatedTotal;

            // 5. Create Sale
            var sale = new Sale
            {
                ClientId = client.Id,
                CreatedByUserId = user.Id,
                SaleDate = DateTime.UtcNow,
                TotalAmount = calculatedTotal,
                PaymentMethod = request.PaymentMethod.Trim(),
                AmountPaid = request.AmountPaid,
                ChangeDue = changeDue,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "COP" : request.Currency.Trim(),
                SaleDetails = saleDetails
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            // 6. Audit Trail
            await _auditService.LogActionAsync(
                user.Id,
                "CREATE_SALE",
                $"Sale #{sale.Id} registered for client {client.FullName} ({client.DocumentNumber}) for a total of {sale.TotalAmount:C2} {sale.Currency}");

            var responseDto = MapToResponseDto(sale, client, user);

            // 7. Auto-send digital invoice via email if client has an email
            if (!string.IsNullOrWhiteSpace(client.Email))
            {
                try
                {
                    var pdfBytes = _pdfInvoiceGenerator.GenerateInvoicePdf(responseDto);
                    await _emailService.SendInvoiceEmailAsync(client.Email, responseDto, pdfBytes);
                }
                catch
                {
                    // Email delivery failure should not cancel the committed sale
                }
            }

            return ApiResponse<SaleResponseDto>.SuccessResponse(responseDto, "Sale registered successfully.", statusCode: 201);
        }

        public async Task<ApiResponse<IEnumerable<SaleResponseDto>>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null, int? clientId = null)
        {
            var query = _context.Sales
                .Include(s => s.Client)
                .Include(s => s.CreatedBy)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(s => s.SaleDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.SaleDate <= endDate.Value);

            if (clientId.HasValue)
                query = query.Where(s => s.ClientId == clientId.Value);

            var sales = await query
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            var result = sales.Select(s => MapToResponseDto(s, s.Client, s.CreatedBy));
            return ApiResponse<IEnumerable<SaleResponseDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse<SaleResponseDto>> GetByIdAsync(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.Client)
                .Include(s => s.CreatedBy)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
                return ApiResponse<SaleResponseDto>.FailureResponse("Sale not found.", statusCode: 404);

            return ApiResponse<SaleResponseDto>.SuccessResponse(MapToResponseDto(sale, sale.Client, sale.CreatedBy));
        }

        public async Task<ApiResponse<byte[]>> GetInvoicePdfAsync(int saleId)
        {
            var saleResult = await GetByIdAsync(saleId);
            if (!saleResult.Success || saleResult.Data == null)
                return ApiResponse<byte[]>.FailureResponse(saleResult.Message, statusCode: saleResult.StatusCode);

            try
            {
                var pdfBytes = _pdfInvoiceGenerator.GenerateInvoicePdf(saleResult.Data);
                return ApiResponse<byte[]>.SuccessResponse(pdfBytes, "Invoice generated successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<byte[]>.FailureResponse($"Error generating the digital invoice: {ex.Message}", statusCode: 500);
            }
        }

        public async Task<ApiResponse<string>> SendInvoiceEmailAsync(int saleId)
        {
            var sale = await _context.Sales
                .Include(s => s.Client)
                .Include(s => s.CreatedBy)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
                return ApiResponse<string>.FailureResponse("Sale not found.", statusCode: 404);

            if (sale.Client == null || string.IsNullOrWhiteSpace(sale.Client.Email))
                return ApiResponse<string>.FailureResponse("The client of this sale does not have an email address registered.", statusCode: 400);

            try
            {
                var saleDto = MapToResponseDto(sale, sale.Client, sale.CreatedBy);
                var pdfBytes = _pdfInvoiceGenerator.GenerateInvoicePdf(saleDto);
                await _emailService.SendInvoiceEmailAsync(sale.Client.Email, saleDto, pdfBytes);

                return ApiResponse<string>.SuccessResponse("Invoice sent successfully to the client's email.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.FailureResponse($"Error generating the digital invoice: {ex.Message}", statusCode: 500);
            }
        }

        public async Task<ApiResponse<SaleReportDto>> GetDailyReportAsync(DateTime? date = null)
        {
            var targetDate = (date ?? DateTime.UtcNow).Date;
            var nextDate = targetDate.AddDays(1);

            var sales = await _context.Sales
                .Include(s => s.SaleDetails)
                .Where(s => s.SaleDate >= targetDate && s.SaleDate < nextDate)
                .ToListAsync();

            var totalSalesCount = sales.Count;
            var totalRevenue = sales.Sum(s => s.TotalAmount);
            var averageTicket = totalSalesCount > 0 ? totalRevenue / totalSalesCount : 0;
            var totalItemsSold = sales.SelectMany(s => s.SaleDetails).Sum(sd => sd.Quantity);

            var report = new SaleReportDto
            {
                Date = targetDate,
                TotalSalesCount = totalSalesCount,
                TotalRevenue = totalRevenue,
                AverageTicket = Math.Round(averageTicket, 2),
                TotalItemsSold = totalItemsSold
            };

            return ApiResponse<SaleReportDto>.SuccessResponse(report);
        }

        private static SaleResponseDto MapToResponseDto(Sale s, Client? client, User? user)
        {
            return new SaleResponseDto
            {
                Id = s.Id,
                ClientId = s.ClientId,
                ClientName = client?.FullName ?? "General Client",
                ClientDocument = client?.DocumentNumber ?? "N/A",
                CreatedByUserId = s.CreatedByUserId,
                CreatedByEmail = user?.Email,
                SaleDate = s.SaleDate,
                TotalAmount = s.TotalAmount,
                PaymentMethod = s.PaymentMethod,
                AmountPaid = s.AmountPaid,
                ChangeDue = s.ChangeDue,
                Currency = s.Currency,
                Items = s.SaleDetails.Select(sd => new SaleDetailResponseDto
                {
                    Id = sd.Id,
                    ProductId = sd.ProductId,
                    ProductName = sd.Product?.Name ?? $"Product #{sd.ProductId}",
                    Quantity = sd.Quantity,
                    UnitPrice = sd.UnitPrice,
                    TotalPrice = sd.TotalPrice
                }).ToList()
            };
        }
    }
}
