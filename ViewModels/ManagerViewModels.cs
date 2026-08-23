using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using NMB_HLabSys.Models;

namespace NMB_HLabSys.Models.ViewModels
{
    // =========================
    // DASHBOARD VIEWMODELS
    // =========================
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
        public string? ConsumableName { get; set; }
        public int QuantityOnHand { get; set; }
        public int ReorderLevel { get; set; }
        public string? SupplierName { get; set; }
        public string? AlertLevel { get; set; } // "Critical", "Warning"
    }

    // =========================
    // TEST CATALOGUE VIEWMODELS
    // =========================
    public class TestCatalogueIndexViewModel
    {
        public List<TestCategoryViewModel>? Categories { get; set; }
        public List<TestTypeViewModel>? TestTypes { get; set; }
        public List<SampleTypeViewModel>? SampleTypes { get; set; }
    }

    public class TestCategoryViewModel
    {
        public int CategoryID { get; set; }
        public string? CategoryName { get; set; }
        public string? Description { get; set; }
        public int TestCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTestCategoryViewModel
    {
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters")]
        [Display(Name = "Category Name")]
        public string? CategoryName { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }
    }

    public class EditTestCategoryViewModel
    {
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters")]
        [Display(Name = "Category Name")]
        public string? CategoryName { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }
    }

    public class TestTypeViewModel
    {
        public int TestID { get; set; }
        public string? TestName { get; set; }
        public string? CategoryName { get; set; }
        public string? SampleTypeName { get; set; }
        public string? UnitOfMeasurement { get; set; }
        public string NormalRangeDisplay => $"{NormalRangeMin} - {NormalRangeMax}";
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
        public int TurnaroundTimeMinutes { get; set; }
        public string TurnaroundTimeDisplay => $"{TurnaroundTimeMinutes / 60}h {TurnaroundTimeMinutes % 60}m";
        public List<string>? AssignedTechnicians { get; set; }
        public List<ConsumableViewModel>? Consumables { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTestTypeViewModel
    {
        [Required(ErrorMessage = "Test name is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Test name must be between 3 and 200 characters")]
        [Display(Name = "Test Name")]
        public string? TestName { get; set; }

        [Required(ErrorMessage = "Please select a category")]
        [Display(Name = "Category")]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Please select a sample type")]
        [Display(Name = "Sample Type")]
        public int SampleTypeID { get; set; }

        [Display(Name = "Unit of Measurement")]
        public string? UnitOfMeasurement { get; set; }

        [Display(Name = "Normal Range Minimum")]
        public decimal? NormalRangeMin { get; set; }

        [Display(Name = "Normal Range Maximum")]
        public decimal? NormalRangeMax { get; set; }

        [Required(ErrorMessage = "Turnaround time is required")]
        [Range(5, 1440, ErrorMessage = "Turnaround time must be between 5 minutes and 24 hours")]
        [Display(Name = "Turnaround Time (minutes)")]
        public int TurnaroundTimeMinutes { get; set; }

        [Display(Name = "Assign Technicians")]
        public List<int>? SelectedTechnicianIDs { get; set; }

        [Display(Name = "Consumables")]
        public Dictionary<int, int>? ConsumableQuantities { get; set; }

        public List<SelectListItem>? AvailableCategories { get; set; }
        public List<SelectListItem>? AvailableSampleTypes { get; set; }
        public List<SelectListItem>? AvailableTechnicians { get; set; }
        public List<SelectListItem>? AvailableConsumables { get; set; }
    }

    public class EditTestTypeViewModel
    {
        public int TestID { get; set; }

        [Required(ErrorMessage = "Test name is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Test name must be between 3 and 200 characters")]
        [Display(Name = "Test Name")]
        public string? TestName { get; set; }

        [Required(ErrorMessage = "Please select a category")]
        [Display(Name = "Category")]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Please select a sample type")]
        [Display(Name = "Sample Type")]
        public int SampleTypeID { get; set; }

        [Display(Name = "Unit of Measurement")]
        public string? UnitOfMeasurement { get; set; }

        [Display(Name = "Normal Range Minimum")]
        public decimal? NormalRangeMin { get; set; }

        [Display(Name = "Normal Range Maximum")]
        public decimal? NormalRangeMax { get; set; }

        [Required(ErrorMessage = "Turnaround time is required")]
        [Range(5, 1440, ErrorMessage = "Turnaround time must be between 5 minutes and 24 hours")]
        [Display(Name = "Turnaround Time (minutes)")]
        public int TurnaroundTimeMinutes { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Assign Technicians")]
        public List<int>? SelectedTechnicianIDs { get; set; }

        [Display(Name = "Consumables")]
        public Dictionary<int, int>? ConsumableQuantities { get; set; }

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
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Sample Type Name")]
        public string SampleTypeName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Collection Tube Type")]
        public string CollectionTubeType { get; set; } = string.Empty;

        [Display(Name = "Storage Requirements")]
        public string StorageRequirements { get; set; } = string.Empty;
    }

    public class TechnicianAssignmentViewModel
    {
        public int TestID { get; set; }
        public string TestName { get; set; } = string.Empty;
        public List<TechnicianAssignmentItemViewModel>? AssignedTechnicians { get; set; }
        public List<TechnicianAssignmentItemViewModel>? UnassignedTechnicians { get; set; }
    }

    public class TechnicianAssignmentItemViewModel
    {
        public int TechnicianID { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public bool IsAssigned { get; set; }
        public DateTime? AssignedDate { get; set; }
    }

    // =========================
    // STOCK MANAGEMENT VIEWMODELS
    // =========================
    public class StockManagementIndexViewModel
    {
        public List<ConsumableViewModel>? Consumables { get; set; }
        public List<SupplierViewModel>? Suppliers { get; set; }
        public List<OrderListViewModel>? PendingOrders { get; set; }
        public LowStockSummaryViewModel LowStockSummary { get; set; } = new();
        public StockStatisticsViewModel Statistics { get; set; } = new();
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
        [StringLength(200, MinimumLength = 2)]
        [Display(Name = "Consumable Name")]
        public string ConsumableName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a supplier")]
        [Display(Name = "Supplier")]
        public int SupplierID { get; set; }

        [Required(ErrorMessage = "Reorder level is required")]
        [Range(0, 1000)]
        [Display(Name = "Reorder Level")]
        public int ReorderLevel { get; set; }

        [Required(ErrorMessage = "Quantity on hand is required")]
        [Range(0, 10000)]
        [Display(Name = "Quantity on Hand")]
        public int QuantityOnHand { get; set; }

        [Display(Name = "Unit of Measure")]
        public string UnitOfMeasure { get; set; } = string.Empty;

        [Display(Name = "Unit Price")]
        public decimal? UnitPrice { get; set; }

        [Display(Name = "Minimum Stock Level")]
        public int? MinimumStockLevel { get; set; }

        [Display(Name = "Storage Location")]
        public string StorageLocation { get; set; } = string.Empty;

        public List<SelectListItem>? AvailableSuppliers { get; set; }
    }

    public class EditConsumableViewModel
    {
        public int ConsumableID { get; set; }

        [Required(ErrorMessage = "Consumable name is required")]
        [StringLength(200, MinimumLength = 2)]
        [Display(Name = "Consumable Name")]
        public string ConsumableName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a supplier")]
        [Display(Name = "Supplier")]
        public int SupplierID { get; set; }

        [Required(ErrorMessage = "Reorder level is required")]
        [Range(0, 1000)]
        [Display(Name = "Reorder Level")]
        public int ReorderLevel { get; set; }

        [Required(ErrorMessage = "Quantity on hand is required")]
        [Range(0, 10000)]
        [Display(Name = "Quantity on Hand")]
        public int QuantityOnHand { get; set; }

        [Display(Name = "Unit of Measure")]
        public string UnitOfMeasure { get; set; } = string.Empty;

        [Display(Name = "Unit Price")]
        public decimal? UnitPrice { get; set; }

        [Display(Name = "Minimum Stock Level")]
        public int? MinimumStockLevel { get; set; }

        [Display(Name = "Storage Location")]
        public string StorageLocation { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public List<SelectListItem>? AvailableSuppliers { get; set; }
    }

    public class AdjustStockViewModel
    {
        public int ConsumableID { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public int CurrentQuantity { get; set; }

        [Required(ErrorMessage = "Adjustment amount is required")]
        [Range(1, 10000, ErrorMessage = "Amount must be between 1 and 10000")]
        [Display(Name = "Adjustment Amount")]
        public int AdjustmentAmount { get; set; }

        [Required(ErrorMessage = "Please select an adjustment type")]
        [Display(Name = "Adjustment Type")]
        public string AdjustmentType { get; set; } = string.Empty;

        [Display(Name = "Reason for Adjustment")]
        public string Reason { get; set; } = string.Empty;

        public List<SelectListItem>? AdjustmentTypes { get; set; }
    }

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
        [StringLength(200, MinimumLength = 2)]
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact person is required")]
        [StringLength(150)]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Preferred Supplier")]
        public bool IsPreferred { get; set; }
    }

    public class LowStockSummaryViewModel
    {
        public int CriticalCount { get; set; }
        public int WarningCount { get; set; }
        public List<LowStockAlertViewModel>? CriticalItems { get; set; }
        public List<LowStockAlertViewModel>? WarningItems { get; set; }
    }

    public class StockStatisticsViewModel
    {
        public int TotalConsumables { get; set; }
        public int ActiveConsumables { get; set; }
        public int TotalSuppliers { get; set; }
        public decimal TotalStockValue { get; set; }
        public int ItemsBelowReorder { get; set; }
    }

    // =========================
    // SUPPLIER VIEWMODELS
    // =========================
    public class EditSupplierViewModel
    {
        public int SupplierID { get; set; }

        [Required(ErrorMessage = "Supplier name is required")]
        [StringLength(200, MinimumLength = 2)]
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact person is required")]
        [StringLength(150)]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Preferred Supplier")]
        public bool IsPreferred { get; set; }

        [Display(Name = "Active")]
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

    public class ConsumableSummaryViewModel
    {
        public int ConsumableID { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public int ReorderLevel { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
    }

    // =========================
    // ORDER MANAGEMENT VIEWMODELS
    // =========================
    public class OrderIndexViewModel
    {
        public List<OrderListViewModel>? Orders { get; set; }
        public List<OrderListViewModel>? PendingOrders { get; set; }
        public List<OrderListViewModel>? CompletedOrders { get; set; }
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
        [Required(ErrorMessage = "Please select a supplier")]
        [Display(Name = "Supplier")]
        public int SupplierID { get; set; }

        [Display(Name = "Expected Delivery Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedDeliveryDate { get; set; }

        public List<OrderItemInputViewModel>? Items { get; set; }

        public List<SelectListItem>? AvailableSuppliers { get; set; }
        public List<SelectListItem>? AvailableConsumables { get; set; }
    }

    public class OrderItemInputViewModel
    {
        public int ConsumableID { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public int SuggestedQuantity { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 10000, ErrorMessage = "Quantity must be at least 1")]
        [Display(Name = "Quantity to Order")]
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
        public string CreatedBy { get; set; } = string.Empty;
        public List<OrderItemDetailViewModel>? Items { get; set; }
        public bool CanReceive { get; set; }
        public bool CanCancel { get; set; }
    }

    public class OrderItemDetailViewModel
    {
        public int OrderItemID { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public int QuantityOrdered { get; set; }
        public int QuantityReceived { get; set; }
        public int RemainingQuantity => QuantityOrdered - QuantityReceived;
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice => UnitPrice * QuantityOrdered;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public string CancellationReason { get; set; } = string.Empty;
        public DateTime? DateReceived { get; set; }
    }

    public class ReceiveOrderViewModel
    {
        public int OrderID { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;

        [Display(Name = "Receive All Items")]
        public bool ReceiveAll { get; set; }

        public List<OrderItemReceiptViewModel>? Items { get; set; }
    }

    public class OrderItemReceiptViewModel
    {
        public int OrderItemID { get; set; }
        public int ConsumableID { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public int QuantityOrdered { get; set; }
        public int QuantityReceived { get; set; }
        public int QuantityRemaining => QuantityOrdered - QuantityReceived;
        public bool IsSelected { get; set; }
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
        public int OrderID { get; set; }
        public string OrderNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cancellation reason is required")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        [Display(Name = "Cancellation Reason")]
        public string CancellationReason { get; set; } = string.Empty;

        [Display(Name = "Notify Supplier")]
        public bool NotifySupplier { get; set; }
    }

    // =========================
    // STAFF MANAGEMENT VIEWMODELS
    // =========================
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
        public int CompletedTestsCount { get; set; }
        public double AverageTurnaroundTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime HireDate { get; set; }
    }

    public class AddTechnicianViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required")]
        [StringLength(100)]
        [Display(Name = "Surname")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "SA ID number is required")]
        [StringLength(13, MinimumLength = 13)]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "ID number must be 13 digits")]
        [Display(Name = "South African ID Number")]
        public string SAIDNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Employee number is required")]
        [StringLength(50)]
        [Display(Name = "Employee Number")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Display(Name = "Specialization")]
        public string Specialization { get; set; } = string.Empty;

        [Display(Name = "Qualification")]
        public string Qualification { get; set; } = string.Empty;

        [Display(Name = "Years of Experience")]
        [Range(0, 50)]
        public int YearsOfExperience { get; set; }

        [Display(Name = "Assign Test Types")]
        public List<int> SelectedTestTypeIDs { get; set; } = new();

        public List<SelectListItem> AvailableTestTypes { get; set; } = new();
    }

    public class EditTechnicianViewModel
    {
        public int TechnicianID { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required")]
        [StringLength(100)]
        [Display(Name = "Surname")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "SA ID number is required")]
        [StringLength(13, MinimumLength = 13)]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "ID number must be 13 digits")]
        [Display(Name = "South African ID Number")]
        public string SAIDNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Employee number is required")]
        [StringLength(50)]
        [Display(Name = "Employee Number")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Display(Name = "Specialization")]
        public string Specialization { get; set; } = string.Empty;

        [Display(Name = "Qualification")]
        public string Qualification { get; set; } = string.Empty;

        [Display(Name = "Years of Experience")]
        [Range(0, 50)]
        public int YearsOfExperience { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Assign Test Types")]
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
        public PerformanceStatisticsViewModel Performance { get; set; } = new();
    }

    public class AssignedTestViewModel
    {
        public int TestID { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class PerformanceStatisticsViewModel
    {
        public int TotalTestsAssigned { get; set; }
        public int TotalTestsCompleted { get; set; }
        public int TestsThisMonth { get; set; }
        public double AverageCompletionTime { get; set; }
        public int VerificationAccuracy { get; set; }
        public List<MonthlyPerformanceViewModel> MonthlyPerformance { get; set; } = new();
    }

    public class MonthlyPerformanceViewModel
    {
        public string Month { get; set; } = string.Empty;
        public int TestsCompleted { get; set; }
        public int TestsVerified { get; set; }
        public double AverageTurnaroundTime { get; set; }
    }

    public class TechnicianPerformanceDetailsViewModel
    {
        public int TechnicianID { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public PerformanceSummaryViewModel Summary { get; set; } = new();
        public List<TestPerformanceViewModel> TestDetails { get; set; } = new();
        public List<DailyPerformanceViewModel> DailyPerformance { get; set; } = new();
    }

    public class TestPerformanceViewModel
    {
        public string TestName { get; set; } = string.Empty;
        public int PerformedCount { get; set; }
        public int VerifiedCount { get; set; }
        public double AverageTurnaroundTime { get; set; }
        public int AbnormalResults { get; set; }
        public int RejectedResults { get; set; }
    }

    public class DailyPerformanceViewModel
    {
        public DateTime Date { get; set; }
        public int TestsCompleted { get; set; }
        public double AverageTurnaroundTime { get; set; }
    }

    public class PerformanceSummaryViewModel
    {
        public int TotalTests { get; set; }
        public int VerifiedTests { get; set; }
        public int PendingTests { get; set; }
        public int RejectedTests { get; set; }
        public double OverallAverageTurnaroundTime { get; set; }
        public double VerificationRate { get; set; }
    }

    public class DoctorListViewModel
    {
        public int DoctorID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string HPCSANumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PracticeName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public int TotalRequests { get; set; }
        public DateTime RegisteredDate { get; set; }
        public bool IsVerified { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }

    public class RegisterDoctorViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required")]
        [StringLength(100)]
        [Display(Name = "Surname")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "HPCSA number is required")]
        [StringLength(20, MinimumLength = 5)]
        [Display(Name = "HPCSA Number")]
        public string HPCSANumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Display(Name = "Practice Name")]
        public string PracticeName { get; set; } = string.Empty;

        [Display(Name = "Practice Address")]
        public string PracticeAddress { get; set; } = string.Empty;

        [Display(Name = "Specialization")]
        public string Specialization { get; set; } = string.Empty;
    }

    public class EditDoctorViewModel
    {
        public int DoctorID { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required")]
        [StringLength(100)]
        [Display(Name = "Surname")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "HPCSA number is required")]
        [StringLength(20, MinimumLength = 5)]
        [Display(Name = "HPCSA Number")]
        public string HPCSANumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Display(Name = "Practice Name")]
        public string PracticeName { get; set; } = string.Empty;

        [Display(Name = "Practice Address")]
        public string PracticeAddress { get; set; } = string.Empty;

        [Display(Name = "Specialization")]
        public string Specialization { get; set; } = string.Empty;

        [Display(Name = "Verified")]
        public bool IsVerified { get; set; }

        [Display(Name = "Active")]
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

    // =========================
    // REPORT VIEWMODELS
    // =========================
    public class ReportIndexViewModel
    {
        public List<ReportTypeViewModel> ReportTypes { get; set; } = new();
        public ReportFilterViewModel Filter { get; set; } = new();
    }

    public class ReportTypeViewModel
    {
        public string ReportType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class ReportFilterViewModel
    {
        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Now;

        [Display(Name = "Report Type")]
        public string ReportType { get; set; } = string.Empty;

        [Display(Name = "Group By")]
        public string GroupBy { get; set; } = string.Empty;

        [Display(Name = "Include Inactive")]
        public bool IncludeInactive { get; set; }
    }

    public class TestsPerformedReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = string.Empty;
        public ReportSummaryViewModel Summary { get; set; } = new();
        public List<CategoryReportViewModel> Categories { get; set; } = new();
        public List<TechnicianReportViewModel> Technicians { get; set; } = new();
        public List<DailyReportViewModel> DailyBreakdown { get; set; } = new();
    }

    public class ReportSummaryViewModel
    {
        public int TotalTests { get; set; }
        public int TotalVerified { get; set; }
        public int TotalPending { get; set; }
        public int AbnormalResults { get; set; }
        public double OverallAverageTurnaroundTime { get; set; }
        public int UniquePatients { get; set; }
        public int UniqueDoctors { get; set; }
    }

    public class CategoryReportViewModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public int TotalTests { get; set; }
        public int VerifiedTests { get; set; }
        public int AbnormalResults { get; set; }
        public double AverageTurnaroundTime { get; set; }
        public decimal PercentageOfTotal { get; set; }
        public List<TestReportDetail> TestDetails { get; set; } = new();
    }

    public class TestReportDetail
    {
        public string TestName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int VerifiedCount { get; set; }
        public int AbnormalCount { get; set; }
        public double AverageTurnaroundTime { get; set; }
        public decimal PercentageOfCategory { get; set; }
    }

    public class TechnicianReportViewModel
    {
        public int TechnicianID { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
        public int TestsPerformed { get; set; }
        public int TestsVerified { get; set; }
        public double AverageTurnaroundTime { get; set; }
        public int AbnormalResultsHandled { get; set; }
        public decimal PercentageOfTotal { get; set; }
    }

    public class DailyReportViewModel
    {
        public DateTime Date { get; set; }
        public int TestsPerformed { get; set; }
        public int TestsVerified { get; set; }
        public double AverageTurnaroundTime { get; set; }
        public int AbnormalResults { get; set; }
    }

    public class StockReportViewModel
    {
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = string.Empty;
        public StockReportSummary Summary { get; set; } = new();
        public List<StockReportItemViewModel> Items { get; set; } = new();
        public List<SupplierStockSummary> SupplierSummaries { get; set; } = new();
    }

    public class StockReportSummary
    {
        public int TotalItems { get; set; }
        public int ActiveItems { get; set; }
        public int InactiveItems { get; set; }
        public int LowStockItems { get; set; }
        public int CriticalStockItems { get; set; }
        public decimal TotalStockValue { get; set; }
        public int TotalSuppliers { get; set; }
    }

    public class StockReportItemViewModel
    {
        public string ConsumableName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public int ReorderLevel { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; }
        public decimal? TotalValue { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string StorageLocation { get; set; } = string.Empty;
        public DateTime LastRestocked { get; set; }
    }

    public class SupplierStockSummary
    {
        public string SupplierName { get; set; } = string.Empty;
        public int ItemsSupplied { get; set; }
        public int ItemsBelowReorder { get; set; }
        public decimal TotalValue { get; set; }
        public string ContactPerson { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
    }

    // =========================
    // TEST VERIFICATION VIEWMODELS
    // =========================
    public class TestVerificationIndexViewModel
    {
        public List<PendingVerificationViewModel> PendingVerifications { get; set; } = new();
        public List<VerifiedTestsViewModel> RecentVerifications { get; set; } = new();
        public VerificationStatisticsViewModel Statistics { get; set; } = new();
        public TestVerificationFilterViewModel Filter { get; set; } = new();
    }

    public class PendingVerificationViewModel
    {
        public int TestResultID { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string TechnicianName { get; set; } = string.Empty;
        public DateTime CompletionDate { get; set; }
        public string ResultValue { get; set; } = string.Empty;
        public string NormalRange { get; set; } = string.Empty;
        public bool IsAbnormal { get; set; }
        public string Notes { get; set; } = string.Empty;
        public int RequestID { get; set; }
    }

    public class VerifiedTestsViewModel
    {
        public int TestResultID { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string VerifiedBy { get; set; } = string.Empty;
        public DateTime VerificationDate { get; set; }
        public string ResultValue { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }

    public class TestVerificationFilterViewModel
    {
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Show Abnormal Only")]
        public bool ShowAbnormalOnly { get; set; }

        [Display(Name = "Category")]
        public int? CategoryID { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();
    }

    public class VerificationStatisticsViewModel
    {
        public int TotalPending { get; set; }
        public int PendingAbnormal { get; set; }
        public int VerifiedToday { get; set; }
        public int VerifiedThisWeek { get; set; }
        public int VerifiedThisMonth { get; set; }
        public int AbnormalResults { get; set; }
        public double AverageVerificationTime { get; set; }
        public double VerificationRate { get; set; }
    }

    // =========================
    // SETTINGS VIEWMODELS
    // =========================
    public class SettingsIndexViewModel
    {
        public ProfileSettingsViewModel Profile { get; set; } = new();
        public SecuritySettingsViewModel Security { get; set; } = new();
        public NotificationSettingsViewModel Notifications { get; set; } = new();
        public SystemSettingsViewModel System { get; set; } = new();
        public List<AuditLogViewModel> RecentAuditLogs { get; set; } = new();
    }

    public class ProfileSettingsViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required")]
        [StringLength(100)]
        [Display(Name = "Surname")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Display(Name = "Profile Picture")]
        public string ProfilePicture { get; set; } = string.Empty;
        public string FullName => $"{Name} {Surname}";
    }

    public class SecuritySettingsViewModel
    {
        [Display(Name = "Two-Factor Authentication")]
        public bool TwoFactorEnabled { get; set; }

        [Display(Name = "Session Timeout (minutes)")]
        [Range(5, 120, ErrorMessage = "Session timeout must be between 5 and 120 minutes")]
        public int SessionTimeout { get; set; } = 30;

        [Display(Name = "Notify on New Login")]
        public bool NotifyOnNewLogin { get; set; } = true;

        [Display(Name = "Password Expiry (days)")]
        [Range(0, 365, ErrorMessage = "Password expiry must be between 0 and 365 days")]
        public int PasswordExpiryDays { get; set; } = 90;

        [Display(Name = "Require Password Change")]
        public bool RequirePasswordChange { get; set; }

        public ChangePasswordViewModel ChangePassword { get; set; } = new();
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one number, and one special character")]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class NotificationSettingsViewModel
    {
        [Display(Name = "Email Notifications")]
        public bool EmailNotifications { get; set; } = true;

        [Display(Name = "Low Stock Alerts")]
        public bool LowStockAlerts { get; set; } = true;

        [Display(Name = "Order Status Updates")]
        public bool OrderStatusUpdates { get; set; } = true;

        [Display(Name = "Test Verification Alerts")]
        public bool TestVerificationAlerts { get; set; } = true;

        [Display(Name = "Alert Threshold (days)")]
        [Range(1, 30, ErrorMessage = "Threshold must be between 1 and 30 days")]
        public int AlertThresholdDays { get; set; } = 5;

        [Display(Name = "Alert Frequency")]
        public string AlertFrequency { get; set; } = "Daily";
    }

    public class SystemSettingsViewModel
    {
        [Display(Name = "Laboratory Name")]
        public string LaboratoryName { get; set; } = string.Empty;

        [Display(Name = "Default Turnaround Time (hours)")]
        [Range(1, 72, ErrorMessage = "Turnaround time must be between 1 and 72 hours")]
        public int DefaultTurnaroundTime { get; set; } = 24;

        [Display(Name = "Auto-Verify Normal Results")]
        public bool AutoVerifyNormalResults { get; set; } = false;

        [Display(Name = "Enable Audit Logging")]
        public bool EnableAuditLogging { get; set; } = true;

        [Display(Name = "Results Retention Period (years)")]
        [Range(1, 10, ErrorMessage = "Retention period must be between 1 and 10 years")]
        public int ResultsRetentionPeriod { get; set; } = 5;

        [Display(Name = "Maximum Failed Login Attempts")]
        [Range(3, 10, ErrorMessage = "Maximum attempts must be between 3 and 10")]
        public int MaxFailedLoginAttempts { get; set; } = 5;

        [Display(Name = "Account Lockout Duration (minutes)")]
        [Range(5, 60, ErrorMessage = "Lockout duration must be between 5 and 60 minutes")]
        public int AccountLockoutDuration { get; set; } = 15;
    }

    public class AuditLogViewModel
    {
        public int LogID { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string IPAddress { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}