using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.Models.ViewModels;
using NMB_HLabSys.Services;

namespace NMB_HLabSys.Services
{
    public class LaboratoryManagerService : ILaboratoryManagerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IPasswordGenerator _passwordGenerator;
        private readonly IStockManagementService _stockService;
        private readonly IPdfReportService _pdfService;
        private readonly ILogger<LaboratoryManagerService> _logger;

        public LaboratoryManagerService(
            ApplicationDbContext context,
            IEmailService emailService,
            IPasswordGenerator passwordGenerator,
            IStockManagementService stockService,
            IPdfReportService pdfService,
            ILogger<LaboratoryManagerService> logger)
        {
            _context = context;
            _emailService = emailService;
            _passwordGenerator = passwordGenerator;
            _stockService = stockService;
            _pdfService = pdfService;
            _logger = logger;
        }

        // ============================================================
        // DASHBOARD
        // ============================================================

        public async Task<DashboardViewModel> GetDashboardAsync()
        {
            var lowStockItems = await _context.Consumables
                .Include(c => c.Supplier)
                .Where(c => c.QuantityOnHand <= (c.ReorderLevel * 1.10))
                .Select(c => new LowStockAlertViewModel
                {
                    ConsumableID = c.ConsumableID,
                    ConsumableName = c.ConsumableName,
                    QuantityOnHand = c.QuantityOnHand,
                    ReorderLevel = c.ReorderLevel,
                    SupplierName = c.Supplier != null ? c.Supplier.SupplierName : "Unknown",
                    AlertLevel = c.QuantityOnHand <= c.ReorderLevel ? "Critical" : "Warning"
                }).ToListAsync();

            var activities = await _context.AuditLogs
                .OrderByDescending(a => a.ActivityDate)
                .Take(10)
                .Select(a => new RecentActivityViewModel
                {
                    ActivityType = a.ActivityType,
                    Description = a.Description,
                    ActivityDate = a.ActivityDate,
                    UserName = a.UserName
                }).ToListAsync();

            return new DashboardViewModel
            {
                TotalTestCategories = await _context.TestCategories.CountAsync(c => c.IsActive),
                TotalTestTypes = await _context.TestTypes.CountAsync(t => t.IsActive),
                TotalConsumables = await _context.Consumables.CountAsync(c => c.IsActive),
                TotalTechnicians = await _context.LabTechnicians.CountAsync(t => t.IsActive),
                TotalDoctors = await _context.Doctors.CountAsync(),
                PendingOrders = await _context.PurchaseOrders.CountAsync(o => o.Status == "Ordered" || o.Status == "Partially complete"),
                LowStockItems = lowStockItems.Count,
                LowStockAlerts = lowStockItems,
                RecentActivities = activities,
                Statistics = new DashboardStatisticsViewModel
                {
                    TestsPerformedThisMonth = await _context.TestRequests.CountAsync(r => r.Status == "Completed" && r.CompletedDate.HasValue && r.CompletedDate.Value.Month == DateTime.Now.Month),
                    OrdersProcessedThisMonth = await _context.PurchaseOrders.CountAsync(o => o.OrderDate.Month == DateTime.Now.Month),
                    TotalStockValue = await _context.Consumables.SumAsync(c => (decimal)(c.QuantityOnHand * (c.UnitPrice ?? 0))),
                    ActiveTechnicians = await _context.LabTechnicians.CountAsync(t => t.IsActive)
                }
            };
        }

        // ============================================================
        // TEST CATALOGUE
        // ============================================================

        public async Task<TestCatalogueIndexViewModel> GetTestCatalogueAsync()
        {
            var categories = await _context.TestCategories
                .Select(c => new TestCategoryViewModel
                {
                    CategoryID = c.CategoryID,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    TestCount = _context.TestTypes.Count(t => t.CategoryID == c.CategoryID),
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                }).ToListAsync();

            var testTypes = await _context.TestTypes
                .Include(t => t.Category)
                .Include(t => t.SampleType)
                .Include(t => t.TechnicianTestAssignments)
                    .ThenInclude(tta => tta.Technician)
                .Include(t => t.TestConsumables)
                    .ThenInclude(tc => tc.Consumable)
                .Select(t => new TestTypeViewModel
                {
                    TestID = t.TestID,
                    TestName = t.TestName,
                    CategoryName = t.Category.CategoryName,
                    SampleTypeName = t.SampleType.SampleTypeName,
                    UnitOfMeasurement = t.UnitOfMeasurement,
                    NormalRangeMin = t.NormalRangeMin,
                    NormalRangeMax = t.NormalRangeMax,
                    TurnaroundTimeMinutes = t.TurnaroundTimeMinutes,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    AssignedTechnicians = t.TechnicianTestAssignments
                        .Where(tta => tta.IsActive)
                        .Select(tta => $"{tta.Technician.User.Email}")
                        .ToList(),
                    Consumables = t.TestConsumables
                        .Where(tc => tc.Consumable.IsActive)
                        .Select(tc => new ConsumableSummaryViewModel
                        {
                            ConsumableID = tc.ConsumableID,
                            ConsumableName = tc.Consumable.ConsumableName,
                            QuantityOnHand = tc.Consumable.QuantityOnHand,
                            ReorderLevel = tc.Consumable.ReorderLevel,
                            UnitOfMeasure = tc.Consumable.UnitOfMeasure
                        }).ToList()
                }).ToListAsync();

            var sampleTypes = await _context.SampleTypes
                .Select(s => new SampleTypeViewModel
                {
                    SampleTypeID = s.SampleTypeID,
                    SampleTypeName = s.SampleTypeName,
                    Description = s.Description,
                    CollectionTubeType = s.CollectionTubeType,
                    StorageRequirements = s.StorageRequirements,
                    IsActive = s.IsActive
                }).ToListAsync();

            return new TestCatalogueIndexViewModel
            {
                Categories = categories,
                TestTypes = testTypes,
                SampleTypes = sampleTypes
            };
        }

        public async Task<CreateTestTypeViewModel> GetCreateTestTypeViewModelAsync()
        {
            return new CreateTestTypeViewModel
            {
                AvailableCategories = await _context.TestCategories
                    .Where(c => c.IsActive)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.CategoryID.ToString(),
                        Text = c.CategoryName
                    }).ToListAsync(),

                AvailableSampleTypes = await _context.SampleTypes
                    .Where(s => s.IsActive)
                    .Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = s.SampleTypeID.ToString(),
                        Text = s.SampleTypeName
                    }).ToListAsync(),

                AvailableTechnicians = await _context.LabTechnicians
                    .Where(t => t.IsActive)
                    .Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = t.TechnicianID.ToString(),
                        Text = $"{t.User.Email}"
                    }).ToListAsync(),

                AvailableConsumables = await _context.Consumables
                    .Where(c => c.IsActive)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.ConsumableID.ToString(),
                        Text = $"{c.ConsumableName} (Stock: {c.QuantityOnHand})"
                    }).ToListAsync()
            };
        }

        public async Task<ServiceResult<int>> CreateTestTypeAsync(CreateTestTypeViewModel model)
        {
            var errors = new List<string>();

            // Validate unique test name
            if (!await IsTestNameUniqueAsync(model.TestName))
                errors.Add($"Test name '{model.TestName}' already exists.");

            if (model.SelectedTechnicianIDs == null || !model.SelectedTechnicianIDs.Any())
                errors.Add("At least one technician must be assigned to perform this test.");

            if (model.ConsumableQuantities == null || !model.ConsumableQuantities.Any())
                errors.Add("At least one consumable must be linked to this test.");

            if (errors.Any())
                return ServiceResult<int>.ValidationFail(errors);

            var testType = new TestType
            {
                TestName = model.TestName,
                CategoryID = model.CategoryID,
                SampleTypeID = model.SampleTypeID,
                UnitOfMeasurement = model.UnitOfMeasurement,
                NormalRangeMin = model.NormalRangeMin,
                NormalRangeMax = model.NormalRangeMax,
                TurnaroundTimeMinutes = model.TurnaroundTimeMinutes,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.TestTypes.Add(testType);
            await _context.SaveChangesAsync();

            // Link assigned technicians
            foreach (var techId in model.SelectedTechnicianIDs)
            {
                _context.TechnicianTestAssignments.Add(new TechnicianTestAssignment
                {
                    TechnicianID = techId,
                    TestID = testType.TestID,
                    AssignedDate = DateTime.Now,
                    IsActive = true
                });
            }

            // Link consumables with quantities
            foreach (var item in model.ConsumableQuantities)
            {
                _context.TestConsumables.Add(new TestConsumable
                {
                    TestID = testType.TestID,
                    ConsumableID = item.Key,
                    QuantityRequired = item.Value
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Test type '{testType.TestName}' created with ID {testType.TestID}");
            return ServiceResult<int>.Ok(testType.TestID);
        }

        public async Task<EditTestTypeViewModel> GetEditTestTypeViewModelAsync(int testId)
        {
            var test = await _context.TestTypes
                .Include(t => t.TechnicianTestAssignments)
                .Include(t => t.TestConsumables)
                .FirstOrDefaultAsync(t => t.TestID == testId);

            if (test == null)
                throw new ArgumentException($"Test type with ID {testId} not found");

            return new EditTestTypeViewModel
            {
                TestID = test.TestID,
                TestName = test.TestName,
                CategoryID = test.CategoryID,
                SampleTypeID = test.SampleTypeID,
                UnitOfMeasurement = test.UnitOfMeasurement,
                NormalRangeMin = test.NormalRangeMin,
                NormalRangeMax = test.NormalRangeMax,
                TurnaroundTimeMinutes = test.TurnaroundTimeMinutes,
                IsActive = test.IsActive,
                SelectedTechnicianIDs = test.TechnicianTestAssignments
                    .Where(tta => tta.IsActive)
                    .Select(tta => tta.TechnicianID)
                    .ToList(),
                ConsumableQuantities = test.TestConsumables
                    .ToDictionary(tc => tc.ConsumableID, tc => tc.QuantityRequired),

                AvailableCategories = await _context.TestCategories
                    .Where(c => c.IsActive)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.CategoryID.ToString(),
                        Text = c.CategoryName
                    }).ToListAsync(),

                AvailableSampleTypes = await _context.SampleTypes
                    .Where(s => s.IsActive)
                    .Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = s.SampleTypeID.ToString(),
                        Text = s.SampleTypeName
                    }).ToListAsync(),

                AvailableTechnicians = await _context.LabTechnicians
                    .Where(t => t.IsActive)
                    .Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = t.TechnicianID.ToString(),
                        Text = $"{t.User.Email}"
                    }).ToListAsync(),

                AvailableConsumables = await _context.Consumables
                    .Where(c => c.IsActive)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.ConsumableID.ToString(),
                        Text = $"{c.ConsumableName} (Stock: {c.QuantityOnHand})"
                    }).ToListAsync()
            };
        }

        public async Task<ServiceResult<bool>> EditTestTypeAsync(EditTestTypeViewModel model)
        {
            var errors = new List<string>();

            if (!await IsTestNameUniqueAsync(model.TestName, model.TestID))
                errors.Add($"Test name '{model.TestName}' already exists.");

            if (model.SelectedTechnicianIDs == null || !model.SelectedTechnicianIDs.Any())
                errors.Add("At least one technician must be assigned to perform this test.");

            if (errors.Any())
                return ServiceResult<bool>.ValidationFail(errors);

            var test = await _context.TestTypes
                .Include(t => t.TechnicianTestAssignments)
                .Include(t => t.TestConsumables)
                .FirstOrDefaultAsync(t => t.TestID == model.TestID);

            if (test == null)
                return ServiceResult<bool>.Fail($"Test type with ID {model.TestID} not found.");

            // Update test properties
            test.TestName = model.TestName;
            test.CategoryID = model.CategoryID;
            test.SampleTypeID = model.SampleTypeID;
            test.UnitOfMeasurement = model.UnitOfMeasurement;
            test.NormalRangeMin = model.NormalRangeMin;
            test.NormalRangeMax = model.NormalRangeMax;
            test.TurnaroundTimeMinutes = model.TurnaroundTimeMinutes;
            test.IsActive = model.IsActive;

            // Update technician assignments (remove old, add new)
            _context.TechnicianTestAssignments.RemoveRange(test.TechnicianTestAssignments);
            foreach (var techId in model.SelectedTechnicianIDs)
            {
                _context.TechnicianTestAssignments.Add(new TechnicianTestAssignment
                {
                    TechnicianID = techId,
                    TestID = test.TestID,
                    AssignedDate = DateTime.Now,
                    IsActive = true
                });
            }

            // Update consumable links
            _context.TestConsumables.RemoveRange(test.TestConsumables);
            foreach (var item in model.ConsumableQuantities)
            {
                _context.TestConsumables.Add(new TestConsumable
                {
                    TestID = test.TestID,
                    ConsumableID = item.Key,
                    QuantityRequired = item.Value
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Test type '{test.TestName}' updated");
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteTestTypeAsync(int testId)
        {
            var test = await _context.TestTypes
                .Include(t => t.TechnicianTestAssignments)
                .Include(t => t.TestConsumables)
                .FirstOrDefaultAsync(t => t.TestID == testId);

            if (test == null)
                return ServiceResult<bool>.Fail($"Test type with ID {testId} not found.");

            // Soft delete
            test.IsActive = false;
            foreach (var assignment in test.TechnicianTestAssignments)
                assignment.IsActive = false;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Test type '{test.TestName}' deleted (soft)");
            return ServiceResult<bool>.Ok(true);
        }

        // ============================================================
        // TEST CATEGORIES
        // ============================================================

        public async Task<ServiceResult<int>> CreateCategoryAsync(CreateTestCategoryViewModel model)
        {
            if (!await IsCategoryNameUniqueAsync(model.CategoryName))
                return ServiceResult<int>.ValidationFail($"Category name '{model.CategoryName}' already exists.");

            var category = new TestCategory
            {
                CategoryName = model.CategoryName,
                Description = model.Description,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.TestCategories.Add(category);
            await _context.SaveChangesAsync();

            return ServiceResult<int>.Ok(category.CategoryID);
        }

        public async Task<EditTestCategoryViewModel> GetEditCategoryViewModelAsync(int categoryId)
        {
            var category = await _context.TestCategories.FindAsync(categoryId);
            if (category == null)
                throw new ArgumentException($"Category with ID {categoryId} not found");

            return new EditTestCategoryViewModel
            {
                CategoryID = category.CategoryID,
                CategoryName = category.CategoryName,
                Description = category.Description,
                IsActive = category.IsActive
            };
        }

        public async Task<ServiceResult<bool>> EditCategoryAsync(EditTestCategoryViewModel model)
        {
            if (!await IsCategoryNameUniqueAsync(model.CategoryName, model.CategoryID))
                return ServiceResult<bool>.ValidationFail($"Category name '{model.CategoryName}' already exists.");

            var category = await _context.TestCategories.FindAsync(model.CategoryID);
            if (category == null)
                return ServiceResult<bool>.Fail($"Category with ID {model.CategoryID} not found.");

            category.CategoryName = model.CategoryName;
            category.Description = model.Description;
            category.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteCategoryAsync(int categoryId)
        {
            var category = await _context.TestCategories.FindAsync(categoryId);
            if (category == null)
                return ServiceResult<bool>.Fail($"Category with ID {categoryId} not found.");

            // Check if category has tests
            var hasTests = await _context.TestTypes.AnyAsync(t => t.CategoryID == categoryId && t.IsActive);
            if (hasTests)
                return ServiceResult<bool>.ValidationFail("Cannot delete category that has active tests. Archive tests first.");

            category.IsActive = false;
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        // ============================================================
        // SAMPLE TYPES
        // ============================================================

        public async Task<ServiceResult<int>> CreateSampleTypeAsync(CreateSampleTypeViewModel model)
        {
            var exists = await _context.SampleTypes.AnyAsync(s => s.SampleTypeName == model.SampleTypeName);
            if (exists)
                return ServiceResult<int>.ValidationFail($"Sample type '{model.SampleTypeName}' already exists.");

            var sampleType = new SampleType
            {
                SampleTypeName = model.SampleTypeName,
                Description = model.Description,
                CollectionTubeType = model.CollectionTubeType,
                StorageRequirements = model.StorageRequirements,
                IsActive = true
            };

            _context.SampleTypes.Add(sampleType);
            await _context.SaveChangesAsync();

            return ServiceResult<int>.Ok(sampleType.SampleTypeID);
        }

        public async Task<EditSampleTypeViewModel> GetEditSampleTypeViewModelAsync(int sampleTypeId)
        {
            var sampleType = await _context.SampleTypes.FindAsync(sampleTypeId);
            if (sampleType == null)
                throw new ArgumentException($"Sample type with ID {sampleTypeId} not found");

            return new EditSampleTypeViewModel
            {
                SampleTypeID = sampleType.SampleTypeID,
                SampleTypeName = sampleType.SampleTypeName,
                Description = sampleType.Description,
                CollectionTubeType = sampleType.CollectionTubeType,
                StorageRequirements = sampleType.StorageRequirements,
                IsActive = sampleType.IsActive
            };
        }

        public async Task<ServiceResult<bool>> EditSampleTypeAsync(EditSampleTypeViewModel model)
        {
            var exists = await _context.SampleTypes.AnyAsync(s => s.SampleTypeName == model.SampleTypeName && s.SampleTypeID != model.SampleTypeID);
            if (exists)
                return ServiceResult<bool>.ValidationFail($"Sample type '{model.SampleTypeName}' already exists.");

            var sampleType = await _context.SampleTypes.FindAsync(model.SampleTypeID);
            if (sampleType == null)
                return ServiceResult<bool>.Fail($"Sample type with ID {model.SampleTypeID} not found.");

            sampleType.SampleTypeName = model.SampleTypeName;
            sampleType.Description = model.Description;
            sampleType.CollectionTubeType = model.CollectionTubeType;
            sampleType.StorageRequirements = model.StorageRequirements;
            sampleType.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteSampleTypeAsync(int sampleTypeId)
        {
            var sampleType = await _context.SampleTypes.FindAsync(sampleTypeId);
            if (sampleType == null)
                return ServiceResult<bool>.Fail($"Sample type with ID {sampleTypeId} not found.");

            var hasTests = await _context.TestTypes.AnyAsync(t => t.SampleTypeID == sampleTypeId && t.IsActive);
            if (hasTests)
                return ServiceResult<bool>.ValidationFail("Cannot delete sample type that is in use by active tests.");

            sampleType.IsActive = false;
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        // ============================================================
        // STOCK MANAGEMENT
        // ============================================================

        public async Task<StockManagementIndexViewModel> GetStockManagementAsync()
        {
            var consumables = await _context.Consumables
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
                }).ToListAsync();

            var lowStockSummary = await _stockService.GetLowStockSummaryAsync();

            return new StockManagementIndexViewModel
            {
                Consumables = consumables,
                LowStockSummary = lowStockSummary
            };
        }

        public async Task<CreateConsumableViewModel> GetCreateConsumableViewModelAsync()
        {
            return new CreateConsumableViewModel
            {
                AvailableSuppliers = await _context.Suppliers
                    .Where(s => s.IsActive)
                    .Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = s.SupplierID.ToString(),
                        Text = s.SupplierName
                    }).ToListAsync()
            };
        }

        public async Task<ServiceResult<int>> CreateConsumableAsync(CreateConsumableViewModel model)
        {
            if (!await IsConsumableNameUniqueAsync(model.ConsumableName))
                return ServiceResult<int>.ValidationFail($"Consumable '{model.ConsumableName}' already exists.");

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
            _context.StockAdjustmentLogs.Add(new StockAdjustmentLog
            {
                ConsumableID = consumable.ConsumableID,
                PreviousQuantity = 0,
                NewQuantity = model.QuantityOnHand,
                AdjustmentAmount = model.QuantityOnHand,
                AdjustmentType = "InitialStock",
                Reason = "Initial stock setup",
                AdjustedBy = 0, // System
                AdjustmentDate = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return ServiceResult<int>.Ok(consumable.ConsumableID);
        }

        public async Task<EditConsumableViewModel> GetEditConsumableViewModelAsync(int consumableId)
        {
            var consumable = await _context.Consumables.FindAsync(consumableId);
            if (consumable == null)
                throw new ArgumentException($"Consumable with ID {consumableId} not found");

            return new EditConsumableViewModel
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
                AvailableSuppliers = await _context.Suppliers
                    .Where(s => s.IsActive)
                    .Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = s.SupplierID.ToString(),
                        Text = s.SupplierName
                    }).ToListAsync()
            };
        }

        public async Task<ServiceResult<bool>> EditConsumableAsync(EditConsumableViewModel model)
        {
            if (!await IsConsumableNameUniqueAsync(model.ConsumableName, model.ConsumableID))
                return ServiceResult<bool>.ValidationFail($"Consumable '{model.ConsumableName}' already exists.");

            var consumable = await _context.Consumables.FindAsync(model.ConsumableID);
            if (consumable == null)
                return ServiceResult<bool>.Fail($"Consumable with ID {model.ConsumableID} not found.");

            consumable.ConsumableName = model.ConsumableName;
            consumable.SupplierID = model.SupplierID;
            consumable.ReorderLevel = model.ReorderLevel;
            consumable.QuantityOnHand = model.QuantityOnHand;
            consumable.UnitOfMeasure = model.UnitOfMeasure;
            consumable.UnitPrice = model.UnitPrice;
            consumable.MinimumStockLevel = model.MinimumStockLevel ?? 0;
            consumable.StorageLocation = model.StorageLocation;
            consumable.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteConsumableAsync(int consumableId)
        {
            var consumable = await _context.Consumables.FindAsync(consumableId);
            if (consumable == null)
                return ServiceResult<bool>.Fail($"Consumable with ID {consumableId} not found.");

            // Check if used in active tests
            var inUse = await _context.TestConsumables.AnyAsync(tc => tc.ConsumableID == consumableId);
            if (inUse)
                return ServiceResult<bool>.ValidationFail("Cannot delete consumable that is linked to active tests.");

            consumable.IsActive = false;
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> AdjustStockAsync(AdjustStockViewModel model)
        {
            var consumable = await _context.Consumables.FindAsync(model.ConsumableID);
            if (consumable == null)
                return ServiceResult<bool>.Fail($"Consumable with ID {model.ConsumableID} not found.");

            int previousQuantity = consumable.QuantityOnHand;
            int newQuantity;

            switch (model.AdjustmentType.ToLower())
            {
                case "increase":
                    newQuantity = previousQuantity + model.AdjustmentAmount;
                    break;
                case "decrease":
                    if (model.AdjustmentAmount > previousQuantity)
                        return ServiceResult<bool>.ValidationFail("Cannot decrease stock below zero.");
                    newQuantity = previousQuantity - model.AdjustmentAmount;
                    break;
                case "set":
                    newQuantity = Math.Max(0, model.AdjustmentAmount);
                    break;
                default:
                    return ServiceResult<bool>.ValidationFail($"Invalid adjustment type: {model.AdjustmentType}");
            }

            consumable.QuantityOnHand = newQuantity;

            _context.StockAdjustmentLogs.Add(new StockAdjustmentLog
            {
                ConsumableID = consumable.ConsumableID,
                PreviousQuantity = previousQuantity,
                NewQuantity = newQuantity,
                AdjustmentAmount = newQuantity - previousQuantity,
                AdjustmentType = model.AdjustmentType,
                Reason = model.Reason ?? "Manual adjustment",
                AdjustedBy = 0, // TODO: Get current user ID
                AdjustmentDate = DateTime.Now
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Stock adjusted for {consumable.ConsumableName}: {previousQuantity} → {newQuantity}");
            return ServiceResult<bool>.Ok(true);
        }

        // ============================================================
        // SUPPLIERS
        // ============================================================

        public async Task<List<SupplierViewModel>> GetSuppliersAsync()
        {
            return await _context.Suppliers
                .Select(s => new SupplierViewModel
                {
                    SupplierID = s.SupplierID,
                    SupplierName = s.SupplierName,
                    ContactPerson = s.ContactPerson,
                    EmailAddress = s.EmailAddress,
                    PhoneNumber = s.PhoneNumber,
                    Address = s.Address,
                    ConsumableCount = _context.Consumables.Count(c => c.SupplierID == s.SupplierID),
                    IsPreferred = s.IsPreferred,
                    IsActive = s.IsActive
                }).ToListAsync();
        }

        public async Task<ServiceResult<int>> CreateSupplierAsync(CreateSupplierViewModel model)
        {
            if (!await IsSupplierNameUniqueAsync(model.SupplierName))
                return ServiceResult<int>.ValidationFail($"Supplier '{model.SupplierName}' already exists.");

            var supplier = new Supplier
            {
                SupplierName = model.SupplierName,
                ContactPerson = model.ContactPerson,
                EmailAddress = model.EmailAddress,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                IsPreferred = model.IsPreferred,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            return ServiceResult<int>.Ok(supplier.SupplierID);
        }

        public async Task<EditSupplierViewModel> GetEditSupplierViewModelAsync(int supplierId)
        {
            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null)
                throw new ArgumentException($"Supplier with ID {supplierId} not found");

            return new EditSupplierViewModel
            {
                SupplierID = supplier.SupplierID,
                SupplierName = supplier.SupplierName,
                ContactPerson = supplier.ContactPerson,
                EmailAddress = supplier.EmailAddress,
                PhoneNumber = supplier.PhoneNumber,
                Address = supplier.Address,
                IsPreferred = supplier.IsPreferred,
                IsActive = supplier.IsActive
            };
        }

        public async Task<ServiceResult<bool>> EditSupplierAsync(EditSupplierViewModel model)
        {
            if (!await IsSupplierNameUniqueAsync(model.SupplierName, model.SupplierID))
                return ServiceResult<bool>.ValidationFail($"Supplier '{model.SupplierName}' already exists.");

            var supplier = await _context.Suppliers.FindAsync(model.SupplierID);
            if (supplier == null)
                return ServiceResult<bool>.Fail($"Supplier with ID {model.SupplierID} not found.");

            supplier.SupplierName = model.SupplierName;
            supplier.ContactPerson = model.ContactPerson;
            supplier.EmailAddress = model.EmailAddress;
            supplier.PhoneNumber = model.PhoneNumber;
            supplier.Address = model.Address;
            supplier.IsPreferred = model.IsPreferred;
            supplier.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteSupplierAsync(int supplierId)
        {
            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null)
                return ServiceResult<bool>.Fail($"Supplier with ID {supplierId} not found.");

            // Check if supplier has consumables
            var hasConsumables = await _context.Consumables.AnyAsync(c => c.SupplierID == supplierId && c.IsActive);
            if (hasConsumables)
                return ServiceResult<bool>.ValidationFail("Cannot delete supplier that has active consumables.");

            supplier.IsActive = false;
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<SupplierDetailsViewModel> GetSupplierDetailsAsync(int supplierId)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.Consumables)
                .FirstOrDefaultAsync(s => s.SupplierID == supplierId);

            if (supplier == null)
                throw new ArgumentException($"Supplier with ID {supplierId} not found");

            return new SupplierDetailsViewModel
            {
                SupplierID = supplier.SupplierID,
                SupplierName = supplier.SupplierName,
                ContactPerson = supplier.ContactPerson,
                EmailAddress = supplier.EmailAddress,
                PhoneNumber = supplier.PhoneNumber,
                Address = supplier.Address,
                IsPreferred = supplier.IsPreferred,
                IsActive = supplier.IsActive,
                CreatedAt = supplier.CreatedAt,
                ConsumableCount = supplier.Consumables.Count,
                Consumables = supplier.Consumables.Select(c => new ConsumableSummaryViewModel
                {
                    ConsumableID = c.ConsumableID,
                    ConsumableName = c.ConsumableName,
                    QuantityOnHand = c.QuantityOnHand,
                    ReorderLevel = c.ReorderLevel,
                    UnitOfMeasure = c.UnitOfMeasure
                }).ToList()
            };
        }

        // ============================================================
        // PURCHASE ORDERS
        // ============================================================

        public async Task<OrderIndexViewModel> GetOrdersAsync()
        {
            var orders = await _context.PurchaseOrders
                .Include(o => o.Supplier)
                .Include(o => o.OrderItems)
                .Select(o => new OrderListViewModel
                {
                    OrderID = o.OrderID,
                    OrderNumber = o.OrderNumber,
                    SupplierName = o.Supplier != null ? o.Supplier.SupplierName : "Unknown",
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    StatusColor = o.Status == "Complete" ? "success" : o.Status == "Cancelled" ? "danger" : "warning",
                    TotalItems = o.OrderItems.Count,
                    ItemsReceived = o.OrderItems.Count(i => i.Status == "Received"),
                    TotalAmount = o.TotalAmount,
                    ExpectedDeliveryDate = o.ExpectedDeliveryDate
                }).ToListAsync();

            return new OrderIndexViewModel
            {
                Orders = orders,
                PendingOrders = orders.Where(o => o.Status == "Ordered" || o.Status == "Partially complete").ToList(),
                CompletedOrders = orders.Where(o => o.Status == "Complete").ToList(),
                Statistics = new OrderStatisticsViewModel
                {
                    TotalOrders = orders.Count,
                    PendingOrders = orders.Count(o => o.Status == "Ordered"),
                    CompletedOrders = orders.Count(o => o.Status == "Complete"),
                    CancelledOrders = orders.Count(o => o.Status == "Cancelled"),
                    PartiallyCompleteOrders = orders.Count(o => o.Status == "Partially complete")
                }
            };
        }

        public async Task<CreateOrderViewModel> GetCreateOrderViewModelAsync()
        {
            return new CreateOrderViewModel
            {
                AvailableSuppliers = await _context.Suppliers
                    .Where(s => s.IsActive)
                    .Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = s.SupplierID.ToString(),
                        Text = s.SupplierName
                    }).ToListAsync()
            };
        }

        public async Task<ServiceResult<int>> CreateOrderAsync(CreateOrderViewModel model)
        {
            var supplier = await _context.Suppliers.FindAsync(model.SupplierID);
            if (supplier == null)
                return ServiceResult<int>.Fail("Selected supplier not found.");

            if (model.Items == null || !model.Items.Any(i => i.IsSelected && i.QuantityToOrder > 0))
                return ServiceResult<int>.ValidationFail("At least one item must be ordered.");

            var order = new PurchaseOrder
            {
                OrderNumber = "ORD-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                SupplierID = model.SupplierID,
                OrderDate = DateTime.Now,
                ExpectedDeliveryDate = model.ExpectedDeliveryDate,
                Status = "Ordered",
                CreatedBy = 0 // TODO: Get current user ID
            };

            _context.PurchaseOrders.Add(order);
            await _context.SaveChangesAsync();

            decimal totalAmount = 0;

            foreach (var item in model.Items.Where(i => i.IsSelected && i.QuantityToOrder > 0))
            {
                var consumable = await _context.Consumables.FindAsync(item.ConsumableID);
                var unitPrice = consumable?.UnitPrice ?? 0;

                var orderItem = new OrderItem
                {
                    OrderID = order.OrderID,
                    ConsumableID = item.ConsumableID,
                    QuantityOrdered = item.QuantityToOrder,
                    QuantityReceived = 0,
                    UnitPrice = unitPrice,
                    Status = "Ordered"
                };

                _context.OrderItems.Add(orderItem);
                totalAmount += unitPrice * item.QuantityToOrder;
            }

            order.TotalAmount = totalAmount;
            await _context.SaveChangesAsync();

            // Send email to supplier
            try
            {
                await _emailService.SendOrderEmailAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send order email to supplier");
            }

            _logger.LogInformation($"Purchase order {order.OrderNumber} created with {model.Items.Count(i => i.IsSelected)} items");
            return ServiceResult<int>.Ok(order.OrderID);
        }

        public async Task<OrderDetailViewModel> GetOrderDetailsAsync(int orderId)
        {
            var order = await _context.PurchaseOrders
                .Include(o => o.Supplier)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Consumable)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
                throw new ArgumentException($"Order with ID {orderId} not found");

            return new OrderDetailViewModel
            {
                OrderID = order.OrderID,
                OrderNumber = order.OrderNumber,
                SupplierName = order.Supplier?.SupplierName ?? "Unknown",
                SupplierContact = order.Supplier?.ContactPerson ?? "",
                SupplierEmail = order.Supplier?.EmailAddress ?? "",
                OrderDate = order.OrderDate,
                ExpectedDeliveryDate = order.ExpectedDeliveryDate,
                Status = order.Status,
                StatusColor = order.Status == "Complete" ? "success" : order.Status == "Cancelled" ? "danger" : "warning",
                CancellationReason = order.CancellationReason,
                DateCompleted = order.DateCompleted,
                DateCancelled = order.DateCancelled,
                TotalAmount = order.TotalAmount,
                CanReceive = order.Status == "Ordered" || order.Status == "Partially complete",
                CanCancel = order.Status == "Ordered" || order.Status == "Partially complete",
                Items = order.OrderItems.Select(oi => new OrderItemDetailViewModel
                {
                    OrderItemID = oi.OrderItemID,
                    ConsumableName = oi.Consumable?.ConsumableName ?? "Unknown",
                    QuantityOrdered = oi.QuantityOrdered,
                    QuantityReceived = oi.QuantityReceived,
                    UnitPrice = oi.UnitPrice,
                    Status = oi.Status,
                    StatusColor = oi.Status == "Received" ? "success" : oi.Status == "Cancelled" ? "danger" : "warning",
                    CancellationReason = oi.CancellationReason,
                    DateReceived = oi.DateReceived
                }).ToList()
            };
        }

        public async Task<ServiceResult<bool>> ReceiveOrderItemAsync(int orderItemId)
        {
            var item = await _context.OrderItems
                .Include(oi => oi.Consumable)
                .Include(oi => oi.Order)
                    .ThenInclude(o => o.OrderItems)
                .FirstOrDefaultAsync(oi => oi.OrderItemID == orderItemId);

            if (item == null)
                return ServiceResult<bool>.Fail("Order item not found.");

            if (item.Status == "Received")
                return ServiceResult<bool>.ValidationFail("This item has already been received.");

            if (item.Status == "Cancelled")
                return ServiceResult<bool>.ValidationFail("Cannot receive a cancelled item.");

            // Update item
            item.Status = "Received";
            item.QuantityReceived = item.QuantityOrdered;
            item.DateReceived = DateTime.Now;

            // Update stock
            if (item.Consumable != null)
            {
                item.Consumable.QuantityOnHand += item.QuantityOrdered;

                _context.StockAdjustmentLogs.Add(new StockAdjustmentLog
                {
                    ConsumableID = item.ConsumableID,
                    PreviousQuantity = item.Consumable.QuantityOnHand - item.QuantityOrdered,
                    NewQuantity = item.Consumable.QuantityOnHand,
                    AdjustmentAmount = item.QuantityOrdered,
                    AdjustmentType = "PurchaseOrderReceipt",
                    Reason = $"Received from order {item.Order.OrderNumber}",
                    AdjustedBy = 0,
                    AdjustmentDate = DateTime.Now
                });
            }

            // Update order status
            var order = item.Order;
            if (order.OrderItems.All(oi => oi.Status == "Received"))
            {
                order.Status = "Complete";
                order.DateCompleted = DateTime.Now;
            }
            else if (order.OrderItems.Any(oi => oi.Status == "Received"))
            {
                order.Status = "Partially complete";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Order item {orderItemId} received for order {order.OrderNumber}");
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> CancelOrderAsync(CancelOrderViewModel model)
        {
            var order = await _context.PurchaseOrders
                .Include(o => o.Supplier)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderID == model.OrderID);

            if (order == null)
                return ServiceResult<bool>.Fail("Order not found.");

            if (order.Status == "Complete")
                return ServiceResult<bool>.ValidationFail("Cannot cancel a completed order.");

            if (order.Status == "Cancelled")
                return ServiceResult<bool>.ValidationFail("Order is already cancelled.");

            order.Status = "Cancelled";
            order.CancellationReason = model.CancellationReason;
            order.DateCancelled = DateTime.Now;

            foreach (var item in order.OrderItems.Where(oi => oi.Status == "Ordered"))
            {
                item.Status = "Cancelled";
                item.CancellationReason = model.CancellationReason;
                item.DateCancelled = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // Notify supplier
            if (model.NotifySupplier && order.Supplier != null)
            {
                try
                {
                    await _emailService.SendOrderCancellationEmailAsync(order, model.CancellationReason);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send cancellation email to supplier");
                }
            }

            _logger.LogInformation($"Order {order.OrderNumber} cancelled");
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> CancelOrderItemAsync(int orderItemId, string reason)
        {
            var item = await _context.OrderItems
                .Include(oi => oi.Order)
                    .ThenInclude(o => o.OrderItems)
                .FirstOrDefaultAsync(oi => oi.OrderItemID == orderItemId);

            if (item == null)
                return ServiceResult<bool>.Fail("Order item not found.");

            if (item.Status == "Received")
                return ServiceResult<bool>.ValidationFail("Cannot cancel a received item.");

            if (item.Status == "Cancelled")
                return ServiceResult<bool>.ValidationFail("Item is already cancelled.");

            item.Status = "Cancelled";
            item.CancellationReason = reason;
            item.DateCancelled = DateTime.Now;

            // Update order status
            var order = item.Order;
            if (order.OrderItems.All(oi => oi.Status == "Cancelled"))
            {
                order.Status = "Cancelled";
                order.CancellationReason = reason;
                order.DateCancelled = DateTime.Now;
            }
            else if (order.OrderItems.All(oi => oi.Status == "Received" || oi.Status == "Cancelled"))
            {
                order.Status = "Complete";
                order.DateCompleted = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        // ============================================================
        // STAFF MANAGEMENT
        // ============================================================

        public async Task<StaffManagementIndexViewModel> GetStaffManagementAsync()
        {
            var technicians = await _context.LabTechnicians
                .Include(t => t.User)
                .Select(t => new TechnicianViewModel
                {
                    TechnicianID = t.TechnicianID,
                    FullName = $"{t.User.Email}",
                    EmployeeNumber = t.EmployeeNumber,
                    SAIDNumber = t.SAIDNumber,
                    Email = t.User.Email,
                    ContactNumber = t.User.Email, // Placeholder
                    Specialization = t.Specialization,
                    IsActive = t.IsActive,
                    HireDate = t.HireDate,
                    AssignedTestsCount = _context.TechnicianTestAssignments.Count(tta => tta.TechnicianID == t.TechnicianID && tta.IsActive)
                }).ToListAsync();

            var doctors = await _context.Doctors
                .Include(d => d.User)
                .Select(d => new DoctorListViewModel
                {
                    DoctorID = d.DoctorID,
                    FullName = $"{d.User.Email}",
                    HPCSANumber = d.HPCSANumber,
                    Email = d.User.Email,
                    PracticeName = d.PracticeName,
                    Specialization = d.Specialization,
                    IsVerified = d.IsVerified,
                    RegisteredDate = d.RegisteredDate
                }).ToListAsync();

            return new StaffManagementIndexViewModel
            {
                Technicians = technicians,
                Doctors = doctors,
                Statistics = new StaffStatisticsViewModel
                {
                    TotalTechnicians = technicians.Count,
                    ActiveTechnicians = technicians.Count(t => t.IsActive),
                    TotalDoctors = doctors.Count,
                    ActiveDoctors = doctors.Count(d => d.IsVerified)
                }
            };
        }

        public async Task<AddTechnicianViewModel> GetAddTechnicianViewModelAsync()
        {
            return new AddTechnicianViewModel
            {
                AvailableTestTypes = await _context.TestTypes
                    .Where(t => t.IsActive)
                    .Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = t.TestID.ToString(),
                        Text = t.TestName
                    }).ToListAsync()
            };
        }

        public async Task<ServiceResult<int>> AddTechnicianAsync(AddTechnicianViewModel model)
        {
            var errors = new List<string>();

            // Validate unique constraints
            if (!await IsEmployeeNumberUniqueAsync(model.EmployeeNumber))
                errors.Add($"Employee number '{model.EmployeeNumber}' already exists.");

            if (!await IsSAIDNumberUniqueAsync(model.SAIDNumber))
                errors.Add($"SA ID number '{model.SAIDNumber}' already exists.");

            if (!await IsEmailUniqueAsync(model.Email))
                errors.Add($"Email '{model.Email}' already exists.");

            if (model.SelectedTestTypeIDs == null || !model.SelectedTestTypeIDs.Any())
                errors.Add("At least one test type must be assigned to the technician.");

            if (errors.Any())
                return ServiceResult<int>.ValidationFail(errors);

            // Generate password
            string rawPassword = _passwordGenerator.GenerateTemporaryPassword();

            // Create user
            var user = new User
            {
                Email = model.Email,
                PasswordHash = rawPassword, // TODO: Hash properly
                UserRole = "Technician",
                IsActive = true,
                IsPasswordChanged = false,
                CreatedDate = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create technician
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

            // Send welcome email
            try
            {
                await _emailService.SendTechnicianWelcomeEmailAsync(model.Email, model.Name, rawPassword);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send welcome email to technician");
            }

            _logger.LogInformation($"Technician added: {model.Email}");
            return ServiceResult<int>.Ok(technician.TechnicianID);
        }

        public async Task<EditTechnicianViewModel> GetEditTechnicianViewModelAsync(int technicianId)
        {
            var technician = await _context.LabTechnicians
                .Include(t => t.User)
                .Include(t => t.TestAssignments)
                .FirstOrDefaultAsync(t => t.TechnicianID == technicianId);

            if (technician == null)
                throw new ArgumentException($"Technician with ID {technicianId} not found");

            return new EditTechnicianViewModel
            {
                TechnicianID = technician.TechnicianID,
                Name = technician.User.Email,
                Surname = "",
                SAIDNumber = technician.SAIDNumber,
                EmployeeNumber = technician.EmployeeNumber,
                Email = technician.User.Email,
                ContactNumber = "",
                Specialization = technician.Specialization,
                Qualification = technician.Qualification,
                YearsOfExperience = technician.YearsOfExperience,
                IsActive = technician.IsActive,
                SelectedTestTypeIDs = technician.TestAssignments
                    .Where(tta => tta.IsActive)
                    .Select(tta => tta.TestID)
                    .ToList(),
                AvailableTestTypes = await _context.TestTypes
                    .Where(t => t.IsActive)
                    .Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = t.TestID.ToString(),
                        Text = t.TestName
                    }).ToListAsync()
            };
        }

        public async Task<ServiceResult<bool>> EditTechnicianAsync(EditTechnicianViewModel model)
        {
            var errors = new List<string>();

            if (!await IsEmployeeNumberUniqueAsync(model.EmployeeNumber, model.TechnicianID))
                errors.Add($"Employee number '{model.EmployeeNumber}' already exists.");

            if (!await IsSAIDNumberUniqueAsync(model.SAIDNumber, model.TechnicianID))
                errors.Add($"SA ID number '{model.SAIDNumber}' already exists.");

            if (!await IsEmailUniqueAsync(model.Email, model.TechnicianID))
                errors.Add($"Email '{model.Email}' already exists.");

            if (model.SelectedTestTypeIDs == null || !model.SelectedTestTypeIDs.Any())
                errors.Add("At least one test type must be assigned to the technician.");

            if (errors.Any())
                return ServiceResult<bool>.ValidationFail(errors);

            var technician = await _context.LabTechnicians
                .Include(t => t.User)
                .Include(t => t.TestAssignments)
                .FirstOrDefaultAsync(t => t.TechnicianID == model.TechnicianID);

            if (technician == null)
                return ServiceResult<bool>.Fail($"Technician with ID {model.TechnicianID} not found.");

            // Update technician
            technician.SAIDNumber = model.SAIDNumber;
            technician.EmployeeNumber = model.EmployeeNumber;
            technician.Specialization = model.Specialization;
            technician.Qualification = model.Qualification;
            technician.YearsOfExperience = model.YearsOfExperience;
            technician.IsActive = model.IsActive;

            // Update user
            technician.User.IsActive = model.IsActive;

            // Update assignments
            _context.TechnicianTestAssignments.RemoveRange(technician.TestAssignments);
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

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<TechnicianProfileViewModel> GetTechnicianProfileAsync(int technicianId)
        {
            var technician = await _context.LabTechnicians
                .Include(t => t.User)
                .Include(t => t.TestAssignments)
                    .ThenInclude(tta => tta.TestType)
                .FirstOrDefaultAsync(t => t.TechnicianID == technicianId);

            if (technician == null)
                throw new ArgumentException($"Technician with ID {technicianId} not found");

            return new TechnicianProfileViewModel
            {
                TechnicianID = technician.TechnicianID,
                FullName = $"{technician.User.Email}",
                EmployeeNumber = technician.EmployeeNumber,
                SAIDNumber = technician.SAIDNumber,
                Email = technician.User.Email,
                ContactNumber = "",
                Specialization = technician.Specialization,
                Qualification = technician.Qualification,
                YearsOfExperience = technician.YearsOfExperience,
                HireDate = technician.HireDate,
                IsActive = technician.IsActive,
                AssignedTests = technician.TestAssignments
                    .Where(tta => tta.IsActive)
                    .Select(tta => new AssignedTestViewModel
                    {
                        TestID = tta.TestID,
                        TestName = tta.TestType.TestName,
                        CategoryName = tta.TestType.Category.CategoryName,
                        AssignedDate = tta.AssignedDate,
                        IsActive = tta.IsActive
                    }).ToList()
            };
        }

        public async Task<ServiceResult<bool>> DeleteTechnicianAsync(int technicianId)
        {
            var technician = await _context.LabTechnicians
                .Include(t => t.User)
                .Include(t => t.TestAssignments)
                .FirstOrDefaultAsync(t => t.TechnicianID == technicianId);

            if (technician == null)
                return ServiceResult<bool>.Fail($"Technician with ID {technicianId} not found.");

            technician.IsActive = false;
            technician.User.IsActive = false;

            foreach (var assignment in technician.TestAssignments)
                assignment.IsActive = false;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<int>> AddDoctorAsync(RegisterDoctorViewModel model)
        {
            var errors = new List<string>();

            if (!await IsHPCSANumberUniqueAsync(model.HPCSANumber))
                errors.Add($"HPCSA number '{model.HPCSANumber}' already exists.");

            if (!await IsEmailUniqueAsync(model.Email))
                errors.Add($"Email '{model.Email}' already exists.");

            if (errors.Any())
                return ServiceResult<int>.ValidationFail(errors);

            string rawPassword = _passwordGenerator.GenerateTemporaryPassword();

            var user = new User
            {
                Email = model.Email,
                PasswordHash = rawPassword, // TODO: Hash properly
                UserRole = "Doctor",
                IsActive = true,
                IsPasswordChanged = false,
                CreatedDate = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

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

            try
            {
                await _emailService.SendDoctorWelcomeEmailAsync(model.Email, $"{model.Name} {model.Surname}", rawPassword);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send welcome email to doctor");
            }

            return ServiceResult<int>.Ok(doctor.DoctorID);
        }

        public async Task<EditDoctorViewModel> GetEditDoctorViewModelAsync(int doctorId)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DoctorID == doctorId);

            if (doctor == null)
                throw new ArgumentException($"Doctor with ID {doctorId} not found");

            return new EditDoctorViewModel
            {
                DoctorID = doctor.DoctorID,
                Name = "",
                Surname = "",
                HPCSANumber = doctor.HPCSANumber,
                Email = doctor.User.Email,
                ContactNumber = "",
                PracticeName = doctor.PracticeName,
                PracticeAddress = doctor.PracticeAddress,
                Specialization = doctor.Specialization,
                IsVerified = doctor.IsVerified,
                IsActive = doctor.User.IsActive
            };
        }

        public async Task<ServiceResult<bool>> EditDoctorAsync(EditDoctorViewModel model)
        {
            if (!await IsHPCSANumberUniqueAsync(model.HPCSANumber, model.DoctorID))
                return ServiceResult<bool>.ValidationFail($"HPCSA number '{model.HPCSANumber}' already exists.");

            if (!await IsEmailUniqueAsync(model.Email, model.DoctorID))
                return ServiceResult<bool>.ValidationFail($"Email '{model.Email}' already exists.");

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DoctorID == model.DoctorID);

            if (doctor == null)
                return ServiceResult<bool>.Fail($"Doctor with ID {model.DoctorID} not found.");

            doctor.HPCSANumber = model.HPCSANumber;
            doctor.PracticeName = model.PracticeName;
            doctor.PracticeAddress = model.PracticeAddress;
            doctor.Specialization = model.Specialization;
            doctor.IsVerified = model.IsVerified;
            doctor.User.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteDoctorAsync(int doctorId)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DoctorID == doctorId);

            if (doctor == null)
                return ServiceResult<bool>.Fail($"Doctor with ID {doctorId} not found.");

            doctor.User.IsActive = false;
            doctor.IsVerified = false;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        // ============================================================
        // REPORTS
        // ============================================================

        public async Task<ReportFilterViewModel> GetReportsViewModelAsync()
        {
            return new ReportFilterViewModel
            {
                StartDate = DateTime.Now.AddMonths(-1),
                EndDate = DateTime.Now
            };
        }

        public async Task<byte[]> GenerateCategoryReportAsync(ReportFilterViewModel filter)
        {
            if (filter.StartDate > filter.EndDate)
                throw new ArgumentException("Start date cannot be later than end date.");

            var reportData = await _context.RequestTestTypes
    .Include(rt => rt.TestType)
    .ThenInclude(tt => tt.Category)
    .Include(rt => rt.TestRequest)
    .Where(rt => rt.Status == "Completed" &&
                  rt.CompletedDateTime.HasValue &&
                  rt.CompletedDateTime.Value >= filter.StartDate &&
                  rt.CompletedDateTime.Value <= filter.EndDate)
    .GroupBy(rt => rt.TestType.Category.CategoryName)
    .Select(g => new CategoryReportData
    {
        CategoryName = g.Key ?? "Uncategorized",
        TotalTests = g.Count(),
        Tests = g.Select(t => new TestSummaryItem
        {
            TestName = t.TestType.TestName,
            CompletedDate = t.CompletedDateTime
        }).ToList()
    }).ToListAsync();

            return _pdfService.GenerateCategorySummaryPdf(reportData, filter.StartDate, filter.EndDate);
        }

        public async Task<byte[]> GenerateStockReportAsync(ReportFilterViewModel filter)
        {
            // TODO: Implement stock report
            throw new NotImplementedException("Stock report generation not yet implemented.");
        }

        public async Task<byte[]> GenerateStaffReportAsync(ReportFilterViewModel filter)
        {
            // TODO: Implement staff report
            throw new NotImplementedException("Staff report generation not yet implemented.");
        }

        public async Task<byte[]> GenerateOrdersReportAsync(ReportFilterViewModel filter)
        {
            // TODO: Implement orders report
            throw new NotImplementedException("Orders report generation not yet implemented.");
        }

        // ============================================================
        // UTILITY METHODS
        // ============================================================

        public async Task<bool> IsTestNameUniqueAsync(string testName, int? excludeTestId = null)
        {
            var query = _context.TestTypes.Where(t => t.TestName.ToLower() == testName.ToLower());
            if (excludeTestId.HasValue)
                query = query.Where(t => t.TestID != excludeTestId.Value);
            return !await query.AnyAsync();
        }

        public async Task<bool> IsCategoryNameUniqueAsync(string categoryName, int? excludeCategoryId = null)
        {
            var query = _context.TestCategories.Where(c => c.CategoryName.ToLower() == categoryName.ToLower());
            if (excludeCategoryId.HasValue)
                query = query.Where(c => c.CategoryID != excludeCategoryId.Value);
            return !await query.AnyAsync();
        }

        public async Task<bool> IsConsumableNameUniqueAsync(string consumableName, int? excludeConsumableId = null)
        {
            var query = _context.Consumables.Where(c => c.ConsumableName.ToLower() == consumableName.ToLower());
            if (excludeConsumableId.HasValue)
                query = query.Where(c => c.ConsumableID != excludeConsumableId.Value);
            return !await query.AnyAsync();
        }

        public async Task<bool> IsSupplierNameUniqueAsync(string supplierName, int? excludeSupplierId = null)
        {
            var query = _context.Suppliers.Where(s => s.SupplierName.ToLower() == supplierName.ToLower());
            if (excludeSupplierId.HasValue)
                query = query.Where(s => s.SupplierID != excludeSupplierId.Value);
            return !await query.AnyAsync();
        }

        public async Task<bool> IsEmployeeNumberUniqueAsync(string employeeNumber, int? excludeTechnicianId = null)
        {
            var query = _context.LabTechnicians.Where(t => t.EmployeeNumber == employeeNumber);
            if (excludeTechnicianId.HasValue)
                query = query.Where(t => t.TechnicianID != excludeTechnicianId.Value);
            return !await query.AnyAsync();
        }

        public async Task<bool> IsSAIDNumberUniqueAsync(string saidNumber, int? excludeTechnicianId = null)
        {
            var query = _context.LabTechnicians.Where(t => t.SAIDNumber == saidNumber);
            if (excludeTechnicianId.HasValue)
                query = query.Where(t => t.TechnicianID != excludeTechnicianId.Value);
            return !await query.AnyAsync();
        }

        public async Task<bool> IsHPCSANumberUniqueAsync(string hpcsaNumber, int? excludeDoctorId = null)
        {
            var query = _context.Doctors.Where(d => d.HPCSANumber == hpcsaNumber);
            if (excludeDoctorId.HasValue)
                query = query.Where(d => d.DoctorID != excludeDoctorId.Value);
            return !await query.AnyAsync();
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeUserId = null)
        {
            var query = _context.Users.Where(u => u.Email.ToLower() == email.ToLower());
            if (excludeUserId.HasValue)
                query = query.Where(u => u.UserID != excludeUserId.Value);
            return !await query.AnyAsync();
        }

        // ============================================================
        // PRIVATE HELPERS
        // ============================================================

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