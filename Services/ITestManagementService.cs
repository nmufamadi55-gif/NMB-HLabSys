//using Microsoft.EntityFrameworkCore;
//using NMB_HLabSys.Data;
//using NMB_HLabSys.Models;
//using NMB_HLabSys.Models.ViewModels;

//namespace NMB_HLabSys.Services
//{
//    public interface ITestManagementService
//    {
//        Task<TestType> CreateTestTypeAsync(CreateTestTypeViewModel model);
//        Task<TestType> UpdateTestTypeAsync(EditTestTypeViewModel model);
//        Task<bool> DeleteTestTypeAsync(int testId);
//        Task<List<TestTypeViewModel>> GetTestTypesWithDetailsAsync();
//    }

//    public class TestManagementService : ITestManagementService
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly ILogger<TestManagementService> _logger;

//        public TestManagementService(ApplicationDbContext context, ILogger<TestManagementService> logger)
//        {
//            _context = context;
//            _logger = logger;
//        }

//        public async Task<TestType> CreateTestTypeAsync(CreateTestTypeViewModel model)
//        {
//            using var transaction = await _context.Database.BeginTransactionAsync();
//            try
//            {
//                var testType = new TestType
//                {
//                    TestName = model.TestName,
//                    CategoryID = model.CategoryID,
//                    SampleTypeID = model.SampleTypeID,

//                    UnitOfMeasurement = model.UnitOfMeasurement,
//                    NormalRangeMin = model.NormalRangeMin,
//                    NormalRangeMax = model.NormalRangeMax,
//                    TurnaroundTimeMinutes = model.TurnaroundTimeMinutes,
//                    IsActive = true,
//                    CreatedAt = DateTime.Now
//                };

//                _context.TestTypes.Add(testType);
//                await _context.SaveChangesAsync();

//                // Assign technicians
//                if (model.SelectedTechnicianIDs != null && model.SelectedTechnicianIDs.Any())
//                {
//                    foreach (var techId in model.SelectedTechnicianIDs)
//                    {
//                        _context.TechnicianTestAssignments.Add(new TechnicianTestAssignment
//                        {
//                            TechnicianID = techId,
//                            TestID = testType.TestID,
//                            AssignedDate = DateTime.Now,
//                            IsActive = true
//                        });
//                    }
//                }

//                // Assign consumables
//                if (model.ConsumableQuantities != null && model.ConsumableQuantities.Any())
//                {
//                    foreach (var item in model.ConsumableQuantities)
//                    {
//                        if (item.Value > 0)
//                        {
//                            _context.TestConsumables.Add(new TestConsumable
//                            {
//                                TestID = testType.TestID,
//                                ConsumableID = item.Key,
//                                QuantityRequired = item.Value
//                            });
//                        }
//                    }
//                }

//                await _context.SaveChangesAsync();
//                await transaction.CommitAsync();

//                _logger.LogInformation($"Test type created: {testType.TestName} (ID: {testType.TestID})");
//                return testType;
//            }
//            catch (Exception ex)
//            {
//                await transaction.RollbackAsync();
//                _logger.LogError(ex, $"Error creating test type: {model.TestName}");
//                throw;
//            }
//        }

//        public async Task<TestType> UpdateTestTypeAsync(EditTestTypeViewModel model)
//        {
//            using var transaction = await _context.Database.BeginTransactionAsync();
//            try
//            {
//                var testType = await _context.TestTypes
//                    .Include(t => t.TechnicianTestAssignments)
//                    .Include(t => t.TestConsumables)
//                    .FirstOrDefaultAsync(t => t.TestID == model.TestID);

//                if (testType == null)
//                    throw new ArgumentException($"Test type with ID {model.TestID} not found");

//                // Update basic info
//                testType.TestName = model.TestName;
//                testType.CategoryID = model.CategoryID;

//                testType.SampleTypeID = model.SampleTypeID;
//                testType.UnitOfMeasurement = model.UnitOfMeasurement;
//                testType.NormalRangeMin = model.NormalRangeMin;
//                testType.NormalRangeMax = model.NormalRangeMax;
//                testType.TurnaroundTimeMinutes = model.TurnaroundTimeMinutes;
//                testType.IsActive = model.IsActive;

//                // Update technician assignments
//                _context.TechnicianTestAssignments.RemoveRange(testType.TechnicianTestAssignments);
//                if (model.SelectedTechnicianIDs != null && model.SelectedTechnicianIDs.Any())
//                {
//                    foreach (var techId in model.SelectedTechnicianIDs)
//                    {
//                        _context.TechnicianTestAssignments.Add(new TechnicianTestAssignment
//                        {
//                            TechnicianID = techId,
//                            TestID = testType.TestID,
//                            AssignedDate = DateTime.Now,
//                            IsActive = true
//                        });
//                    }
//                }

//                // Update consumables
//                _context.TestConsumables.RemoveRange(testType.TestConsumables);
//                if (model.ConsumableQuantities != null && model.ConsumableQuantities.Any())
//                {
//                    foreach (var item in model.ConsumableQuantities)
//                    {
//                        if (item.Value > 0)
//                        {
//                            _context.TestConsumables.Add(new TestConsumable
//                            {
//                                TestID = testType.TestID,
//                                ConsumableID = item.Key,
//                                QuantityRequired = item.Value
//                            });
//                        }
//                    }
//                }

//                await _context.SaveChangesAsync();
//                await transaction.CommitAsync();

//                _logger.LogInformation($"Test type updated: {testType.TestName} (ID: {testType.TestID})");
//                return testType;
//            }
//            catch (Exception ex)
//            {
//                await transaction.RollbackAsync();
//                _logger.LogError(ex, $"Error updating test type ID {model.TestID}");
//                throw;
//            }
//        }

//        public async Task<bool> DeleteTestTypeAsync(int testId)
//        {
//            try
//            {
//                var testType = await _context.TestTypes.FindAsync(testId);
//                if (testType == null)
//                    return false;

//                testType.IsActive = false; // Soft delete
//                await _context.SaveChangesAsync();

//                _logger.LogInformation($"Test type deactivated: {testType.TestName} (ID: {testId})");
//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Error deleting test type ID {testId}");
//                return false;
//            }
//        }

//        public async Task<List<TestTypeViewModel>> GetTestTypesWithDetailsAsync()
//        {
//            return await _context.TestTypes
//                .Include(t => t.Category)
//                .Include(t => t.SampleType)
//                .Include(t => t.TechnicianTestAssignments)
//                    .ThenInclude(ta => ta.Technician)
//                        .ThenInclude(t => t.User)
//                .Include(t => t.TestConsumables)
//                    .ThenInclude(tc => tc.Consumable)
//                .Select(t => new TestTypeViewModel
//                {
//                    TestID = t.TestID,
//                    TestName = t.TestName,
//                    CategoryName = t.Category.CategoryName,
//                    SampleTypeName = t.SampleType.SampleTypeName,
//                    UnitOfMeasurement = t.UnitOfMeasurement,
//                    NormalRangeMin = t.NormalRangeMin,
//                    NormalRangeMax = t.NormalRangeMax,
//                    TurnaroundTimeMinutes = t.TurnaroundTimeMinutes,
//                    AssignedTechnicians = t.TechnicianTestAssignments
//                        .Where(ta => ta.IsActive)
//                        .,
//                    IsActive = t.IsActive,
//                    CreatedAt = t.CreatedAt
//                })
//                .ToListAsync();
//        }
//    }
//}