using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NMB_HLabSys.Models.ViewModels;
using NMB_HLabSys.Services;

namespace NMB_HLabSys.Controllers
{
    [Route("Manager/[action]")]
    [Route("[controller]/[action]")]
    public class LaboratoryManagerController : Controller
    {
        private readonly ILaboratoryManagerService _service;
        private readonly ILogger<LaboratoryManagerController> _logger;

        public LaboratoryManagerController(
            ILaboratoryManagerService service,
            ILogger<LaboratoryManagerController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ============================================================
        // DASHBOARD
        // ============================================================

        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = await _service.GetDashboardAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["ErrorMessage"] = "Failed to load dashboard data.";
                return View(new DashboardViewModel());
            }
        }

        // ============================================================
        // TEST CATALOGUE
        // ============================================================

        public async Task<IActionResult> TestCatalogue()
        {
            var viewModel = await _service.GetTestCatalogueAsync();
            return View(viewModel);
        }

        public async Task<IActionResult> CreateTestType()
        {
            var viewModel = await _service.GetCreateTestTypeViewModelAsync();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTestType(CreateTestTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // FIX: Get the view model properly
                var viewModel = await _service.GetCreateTestTypeViewModelAsync();
                model.AvailableCategories = viewModel.AvailableCategories;
                model.AvailableSampleTypes = viewModel.AvailableSampleTypes;
                model.AvailableTechnicians = viewModel.AvailableTechnicians;
                model.AvailableConsumables = viewModel.AvailableConsumables;
                return View(model);
            }

            try
            {
                var result = await _service.CreateTestTypeAsync(model);
                if (!result.Success)
                {
                    foreach (var error in result.ValidationErrors)
                        ModelState.AddModelError("", error);
                    var viewModel = await _service.GetCreateTestTypeViewModelAsync();
                    model.AvailableCategories = viewModel.AvailableCategories;
                    model.AvailableSampleTypes = viewModel.AvailableSampleTypes;
                    model.AvailableTechnicians = viewModel.AvailableTechnicians;
                    model.AvailableConsumables = viewModel.AvailableConsumables;
                    return View(model);
                }

                TempData["SuccessMessage"] = "Test type created successfully.";
                return RedirectToAction(nameof(TestCatalogue));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test type");
                TempData["ErrorMessage"] = "An error occurred while creating the test type.";
                return View(model);
            }
        }

        public async Task<IActionResult> EditTestType(int id)
        {
            try
            {
                var viewModel = await _service.GetEditTestTypeViewModelAsync(id);
                return View(viewModel);
            }
            catch (ArgumentException)
            {
                TempData["ErrorMessage"] = $"Test type with ID {id} not found.";
                return RedirectToAction(nameof(TestCatalogue));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTestType(EditTestTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var viewModel = await _service.GetEditTestTypeViewModelAsync(model.TestID);
                model.AvailableCategories = viewModel.AvailableCategories;
                model.AvailableSampleTypes = viewModel.AvailableSampleTypes;
                model.AvailableTechnicians = viewModel.AvailableTechnicians;
                model.AvailableConsumables = viewModel.AvailableConsumables;
                return View(model);
            }

            try
            {
                var result = await _service.EditTestTypeAsync(model);
                if (!result.Success)
                {
                    foreach (var error in result.ValidationErrors)
                        ModelState.AddModelError("", error);
                    return View(model);
                }

                TempData["SuccessMessage"] = "Test type updated successfully.";
                return RedirectToAction(nameof(TestCatalogue));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating test type");
                TempData["ErrorMessage"] = "An error occurred while updating the test type.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTestType(int id)
        {
            try
            {
                var result = await _service.DeleteTestTypeAsync(id);
                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                }
                else
                {
                    TempData["SuccessMessage"] = "Test type deleted successfully.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting test type");
                TempData["ErrorMessage"] = "An error occurred while deleting the test type.";
            }

            return RedirectToAction(nameof(TestCatalogue));
        }

        // ============================================================
        // TEST CATEGORIES
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CreateTestCategoryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _service.CreateCategoryAsync(model);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.ErrorMessage);
                    return View(model);
                }

                TempData["SuccessMessage"] = "Category created successfully.";
                return RedirectToAction(nameof(TestCatalogue));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                TempData["ErrorMessage"] = "An error occurred while creating the category.";
                return View(model);
            }
        }

        public async Task<IActionResult> EditCategory(int id)
        {
            try
            {
                var viewModel = await _service.GetEditCategoryViewModelAsync(id);
                return View(viewModel);
            }
            catch (ArgumentException)
            {
                TempData["ErrorMessage"] = $"Category with ID {id} not found.";
                return RedirectToAction(nameof(TestCatalogue));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(EditTestCategoryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _service.EditCategoryAsync(model);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.ErrorMessage);
                    return View(model);
                }

                TempData["SuccessMessage"] = "Category updated successfully.";
                return RedirectToAction(nameof(TestCatalogue));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                TempData["ErrorMessage"] = "An error occurred while updating the category.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var result = await _service.DeleteCategoryAsync(id);
                if (!result.Success)
                    TempData["ErrorMessage"] = result.ErrorMessage;
                else
                    TempData["SuccessMessage"] = "Category deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                TempData["ErrorMessage"] = "An error occurred while deleting the category.";
            }

            return RedirectToAction(nameof(TestCatalogue));
        }

        // ============================================================
        // SAMPLE TYPES
        // ============================================================

        public IActionResult CreateSampleType() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSampleType(CreateSampleTypeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _service.CreateSampleTypeAsync(model);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.ErrorMessage);
                    return View(model);
                }

                TempData["SuccessMessage"] = "Sample type created successfully.";
                return RedirectToAction(nameof(TestCatalogue));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sample type");
                TempData["ErrorMessage"] = "An error occurred while creating the sample type.";
                return View(model);
            }
        }

        public async Task<IActionResult> EditSampleType(int id)
        {
            try
            {
                var viewModel = await _service.GetEditSampleTypeViewModelAsync(id);
                return View(viewModel);
            }
            catch (ArgumentException)
            {
                TempData["ErrorMessage"] = $"Sample type with ID {id} not found.";
                return RedirectToAction(nameof(TestCatalogue));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSampleType(EditSampleTypeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _service.EditSampleTypeAsync(model);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.ErrorMessage);
                    return View(model);
                }

                TempData["SuccessMessage"] = "Sample type updated successfully.";
                return RedirectToAction(nameof(TestCatalogue));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sample type");
                TempData["ErrorMessage"] = "An error occurred while updating the sample type.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSampleType(int id)
        {
            try
            {
                var result = await _service.DeleteSampleTypeAsync(id);
                if (!result.Success)
                    TempData["ErrorMessage"] = result.ErrorMessage;
                else
                    TempData["SuccessMessage"] = "Sample type deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting sample type");
                TempData["ErrorMessage"] = "An error occurred while deleting the sample type.";
            }

            return RedirectToAction(nameof(TestCatalogue));
        }

        // ============================================================
        // STOCK MANAGEMENT
        // ============================================================

        public async Task<IActionResult> Stock()
        {
            var viewModel = await _service.GetStockManagementAsync();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateConsumable(CreateConsumableViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var viewModel = await _service.GetCreateConsumableViewModelAsync();
                model.AvailableSuppliers = viewModel.AvailableSuppliers;
                return View(model);
            }

            try
            {
                var result = await _service.CreateConsumableAsync(model);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.ErrorMessage);
                    var viewModel = await _service.GetCreateConsumableViewModelAsync();
                    model.AvailableSuppliers = viewModel.AvailableSuppliers;
                    return View(model);
                }

                TempData["SuccessMessage"] = "Consumable created successfully.";
                return RedirectToAction(nameof(Stock));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating consumable");
                TempData["ErrorMessage"] = "An error occurred while creating the consumable.";
                return View(model);
            }
        }

        public async Task<IActionResult> EditConsumable(int id)
        {
            try
            {
                var viewModel = await _service.GetEditConsumableViewModelAsync(id);
                return View(viewModel);
            }
            catch (ArgumentException)
            {
                TempData["ErrorMessage"] = $"Consumable with ID {id} not found.";
                return RedirectToAction(nameof(Stock));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConsumable(EditConsumableViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var viewModel = await _service.GetEditConsumableViewModelAsync(model.ConsumableID);
                model.AvailableSuppliers = viewModel.AvailableSuppliers;
                return View(model);
            }

            try
            {
                var result = await _service.EditConsumableAsync(model);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.ErrorMessage);
                    var viewModel = await _service.GetEditConsumableViewModelAsync(model.ConsumableID);
                    model.AvailableSuppliers = viewModel.AvailableSuppliers;
                    return View(model);
                }

                TempData["SuccessMessage"] = "Consumable updated successfully.";
                return RedirectToAction(nameof(Stock));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating consumable");
                TempData["ErrorMessage"] = "An error occurred while updating the consumable.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConsumable(int id)
        {
            try
            {
                var result = await _service.DeleteConsumableAsync(id);
                if (!result.Success)
                    TempData["ErrorMessage"] = result.ErrorMessage;
                else
                    TempData["SuccessMessage"] = "Consumable deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting consumable");
                TempData["ErrorMessage"] = "An error occurred while deleting the consumable.";
            }

            return RedirectToAction(nameof(Stock));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(AdjustStockViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _service.AdjustStockAsync(model);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.ErrorMessage);
                    return View(model);
                }

                TempData["SuccessMessage"] = "Stock adjusted successfully.";
                return RedirectToAction(nameof(Stock));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting stock");
                TempData["ErrorMessage"] = "An error occurred while adjusting stock.";
                return View(model);
            }
        }

        // ============================================================
        // SUPPLIERS
        // ============================================================

        public async Task<IActionResult> Suppliers()
        {
            var viewModel = await _service.GetSuppliersAsync();
            return View(viewModel);
        }

        public IActionResult CreateSupplier() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplier(CreateSupplierViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _service.CreateSupplierAsync(model);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.ErrorMessage);
                    return View(model);
                }

                TempData["SuccessMessage"] = "Supplier created successfully.";
                return RedirectToAction(nameof(Suppliers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier");
                TempData["ErrorMessage"] = "An error occurred while creating the supplier.";
                return View(model);
            }
        }

        public async Task<IActionResult> EditSupplier(int id)
        {
            try
            {
                var viewModel = await _service.GetEditSupplierViewModelAsync(id);
                return View(viewModel);
            }
            catch (ArgumentException)
            {
                TempData["ErrorMessage"] = $"Supplier with ID {id} not found.";
                return RedirectToAction(nameof(Suppliers));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(EditSupplierViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _service.EditSupplierAsync(model);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.ErrorMessage);
                    return View(model);
                }

                TempData["SuccessMessage"] = "Supplier updated successfully.";
                return RedirectToAction(nameof(Suppliers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier");
                TempData["ErrorMessage"] = "An error occurred while updating the supplier.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            try
            {
                var result = await _service.DeleteSupplierAsync(id);
                if (!result.Success)
                    TempData["ErrorMessage"] = result.ErrorMessage;
                else
                    TempData["SuccessMessage"] = "Supplier deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier");
                TempData["ErrorMessage"] = "An error occurred while deleting the supplier.";
            }

            return RedirectToAction(nameof(Suppliers));
        }

        public async Task<IActionResult> SupplierDetails(int id)
        {
            try
            {
                var viewModel = await _service.GetSupplierDetailsAsync(id);
                return View(viewModel);
            }
            catch (ArgumentException)
            {
                TempData["ErrorMessage"] = $"Supplier with ID {id} not found.";
                return RedirectToAction(nameof(Suppliers));
            }
        }

        // ============================================================
        // PURCHASE ORDERS
        // ============================================================

        public async Task<IActionResult> Orders()
        {
            var viewModel = await _service.GetOrdersAsync();
            return View(viewModel);
        }

        public async Task<IActionResult> CreateOrder()
        {
            var viewModel = await _service.GetCreateOrderViewModelAsync();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(CreateOrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var viewModel = await _service.GetCreateOrderViewModelAsync();
                model.AvailableSuppliers = viewModel.AvailableSuppliers;
                return View(model);
            }

            try
            {
                var result = await _service.CreateOrderAsync(model);
                if (!result.Success)
                {
                    foreach (var error in result.ValidationErrors)
                        ModelState.AddModelError("", error);
                    var viewModel = await _service.GetCreateOrderViewModelAsync();
                    model.AvailableSuppliers = viewModel.AvailableSuppliers;
                    return View(model);
                }

                TempData["SuccessMessage"] = $"Order #{result.Data} created successfully.";
                return RedirectToAction(nameof(Orders));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                TempData["ErrorMessage"] = "An error occurred while creating the order.";
                return View(model);
            }
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            try
            {
                var viewModel = await _service.GetOrderDetailsAsync(id);
                return View(viewModel);
            }
            catch (ArgumentException)
            {
                TempData["ErrorMessage"] = $"Order with ID {id} not found.";
                return RedirectToAction(nameof(Orders));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveOrderItem(int orderItemId)
        {
            try
            {
                var result = await _service.ReceiveOrderItemAsync(orderItemId);
                if (!result.Success)
                    TempData["ErrorMessage"] = result.ErrorMessage;
                else
                    TempData["SuccessMessage"] = "Item received successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving order item");
                TempData["ErrorMessage"] = "An error occurred while receiving the item.";
            }

            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(CancelOrderViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _service.CancelOrderAsync(model);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.ErrorMessage);
                    return View(model);
                }

                TempData["SuccessMessage"] = "Order cancelled successfully.";
                return RedirectToAction(nameof(Orders));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order");
                TempData["ErrorMessage"] = "An error occurred while cancelling the order.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrderItem(int orderItemId, string reason)
        {
            try
            {
                var result = await _service.CancelOrderItemAsync(orderItemId, reason);
                if (!result.Success)
                    TempData["ErrorMessage"] = result.ErrorMessage;
                else
                    TempData["SuccessMessage"] = "Order item cancelled successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order item");
                TempData["ErrorMessage"] = "An error occurred while cancelling the item.";
            }

            return RedirectToAction(nameof(Orders));
        }

        // ============================================================
        // STAFF MANAGEMENT
        // ============================================================

        public async Task<IActionResult> Staff()
        {
            var viewModel = await _service.GetStaffManagementAsync();
            return View(viewModel);
        }

        public async Task<IActionResult> AddTechnician()
        {
            var viewModel = await _service.GetAddTechnicianViewModelAsync();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTechnician(AddTechnicianViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var viewModel = await _service.GetAddTechnicianViewModelAsync();
                model.AvailableTestTypes = viewModel.AvailableTestTypes;
                return View(model);
            }

            try
            {
                var result = await _service.AddTechnicianAsync(model);
                if (!result.Success)
                {
                    foreach (var error in result.ValidationErrors)
                        ModelState.AddModelError("", error);
                    var viewModel = await _service.GetAddTechnicianViewModelAsync();
                    model.AvailableTestTypes = viewModel.AvailableTestTypes;
                    return View(model);
                }

                TempData["SuccessMessage"] = "Technician added successfully. Credentials have been emailed.";
                return RedirectToAction(nameof(Staff));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding technician");
                TempData["ErrorMessage"] = "An error occurred while adding the technician.";
                return View(model);
            }
        }

        public async Task<IActionResult> EditTechnician(int id)
        {
            try
            {
                var viewModel = await _service.GetEditTechnicianViewModelAsync(id);
                return View(viewModel);
            }
            catch (ArgumentException)
            {
                TempData["ErrorMessage"] = $"Technician with ID {id} not found.";
                return RedirectToAction(nameof(Staff));
            }
        }

        public async Task<IActionResult> TechnicianProfile(int id)
        {
            try
            {
                var viewModel = await _service.GetTechnicianProfileAsync(id);
                return View(viewModel);
            }
            catch (ArgumentException)
            {
                TempData["ErrorMessage"] = $"Technician with ID {id} not found.";
                return RedirectToAction(nameof(Staff));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTechnician(int id)
        {
            try
            {
                var result = await _service.DeleteTechnicianAsync(id);
                if (!result.Success)
                    TempData["ErrorMessage"] = result.ErrorMessage;
                else
                    TempData["SuccessMessage"] = "Technician removed successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting technician");
                TempData["ErrorMessage"] = "An error occurred while removing the technician.";
            }

            return RedirectToAction(nameof(Staff));
        }

        public IActionResult AddDoctor() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctor(RegisterDoctorViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _service.AddDoctorAsync(model);
                if (!result.Success)
                {
                    foreach (var error in result.ValidationErrors)
                        ModelState.AddModelError("", error);
                    return View(model);
                }

                TempData["SuccessMessage"] = "Doctor added successfully. Credentials have been emailed.";
                return RedirectToAction(nameof(Staff));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding doctor");
                TempData["ErrorMessage"] = "An error occurred while adding the doctor.";
                return View(model);
            }
        }

        public async Task<IActionResult> EditDoctor(int id)
        {
            try
            {
                var viewModel = await _service.GetEditDoctorViewModelAsync(id);
                return View(viewModel);
            }
            catch (ArgumentException)
            {
                TempData["ErrorMessage"] = $"Doctor with ID {id} not found.";
                return RedirectToAction(nameof(Staff));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(EditDoctorViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _service.EditDoctorAsync(model);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.ErrorMessage);
                    return View(model);
                }

                TempData["SuccessMessage"] = "Doctor updated successfully.";
                return RedirectToAction(nameof(Staff));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating doctor");
                TempData["ErrorMessage"] = "An error occurred while updating the doctor.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            try
            {
                var result = await _service.DeleteDoctorAsync(id);
                if (!result.Success)
                    TempData["ErrorMessage"] = result.ErrorMessage;
                else
                    TempData["SuccessMessage"] = "Doctor removed successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting doctor");
                TempData["ErrorMessage"] = "An error occurred while removing the doctor.";
            }

            return RedirectToAction(nameof(Staff));
        }

        // ============================================================
        // REPORTS
        // ============================================================

        public async Task<IActionResult> Reports()
        {
            var viewModel = await _service.GetReportsViewModelAsync();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateCategoryReport(ReportFilterViewModel filter)
        {
            try
            {
                var bytes = await _service.GenerateCategoryReportAsync(filter);
                var fileName = $"Category_Tests_Report_{filter.StartDate:yyyyMMdd}_to_{filter.EndDate:yyyyMMdd}.pdf";
                return File(bytes, "application/pdf", fileName);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("Reports", filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating category report");
                TempData["ErrorMessage"] = "Failed to generate report.";
                return RedirectToAction(nameof(Reports));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateStockReport(ReportFilterViewModel filter)
        {
            try
            {
                var bytes = await _service.GenerateStockReportAsync(filter);
                var fileName = $"Stock_Report_{filter.StartDate:yyyyMMdd}_to_{filter.EndDate:yyyyMMdd}.pdf";
                return File(bytes, "application/pdf", fileName);
            }
            catch (NotImplementedException)
            {
                TempData["ErrorMessage"] = "Stock report is not yet implemented.";
                return RedirectToAction(nameof(Reports));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating stock report");
                TempData["ErrorMessage"] = "Failed to generate report.";
                return RedirectToAction(nameof(Reports));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateStaffReport(ReportFilterViewModel filter)
        {
            try
            {
                var bytes = await _service.GenerateStaffReportAsync(filter);
                var fileName = $"Staff_Report_{filter.StartDate:yyyyMMdd}_to_{filter.EndDate:yyyyMMdd}.pdf";
                return File(bytes, "application/pdf", fileName);
            }
            catch (NotImplementedException)
            {
                TempData["ErrorMessage"] = "Staff report is not yet implemented.";
                return RedirectToAction(nameof(Reports));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating staff report");
                TempData["ErrorMessage"] = "Failed to generate report.";
                return RedirectToAction(nameof(Reports));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateOrdersReport(ReportFilterViewModel filter)
        {
            try
            {
                var bytes = await _service.GenerateOrdersReportAsync(filter);
                var fileName = $"Orders_Report_{filter.StartDate:yyyyMMdd}_to_{filter.EndDate:yyyyMMdd}.pdf";
                return File(bytes, "application/pdf", fileName);
            }
            catch (NotImplementedException)
            {
                TempData["ErrorMessage"] = "Orders report is not yet implemented.";
                return RedirectToAction(nameof(Reports));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating orders report");
                TempData["ErrorMessage"] = "Failed to generate report.";
                return RedirectToAction(nameof(Reports));
            }
        }
    }
}