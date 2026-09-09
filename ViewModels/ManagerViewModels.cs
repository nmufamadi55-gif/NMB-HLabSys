using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace NMB_HLabSys.Models.ViewModels
{
    // ============================================================
    // DASHBOARD VIEWMODELS
    // ============================================================

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

    // ============================================================
    // TEST CATALOGUE VIEWMODELS
    // ============================================================

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
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Category name is required")]
        [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters")]
        public string CategoryName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }

    public class EditTestCategoryViewModel
    {
        public int CategoryID { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Category name is required")]
        [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters")]
        public string CategoryName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
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

    public class CreateTestTypeViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Test name is required")]
        [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 3)]
        public string TestName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please select a category")]
        public int CategoryID { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please select a sample type")]
        public int SampleTypeID { get; set; }

        public string? UnitOfMeasurement { get; set; }
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Turnaround time is required")]
        [System.ComponentModel.DataAnnotations.Range(5, 1440)]
        public int TurnaroundTimeMinutes { get; set; }

        public List<int>? SelectedTechnicianIDs { get; set; }
        public Dictionary<int, int>? ConsumableQuantities { get; set; }

        public List<SelectListItem>? AvailableCategories { get; set; }
        public List<SelectListItem>? AvailableSampleTypes { get; set; }
        public List<SelectListItem>? AvailableTechnicians { get; set; }
        public List<SelectListItem>? AvailableConsumables { get; set; }
    }

    public class EditTestTypeViewModel
    {
        public int TestID { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Test name is required")]
        [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 3)]
        public string TestName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please select a category")]
        public int CategoryID { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please select a sample type")]
        public int SampleTypeID { get; set; }

        public string? UnitOfMeasurement { get; set; }
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Turnaround time is required")]
        [System.ComponentModel.DataAnnotations.Range(5, 1440)]
        public int TurnaroundTimeMinutes { get; set; }

        public bool IsActive { get; set; }

        public List<int>? SelectedTechnicianIDs { get; set; }
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
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Sample type name is required")]
        [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 2)]
        public string SampleTypeName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public string CollectionTubeType { get; set; } = string.Empty;
        public string StorageRequirements { get; set; } = string.Empty;
    }

    public class EditSampleTypeViewModel
    {
        public int SampleTypeID { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Sample type name is required")]
        [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 2)]
        public string SampleTypeName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public string CollectionTubeType { get; set; } = string.Empty;
        public string StorageRequirements { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    // ============================================================
    // STOCK MANAGEMENT VIEWMODELS
    // ============================================================

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
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Consumable name is required")]
        [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 2)]
        public string ConsumableName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please select a supplier")]
        public int SupplierID { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Reorder level is required")]
        [System.ComponentModel.DataAnnotations.Range(0, 1000)]
        public int ReorderLevel { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Quantity on hand is required")]
        [System.ComponentModel.DataAnnotations.Range(0, 10000)]
        public int QuantityOnHand { get; set; }

        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; }
        public int? MinimumStockLevel { get; set; }
        public string StorageLocation { get; set; } = string.Empty;

        public List<SelectListItem>? AvailableSuppliers { get; set; }
    }

    public class EditConsumableViewModel
    {
        public int ConsumableID { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Consumable name is required")]
        [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 2)]
        public string ConsumableName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please select a supplier")]
        public int SupplierID { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Reorder level is required")]
        [System.ComponentModel.DataAnnotations.Range(0, 1000)]
        public int ReorderLevel { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Quantity on hand is required")]
        [System.ComponentModel.DataAnnotations.Range(0, 10000)]
        public int QuantityOnHand { get; set; }

        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; }
        public int? MinimumStockLevel { get; set; }
        public string StorageLocation { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public List<SelectListItem>? AvailableSuppliers { get; set; }
    }

    public class AdjustStockViewModel
    {
        public int ConsumableID { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public int CurrentQuantity { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Adjustment amount is required")]
        [System.ComponentModel.DataAnnotations.Range(1, 10000)]
        public int AdjustmentAmount { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please select an adjustment type")]
        public string AdjustmentType { get; set; } = string.Empty;

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

    // ============================================================
    // SUPPLIER VIEWMODELS
    // ============================================================

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
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Supplier name is required")]
        [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 2)]
        public string SupplierName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Contact person is required")]
        [System.ComponentModel.DataAnnotations.StringLength(150)]
        public string ContactPerson { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Email address is required")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Invalid email format")]
        public string EmailAddress { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsPreferred { get; set; }
    }

    public class EditSupplierViewModel
    {
        public int SupplierID { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Supplier name is required")]
        [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 2)]
        public string SupplierName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Contact person is required")]
        [System.ComponentModel.DataAnnotations.StringLength(150)]
        public string ContactPerson { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Email address is required")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Invalid email format")]
        public string EmailAddress { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;
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

    // ============================================================
    // PURCHASE ORDER VIEWMODELS
    // ============================================================

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
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please select a supplier")]
        public int SupplierID { get; set; }

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime? ExpectedDeliveryDate { get; set; }

        public List<OrderItemInputViewModel>? Items { get; set; }
        public List<SelectListItem>? AvailableSuppliers { get; set; }
    }

    public class OrderItemInputViewModel
    {
        public int ConsumableID { get; set; }
        public string ConsumableName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public int SuggestedQuantity { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Quantity is required")]
        [System.ComponentModel.DataAnnotations.Range(1, 10000)]
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
        public int OrderID { get; set; }
        public string OrderNumber { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Cancellation reason is required")]
        [System.ComponentModel.DataAnnotations.StringLength(500)]
        public string CancellationReason { get; set; } = string.Empty;

        public bool NotifySupplier { get; set; }
    }

    // ============================================================
    // STAFF MANAGEMENT VIEWMODELS
    // ============================================================

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
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Surname is required")]
        public string Surname { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "SA ID number is required")]
        [System.ComponentModel.DataAnnotations.StringLength(13, MinimumLength = 13)]
        [System.ComponentModel.DataAnnotations.RegularExpression(@"^\d{13}$", ErrorMessage = "ID number must be 13 digits")]
        public string SAIDNumber { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Employee number is required")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Email address is required")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Contact number is required")]
        public string ContactNumber { get; set; } = string.Empty;

        public string Specialization { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Range(0, 50)]
        public int YearsOfExperience { get; set; }

        public List<int> SelectedTestTypeIDs { get; set; } = new();
        public List<SelectListItem> AvailableTestTypes { get; set; } = new();
    }

    public class EditTechnicianViewModel
    {
        public int TechnicianID { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Surname is required")]
        public string Surname { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "SA ID number is required")]
        [System.ComponentModel.DataAnnotations.StringLength(13, MinimumLength = 13)]
        [System.ComponentModel.DataAnnotations.RegularExpression(@"^\d{13}$", ErrorMessage = "ID number must be 13 digits")]
        public string SAIDNumber { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Employee number is required")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Email address is required")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Contact number is required")]
        public string ContactNumber { get; set; } = string.Empty;

        public string Specialization { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Range(0, 50)]
        public int YearsOfExperience { get; set; }

        public bool IsActive { get; set; }

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
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Surname is required")]
        public string Surname { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "HPCSA number is required")]
        [System.ComponentModel.DataAnnotations.StringLength(20, MinimumLength = 5)]
        public string HPCSANumber { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Email address is required")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Contact number is required")]
        public string ContactNumber { get; set; } = string.Empty;

        public string PracticeName { get; set; } = string.Empty;
        public string PracticeAddress { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
    }

    public class EditDoctorViewModel
    {
        public int DoctorID { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Surname is required")]
        public string Surname { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "HPCSA number is required")]
        [System.ComponentModel.DataAnnotations.StringLength(20, MinimumLength = 5)]
        public string HPCSANumber { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Email address is required")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Contact number is required")]
        public string ContactNumber { get; set; } = string.Empty;

        public string PracticeName { get; set; } = string.Empty;
        public string PracticeAddress { get; set; } = string.Empty;
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

    // ============================================================
    // REPORT VIEWMODELS
    // ============================================================

    public class ReportFilterViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Start date is required")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "End date is required")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
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
}