using Microsoft.AspNetCore.Mvc;

namespace NMB_HLabSys.Controllers
{
    [NMB_HLabSys.Filters.RequireRole("Technician")]
    public class TechnicianController : Controller
    {
        private readonly NMB_HLabSys.Services.ITechnicianService _service;
        public TechnicianController(NMB_HLabSys.Services.ITechnicianService service) => _service = service;
        public async Task<IActionResult> Dashboard(NMB_HLabSys.ViewModels.TechnicianDashboardFilterViewModel filter) => await LoadAsync(() => _service.GetDashboardAsync(filter), "DashboardDb");
        public IActionResult Reports() => View();
        public IActionResult Help() => View();
        public async Task<IActionResult> Profile() => await LoadAsync(_service.GetProfileAsync, "Profile");
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Profile(NMB_HLabSys.ViewModels.TechnicianProfileViewModel model) { if(!ModelState.IsValid) return View(model); return await RunAsync(() => _service.UpdateProfileAsync(model), nameof(Profile), "Profile updated successfully."); }
        [HttpGet] public async Task<IActionResult> CompletedTestsReport(NMB_HLabSys.ViewModels.TechnicianCompletedTestsReportViewModel filter) { try { var bytes=await _service.GenerateCompletedTestsReportAsync(filter); return File(bytes,"application/pdf",$"Technician-Completed-Tests-{filter.StartDate:yyyyMMdd}-{filter.EndDate:yyyyMMdd}.pdf"); } catch(Exception ex) { TempData["ErrorMessage"]=ex.Message; return RedirectToAction(nameof(Dashboard)); } }
        public async Task<IActionResult> ReceiveSamples() => await LoadAsync(_service.GetReceiveSamplesAsync, "ReceiveSamplesDb");
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ReceiveSample(int sampleId) => await RunAsync(() => _service.ReceiveSampleAsync(sampleId), nameof(ReceiveSamples), "Sample received.");
        public async Task<IActionResult> PerformTests() => await LoadAsync(_service.GetPerformTestsAsync, "PerformTestsDb");
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> SelectTest(int requestTestId) => await RunAsync(() => _service.SelectTestAsync(requestTestId), nameof(PerformTests), "Test selected and started.");
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> CaptureResult(NMB_HLabSys.ViewModels.CaptureResultViewModel model) => await RunAsync(() => _service.CaptureResultAsync(model), nameof(PerformTests), "Result captured.");
        public async Task<IActionResult> Verification() => await LoadAsync(_service.GetVerificationQueueAsync, "VerificationDb");
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> VerifyResult(NMB_HLabSys.ViewModels.VerifyResultViewModel model) => await RunAsync(() => _service.VerifyResultAsync(model, true), nameof(Verification), "Result independently verified.");
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ReturnForReview(NMB_HLabSys.ViewModels.VerifyResultViewModel model) => await RunAsync(() => _service.VerifyResultAsync(model, false), nameof(Verification), "Result returned for correction.");
        private async Task<IActionResult> LoadAsync<T>(Func<Task<T>> action, string view) { try { return View(view, await action()); } catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; return RedirectToAction("Index", "Home"); } }
        private async Task<IActionResult> RunAsync(Func<Task> action, string target, string message) { try { await action(); TempData["SuccessMessage"] = message; } catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; } return RedirectToAction(target); }
    }
}
