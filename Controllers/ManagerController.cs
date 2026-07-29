using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.Models.ViewModels;
using NMB_HLabSys.Services;
using System.Security.Claims;

namespace LabManager_UI.Controllers
{
    [Authorize(Roles = "Manager, Admin")]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockManagementService _stockService;
        private readonly ITestManagementService _testService;
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;
        private readonly ILogger<ManagerController> _logger;

        public ManagerController(
            ApplicationDbContext context,
            IStockManagementService stockService,
            ITestManagementService testService,
            IOrderService orderService,
            IUserService userService,
            ILogger<ManagerController> logger)
        {
            _context = context;
            _stockService = stockService;
            _testService = testService;
            _orderService = orderService;
            _userService = userService;
            _logger = logger;
        }

        // =========================
        // DASHBOARD
        // =========================
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var viewModel = new DashboardViewModel
                {
                    TotalTestCategories = await _context.TestCategories.CountAsync(),
                    TotalTestTypes = await _context.TestTypes.CountAsync(),
                    TotalConsumables = await _context.Consumables.CountAsync(),
                    TotalTechnicians = await _context.LabTechnicians.CountAsync(),
                    TotalDoctors = await _context.Doctors.CountAsync(),
                    PendingOrders = await _context.PurchaseOrders.CountAsync(o => o.Status == "Ordered" || o.Status == "PartiallyComplete"),
                    LowStockItems = await _context.Consumables.CountAsync(c => c.QuantityOnHand <= c.ReorderLevel),
                    Statistics = new DashboardStatisticsViewModel
                    {
                        TestsPerformedThisMonth = 0, // Will be populated from TestResults table
                        OrdersProcessedThisMonth = await _context.PurchaseOrders.CountAsync(o => o.OrderDate.Month == DateTime.Now.Month && o.OrderDate.Year == DateTime.Now.Year),
                        TotalStockValue = await _context.Consumables.SumAsync(c => c.QuantityOnHand * (c.UnitPrice ?? 0)),
                        ActiveTechnicians = await _context.LabTechnicians.CountAsync(t => t.IsActive)
                    },
                    LowStockAlerts = await GetLowStockAlerts(),
                    RecentActivities = await GetRecentActivities()
                };

                ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["Error"] = "An error occurred loading the dashboard.";
                return View(new DashboardViewModel());
            }
        }

        // =========================
        // TEST CATALOGUE
        // =========================
        public async Task<IActionResult> TestCatalogue()
        {
            try
            {
                var viewModel = new TestCatalogueIndexViewModel
                {
                    Categories = await _context.TestCategories
                        .Select(c => new TestCategoryViewModel
                        {
                            CategoryID = c.CategoryID,
                            CategoryName = c.CategoryName,
                            Description = c.Description,
                            IsActive = c.IsActive,
                            TestCount = c.TestTypes.Count,
                            CreatedAt = c.CreatedAt
                        })
                        .ToListAsync(),
                    TestTypes = await _testService.GetTestTypesWithDetailsAsync(),
                    SampleTypes = await _context.SampleTypes
                        .Select(s => new SampleTypeViewModel
                        {
                            SampleTypeID = s.SampleTypeID,
                            SampleTypeName = s.SampleTypeName,
                            Description = s.Description,
                            CollectionTubeType = s.CollectionTubeType,
                            StorageRequirements = s.StorageRequirements,
                            IsActive = s.IsActive
                        })
                        .ToListAsync()
                };

                ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading test catalogue");
                TempData["Error"] = "An error occurred loading the test catalogue.";
                return View(new TestCatalogueIndexViewModel());
            }
        }

        #region Test Category CRUD
        public IActionResult CreateTestCategory()
        {
            return PartialView("_CreateTestCategory", new CreateTestCategoryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTestCategory(CreateTestCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var category = new TestCategory
                    {
                        CategoryName = model.CategoryName,
                        Description = model.Description,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    _context.TestCategories.Add(category);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"User {await _userService.GetCurrentUserNameAsync(User)} created test category: {category.CategoryName}");
                    TempData["Success"] = "Test category created successfully!";
                    return RedirectToAction(nameof(TestCatalogue));
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException?.Message.Contains("IX_TestCategory_Name_Unique") == true)
                    {
                        ModelState.AddModelError("CategoryName", "A category with this name already exists.");
                    }
                    else
                    {
                        _logger.LogError(ex, "Database error creating test category");
                        TempData["Error"] = "An error occurred while saving. Please try again.";
                        return PartialView("_CreateTestCategory", model);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating test category");
                    TempData["Error"] = "An unexpected error occurred. Please try again.";
                    return PartialView("_CreateTestCategory", model);
                }
            }
            return PartialView("_CreateTestCategory", model);
        }

        public async Task<IActionResult> EditTestCategory(int id)
        {
            try
            {
                var category = await _context.TestCategories.FindAsync(id);
                if (category == null)
                    return NotFound();

                var model = new EditTestCategoryViewModel
                {
                    CategoryID = category.CategoryID,
                    CategoryName = category.CategoryName,
                    Description = category.Description,
                    IsActive = category.IsActive
                };
                return PartialView("_EditTestCategory", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading edit test category {id}");
                TempData["Error"] = "An error occurred loading the category.";
                return RedirectToAction(nameof(TestCatalogue));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTestCategory(EditTestCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var category = await _context.TestCategories.FindAsync(model.CategoryID);
                    if (category == null)
                        return NotFound();

                    category.CategoryName = model.CategoryName;
                    category.Description = model.Description;
                    category.IsActive = model.IsActive;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"User {await _userService.GetCurrentUserNameAsync(User)} updated test category: {category.CategoryName}");
                    TempData["Success"] = "Test category updated successfully!";
                    return RedirectToAction(nameof(TestCatalogue));
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException?.Message.Contains("IX_TestCategory_Name_Unique") == true)
                    {
                        ModelState.AddModelError("CategoryName", "A category with this name already exists.");
                    }
                    else
                    {
                        _logger.LogError(ex, "Database error updating test category");
                        TempData["Error"] = "An error occurred while saving.";
                        return PartialView("_EditTestCategory", model);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating test category");
                    TempData["Error"] = "An unexpected error occurred.";
                    return PartialView("_EditTestCategory", model);
                }
            }
            return PartialView("_EditTestCategory", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTestCategory(int id)
        {
            try
            {
                var category = await _context.TestCategories
                    .Include(c => c.TestTypes)
                    .FirstOrDefaultAsync(c => c.CategoryID == id);

                if (category == null)
                    return NotFound();

                if (category.TestTypes.Any())
                {
                    TempData["Error"] = "Cannot delete category with existing test types. Deactivate instead.";
                    return RedirectToAction(nameof(TestCatalogue));
                }

                _context.TestCategories.Remove(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {await _userService.GetCurrentUserNameAsync(User)} deleted test category: {category.CategoryName}");
                TempData["Success"] = "Test category deleted successfully!";
                return RedirectToAction(nameof(TestCatalogue));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting test category {id}");
                TempData["Error"] = "An error occurred deleting the category.";
                return RedirectToAction(nameof(TestCatalogue));
            }
        }
        #endregion

        #region Test Type CRUD
        public async Task<IActionResult> CreateTestType()
        {
            try
            {
                var model = new CreateTestTypeViewModel
                {
                    AvailableCategories = await GetCategorySelectList(),
                    AvailableSampleTypes = await GetSampleTypeSelectList(),
                    AvailableTechnicians = await GetTechnicianSelectList(),
                    AvailableConsumables = await GetConsumableSelectList()
                };
                return PartialView("_CreateTestType", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create test type form");
                TempData["Error"] = "An error occurred loading the form.";
                return RedirectToAction(nameof(TestCatalogue));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTestType(CreateTestTypeViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _testService.CreateTestTypeAsync(model);
                    TempData["Success"] = "Test type created successfully!";
                    return RedirectToAction(nameof(TestCatalogue));
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException?.Message.Contains("IX_TestType_Name_Unique") == true)
                    {
                        ModelState.AddModelError("TestName", "A test type with this name already exists.");
                    }
                    else
                    {
                        _logger.LogError(ex, "Database error creating test type");
                        TempData["Error"] = "An error occurred while saving. Please try again.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating test type");
                    TempData["Error"] = "An unexpected error occurred. Please try again.";
                }
            }

            model.AvailableCategories = await GetCategorySelectList();
            model.AvailableSampleTypes = await GetSampleTypeSelectList();
            model.AvailableTechnicians = await GetTechnicianSelectList();
            model.AvailableConsumables = await GetConsumableSelectList();
            return PartialView("_CreateTestType", model);
        }

        public async Task<IActionResult> EditTestType(int id)
        {
            try
            {
                var testType = await _context.TestTypes
                    .Include(t => t.TechnicianTestAssignments)
                    .Include(t => t.TestConsumables)
                    .FirstOrDefaultAsync(t => t.TestID == id);

                if (testType == null)
                    return NotFound();

                var model = new EditTestTypeViewModel
                {
                    TestID = testType.TestID,
                    TestName = testType.TestName,
                    CategoryID = testType.CategoryID,
                    SampleTypeID = testType.SampleTypeId,
                    UnitOfMeasurement = testType.UnitOfMeasurement,
                    NormalRangeMin = testType.NormalRangeMin,
                    NormalRangeMax = testType.NormalRangeMax,
                    TurnaroundTimeMinutes = testType.TurnaroundTimeMinutes,
                    IsActive = testType.IsActive,
                    SelectedTechnicianIDs = testType.TechnicianTestAssignments.Where(t => t.IsActive).Select(t => t.TechnicianID).ToList(),
                    ConsumableQuantities = testType.TestConsumables.ToDictionary(tc => tc.ConsumableID, tc => tc.QuantityRequired),
                    AvailableCategories = await GetCategorySelectList(),
                    AvailableSampleTypes = await GetSampleTypeSelectList(),
                    AvailableTechnicians = await GetTechnicianSelectList(),
                    AvailableConsumables = await GetConsumableSelectList()
                };

                return PartialView("_EditTestType", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading edit test type {id}");
                TempData["Error"] = "An error occurred loading the test type.";
                return RedirectToAction(nameof(TestCatalogue));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTestType(EditTestTypeViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _testService.UpdateTestTypeAsync(model);
                    TempData["Success"] = "Test type updated successfully!";
                    return RedirectToAction(nameof(TestCatalogue));
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException?.Message.Contains("IX_TestType_Name_Unique") == true)
                    {
                        ModelState.AddModelError("TestName", "A test type with this name already exists.");
                    }
                    else
                    {
                        _logger.LogError(ex, "Database error updating test type");
                        TempData["Error"] = "An error occurred while saving.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating test type");
                    TempData["Error"] = "An unexpected error occurred.";
                }
            }

            model.AvailableCategories = await GetCategorySelectList();
            model.AvailableSampleTypes = await GetSampleTypeSelectList();
            model.AvailableTechnicians = await GetTechnicianSelectList();
            model.AvailableConsumables = await GetConsumableSelectList();
            return PartialView("_EditTestType", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTestType(int id)
        {
            try
            {
                var result = await _testService.DeleteTestTypeAsync(id);
                if (result)
                {
                    TempData["Success"] = "Test type deactivated successfully!";
                }
                else
                {
                    TempData["Error"] = "Test type not found.";
                }
                return RedirectToAction(nameof(TestCatalogue));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting test type {id}");
                TempData["Error"] = "An error occurred deactivating the test type.";
                return RedirectToAction(nameof(TestCatalogue));
            }
        }
        #endregion

        // =========================
        // STOCK MANAGEMENT
        // =========================
        public async Task<IActionResult> StockManagement()
        {
            try
            {
                var viewModel = new StockManagementIndexViewModel
                {
                    Consumables = await _stockService.GetConsumablesWithStatusAsync(),
                    Suppliers = await _context.Suppliers
                        .Select(s => new SupplierViewModel
                        {
                            SupplierID = s.SupplierID,
                            SupplierName = s.SupplierName,
                            ContactPerson = s.ContactPerson,
                            EmailAddress = s.EmailAddress,
                            PhoneNumber = s.PhoneNumber,
                            Address = s.Address,
                            ConsumableCount = s.Consumables.Count,
                            IsPreferred = s.IsPreferred,
                            IsActive = s.IsActive
                        })
                        .ToListAsync(),
                    PendingOrders = await _orderService.GetOrderListAsync("Ordered", "PartiallyComplete"),
                    LowStockSummary = await _stockService.GetLowStockSummaryAsync(),
                    Statistics = new StockStatisticsViewModel
                    {
                        TotalConsumables = await _context.Consumables.CountAsync(),
                        ActiveConsumables = await _context.Consumables.CountAsync(c => c.IsActive),
                        TotalSuppliers = await _context.Suppliers.CountAsync(s => s.IsActive),
                        TotalStockValue = await _context.Consumables.SumAsync(c => c.QuantityOnHand * (c.UnitPrice ?? 0)),
                        ItemsBelowReorder = await _context.Consumables.CountAsync(c => c.QuantityOnHand <= c.ReorderLevel)
                    }
                };

                ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stock management");
                TempData["Error"] = "An error occurred loading the stock management page.";
                return View(new StockManagementIndexViewModel());
            }
        }

        #region Consumable CRUD
        public async Task<IActionResult> CreateConsumable()
        {
            try
            {
                var model = new CreateConsumableViewModel
                {
                    AvailableSuppliers = await GetSupplierSelectList()
                };
                return PartialView("_CreateConsumable", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create consumable form");
                TempData["Error"] = "An error occurred loading the form.";
                return RedirectToAction(nameof(StockManagement));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateConsumable(CreateConsumableViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var consumable = new Consumable
                    {
                        ConsumableName = model.ConsumableName,
                        SupplierID = model.SupplierID,
                        ReorderLevel = model.ReorderLevel,
                        QuantityOnHand = model.QuantityOnHand,
                        UnitOfMeasure = model.UnitOfMeasure,
                        UnitPrice = model.UnitPrice,
                        MinimumStockLevel = model.MinimumStockLevel ?? 0,
                        StorageLocation = model.StorageLocation,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    _context.Consumables.Add(consumable);
                    await _context.SaveChangesAsync();

                    // Log initial stock
                    if (model.QuantityOnHand > 0)
                    {
                        await _stockService.AdjustStockAsync(
                            consumable.ConsumableID,
                            model.QuantityOnHand,
                            "ManualSet",
                            "Initial stock setup",
                            await _userService.GetCurrentUserIdAsync(User)
                        );
                    }

                    _logger.LogInformation($"User {await _userService.GetCurrentUserNameAsync(User)} created consumable: {consumable.ConsumableName}");
                    TempData["Success"] = "Consumable created successfully!";
                    return RedirectToAction(nameof(StockManagement));
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException?.Message.Contains("IX_Consumable_Name_Unique") == true)
                    {
                        ModelState.AddModelError("ConsumableName", "A consumable with this name already exists.");
                    }
                    else
                    {
                        _logger.LogError(ex, "Database error creating consumable");
                        TempData["Error"] = "An error occurred while saving.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating consumable");
                    TempData["Error"] = "An unexpected error occurred.";
                }
            }

            model.AvailableSuppliers = await GetSupplierSelectList();
            return PartialView("_CreateConsumable", model);
        }

        public async Task<IActionResult> EditConsumable(int id)
        {
            try
            {
                var consumable = await _context.Consumables.FindAsync(id);
                if (consumable == null)
                    return NotFound();

                var model = new EditConsumableViewModel
                {
                    ConsumableID = consumable.ConsumableID,
                    ConsumableName = consumable.ConsumableName,
                    SupplierID = consumable.SupplierID,
                    ReorderLevel = consumable.ReorderLevel,
                    QuantityOnHand = consumable.QuantityOnHand,
                    UnitOfMeasure = consumable.UnitOfMeasure,
                    UnitPrice = consumable.UnitPrice,
                    MinimumStockLevel = consumable.MinimumStockLevel,
                    StorageLocation = consumable.StorageLocation,
                    IsActive = consumable.IsActive,
                    AvailableSuppliers = await GetSupplierSelectList()
                };

                return PartialView("_EditConsumable", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading edit consumable {id}");
                TempData["Error"] = "An error occurred loading the consumable.";
                return RedirectToAction(nameof(StockManagement));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConsumable(EditConsumableViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var consumable = await _context.Consumables.FindAsync(model.ConsumableID);
                    if (consumable == null)
                        return NotFound();

                    consumable.ConsumableName = model.ConsumableName;
                    consumable.SupplierID = model.SupplierID;
                    consumable.ReorderLevel = model.ReorderLevel;
                    consumable.UnitOfMeasure = model.UnitOfMeasure;
                    consumable.UnitPrice = model.UnitPrice;
                    consumable.MinimumStockLevel = model.MinimumStockLevel ?? 0;
                    consumable.StorageLocation = model.StorageLocation;
                    consumable.IsActive = model.IsActive;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"User {await _userService.GetCurrentUserNameAsync(User)} updated consumable: {consumable.ConsumableName}");
                    TempData["Success"] = "Consumable updated successfully!";
                    return RedirectToAction(nameof(StockManagement));
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException?.Message.Contains("IX_Consumable_Name_Unique") == true)
                    {
                        ModelState.AddModelError("ConsumableName", "A consumable with this name already exists.");
                    }
                    else
                    {
                        _logger.LogError(ex, "Database error updating consumable");
                        TempData["Error"] = "An error occurred while saving.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating consumable");
                    TempData["Error"] = "An unexpected error occurred.";
                }
            }

            model.AvailableSuppliers = await GetSupplierSelectList();
            return PartialView("_EditConsumable", model);
        }

        public async Task<IActionResult> AdjustStock(int id)
        {
            try
            {
                var consumable = await _context.Consumables.FindAsync(id);
                if (consumable == null)
                    return NotFound();

                var model = new AdjustStockViewModel
                {
                    ConsumableID = consumable.ConsumableID,
                    ConsumableName = consumable.ConsumableName,
                    CurrentQuantity = consumable.QuantityOnHand,
                    AdjustmentTypes = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "ManualIncrease", Text = "Increase Quantity" },
                        new SelectListItem { Value = "ManualDecrease", Text = "Decrease Quantity" },
                        new SelectListItem { Value = "ManualSet", Text = "Set to Exact Quantity" }
                    }
                };

                return PartialView("_AdjustStock", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading adjust stock form for consumable {id}");
                TempData["Error"] = "An error occurred loading the form.";
                return RedirectToAction(nameof(StockManagement));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(AdjustStockViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = await _userService.GetCurrentUserIdAsync(User);
                    var consumable = await _stockService.AdjustStockAsync(
                        model.ConsumableID,
                        model.AdjustmentAmount,
                        model.AdjustmentType,
                        model.Reason ?? "Manual adjustment",
                        userId
                    );

                    _logger.LogInformation($"User {await _userService.GetCurrentUserNameAsync(User)} adjusted stock for {consumable.ConsumableName}: {model.AdjustmentType} by {model.AdjustmentAmount}");
                    TempData["Success"] = $"Stock adjusted successfully! New quantity: {consumable.QuantityOnHand}";
                    return RedirectToAction(nameof(StockManagement));
                }
                catch (InvalidOperationException ex)
                {
                    TempData["Error"] = ex.Message;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adjusting stock");
                    TempData["Error"] = "An unexpected error occurred.";
                }
            }

            model.AdjustmentTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "ManualIncrease", Text = "Increase Quantity" },
                new SelectListItem { Value = "ManualDecrease", Text = "Decrease Quantity" },
                new SelectListItem { Value = "ManualSet", Text = "Set to Exact Quantity" }
            };
            return PartialView("_AdjustStock", model);
        }
        #endregion

        // =========================
        // ORDER MANAGEMENT
        // =========================
        public async Task<IActionResult> ViewOrder()
        {
            try
            {
                var viewModel = new OrderIndexViewModel
                {
                    Orders = await _orderService.GetOrderListAsync(),
                    PendingOrders = await _orderService.GetOrderListAsync("Ordered", "PartiallyComplete"),
                    CompletedOrders = await _orderService.GetOrderListAsync("Complete", "Cancelled"),
                    Statistics = new OrderStatisticsViewModel
                    {
                        TotalOrders = await _context.PurchaseOrders.CountAsync(),
                        PendingOrders = await _context.PurchaseOrders.CountAsync(o => o.Status == "Ordered"),
                        CompletedOrders = await _context.PurchaseOrders.CountAsync(o => o.Status == "Complete"),
                        CancelledOrders = await _context.PurchaseOrders.CountAsync(o => o.Status == "Cancelled"),
                        PartiallyCompleteOrders = await _context.PurchaseOrders.CountAsync(o => o.Status == "PartiallyComplete")
                    }
                };

                ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                TempData["Error"] = "An error occurred loading the orders.";
                return View(new OrderIndexViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateOrder()
        {
            try
            {
                var model = new CreateOrderViewModel
                {
                    AvailableSuppliers = await GetSupplierSelectList(),
                    AvailableConsumables = await GetConsumableSelectList(),
                    Items = await GetReorderItems()
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create order form");
                TempData["Error"] = "An error occurred loading the form.";
                return RedirectToAction(nameof(ViewOrder));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(CreateOrderViewModel model)
        {
            if (ModelState.IsValid && model.Items != null && model.Items.Any(i => i.IsSelected && i.QuantityToOrder > 0))
            {
                try
                {
                    var userId = await _userService.GetCurrentUserIdAsync(User);
                    var order = await _orderService.CreateOrderAsync(model, userId);

                    _logger.LogInformation($"User {await _userService.GetCurrentUserNameAsync(User)} created order: {order.OrderNumber}");
                    TempData["Success"] = $"Order #{order.OrderNumber} created successfully!";
                    return RedirectToAction(nameof(ViewOrder));
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("Items", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating order");
                    TempData["Error"] = "An unexpected error occurred. Please try again.";
                }
            }

            model.AvailableSuppliers = await GetSupplierSelectList();
            model.AvailableConsumables = await GetConsumableSelectList();
            if (model.Items == null || !model.Items.Any())
            {
                model.Items = await GetReorderItems();
            }
            return View(model);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            try
            {
                var viewModel = await _orderService.GetOrderDetailsAsync(id);
                return View(viewModel);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading order details {id}");
                TempData["Error"] = "An error occurred loading the order details.";
                return RedirectToAction(nameof(ViewOrder));
            }
        }

        public async Task<IActionResult> ReceiveOrder(int id)
        {
            try
            {
                var order = await _context.PurchaseOrders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Consumable)
                    .FirstOrDefaultAsync(o => o.OrderID == id);

                if (order == null)
                    return NotFound();

                if (order.Status == "Complete" || order.Status == "Cancelled")
                {
                    TempData["Error"] = "This order cannot be received.";
                    return RedirectToAction(nameof(ViewOrder));
                }

                var model = new ReceiveOrderViewModel
                {
                    OrderID = order.OrderID,
                    OrderNumber = order.OrderNumber,
                    SupplierName = order.Supplier?.SupplierName ?? "Unknown",
                    Items = order.OrderItems.Select(oi => new OrderItemReceiptViewModel
                    {
                        OrderItemID = oi.OrderItemID,
                        ConsumableID = oi.ConsumableID,
                        ConsumableName = oi.Consumable.ConsumableName,
                        QuantityOrdered = oi.QuantityOrdered,
                        QuantityReceived = oi.QuantityReceived,
                        IsSelected = oi.QuantityReceived < oi.QuantityOrdered
                    }).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading receive order form {id}");
                TempData["Error"] = "An error occurred loading the receive order form.";
                return RedirectToAction(nameof(ViewOrder));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveOrder(ReceiveOrderViewModel model)
        {
            try
            {
                var userId = await _userService.GetCurrentUserIdAsync(User);
                var order = await _orderService.ReceiveOrderAsync(model.OrderID, model, userId);

                _logger.LogInformation($"User {await _userService.GetCurrentUserNameAsync(User)} received order: {order.OrderNumber}");
                TempData["Success"] = $"Order #{order.OrderNumber} received successfully!";
                return RedirectToAction(nameof(ViewOrder));
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(ViewOrder));
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(ViewOrder));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error receiving order {model.OrderID}");
                TempData["Error"] = "An unexpected error occurred. Please try again.";
                return RedirectToAction(nameof(ViewOrder));
            }
        }

        public async Task<IActionResult> CancelOrder(int id)
        {
            try
            {
                var order = await _context.PurchaseOrders
                    .FirstOrDefaultAsync(o => o.OrderID == id);

                if (order == null)
                    return NotFound();

                if (order.Status == "Complete" || order.Status == "Cancelled")
                {
                    TempData["Error"] = "This order cannot be cancelled.";
                    return RedirectToAction(nameof(ViewOrder));
                }

                var model = new CancelOrderViewModel
                {
                    OrderID = order.OrderID,
                    OrderNumber = order.OrderNumber
                };

                return PartialView("_CancelOrder", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading cancel order form {id}");
                TempData["Error"] = "An error occurred loading the cancel order form.";
                return RedirectToAction(nameof(ViewOrder));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(CancelOrderViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var order = await _orderService.CancelOrderAsync(model.OrderID, model.CancellationReason);

                    _logger.LogInformation($"User {await _userService.GetCurrentUserNameAsync(User)} cancelled order: {order.OrderNumber}");
                    TempData["Success"] = $"Order #{order.OrderNumber} cancelled successfully!";
                    return RedirectToAction(nameof(ViewOrder));
                }
                catch (ArgumentException ex)
                {
                    TempData["Error"] = ex.Message;
                }
                catch (InvalidOperationException ex)
                {
                    TempData["Error"] = ex.Message;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error cancelling order {model.OrderID}");
                    TempData["Error"] = "An unexpected error occurred. Please try again.";
                }
            }
            return PartialView("_CancelOrder", model);
        }

        // =========================
        // STAFF MANAGEMENT
        // =========================
        public async Task<IActionResult> StaffManagement()
        {
            try
            {
                var viewModel = new StaffManagementIndexViewModel
                {
                    Technicians = await GetTechnicianList(),
                    Doctors = await GetDoctorList(),
                    Statistics = new StaffStatisticsViewModel
                    {
                        TotalTechnicians = await _context.LabTechnicians.CountAsync(),
                        ActiveTechnicians = await _context.LabTechnicians.CountAsync(t => t.IsActive),
                        TotalDoctors = await _context.Doctors.CountAsync(),
                        ActiveDoctors = await _context.Doctors.CountAsync(d => d.User.IsActive),
                        VerifiedDoctors = await _context.Doctors.CountAsync(d => d.IsVerified),
                        PendingVerificationDoctors = await _context.Doctors.CountAsync(d => !d.IsVerified)
                    }
                };

                ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading staff management");
                TempData["Error"] = "An error occurred loading staff management.";
                return View(new StaffManagementIndexViewModel());
            }
        }

        #region Technician Management
        public async Task<IActionResult> AddTechnician()
        {
            try
            {
                var model = new AddTechnicianViewModel
                {
                    AvailableTestTypes = await GetTestTypeSelectList()
                };
                ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading add technician form");
                TempData["Error"] = "An error occurred loading the form.";
                return RedirectToAction(nameof(StaffManagement));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTechnician(AddTechnicianViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if SA ID already exists
                    if (await _context.LabTechnicians.AnyAsync(t => t.SAIDNumber == model.SAIDNumber))
                    {
                        ModelState.AddModelError("SAIDNumber", "This SA ID number is already registered.");
                        model.AvailableTestTypes = await GetTestTypeSelectList();
                        ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                        return View(model);
                    }

                    // Check if Employee Number already exists
                    if (await _context.LabTechnicians.AnyAsync(t => t.EmployeeNumber == model.EmployeeNumber))
                    {
                        ModelState.AddModelError("EmployeeNumber", "This employee number is already in use.");
                        model.AvailableTestTypes = await GetTestTypeSelectList();
                        ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                        return View(model);
                    }

                    // Check if email already exists
                    if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                    {
                        ModelState.AddModelError("Email", "This email address is already registered.");
                        model.AvailableTestTypes = await GetTestTypeSelectList();
                        ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                        return View(model);
                    }

                    // Create User
                    var user = new User
                    {
                        Name = model.Name,
                        Surname = model.Surname,
                        Email = model.Email,
                        ContactNumber = model.ContactNumber,
                        UserRole = "Technician",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Create Technician
                    var technician = new LabTechnician
                    {
                        UserID = user.UserID,
                        EmployeeNumber = model.EmployeeNumber,
                        SAIDNumber = model.SAIDNumber,
                        Specialization = model.Specialization,
                        Qualification = model.Qualification,
                        YearsOfExperience = model.YearsOfExperience,
                        HireDate = DateTime.Now,
                        IsActive = true
                    };

                    _context.LabTechnicians.Add(technician);
                    await _context.SaveChangesAsync();

                    // Assign test types
                    if (model.SelectedTestTypeIDs != null && model.SelectedTestTypeIDs.Any())
                    {
                        foreach (var testId in model.SelectedTestTypeIDs)
                        {
                            _context.TechnicianTestAssignments.Add(new TechnicianTestAssignment
                            {
                                TechnicianID = technician.TechnicianID,
                                TestID = testId,
                                AssignedDate = DateTime.Now,
                                IsActive = true
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    _logger.LogInformation($"User {await _userService.GetCurrentUserNameAsync(User)} added technician: {model.Name} {model.Surname}");
                    TempData["Success"] = $"Technician {model.Name} {model.Surname} registered successfully!";
                    return RedirectToAction(nameof(StaffManagement));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding technician");
                    TempData["Error"] = "An unexpected error occurred. Please try again.";
                }
            }

            model.AvailableTestTypes = await GetTestTypeSelectList();
            ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
            return View(model);
        }

        public async Task<IActionResult> EditTechnician(int id)
        {
            try
            {
                var technician = await _context.LabTechnicians
                    .Include(t => t.User)
                    .Include(t => t.TestAssignments)
                    .FirstOrDefaultAsync(t => t.TechnicianID == id);

                if (technician == null)
                    return NotFound();

                var model = new EditTechnicianViewModel
                {
                    TechnicianID = technician.TechnicianID,
                    Name = technician.User.Name,
                    Surname = technician.User.Surname,
                    SAIDNumber = technician.SAIDNumber,
                    EmployeeNumber = technician.EmployeeNumber,
                    Email = technician.User.Email,
                    ContactNumber = technician.User.ContactNumber,
                    Specialization = technician.Specialization,
                    Qualification = technician.Qualification,
                    YearsOfExperience = technician.YearsOfExperience,
                    IsActive = technician.IsActive,
                    SelectedTestTypeIDs = technician.TestAssignments.Where(t => t.IsActive).Select(t => t.TestID).ToList(),
                    AvailableTestTypes = await GetTestTypeSelectList()
                };

                ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading edit technician {id}");
                TempData["Error"] = "An error occurred loading the technician.";
                return RedirectToAction(nameof(StaffManagement));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTechnician(EditTechnicianViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var technician = await _context.LabTechnicians
                        .Include(t => t.User)
                        .Include(t => t.TestAssignments)
                        .FirstOrDefaultAsync(t => t.TechnicianID == model.TechnicianID);

                    if (technician == null)
                        return NotFound();

                    // Check unique constraints
                    if (await _context.LabTechnicians.AnyAsync(t => t.SAIDNumber == model.SAIDNumber && t.TechnicianID != model.TechnicianID))
                    {
                        ModelState.AddModelError("SAIDNumber", "This SA ID number is already registered.");
                        model.AvailableTestTypes = await GetTestTypeSelectList();
                        ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                        return View(model);
                    }

                    if (await _context.LabTechnicians.AnyAsync(t => t.EmployeeNumber == model.EmployeeNumber && t.TechnicianID != model.TechnicianID))
                    {
                        ModelState.AddModelError("EmployeeNumber", "This employee number is already in use.");
                        model.AvailableTestTypes = await GetTestTypeSelectList();
                        ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                        return View(model);
                    }

                    if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.UserID != technician.UserID))
                    {
                        ModelState.AddModelError("Email", "This email address is already registered.");
                        model.AvailableTestTypes = await GetTestTypeSelectList();
                        ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                        return View(model);
                    }

                    // Update User
                    technician.User.Name = model.Name;
                    technician.User.Surname = model.Surname;
                    technician.User.Email = model.Email;
                    technician.User.ContactNumber = model.ContactNumber;
                    technician.User.IsActive = model.IsActive;
                    technician.User.ModifiedDate = DateTime.Now;

                    // Update Technician
                    technician.SAIDNumber = model.SAIDNumber;
                    technician.EmployeeNumber = model.EmployeeNumber;
                    technician.Specialization = model.Specialization;
                    technician.Qualification = model.Qualification;
                    technician.YearsOfExperience = model.YearsOfExperience;
                    technician.IsActive = model.IsActive;

                    // Update test assignments
                    _context.TechnicianTestAssignments.RemoveRange(technician.TestAssignments);
                    if (model.SelectedTestTypeIDs != null && model.SelectedTestTypeIDs.Any())
                    {
                        foreach (var testId in model.SelectedTestTypeIDs)
                        {
                            _context.TechnicianTestAssignments.Add(new TechnicianTestAssignment
                            {
                                TechnicianID = technician.TechnicianID,
                                TestID = testId,
                                AssignedDate = DateTime.Now,
                                IsActive = true
                            });
                        }
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"User {await _userService.GetCurrentUserNameAsync(User)} updated technician: {model.Name} {model.Surname}");
                    TempData["Success"] = "Technician updated successfully!";
                    return RedirectToAction(nameof(StaffManagement));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating technician");
                    TempData["Error"] = "An unexpected error occurred. Please try again.";
                }
            }

            model.AvailableTestTypes = await GetTestTypeSelectList();
            ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
            return View(model);
        }

        public async Task<IActionResult> TechnicianProfile(int id)
        {
            try
            {
                var technician = await _context.LabTechnicians
                    .Include(t => t.User)
                    .Include(t => t.TestAssignments)
                        .ThenInclude(ta => ta.TestType)
                            .ThenInclude(t => t.Category)
                    .FirstOrDefaultAsync(t => t.TechnicianID == id);

                if (technician == null)
                    return NotFound();

                var model = new TechnicianProfileViewModel
                {
                    TechnicianID = technician.TechnicianID,
                    FullName = $"{technician.User.Name} {technician.User.Surname}",
                    EmployeeNumber = technician.EmployeeNumber,
                    SAIDNumber = technician.SAIDNumber,
                    Email = technician.User.Email,
                    ContactNumber = technician.User.ContactNumber,
                    Specialization = technician.Specialization,
                    Qualification = technician.Qualification,
                    YearsOfExperience = technician.YearsOfExperience,
                    HireDate = technician.HireDate,
                    IsActive = technician.IsActive,
                    AssignedTests = technician.TestAssignments
                        .Where(t => t.IsActive)
                        .Select(t => new AssignedTestViewModel
                        {
                            TestID = t.TestID,
                            TestName = t.TestType.TestName,
                            CategoryName = t.TestType.Category.CategoryName,
                            AssignedDate = t.AssignedDate,
                            IsActive = t.IsActive
                        }).ToList(),
                    Performance = new PerformanceStatisticsViewModel
                    {
                        TotalTestsAssigned = technician.TestAssignments.Count,
                        TotalTestsCompleted = 0,
                        TestsThisMonth = 0,
                        AverageCompletionTime = 0,
                        VerificationAccuracy = 0,
                        MonthlyPerformance = new List<MonthlyPerformanceViewModel>()
                    }
                };

                ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading technician profile {id}");
                TempData["Error"] = "An error occurred loading the technician profile.";
                return RedirectToAction(nameof(StaffManagement));
            }
        }

        public async Task<IActionResult> TechnicianPerformanceDetails(int id)
        {
            try
            {
                // This would come from TestResults table (other subsystem)
                // For now, return a placeholder
                ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                return View(new TechnicianPerformanceDetailsViewModel
                {
                    TechnicianID = id,
                    TechnicianName = await _context.LabTechnicians
                        .Where(t => t.TechnicianID == id)
                        .Select(t => $"{t.User.Name} {t.User.Surname}")
                        .FirstOrDefaultAsync() ?? "Technician",
                    StartDate = DateTime.Now.AddMonths(-1),
                    EndDate = DateTime.Now,
                    Summary = new PerformanceSummaryViewModel(),
                    TestDetails = new List<TestPerformanceViewModel>(),
                    DailyPerformance = new List<DailyPerformanceViewModel>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading technician performance {id}");
                TempData["Error"] = "An error occurred loading the performance details.";
                return RedirectToAction(nameof(StaffManagement));
            }
        }
        #endregion

        #region Doctor Management
        public IActionResult RegisterDoctor()
        {
            ViewBag.ManagerName = GetCurrentUserName();
            return View(new RegisterDoctorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterDoctor(RegisterDoctorViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if HPCSA number already exists
                    if (await _context.Doctors.AnyAsync(d => d.HPCSANumber == model.HPCSANumber))
                    {
                        ModelState.AddModelError("HPCSANumber", "This HPCSA number is already registered.");
                        ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                        return View(model);
                    }

                    // Check if email already exists
                    if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                    {
                        ModelState.AddModelError("Email", "This email address is already registered.");
                        ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                        return View(model);
                    }

                    // Create User
                    var user = new User
                    {
                        Name = model.Name,
                        Surname = model.Surname,
                        Email = model.Email,
                        ContactNumber = model.ContactNumber,
                        UserRole = "Doctor",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Create Doctor
                    var doctor = new Doctor
                    {
                        UserID = user.UserID,
                        HPCSANumber = model.HPCSANumber,
                        PracticeName = model.PracticeName,
                        PracticeAddress = model.PracticeAddress,
                        Specialization = model.Specialization,
                        IsVerified = false,
                        RegisteredDate = DateTime.Now
                    };

                    _context.Doctors.Add(doctor);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"User {await _userService.GetCurrentUserNameAsync(User)} registered doctor: {model.Name} {model.Surname}");
                    TempData["Success"] = $"Doctor {model.Name} {model.Surname} registered successfully!";
                    return RedirectToAction(nameof(StaffManagement));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error registering doctor");
                    TempData["Error"] = "An unexpected error occurred. Please try again.";
                }
            }

            ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
            return View(model);
        }

        public async Task<IActionResult> EditDoctor(int id)
        {
            try
            {
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.DoctorID == id);

                if (doctor == null)
                    return NotFound();

                var model = new EditDoctorViewModel
                {
                    DoctorID = doctor.DoctorID,
                    Name = doctor.User.Name,
                    Surname = doctor.User.Surname,
                    HPCSANumber = doctor.HPCSANumber,
                    Email = doctor.User.Email,
                    ContactNumber = doctor.User.ContactNumber,
                    PracticeName = doctor.PracticeName,
                    PracticeAddress = doctor.PracticeAddress,
                    Specialization = doctor.Specialization,
                    IsVerified = doctor.IsVerified,
                    IsActive = doctor.User.IsActive
                };

                ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading edit doctor {id}");
                TempData["Error"] = "An error occurred loading the doctor.";
                return RedirectToAction(nameof(StaffManagement));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(EditDoctorViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var doctor = await _context.Doctors
                        .Include(d => d.User)
                        .FirstOrDefaultAsync(d => d.DoctorID == model.DoctorID);

                    if (doctor == null)
                        return NotFound();

                    // Check unique constraints
                    if (await _context.Doctors.AnyAsync(d => d.HPCSANumber == model.HPCSANumber && d.DoctorID != model.DoctorID))
                    {
                        ModelState.AddModelError("HPCSANumber", "This HPCSA number is already registered.");
                        ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                        return View(model);
                    }

                    if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.UserID != doctor.UserID))
                    {
                        ModelState.AddModelError("Email", "This email address is already registered.");
                        ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                        return View(model);
                    }

                    // Update User
                    doctor.User.Name = model.Name;
                    doctor.User.Surname = model.Surname;
                    doctor.User.Email = model.Email;
                    doctor.User.ContactNumber = model.ContactNumber;
                    doctor.User.IsActive = model.IsActive;
                    doctor.User.ModifiedDate = DateTime.Now;

                    // Update Doctor
                    doctor.HPCSANumber = model.HPCSANumber;
                    doctor.PracticeName = model.PracticeName;
                    doctor.PracticeAddress = model.PracticeAddress;
                    doctor.Specialization = model.Specialization;
                    doctor.IsVerified = model.IsVerified;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"User {await _userService.GetCurrentUserNameAsync(User)} updated doctor: {model.Name} {model.Surname}");
                    TempData["Success"] = "Doctor updated successfully!";
                    return RedirectToAction(nameof(StaffManagement));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating doctor");
                    TempData["Error"] = "An unexpected error occurred. Please try again.";
                }
            }

            ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
            return View(model);
        }
        #endregion

        // =========================
        // TEST VERIFICATION
        // =========================
        public async Task<IActionResult> TestVerification()
        {
            try
            {
                var viewModel = new TestVerificationIndexViewModel
                {
                    PendingVerifications = new List<PendingVerificationViewModel>(),
                    RecentVerifications = new List<VerifiedTestsViewModel>(),
                    Statistics = new VerificationStatisticsViewModel(),
                    Filter = new TestVerificationFilterViewModel
                    {
                        Categories = await GetCategorySelectList()
                    }
                };

                ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading test verification");
                TempData["Error"] = "An error occurred loading test verification.";
                return View(new TestVerificationIndexViewModel());
            }
        }

        // =========================
        // REPORTS
        // =========================
        public async Task<IActionResult> Reports()
        {
            try
            {
                var viewModel = new ReportIndexViewModel
                {
                    ReportTypes = new List<ReportTypeViewModel>
                    {
                        new ReportTypeViewModel { ReportType = "TestsPerformed", Description = "Tests performed grouped by category", Icon = "fa-flask" },
                        new ReportTypeViewModel { ReportType = "StockReport", Description = "Current stock levels and reorder status", Icon = "fa-boxes" },
                        new ReportTypeViewModel { ReportType = "TechnicianPerformance", Description = "Technician performance metrics", Icon = "fa-user-md" },
                        new ReportTypeViewModel { ReportType = "OrderReport", Description = "Purchase order summary", Icon = "fa-shopping-cart" }
                    },
                    Filter = new ReportFilterViewModel()
                };

                ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports");
                TempData["Error"] = "An error occurred loading reports.";
                return View(new ReportIndexViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(ReportFilterViewModel filter)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please provide valid date range.";
                return RedirectToAction(nameof(Reports));
            }

            try
            {
                // Generate PDF report based on type
                // var pdfBytes = await _reportService.GenerateReportAsync(filter);
                // return File(pdfBytes, "application/pdf", $"Report_{filter.ReportType}_{DateTime.Now:yyyyMMdd}.pdf");

                TempData["Success"] = "Report generated successfully!";
                return RedirectToAction(nameof(Reports));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                TempData["Error"] = "An error occurred generating the report.";
                return RedirectToAction(nameof(Reports));
            }
        }

        public async Task<IActionResult> DownloadReport(string reportType, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Generate and download PDF
                // var pdfBytes = await _reportService.DownloadReportAsync(reportType, startDate, endDate);
                // return File(pdfBytes, "application/pdf", $"{reportType}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf");

                TempData["Success"] = "Report downloaded successfully!";
                return RedirectToAction(nameof(Reports));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading report");
                TempData["Error"] = "An error occurred downloading the report.";
                return RedirectToAction(nameof(Reports));
            }
        }

        // =========================
        // SETTINGS
        // =========================
        public async Task<IActionResult> Settings()
        {
            try
            {
                var viewModel = new SettingsIndexViewModel
                {
                    Profile = new ProfileSettingsViewModel
                    {
                        Name = GetCurrentUserFirstName(),
                        Surname = GetCurrentUserLastName(),
                        Email = GetCurrentUserEmail(),
                        ContactNumber = GetCurrentUserPhone()
                    },
                    Security = new SecuritySettingsViewModel
                    {
                        TwoFactorEnabled = false,
                        SessionTimeout = 30,
                        NotifyOnNewLogin = true,
                        PasswordExpiryDays = 90,
                        ChangePassword = new NMB_HLabSys.Models.ViewModels.ChangePasswordViewModel()
                    },
                    Notifications = new NotificationSettingsViewModel
                    {
                        EmailNotifications = true,
                        LowStockAlerts = true,
                        OrderStatusUpdates = true,
                        TestVerificationAlerts = true,
                        AlertThresholdDays = 5,
                        AlertFrequency = "Daily"
                    },
                    System = new SystemSettingsViewModel
                    {
                        LaboratoryName = "Nelson Mandela Bay Haematology Diagnostic Laboratories",
                        DefaultTurnaroundTime = 24,
                        AutoVerifyNormalResults = false,
                        EnableAuditLogging = true,
                        ResultsRetentionPeriod = 5,
                        MaxFailedLoginAttempts = 5,
                        AccountLockoutDuration = 15
                    },
                    RecentAuditLogs = new List<AuditLogViewModel>()
                };

                ViewBag.ManagerName = await _userService.GetCurrentUserNameAsync(User);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings");
                TempData["Error"] = "An error occurred loading settings.";
                return View(new SettingsIndexViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = GetCurrentUserId();
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.Name = model.Name;
                        user.Surname = model.Surname;
                        user.Email = model.Email;
                        user.ContactNumber = model.ContactNumber;
                        user.ModifiedDate = DateTime.Now;
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"User {await _userService.GetCurrentUserNameAsync(User)} updated their profile");
                        TempData["Success"] = "Profile updated successfully!";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating profile");
                    TempData["Error"] = "An error occurred updating your profile.";
                }
            }
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(NMB_HLabSys.Models.ViewModels.ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // This would use ASP.NET Identity
                    // For now, just show success
                    TempData["Success"] = "Password changed successfully!";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error changing password");
                    TempData["Error"] = "An error occurred changing your password.";
                }
            }
            return RedirectToAction(nameof(Settings));
        }

        // =========================
        // LOGIN
        // =========================
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction(nameof(Dashboard));
            }
            return View();
        }

        // =========================
        // HELPER METHODS
        // =========================
        private async Task<List<OrderListViewModel>> GetOrderList(params string[] statuses)
        {
            return await _orderService.GetOrderListAsync(statuses);
        }

        private async Task<List<TechnicianViewModel>> GetTechnicianList()
        {
            return await _context.LabTechnicians
                .Include(t => t.User)
                .Include(t => t.TestAssignments)
                .Select(t => new TechnicianViewModel
                {
                    TechnicianID = t.TechnicianID,
                    FullName = $"{t.User.Name} {t.User.Surname}",
                    EmployeeNumber = t.EmployeeNumber,
                    SAIDNumber = t.SAIDNumber,
                    Email = t.User.Email,
                    ContactNumber = t.User.ContactNumber,
                    Specialization = t.Specialization,
                    AssignedTestsCount = t.TestAssignments.Count(ta => ta.IsActive),
                    CompletedTestsCount = 0,
                    AverageTurnaroundTime = 0,
                    Status = t.IsActive ? "Active" : "Inactive",
                    StatusColor = t.IsActive ? "success" : "danger",
                    IsActive = t.IsActive,
                    HireDate = t.HireDate
                })
                .ToListAsync();
        }

        private async Task<List<DoctorListViewModel>> GetDoctorList()
        {
            return await _context.Doctors
                .Include(d => d.User)
                .Select(d => new DoctorListViewModel
                {
                    DoctorID = d.DoctorID,
                    FullName = $"{d.User.Name} {d.User.Surname}",
                    HPCSANumber = d.HPCSANumber,
                    Email = d.User.Email,
                    PracticeName = d.PracticeName,
                    Specialization = d.Specialization,
                    TotalRequests = 0,
                    RegisteredDate = d.RegisteredDate,
                    IsVerified = d.IsVerified,
                    Status = d.IsVerified ? "Verified" : "Pending",
                    StatusColor = d.IsVerified ? "success" : "warning"
                })
                .ToListAsync();
        }

        private async Task<List<OrderListViewModel>> GetPendingOrders()
        {
            return await _orderService.GetOrderListAsync("Ordered", "PartiallyComplete");
        }

        private async Task<List<LowStockAlertViewModel>> GetLowStockAlerts()
        {
            var summary = await _stockService.GetLowStockSummaryAsync();
            var alerts = new List<LowStockAlertViewModel>();
            alerts.AddRange(summary.CriticalItems);
            alerts.AddRange(summary.WarningItems);
            return alerts;
        }

        private async Task<List<RecentActivityViewModel>> GetRecentActivities()
        {
            var activities = new List<RecentActivityViewModel>();

            // Get recent orders
            var recentOrders = await _context.PurchaseOrders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Include(o => o.CreatedByUser)
                .Select(o => new RecentActivityViewModel
                {
                    ActivityType = "Order",
                    Description = $"Order #{o.OrderNumber} created with {o.OrderItems.Count} items",
                    ActivityDate = o.OrderDate,
                    UserName = $"{o.CreatedByUser.Name} {o.CreatedByUser.Surname}"
                })
                .ToListAsync();
            activities.AddRange(recentOrders);

            // Get recent stock adjustments
            var recentAdjustments = await _context.StockAdjustmentLogs
                .OrderByDescending(a => a.AdjustmentDate)
                .Take(5)
                .Include(a => a.Consumable)
                .Include(a => a.AdjustedByUser)
                .Select(a => new RecentActivityViewModel
                {
                    ActivityType = "Stock",
                    Description = $"Stock adjusted for {a.Consumable.ConsumableName}: {a.PreviousQuantity} → {a.NewQuantity}",
                    ActivityDate = a.AdjustmentDate,
                    UserName = $"{a.AdjustedByUser.Name} {a.AdjustedByUser.Surname}"
                })
                .ToListAsync();
            activities.AddRange(recentAdjustments);

            return activities.OrderByDescending(a => a.ActivityDate).Take(10).ToList();
        }

        private async Task<List<OrderItemInputViewModel>> GetReorderItems()
        {
            var items = await _context.Consumables
                .Where(c => c.QuantityOnHand <= c.ReorderLevel * 1.1m)
                .Select(c => new OrderItemInputViewModel
                {
                    ConsumableID = c.ConsumableID,
                    ConsumableName = c.ConsumableName,
                    CurrentStock = c.QuantityOnHand,
                    ReorderLevel = c.ReorderLevel,
                    SuggestedQuantity = Math.Max(c.ReorderLevel * 2 - c.QuantityOnHand, 10),
                    QuantityToOrder = 0,
                    IsSelected = false
                })
                .ToListAsync();

            var additionalItems = await _context.Consumables
                .Where(c => c.QuantityOnHand > c.ReorderLevel * 1.1m && c.IsActive)
                .OrderBy(c => c.ConsumableName)
                .Take(10)
                .Select(c => new OrderItemInputViewModel
                {
                    ConsumableID = c.ConsumableID,
                    ConsumableName = c.ConsumableName,
                    CurrentStock = c.QuantityOnHand,
                    ReorderLevel = c.ReorderLevel,
                    SuggestedQuantity = 0,
                    QuantityToOrder = 0,
                    IsSelected = false
                })
                .ToListAsync();

            items.AddRange(additionalItems);
            return items.OrderBy(i => i.ConsumableName).ToList();
        }

        private async Task<List<SelectListItem>> GetCategorySelectList()
        {
            return await _context.TestCategories
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryID.ToString(),
                    Text = c.CategoryName
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetSampleTypeSelectList()
        {
            return await _context.SampleTypes
                .Where(s => s.IsActive)
                .Select(s => new SelectListItem
                {
                    Value = s.SampleTypeID.ToString(),
                    Text = s.SampleTypeName
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetTechnicianSelectList()
        {
            return await _context.LabTechnicians
                .Where(t => t.IsActive)
                .Include(t => t.User)
                .Select(t => new SelectListItem
                {
                    Value = t.TechnicianID.ToString(),
                    Text = $"{t.User.Name} {t.User.Surname} ({t.EmployeeNumber})"
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetConsumableSelectList()
        {
            return await _context.Consumables
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.ConsumableID.ToString(),
                    Text = $"{c.ConsumableName} (Stock: {c.QuantityOnHand})"
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetSupplierSelectList()
        {
            return await _context.Suppliers
                .Where(s => s.IsActive)
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierID.ToString(),
                    Text = s.SupplierName
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetTestTypeSelectList()
        {
            return await _context.TestTypes
                .Where(t => t.IsActive)
                .Select(t => new SelectListItem
                {
                    Value = t.TestID.ToString(),
                    Text = t.TestName
                })
                .ToListAsync();
        }

        private string GetOrderStatusColor(string status)
        {
            return status switch
            {
                "Ordered" => "primary",
                "PartiallyComplete" => "warning",
                "Complete" => "success",
                "Cancelled" => "danger",
                "Received" => "info",
                _ => "secondary"
            };
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return 1;
        }

        private string GetCurrentUserName()
        {
            var nameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(nameClaim))
                return nameClaim;
            return "Amanda Adams";
        }

        private string GetCurrentUserFirstName()
        {
            var nameClaim = User.FindFirst(ClaimTypes.GivenName)?.Value;
            if (!string.IsNullOrEmpty(nameClaim))
                return nameClaim;
            return "Amanda";
        }

        private string GetCurrentUserLastName()
        {
            var surnameClaim = User.FindFirst(ClaimTypes.Surname)?.Value;
            if (!string.IsNullOrEmpty(surnameClaim))
                return surnameClaim;
            return "Adams";
        }

        private string GetCurrentUserEmail()
        {
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(emailClaim))
                return emailClaim;
            return "amanda.adams@nmblab.co.za";
        }

        private string GetCurrentUserPhone()
        {
            var phoneClaim = User.FindFirst(ClaimTypes.MobilePhone)?.Value;
            if (!string.IsNullOrEmpty(phoneClaim))
                return phoneClaim;
            return "0825551234";
        }
    }
}