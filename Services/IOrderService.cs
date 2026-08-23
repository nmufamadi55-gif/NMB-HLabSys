//using Microsoft.EntityFrameworkCore;
//using NMB_HLabSys.Data;
//using NMB_HLabSys.Models;
//using NMB_HLabSys.Models.ViewModels;

//namespace NMB_HLabSys.Services
//{
//    public interface IOrderService
//    {
//        Task<PurchaseOrder> CreateOrderAsync(CreateOrderViewModel model, int createdBy);
//        Task<PurchaseOrder> ReceiveOrderAsync(int orderId, ReceiveOrderViewModel model, int receivedBy);
//        Task<PurchaseOrder> CancelOrderAsync(int orderId, string reason);
//        Task<List<OrderListViewModel>> GetOrderListAsync(params string[] statuses);
//        Task<OrderDetailViewModel> GetOrderDetailsAsync(int orderId);
//    }

//    public class OrderService : IOrderService
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IStockManagementService _stockService;
//        private readonly ILogger<OrderService> _logger;

//        public OrderService(ApplicationDbContext context, IStockManagementService stockService, ILogger<OrderService> logger)
//        {
//            _context = context;
//            _stockService = stockService;
//            _logger = logger;
//        }

//        public async Task<PurchaseOrder> CreateOrderAsync(CreateOrderViewModel model, int createdBy)
//        {
//            using var transaction = await _context.Database.BeginTransactionAsync();
//            try
//            {
//                var selectedItems = model.Items.Where(i => i.IsSelected && i.QuantityToOrder > 0).ToList();

//                if (!selectedItems.Any())
//                    throw new ArgumentException("At least one item must be selected");

//                string orderNumber = $"PO-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";

//                var order = new PurchaseOrder
//                {
//                    OrderNumber = orderNumber,
//                    SupplierID = model.SupplierID,
//                    OrderDate = DateTime.Now,
//                    ExpectedDeliveryDate = model.ExpectedDeliveryDate,
//                    Status = "Ordered",
//                    CreatedBy = createdBy,
//                    OrderItems = selectedItems.Select(item => new OrderItem
//                    {
//                        ConsumableID = item.ConsumableID,
//                        QuantityOrdered = item.QuantityToOrder,
//                        QuantityReceived = 0,
//                        Status = "Ordered",
//                        UnitPrice = _context.Consumables.First(c => c.ConsumableID == item.ConsumableID).UnitPrice
//                    }).ToList()
//                };

//                order.TotalAmount = order.OrderItems.Sum(o => o.QuantityOrdered * (o.UnitPrice ?? 0));

//                _context.PurchaseOrders.Add(order);
//                await _context.SaveChangesAsync();
//                await transaction.CommitAsync();

//                _logger.LogInformation($"Order created: {orderNumber} with {order.OrderItems.Count} items");
//                return order;
//            }
//            catch (Exception ex)
//            {
//                await transaction.RollbackAsync();
//                _logger.LogError(ex, "Error creating order");
//                throw;
//            }
//        }

//        public async Task<PurchaseOrder> ReceiveOrderAsync(int orderId, ReceiveOrderViewModel model, int receivedBy)
//        {
//            using var transaction = await _context.Database.BeginTransactionAsync();
//            try
//            {
//                var order = await _context.PurchaseOrders
//                    .Include(o => o.OrderItems)
//                    .FirstOrDefaultAsync(o => o.OrderID == orderId);

//                if (order == null)
//                    throw new ArgumentException($"Order with ID {orderId} not found");

//                if (order.Status == "Complete" || order.Status == "Cancelled")
//                    throw new InvalidOperationException($"Cannot receive order with status: {order.Status}");

//                bool allReceived = true;
//                bool anyReceived = false;

//                foreach (var item in order.OrderItems)
//                {
//                    var receiptItem = model.Items.FirstOrDefault(i => i.OrderItemID == item.OrderItemID);
//                    if (receiptItem != null && receiptItem.IsSelected)
//                    {
//                        item.QuantityReceived = item.QuantityOrdered;
//                        item.Status = "Received";
//                        item.DateReceived = DateTime.Now;
//                        anyReceived = true;

//                        // Update stock
//                        var consumable = await _context.Consumables.FindAsync(item.ConsumableID);
//                        if (consumable != null)
//                        {
//                            await _stockService.AdjustStockAsync(
//                                consumable.ConsumableID,
//                                item.QuantityOrdered,
//                                "OrderReceived",
//                                $"Received from order #{order.OrderNumber}",
//                                receivedBy
//                            );
//                        }
//                    }
//                    else if (item.QuantityReceived < item.QuantityOrdered)
//                    {
//                        allReceived = false;
//                    }
//                }

//                // Update order status
//                if (allReceived && anyReceived)
//                {
//                    order.Status = "Complete";
//                    order.DateCompleted = DateTime.Now;
//                }
//                else if (anyReceived)
//                {
//                    order.Status = "PartiallyComplete";
//                }

//                await _context.SaveChangesAsync();
//                await transaction.CommitAsync();

//                _logger.LogInformation($"Order received: {order.OrderNumber} (Status: {order.Status})");
//                return order;
//            }
//            catch (Exception ex)
//            {
//                await transaction.RollbackAsync();
//                _logger.LogError(ex, $"Error receiving order {orderId}");
//                throw;
//            }
//        }

//        public async Task<PurchaseOrder> CancelOrderAsync(int orderId, string reason)
//        {
//            using var transaction = await _context.Database.BeginTransactionAsync();
//            try
//            {
//                var order = await _context.PurchaseOrders
//                    .Include(o => o.OrderItems)
//                    .FirstOrDefaultAsync(o => o.OrderID == orderId);

//                if (order == null)
//                    throw new ArgumentException($"Order with ID {orderId} not found");

//                if (order.Status == "Complete" || order.Status == "Cancelled")
//                    throw new InvalidOperationException($"Cannot cancel order with status: {order.Status}");

//                order.Status = "Cancelled";
//                order.CancellationReason = reason;
//                order.DateCancelled = DateTime.Now;

//                foreach (var item in order.OrderItems)
//                {
//                    if (item.Status != "Received")
//                    {
//                        item.Status = "Cancelled";
//                        item.CancellationReason = reason;
//                    }
//                }

//                await _context.SaveChangesAsync();
//                await transaction.CommitAsync();

//                _logger.LogInformation($"Order cancelled: {order.OrderNumber} (Reason: {reason})");
//                return order;
//            }
//            catch (Exception ex)
//            {
//                await transaction.RollbackAsync();
//                _logger.LogError(ex, $"Error cancelling order {orderId}");
//                throw;
//            }
//        }

//        public async Task<List<OrderListViewModel>> GetOrderListAsync(params string[] statuses)
//        {
//            var query = _context.PurchaseOrders
//                .Include(o => o.Supplier)
//                .Include(o => o.OrderItems)
//                .AsQueryable();

//            if (statuses != null && statuses.Any())
//            {
//                query = query.Where(o => statuses.Contains(o.Status));
//            }

//            return await query
//                .OrderByDescending(o => o.OrderDate)
//                .Select(o => new OrderListViewModel
//                {
//                    OrderID = o.OrderID,
//                    OrderNumber = o.OrderNumber,
//                    SupplierName = o.Supplier != null ? o.Supplier.SupplierName : "Unknown",
//                    OrderDate = o.OrderDate,
//                    Status = o.Status,
//                    StatusColor = GetOrderStatusColor(o.Status),
//                    TotalItems = o.OrderItems.Count,
//                    ItemsReceived = o.OrderItems.Count(oi => oi.Status == "Received"),
//                    TotalAmount = o.TotalAmount,
//                    ExpectedDeliveryDate = o.ExpectedDeliveryDate
//                })
//                .ToListAsync();
//        }

//        public async Task<OrderDetailViewModel> GetOrderDetailsAsync(int orderId)
//        {
//            var order = await _context.PurchaseOrders
//                .Include(o => o.Supplier)
//                .Include(o => o.CreatedByUser)
//                .Include(o => o.OrderItems)
//                    .ThenInclude(oi => oi.Consumable)
//                .FirstOrDefaultAsync(o => o.OrderID == orderId);

//            if (order == null)
//                throw new ArgumentException($"Order with ID {orderId} not found");

//            return new OrderDetailViewModel
//            {
//                OrderID = order.OrderID,
//                OrderNumber = order.OrderNumber,
//                SupplierName = order.Supplier != null ? order.Supplier.SupplierName : "Unknown",
//                SupplierContact = order.Supplier != null ? order.Supplier.ContactPerson : "Unknown",
//                SupplierEmail = order.Supplier != null ? order.Supplier.EmailAddress : "Unknown",
//                OrderDate = order.OrderDate,
//                ExpectedDeliveryDate = order.ExpectedDeliveryDate,
//                Status = order.Status,
//                StatusColor = GetOrderStatusColor(order.Status),
//                CancellationReason = order.CancellationReason,
//                DateCompleted = order.DateCompleted,
//                DateCancelled = order.DateCancelled,
//                TotalAmount = order.TotalAmount,
//                CreatedBy = order.CreatedByUser != null ? $"{order.CreatedByUser.Name} {order.CreatedByUser.Surname}" : "Unknown",
//                CanReceive = order.Status == "Ordered" || order.Status == "PartiallyComplete",
//                CanCancel = order.Status == "Ordered" || order.Status == "PartiallyComplete",
//                Items = order.OrderItems.Select(oi => new OrderItemDetailViewModel
//                {
//                    OrderItemID = oi.OrderItemID,
//                    ConsumableName = oi.Consumable != null ? oi.Consumable.ConsumableName : "Unknown",
//                    QuantityOrdered = oi.QuantityOrdered,
//                    QuantityReceived = oi.QuantityReceived,
//                    UnitPrice = oi.UnitPrice,
//                    Status = oi.Status,
//                    StatusColor = GetOrderStatusColor(oi.Status),
//                    CancellationReason = oi.CancellationReason,
//                    DateReceived = oi.DateReceived
//                }).ToList()
//            };
//        }

//        private static string GetOrderStatusColor(string status)
//        {
//            return status switch
//            {
//                "Ordered" => "primary",
//                "PartiallyComplete" => "warning",
//                "Complete" => "success",
//                "Cancelled" => "danger",
//                "Received" => "info",
//                _ => "secondary"
//            };
//        }
//    }
//}