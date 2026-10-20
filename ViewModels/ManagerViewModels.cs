using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace NMB_HLabSys.Models.ViewModels
{
    // ═══════════════════════════════════════════════════════════
    // CUSTOM VALIDATION ATTRIBUTES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Ensures a numeric Min property is strictly less than its Max property.
    /// Only fires when BOTH have values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class MinLessThanMaxAttribute : ValidationAttribute
    {
        private readonly string _minProp;
        private readonly string _maxProp;

        public MinLessThanMaxAttribute(string minProp, string maxProp)
        {
            _minProp = minProp;
            _maxProp = maxProp;
            ErrorMessage = "The minimum value must be less than the maximum value.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
        {
            if (value == null) return ValidationResult.Success;
            var t = value.GetType();
            var minP = t.GetProperty(_minProp);
            var maxP = t.GetProperty(_maxProp);
            if (minP == null || maxP == null) return ValidationResult.Success;

            var min = minP.GetValue(value) as decimal?;
            var max = maxP.GetValue(value) as decimal?;

            if (min.HasValue && max.HasValue && min.Value >= max.Value)
                return new ValidationResult(ErrorMessage, new[] { _minProp, _maxProp });

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates a South African ID number: 13 digits + Luhn checksum + YYMMDD sanity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ValidSAIDAttribute : ValidationAttribute
    {
        public ValidSAIDAttribute() { ErrorMessage = "Invalid South African ID number."; }

        protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s)) return ValidationResult.Success;

            if (s.Length != 13 || !s.All(char.IsDigit))
                return new ValidationResult(ErrorMessageString);

            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int d = s[i] - '0';
                if (i % 2 == 1) { d *= 2; if (d > 9) d -= 9; }
                sum += d;
            }
            int check = (10 - (sum % 10)) % 10;
            if (check != (s[12] - '0'))
                return new ValidationResult(ErrorMessageString);

            int mm = int.Parse(s.Substring(2, 2));
            int dd = int.Parse(s.Substring(4, 2));
            if (mm < 1 || mm > 12 || dd < 1 || dd > 31)
                return new ValidationResult("SA ID number contains an invalid birth date.");

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Rejects dates in the past (compared to today, date-only).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NotPastDateAttribute : ValidationAttribute
    {
        public NotPastDateAttribute() { ErrorMessage = "Date cannot be in the past."; }

        protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
        {
            if (value is not DateTime dt) return ValidationResult.Success;
            return dt.Date >= DateTime.Today
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessageString);
        }
    }

    /// <summary>
    /// Rejects dates in the future (compared to today, date-only).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NotFutureDateAttribute : ValidationAttribute
    {
        public NotFutureDateAttribute() { ErrorMessage = "Date cannot be in the future."; }

        protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
        {
            if (value is not DateTime dt) return ValidationResult.Success;
            return dt.Date <= DateTime.Today
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessageString);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // DASHBOARD VIEWMODELS
    // ═══════════════════════════════════════════════════════════

    public class DashboardViewModel
    {
        public int TotalTestCategories { get; set; }
        public int TotalTestTypes { get; set; }
        public int TotalConsumables { get; set; }
        public int TotalTechnicians { get; set; }
        public int TotalDoctors { get; set; }
        public int PendingOrders { get; set; }
        public int LowStockItems { get; set; }
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
        public List<LowStockAlertViewModel> LowStockAlerts { get; set; } = new();
        public DashboardStatisticsViewModel Statistics { get; set; } = new();
    }

    public class DashboardStatisticsViewModel
    {
        public int TestsPerformedThisMonth { get; set; }
        public int OrdersProcessedThisMonth { get; set; }
        public decimal TotalStockValue { get; set; }
        public int ActiveTechnicians { get; set; }
    }

    public class RecentActivityViewModel
    {
        public string ActivityType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ActivityDate { get; set; }
        public string? UserName { get; set; }
    }

    public class LowStockAlertViewModel
    {
        public int ConsumableID { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public int ReorderLevel { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string AlertLevel { get; set; } = string.Empty; // "Critical", "Warning"
    }

    // ═══════════════════════════════════════════════════════════
    // TEST CATALOGUE VIEWMODELS
    // ═══════════════════════════════════════════════════════════

    public class TestCatalogueIndexViewModel
    {
        public List<TestCategoryViewModel> Categories { get; set; } = new();
        public List<TestTypeViewModel> TestTypes { get; set; } = new();
        public List<SampleTypeViewModel> SampleTypes { get; set; } = new();
    }

    public class TestCategoryViewModel
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TestCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTestCategoryViewModel
    {
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }

    public class EditTestCategoryViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Category ID is invalid.")]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }

    public class TestTypeViewModel
    {
        public int TestID { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string SampleTypeName { get; set; } = string.Empty;
        public string? UnitOfMeasurement { get; set; }
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
        public int TurnaroundTimeMinutes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> AssignedTechnicians { get; set; } = new();
        public List<ConsumableSummaryViewModel> Consumables { get; set; } = new();
    }

    public class ConsumableInputViewModel
    {
        public int ConsumableID { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public int QuantityRequired { get; set; } = 1;
        public bool IsSelected { get; set; }
    }

    [MinLessThanMax(nameof(NormalRangeMin), nameof(NormalRangeMax))]
    public class CreateTestTypeViewModel
    {
        [Required(ErrorMessage = "Test name is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Test name must be between 3 and 200 characters.")]
        public string TestName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
        public int CategoryID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a sample type.")]
        public int SampleTypeID { get; set; }

        [Required(ErrorMessage = "Unit of measurement is required.")]
        [StringLength(50, ErrorMessage = "Unit of measurement cannot exceed 50 characters.")]
        public string UnitOfMeasurement { get; set; } = string.Empty;

        [Range(0, 1_000_000, ErrorMessage = "Minimum normal range cannot be negative.")]
        public decimal? NormalRangeMin { get; set; }

        [Range(0, 1_000_000, ErrorMessage = "Maximum normal range cannot be negative.")]
        public decimal? NormalRangeMax { get; set; }

        [Required(ErrorMessage = "Turnaround time is required.")]
        [Range(5, 1440, ErrorMessage = "Turnaround time must be between 5 and 1440 minutes.")]
        public int TurnaroundTimeMinutes { get; set; }

        public List<int>? SelectedTechnicianIDs { get; set; }
        public List<ConsumableInputViewModel> Consumables { get; set; } = new();

        public List<SelectListItem>? AvailableCategories { get; set; }
        public List<SelectListItem>? AvailableSampleTypes { get; set; }
        public List<SelectListItem>? AvailableTechnicians { get; set; }
        public List<SelectListItem>? AvailableConsumables { get; set; }
    }

    [MinLessThanMax(nameof(NormalRangeMin), nameof(NormalRangeMax))]
    public class EditTestTypeViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Test ID is invalid.")]
        public int TestID { get; set; }

        [Required(ErrorMessage = "Test name is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Test name must be between 3 and 200 characters.")]
        public string TestName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
        public int CategoryID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a sample type.")]
        public int SampleTypeID { get; set; }

        [Required(ErrorMessage = "Unit of measurement is required.")]
        [StringLength(50, ErrorMessage = "Unit of measurement cannot exceed 50 characters.")]
        public string UnitOfMeasurement { get; set; } = string.Empty;

        [Range(0, 1_000_000, ErrorMessage = "Minimum normal range cannot be negative.")]
        public decimal? NormalRangeMin { get; set; }

        [Range(0, 1_000_000, ErrorMessage = "Maximum normal range cannot be negative.")]
        public decimal? NormalRangeMax { get; set; }

        [Required(ErrorMessage = "Turnaround time is required.")]
        [Range(5, 1440, ErrorMessage = "Turnaround time must be between 5 and 1440 minutes.")]
        public int TurnaroundTimeMinutes { get; set; }

        public bool IsActive { get; set; }

        public List<int>? SelectedTechnicianIDs { get; set; }
        public List<ConsumableInputViewModel> Consumables { get; set; } = new();

        public List<SelectListItem>? AvailableCategories { get; set; }
        public List<SelectListItem>? AvailableSampleTypes { get; set; }
        public List<SelectListItem>? AvailableTechnicians { get; set; }
        public List<SelectListItem>? AvailableConsumables { get; set; }
    }

    public class SampleTypeViewModel
    {
        public int SampleTypeID { get; set; }
        public string SampleTypeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CollectionTubeType { get; set; } = string.Empty;
        public string StorageRequirements { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateSampleTypeViewModel
    {
        [Required(ErrorMessage = "Sample type name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Sample type name must be between 2 and 100 characters.")]
        public string SampleTypeName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Collection tube type cannot exceed 100 characters.")]
        public string CollectionTubeType { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Storage requirements cannot exceed 500 characters.")]
        public string StorageRequirements { get; set; } = string.Empty;
    }

    public class EditSampleTypeViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Sample type ID is invalid.")]
        public int SampleTypeID { get; set; }

        [Required(ErrorMessage = "Sample type name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Sample type name must be between 2 and 100 characters.")]
        public string SampleTypeName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Collection tube type cannot exceed 100 characters.")]
        public string CollectionTubeType { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Storage requirements cannot exceed 500 characters.")]
        public string StorageRequirements { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // STOCK MANAGEMENT VIEWMODELS
    // ═══════════════════════════════════════════════════════════

    public class StockManagementIndexViewModel
    {
        public List<ConsumableViewModel> Consumables { get; set; } = new();
        public LowStockSummaryViewModel LowStockSummary { get; set; } = new();
    }

    public class ConsumableViewModel
    {
        public int ConsumableID { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public int ReorderLevel { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string StorageLocation { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateConsumableViewModel
    {
        [Required(ErrorMessage = "Consumable name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Consumable name must be between 2 and 200 characters.")]
        public string ConsumableName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Please select a supplier.")]
        public int SupplierID { get; set; }

        [Range(0, 100_000, ErrorMessage = "Reorder level must be between 0 and 100,000.")]
        public int ReorderLevel { get; set; }

        [Range(0, 1_000_000, ErrorMessage = "Quantity on hand must be between 0 and 1,000,000.")]
        public int QuantityOnHand { get; set; }

        [StringLength(20, ErrorMessage = "Unit of measure cannot exceed 20 characters.")]
        public string UnitOfMeasure { get; set; } = string.Empty;

        [Range(0, 9_999_999.99, ErrorMessage = "Unit price cannot be negative.")]
        public decimal? UnitPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Minimum stock level cannot be negative.")]
        public int? MinimumStockLevel { get; set; }

        [StringLength(100, ErrorMessage = "Storage location cannot exceed 100 characters.")]
        public string StorageLocation { get; set; } = string.Empty;

        public List<SelectListItem>? AvailableSuppliers { get; set; }
    }

    public class EditConsumableViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Consumable ID is invalid.")]
        public int ConsumableID { get; set; }

        [Required(ErrorMessage = "Consumable name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Consumable name must be between 2 and 200 characters.")]
        public string ConsumableName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Please select a supplier.")]
        public int SupplierID { get; set; }

        [Range(0, 100_000, ErrorMessage = "Reorder level must be between 0 and 100,000.")]
        public int ReorderLevel { get; set; }

        [Range(0, 1_000_000, ErrorMessage = "Quantity on hand must be between 0 and 1,000,000.")]
        public int QuantityOnHand { get; set; }

        [StringLength(20, ErrorMessage = "Unit of measure cannot exceed 20 characters.")]
        public string UnitOfMeasure { get; set; } = string.Empty;

        [Range(0, 9_999_999.99, ErrorMessage = "Unit price cannot be negative.")]
        public decimal? UnitPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Minimum stock level cannot be negative.")]
        public int? MinimumStockLevel { get; set; }

        [StringLength(100, ErrorMessage = "Storage location cannot exceed 100 characters.")]
        public string StorageLocation { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public List<SelectListItem>? AvailableSuppliers { get; set; }
    }

    public class AdjustStockViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Consumable ID is invalid.")]
        public int ConsumableID { get; set; }

        public string ConsumableName { get; set; } = string.Empty;
        public int CurrentQuantity { get; set; }

        [Range(1, 100_000, ErrorMessage = "Adjustment amount must be between 1 and 100,000.")]
        public int AdjustmentAmount { get; set; }

        [Required(ErrorMessage = "Please select an adjustment type.")]
        [StringLength(20)]
        public string AdjustmentType { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;

        public List<SelectListItem>? AdjustmentTypes { get; set; }
    }

    public class LowStockSummaryViewModel
    {
        public int CriticalCount { get; set; }
        public int WarningCount { get; set; }
        public List<LowStockAlertViewModel> CriticalItems { get; set; } = new();
        public List<LowStockAlertViewModel> WarningItems { get; set; } = new();
    }

    public class ConsumableSummaryViewModel
    {
        public int ConsumableID { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public int ReorderLevel { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════
    // SUPPLIER VIEWMODELS
    // ═══════════════════════════════════════════════════════════

    public class SupplierViewModel
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int ConsumableCount { get; set; }
        public bool IsPreferred { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateSupplierViewModel
    {
        [Required(ErrorMessage = "Supplier name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Supplier name must be between 2 and 200 characters.")]
        public string SupplierName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact person is required")]
        [StringLength(150, ErrorMessage = "Contact person cannot exceed 150 characters.")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string EmailAddress { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
        public string Address { get; set; } = string.Empty;

        public bool IsPreferred { get; set; }
    }

    public class EditSupplierViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Supplier ID is invalid.")]
        public int SupplierID { get; set; }

        [Required(ErrorMessage = "Supplier name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Supplier name must be between 2 and 200 characters.")]
        public string SupplierName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact person is required")]
        [StringLength(150, ErrorMessage = "Contact person cannot exceed 150 characters.")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string EmailAddress { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
        public string Address { get; set; } = string.Empty;

        public bool IsPreferred { get; set; }
        public bool IsActive { get; set; }
    }

    public class SupplierDetailsViewModel
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsPreferred { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ConsumableCount { get; set; }
        public List<ConsumableSummaryViewModel> Consumables { get; set; } = new();
    }

    // ═══════════════════════════════════════════════════════════
    // PURCHASE ORDER VIEWMODELS
    // ═══════════════════════════════════════════════════════════

    public class OrderIndexViewModel
    {
        public List<OrderListViewModel> Orders { get; set; } = new();
        public List<OrderListViewModel> PendingOrders { get; set; } = new();
        public List<OrderListViewModel> CompletedOrders { get; set; } = new();
        public OrderStatisticsViewModel Statistics { get; set; } = new();
    }

    public class OrderListViewModel
    {
        public int OrderID { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public int ItemsReceived { get; set; }
        public decimal? TotalAmount { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
    }

    public class CreateOrderViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Please select a supplier.")]
        public int SupplierID { get; set; }

        [DataType(DataType.Date)]
        [NotPastDate(ErrorMessage = "Expected delivery date cannot be in the past.")]
        public DateTime? ExpectedDeliveryDate { get; set; }

        public List<OrderItemInputViewModel>? Items { get; set; }
        public List<SelectListItem>? AvailableSuppliers { get; set; }
    }

    public class OrderItemInputViewModel
    {
        public int ConsumableID { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public int SupplierID { get; set; }
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public int SuggestedQuantity { get; set; }
        public int QuantityToOrder { get; set; }
        public bool IsSelected { get; set; }
    }

    public class OrderDetailViewModel
    {
        public int OrderID { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierContact { get; set; } = string.Empty;
        public string SupplierEmail { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public string CancellationReason { get; set; } = string.Empty;
        public DateTime? DateCompleted { get; set; }
        public DateTime? DateCancelled { get; set; }
        public decimal? TotalAmount { get; set; }
        public bool CanReceive { get; set; }
        public bool CanCancel { get; set; }
        public List<OrderItemDetailViewModel> Items { get; set; } = new();
    }

    public class OrderItemDetailViewModel
    {
        public int OrderItemID { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public int QuantityOrdered { get; set; }
        public int QuantityReceived { get; set; }
        public decimal? UnitPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public string CancellationReason { get; set; } = string.Empty;
        public DateTime? DateReceived { get; set; }
    }

    public class OrderStatisticsViewModel
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int PartiallyCompleteOrders { get; set; }
    }

    public class CancelOrderViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Order ID is invalid.")]
        public int OrderID { get; set; }

        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cancellation reason is required")]
        [StringLength(500, ErrorMessage = "Cancellation reason cannot exceed 500 characters.")]
        public string CancellationReason { get; set; } = string.Empty;

        public bool NotifySupplier { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // STAFF MANAGEMENT VIEWMODELS
    // ═══════════════════════════════════════════════════════════

    public class StaffManagementIndexViewModel
    {
        public List<TechnicianViewModel> Technicians { get; set; } = new();
        public List<DoctorListViewModel> Doctors { get; set; } = new();
        public StaffStatisticsViewModel Statistics { get; set; } = new();
    }

    public class TechnicianViewModel
    {
        public int TechnicianID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string SAIDNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public int AssignedTestsCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime HireDate { get; set; }
    }

    public class AddTechnicianViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required")]
        [StringLength(100, ErrorMessage = "Surname cannot exceed 100 characters.")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "SA ID number is required")]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "SA ID must be exactly 13 digits.")]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "ID number must be 13 digits")]
        public string SAIDNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Employee number is required")]
        [StringLength(50, ErrorMessage = "Employee number cannot exceed 50 characters.")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required")]
        [StringLength(20, ErrorMessage = "Contact number cannot exceed 20 characters.")]
        public string ContactNumber { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Specialization cannot exceed 100 characters.")]
        public string Specialization { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Qualification cannot exceed 200 characters.")]
        public string Qualification { get; set; } = string.Empty;

        [Range(0, 50, ErrorMessage = "Years of experience must be between 0 and 50.")]
        public int YearsOfExperience { get; set; }

        [MinLength(1, ErrorMessage = "At least one test type must be assigned.")]
        public List<int> SelectedTestTypeIDs { get; set; } = new();

        public List<SelectListItem> AvailableTestTypes { get; set; } = new();
    }

    public class EditTechnicianViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Technician ID is invalid.")]
        public int TechnicianID { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required")]
        [StringLength(100, ErrorMessage = "Surname cannot exceed 100 characters.")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "SA ID number is required")]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "SA ID must be exactly 13 digits.")]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "ID number must be 13 digits")]
        public string SAIDNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Employee number is required")]
        [StringLength(50, ErrorMessage = "Employee number cannot exceed 50 characters.")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required")]
        [StringLength(20, ErrorMessage = "Contact number cannot exceed 20 characters.")]
        public string ContactNumber { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Specialization cannot exceed 100 characters.")]
        public string Specialization { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Qualification cannot exceed 200 characters.")]
        public string Qualification { get; set; } = string.Empty;

        [Range(0, 50, ErrorMessage = "Years of experience must be between 0 and 50.")]
        public int YearsOfExperience { get; set; }

        public bool IsActive { get; set; }

        [MinLength(1, ErrorMessage = "At least one test type must be assigned.")]
        public List<int> SelectedTestTypeIDs { get; set; } = new();

        public List<SelectListItem> AvailableTestTypes { get; set; } = new();
    }

    public class TechnicianProfileViewModel
    {
        public int TechnicianID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string SAIDNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; }
        public List<AssignedTestViewModel> AssignedTests { get; set; } = new();
    }

    public class AssignedTestViewModel
    {
        public int TestID { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class DoctorListViewModel
    {
        public int DoctorID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string HPCSANumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PracticeName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public DateTime RegisteredDate { get; set; }
    }

    public class RegisterDoctorViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required")]
        [StringLength(100, ErrorMessage = "Surname cannot exceed 100 characters.")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "HPCSA number is required")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "HPCSA number must be between 5 and 20 characters.")]
        [RegularExpression(@"^[A-Za-z0-9]{5,20}$", ErrorMessage = "HPCSA number must be 5–20 alphanumeric characters.")]
        public string HPCSANumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required")]
        [StringLength(20, ErrorMessage = "Contact number cannot exceed 20 characters.")]
        public string ContactNumber { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Practice name cannot exceed 200 characters.")]
        public string PracticeName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Practice address cannot exceed 500 characters.")]
        public string PracticeAddress { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Specialization cannot exceed 100 characters.")]
        public string Specialization { get; set; } = string.Empty;
    }

    public class EditDoctorViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Doctor ID is invalid.")]
        public int DoctorID { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required")]
        [StringLength(100, ErrorMessage = "Surname cannot exceed 100 characters.")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "HPCSA number is required")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "HPCSA number must be between 5 and 20 characters.")]
        [RegularExpression(@"^[A-Za-z0-9]{5,20}$", ErrorMessage = "HPCSA number must be 5–20 alphanumeric characters.")]
        public string HPCSANumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required")]
        [StringLength(20, ErrorMessage = "Contact number cannot exceed 20 characters.")]
        public string ContactNumber { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Practice name cannot exceed 200 characters.")]
        public string PracticeName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Practice address cannot exceed 500 characters.")]
        public string PracticeAddress { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Specialization cannot exceed 100 characters.")]
        public string Specialization { get; set; } = string.Empty;

        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
    }

    public class StaffStatisticsViewModel
    {
        public int TotalTechnicians { get; set; }
        public int ActiveTechnicians { get; set; }
        public int TotalDoctors { get; set; }
        public int ActiveDoctors { get; set; }
        public int VerifiedDoctors { get; set; }
        public int PendingVerificationDoctors { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // REPORT VIEWMODELS
    // ═══════════════════════════════════════════════════════════

    public class ReportFilterViewModel
    {
        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        [NotFutureDate(ErrorMessage = "End date cannot be in the future.")]
        public DateTime EndDate { get; set; } = DateTime.Now;

        public string? ReportType { get; set; }
        public string? GroupBy { get; set; }
        public bool IncludeInactive { get; set; }
    }

    public class CategoryReportData
    {
        public string CategoryName { get; set; } = string.Empty;
        public int TotalTests { get; set; }
        public List<TestSummaryItem> Tests { get; set; } = new();
    }

    public class TestSummaryItem
    {
        public string TestName { get; set; } = string.Empty;
        public DateTime? CompletedDate { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // CHART DATA
    // ═══════════════════════════════════════════════════════════

    public class ChartDataDto
    {
        public List<string> MonthlyLabels { get; set; } = new();
        public List<int> TestsCompletedByMonth { get; set; } = new();
        public List<int> OrdersByMonth { get; set; } = new();
        public List<ChartSlice> TestsByCategory { get; set; } = new();
        public List<ChartSlice> StockByStatus { get; set; } = new();
    }

    public class ChartSlice
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}