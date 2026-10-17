using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NMB_HLabSys.Models.ViewModels;

namespace NMB_HLabSys.Services
{
    public interface ILaboratoryManagerService
    {
        // ============ DASHBOARD ============
        Task<DashboardViewModel> GetDashboardAsync();

        // ============ TEST CATALOGUE ============
        Task<TestCatalogueIndexViewModel> GetTestCatalogueAsync();
        Task<CreateTestTypeViewModel> GetCreateTestTypeViewModelAsync();
        Task<ServiceResult<int>> CreateTestTypeAsync(CreateTestTypeViewModel model);
        Task<EditTestTypeViewModel> GetEditTestTypeViewModelAsync(int testId);
        Task<ServiceResult<bool>> EditTestTypeAsync(EditTestTypeViewModel model);
        Task<ServiceResult<bool>> DeleteTestTypeAsync(int testId);

        // ============ TEST CATEGORIES ============
        Task<ServiceResult<int>> CreateCategoryAsync(CreateTestCategoryViewModel model);
        Task<EditTestCategoryViewModel> GetEditCategoryViewModelAsync(int categoryId);
        Task<ServiceResult<bool>> EditCategoryAsync(EditTestCategoryViewModel model);
        Task<ServiceResult<bool>> DeleteCategoryAsync(int categoryId);

        // ============ SAMPLE TYPES ============
        Task<ServiceResult<int>> CreateSampleTypeAsync(CreateSampleTypeViewModel model);
        Task<EditSampleTypeViewModel> GetEditSampleTypeViewModelAsync(int sampleTypeId);
        Task<ServiceResult<bool>> EditSampleTypeAsync(EditSampleTypeViewModel model);
        Task<ServiceResult<bool>> DeleteSampleTypeAsync(int sampleTypeId);

        // ============ STOCK MANAGEMENT ============
        Task<StockManagementIndexViewModel> GetStockManagementAsync();
        Task<CreateConsumableViewModel> GetCreateConsumableViewModelAsync();
        Task<ServiceResult<int>> CreateConsumableAsync(CreateConsumableViewModel model);
        Task<EditConsumableViewModel> GetEditConsumableViewModelAsync(int consumableId);
        Task<ServiceResult<bool>> EditConsumableAsync(EditConsumableViewModel model);
        Task<ServiceResult<bool>> DeleteConsumableAsync(int consumableId);
        Task<ServiceResult<bool>> AdjustStockAsync(AdjustStockViewModel model);

        // ============ SUPPLIERS ============
        Task<List<SupplierViewModel>> GetSuppliersAsync();
        Task<ServiceResult<int>> CreateSupplierAsync(CreateSupplierViewModel model);
        Task<EditSupplierViewModel> GetEditSupplierViewModelAsync(int supplierId);
        Task<ServiceResult<bool>> EditSupplierAsync(EditSupplierViewModel model);
        Task<ServiceResult<bool>> DeleteSupplierAsync(int supplierId);
        Task<SupplierDetailsViewModel> GetSupplierDetailsAsync(int supplierId);

        // ============ PURCHASE ORDERS ============
        Task<OrderIndexViewModel> GetOrdersAsync();
        Task<CreateOrderViewModel> GetCreateOrderViewModelAsync();
        Task<ServiceResult<int>> CreateOrderAsync(CreateOrderViewModel model);
        Task<OrderDetailViewModel> GetOrderDetailsAsync(int orderId);
        Task<ServiceResult<bool>> ReceiveOrderItemAsync(int orderItemId);
        Task<ServiceResult<bool>> CancelOrderAsync(CancelOrderViewModel model);
        Task<ServiceResult<bool>> CancelOrderItemAsync(int orderItemId, string reason);

        // ============ STAFF MANAGEMENT ============
        Task<StaffManagementIndexViewModel> GetStaffManagementAsync();
        Task<AddTechnicianViewModel> GetAddTechnicianViewModelAsync();
        Task<ServiceResult<int>> AddTechnicianAsync(AddTechnicianViewModel model);
        Task<EditTechnicianViewModel> GetEditTechnicianViewModelAsync(int technicianId);
        Task<ServiceResult<bool>> EditTechnicianAsync(EditTechnicianViewModel model);
        Task<TechnicianProfileViewModel> GetTechnicianProfileAsync(int technicianId);
        Task<ServiceResult<bool>> DeleteTechnicianAsync(int technicianId);
        Task<ServiceResult<int>> AddDoctorAsync(RegisterDoctorViewModel model);
        Task<EditDoctorViewModel> GetEditDoctorViewModelAsync(int doctorId);
        Task<ServiceResult<bool>> EditDoctorAsync(EditDoctorViewModel model);
        Task<ServiceResult<bool>> DeleteDoctorAsync(int doctorId);

        // ============ REPORTS ============
        Task<ReportFilterViewModel> GetReportsViewModelAsync();
        Task<byte[]> GenerateCategoryReportAsync(ReportFilterViewModel filter);
        Task<byte[]> GenerateStockReportAsync(ReportFilterViewModel filter);
        Task<byte[]> GenerateStaffReportAsync(ReportFilterViewModel filter);
        Task<byte[]> GenerateOrdersReportAsync(ReportFilterViewModel filter);

        // ============ UTILITY ============
        Task<bool> IsTestNameUniqueAsync(string testName, int? excludeTestId = null);
        Task<bool> IsCategoryNameUniqueAsync(string categoryName, int? excludeCategoryId = null);
        Task<bool> IsConsumableNameUniqueAsync(string consumableName, int? excludeConsumableId = null);
        Task<bool> IsSupplierNameUniqueAsync(string supplierName, int? excludeSupplierId = null);
        Task<bool> IsEmployeeNumberUniqueAsync(string employeeNumber, int? excludeTechnicianId = null);
        Task<bool> IsSAIDNumberUniqueAsync(string saidNumber, int? excludeTechnicianId = null);
        Task<bool> IsHPCSANumberUniqueAsync(string hpcsaNumber, int? excludeDoctorId = null);
        Task<bool> IsEmailUniqueAsync(string email, int? excludeUserId = null);

        // ═══════════════════════════════════════════════════════════
        // CHART DATA
        // ═══════════════════════════════════════════════════════════
        Task<ChartDataDto> GetChartDataAsync();
    }
}