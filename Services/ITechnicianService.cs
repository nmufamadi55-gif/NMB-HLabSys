using NMB_HLabSys.ViewModels;
namespace NMB_HLabSys.Services;
public interface ITechnicianService
{ Task<TechnicianDashboardViewModel> GetDashboardAsync(TechnicianDashboardFilterViewModel? filter = null); Task<TechnicianProfileViewModel> GetProfileAsync(); Task UpdateProfileAsync(TechnicianProfileViewModel model); Task<byte[]> GenerateCompletedTestsReportAsync(TechnicianCompletedTestsReportViewModel filter); Task<ReceiveSamplesViewModel> GetReceiveSamplesAsync(); Task ReceiveSampleAsync(int sampleId); Task<PerformTestsViewModel> GetPerformTestsAsync(); Task SelectTestAsync(int requestTestId); Task CaptureResultAsync(CaptureResultViewModel model); Task<VerificationQueueViewModel> GetVerificationQueueAsync(); Task VerifyResultAsync(VerifyResultViewModel model, bool approve); }


