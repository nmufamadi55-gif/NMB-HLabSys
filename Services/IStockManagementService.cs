using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.Models.ViewModels;

namespace NMB_HLabSys.Services
{
    public interface IStockManagementService
    {
        Task<Consumable> AdjustStockAsync(int consumableId, int adjustmentAmount, string adjustmentType, string reason, int adjustedBy);
        Task<LowStockSummaryViewModel> GetLowStockSummaryAsync();
        Task<List<ConsumableViewModel>> GetConsumablesWithStatusAsync();
        Task<bool> DeductStockForTestAsync(int testId, int quantity);
    }

    public class StockManagementService : IStockManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StockManagementService> _logger;

        public StockManagementService(ApplicationDbContext context, ILogger<StockManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Consumable> AdjustStockAsync(int consumableId, int adjustmentAmount, string adjustmentType, string reason, int adjustedBy)
        {
            try
            {
                var consumable = await _context.Consumables.FindAsync(consumableId);
                if (consumable == null)
                    throw new ArgumentException($"Consumable with ID {consumableId} not found");

                int previousQuantity = consumable.QuantityOnHand;
                int newQuantity = 0;

                switch (adjustmentType)
                {
                    case "ManualIncrease":
                        newQuantity = previousQuantity + adjustmentAmount;
                        break;
                    case "ManualDecrease":
                        if (adjustmentAmount > previousQuantity)
                            throw new InvalidOperationException("Cannot decrease stock below zero");
                        newQuantity = previousQuantity - adjustmentAmount;
                        break;
                    case "ManualSet":
                        newQuantity = adjustmentAmount;
                        break;
                    default:
                        throw new ArgumentException($"Invalid adjustment type: {adjustmentType}");
                }

                consumable.QuantityOnHand = newQuantity;

                // Log adjustment
                var log = new StockAdjustmentLog
                {
                    ConsumableID = consumableId,
                    PreviousQuantity = previousQuantity,
                    NewQuantity = newQuantity,
                    AdjustmentAmount = adjustmentType == "ManualSet" ? newQuantity - previousQuantity :
                                     adjustmentType == "ManualIncrease" ? adjustmentAmount : -adjustmentAmount,
                    AdjustmentType = adjustmentType,
                    Reason = reason,
                    AdjustedBy = adjustedBy,
                    AdjustmentDate = DateTime.Now
                };

                _context.StockAdjustmentLogs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Stock adjusted for consumable {consumable.ConsumableName}: {previousQuantity} → {newQuantity}");
                return consumable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adjusting stock for consumable ID {consumableId}");
                throw;
            }
        }

        public async Task<LowStockSummaryViewModel> GetLowStockSummaryAsync()
        {
            var criticalItems = await _context.Consumables
                .Where(c => c.QuantityOnHand <= 0)
                .Select(c => new LowStockAlertViewModel
                {
                    ConsumableID = c.ConsumableID,
                    ConsumableName = c.ConsumableName,
                    QuantityOnHand = c.QuantityOnHand,
                    ReorderLevel = c.ReorderLevel,
                    SupplierName = c.Supplier != null ? c.Supplier.SupplierName : "Unknown",
                    AlertLevel = "Critical"
                })
                .ToListAsync();

            var warningItems = await _context.Consumables
                .Where(c => c.QuantityOnHand > 0 && c.QuantityOnHand <= c.ReorderLevel)
                .Select(c => new LowStockAlertViewModel
                {
                    ConsumableID = c.ConsumableID,
                    ConsumableName = c.ConsumableName,
                    QuantityOnHand = c.QuantityOnHand,
                    ReorderLevel = c.ReorderLevel,
                    SupplierName = c.Supplier != null ? c.Supplier.SupplierName : "Unknown",
                    AlertLevel = "Warning"
                })
                .ToListAsync();

            return new LowStockSummaryViewModel
            {
                CriticalCount = criticalItems.Count,
                WarningCount = warningItems.Count,
                CriticalItems = criticalItems,
                WarningItems = warningItems
            };
        }

        public async Task<List<ConsumableViewModel>> GetConsumablesWithStatusAsync()
        {
            return await _context.Consumables
                .Include(c => c.Supplier)
                .Select(c => new ConsumableViewModel
                {
                    ConsumableID = c.ConsumableID,
                    ConsumableName = c.ConsumableName,
                    SupplierName = c.Supplier != null ? c.Supplier.SupplierName : "Unknown",
                    QuantityOnHand = c.QuantityOnHand,
                    ReorderLevel = c.ReorderLevel,
                    StockStatus = GetStockStatus(c.QuantityOnHand, c.ReorderLevel),
                    StatusColor = GetStockStatusColor(c.QuantityOnHand, c.ReorderLevel),
                    UnitPrice = c.UnitPrice,
                    UnitOfMeasure = c.UnitOfMeasure,
                    StorageLocation = c.StorageLocation,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> DeductStockForTestAsync(int testId, int quantity)
        {
            try
            {
                var testConsumables = await _context.TestConsumables
                    .Where(tc => tc.TestID == testId)
                    .ToListAsync();

                foreach (var tc in testConsumables)
                {
                    var consumable = await _context.Consumables.FindAsync(tc.ConsumableID);
                    if (consumable != null)
                    {
                        int requiredQuantity = tc.QuantityRequired * quantity;
                        if (consumable.QuantityOnHand < requiredQuantity)
                            return false;

                        consumable.QuantityOnHand -= requiredQuantity;
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deducting stock for test ID {testId}");
                return false;
            }
        }

        private static string GetStockStatus(int quantity, int reorderLevel)
        {
            if (quantity <= 0) return "Critical";
            if (quantity <= reorderLevel) return "Low";
            if (quantity <= reorderLevel * 1.5m) return "Warning";
            return "Normal";
        }

        private static string GetStockStatusColor(int quantity, int reorderLevel)
        {
            if (quantity <= 0) return "danger";
            if (quantity <= reorderLevel) return "warning";
            if (quantity <= reorderLevel * 1.5m) return "info";
            return "success";
        }
    }
}