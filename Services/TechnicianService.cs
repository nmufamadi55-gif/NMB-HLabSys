using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NMB_HLabSys.Services;
public sealed class TechnicianService : ITechnicianService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IEmailService _emailService;

    public TechnicianService(ApplicationDbContext db, ICurrentUserService current, IEmailService emailService)
    {
        _db = db;
        _current = current;
        _emailService = emailService;
    }
    private async Task<LabTechnician> TechAsync() => await _db.LabTechnicians.Include(x => x.User).SingleOrDefaultAsync(x => x.UserID == _current.UserId && x.IsActive) ?? throw new UnauthorizedAccessException("Your active technician profile could not be found.");
    private static TechnicianIdentityViewModel Identity(LabTechnician t) => new() { Name = $"{t.User.Name} {t.User.Surname}".Trim(), EmployeeNumber = t.EmployeeNumber };
    private IQueryable<RequestTestType> Assigned(int techId) => _db.RequestTestTypes.Include(x => x.TestRequest).ThenInclude(x => x.Patient).ThenInclude(x => x.PatientConditions).ThenInclude(x => x.Condition).Include(x => x.TestRequest).ThenInclude(x => x.Patient).ThenInclude(x => x.PatientAllergies).ThenInclude(x => x.Allergy).Include(x => x.TestRequest).ThenInclude(x => x.Patient).ThenInclude(x => x.PatientMedications).ThenInclude(x => x.Medication).Include(x => x.TestResults!).ThenInclude(x => x.Verifications).Include(x => x.TestType).ThenInclude(x => x.Category).Where(x => _db.TechnicianTestAssignments.Any(a => a.TechnicianID == techId && a.TestID == x.TestTypeID && a.IsActive));
    private static TechnicianWorkItemViewModel Map(RequestTestType x)
    {
        var due = x.StartDateTime?.AddMinutes(x.TestType!.TurnaroundTimeMinutes);
        var patient=x.TestRequest!.Patient!; var latestReview=x.TestResults?.SelectMany(r=>r.Verifications??[]).OrderByDescending(v=>v.VerificationDate).FirstOrDefault(v=>v.Status=="Returned"); return new() { RequestTestId=x.RequestTestID, RequestNumber=x.TestRequest.RequestNumber, Patient=$"{patient.FirstName} {patient.LastName}", TestName=x.TestType!.TestName, Category=x.TestType.Category?.CategoryName ?? "Uncategorised", Urgency=x.TestRequest.Urgency, Status=x.Status, StartedAt=x.StartDateTime, DueAt=due, IsOverdue=due.HasValue && due < DateTime.Now && x.Status is "In Progress" or "Completed", ClinicalNotes=x.TestRequest.ClinicalNotes??"", Conditions=string.Join(", ",patient.PatientConditions.Where(p=>p.IsActive).Select(p=>p.Condition?.ConditionName).Where(n=>!string.IsNullOrWhiteSpace(n))), Allergies=string.Join(", ",patient.PatientAllergies.Select(p=>$"{p.Allergy?.AllergyName}{(string.IsNullOrWhiteSpace(p.Severity)?"":$" ({p.Severity})")}")), Medications=string.Join(", ",patient.PatientMedications.Where(p=>p.IsCurrent).Select(p=>p.Medication?.MedicationName).Where(n=>!string.IsNullOrWhiteSpace(n))), ReviewNotes=latestReview?.ReviewNotes??"" };
    }
    public async Task<TechnicianDashboardViewModel> GetDashboardAsync(TechnicianDashboardFilterViewModel? filter = null)
    { var tech=await TechAsync(); filter ??= new(); var active=(await Assigned(tech.TechnicianID).ToListAsync()).Where(x=>x.AssignedTechnicianID==tech.TechnicianID || x.AssignedTechnicianID==null).Select(Map).ToList(); var now=DateTime.Now; var all=active; var filtered=all.AsEnumerable(); if(!string.IsNullOrWhiteSpace(filter.Status)) filtered=filtered.Where(x=>x.Status.Equals(filter.Status,StringComparison.OrdinalIgnoreCase)); if(!string.IsNullOrWhiteSpace(filter.Urgency)) filtered=filter.Urgency.Equals("priority",StringComparison.OrdinalIgnoreCase)?filtered.Where(x=>x.Urgency.Equals("urgent",StringComparison.OrdinalIgnoreCase)||x.Urgency.Equals("stat",StringComparison.OrdinalIgnoreCase)):filtered.Where(x=>x.Urgency.Equals(filter.Urgency,StringComparison.OrdinalIgnoreCase)); if(!string.IsNullOrWhiteSpace(filter.Category)) filtered=filtered.Where(x=>x.Category.Equals(filter.Category,StringComparison.OrdinalIgnoreCase)); if(!string.IsNullOrWhiteSpace(filter.RequestNumber)) filtered=filtered.Where(x=>x.RequestNumber.Contains(filter.RequestNumber,StringComparison.OrdinalIgnoreCase)); if(filter.Due=="overdue") filtered=filtered.Where(x=>x.IsOverdue); else if(filter.Due=="today") filtered=filtered.Where(x=>x.DueAt?.Date==now.Date); else if(filter.Due=="nearing") filtered=filtered.Where(x=>x.DueAt.HasValue&&!x.IsOverdue&&x.DueAt.Value<=now.AddMinutes(60)); var workload=new List<TechnicianChartItemViewModel>{new(){Label="To select",Value=all.Count(x=>x.Status=="Submitted"),Status="Submitted"},new(){Label="In progress",Value=all.Count(x=>x.Status=="In Progress"),Status="In Progress"},new(){Label="Verify",Value=all.Count(x=>x.Status=="Completed"),Status="Completed"},new(){Label="Review",Value=all.Count(x=>x.Status=="To Be Reviewed"),Status="To Be Reviewed"},new(){Label="Overdue",Value=all.Count(x=>x.IsOverdue),Due="overdue"}}; return new() { Technician=Identity(tech), Filter=filter, Categories=all.Select(x=>x.Category).Distinct().OrderBy(x=>x).ToList(), CategoryChart=all.GroupBy(x=>x.Category).OrderByDescending(g=>g.Count()).ThenBy(g=>g.Key).Take(6).Select(g=>new TechnicianChartItemViewModel{Label=g.Key,Value=g.Count(),Category=g.Key}).ToList(), WorkloadChart=workload, PendingSamples=await _db.Samples.CountAsync(x=>x.Status=="Pending"), InProgress=all.Count(x=>x.Status=="In Progress"), Completed=all.Count(x=>x.Status=="Completed"||x.Status=="Verified"), AwaitingVerification=all.Count(x=>x.Status=="Completed"), WaitingToBeSelected=all.Count(x=>x.Status=="Submitted"), WaitingToBeReviewed=all.Count(x=>x.Status=="To Be Reviewed"), Urgent=all.Count(x=>x.Urgency.Equals("urgent",StringComparison.OrdinalIgnoreCase)||x.Urgency.Equals("stat",StringComparison.OrdinalIgnoreCase)), Overdue=all.Count(x=>x.IsOverdue), NearingDue=all.Count(x=>x.DueAt.HasValue&&!x.IsOverdue&&x.DueAt.Value<=now.AddMinutes(60)), WorkItems=filtered.OrderByDescending(x=>x.IsOverdue).ThenBy(x=>x.DueAt).Take(100).ToList() }; }
    public async Task<TechnicianProfileViewModel> GetProfileAsync(){var tech=await TechAsync();return new(){Name=tech.User.Name,Surname=tech.User.Surname,Email=tech.User.Email,ContactNumber=tech.User.ContactNumber,EmployeeNumber=tech.EmployeeNumber,Specialization=tech.Specialization,Qualification=tech.Qualification,YearsOfExperience=tech.YearsOfExperience};}
    public async Task UpdateProfileAsync(TechnicianProfileViewModel model){var tech=await TechAsync();var email=model.Email.Trim();if(await _db.Users.AnyAsync(u=>u.UserID!=tech.UserID&&u.Email==email))throw new ArgumentException("That email address is already in use.");tech.User.Name=model.Name.Trim();tech.User.Surname=model.Surname.Trim();tech.User.Email=email;tech.User.ContactNumber=model.ContactNumber?.Trim()??"";tech.User.ModifiedDate=DateTime.Now;tech.Specialization=model.Specialization?.Trim()??"";tech.Qualification=model.Qualification?.Trim()??"";tech.YearsOfExperience=Math.Max(0,model.YearsOfExperience);await _db.SaveChangesAsync();}
    public async Task<byte[]> GenerateCompletedTestsReportAsync(TechnicianCompletedTestsReportViewModel filter)
    { if(filter.EndDate.Date<filter.StartDate.Date) throw new ArgumentException("The end date must be on or after the start date."); var tech=await TechAsync(); var start=filter.StartDate.Date; var end=filter.EndDate.Date.AddDays(1).AddTicks(-1); var rows=await Assigned(tech.TechnicianID).Include(x=>x.TestResults).Where(x=>x.AssignedTechnicianID==tech.TechnicianID&&x.CompletedDateTime.HasValue&&x.CompletedDateTime>=start&&x.CompletedDateTime<=end&&(x.Status=="Completed"||x.Status=="Verified")).ToListAsync(); var data=new TechnicianReportDataViewModel{Technician=Identity(tech),StartDate=start,EndDate=filter.EndDate.Date,TotalCompleted=rows.Count,StatCount=rows.Count(x=>x.TestRequest!.Urgency.Equals("stat",StringComparison.OrdinalIgnoreCase)),OverdueCompleted=rows.Count(x=>x.CompletedDateTime>x.StartDateTime!.Value.AddMinutes(x.TestType!.TurnaroundTimeMinutes)),Categories=rows.GroupBy(x=>x.TestType!.Category?.CategoryName??"Uncategorised").OrderBy(x=>x.Key).Select(g=>new TechnicianReportCategoryViewModel{Category=g.Key,Tests=g.OrderBy(x=>x.CompletedDateTime).Select(x=>new TechnicianReportTestViewModel{RequestNumber=x.TestRequest!.RequestNumber,TestName=x.TestType!.TestName,Urgency=x.TestRequest.Urgency,CompletedAt=x.CompletedDateTime!.Value,IsOverdue=x.StartDateTime.HasValue&&x.CompletedDateTime>x.StartDateTime.Value.AddMinutes(x.TestType.TurnaroundTimeMinutes),IsAbnormal=x.TestResults?.OrderByDescending(r=>r.ResultDate).FirstOrDefault()?.IsAbnormal??false}).ToList()}).ToList()}; return BuildCompletedTestsPdf(data); }
    private static byte[] BuildCompletedTestsPdf(TechnicianReportDataViewModel data) => Document.Create(document=>document.Page(page=>{page.Size(PageSizes.A4);page.Margin(30);page.DefaultTextStyle(x=>x.FontFamily("Arial").FontSize(9));page.Header().Row(row=>{row.ConstantItem(48).Height(48).Background("0D5AC6").AlignCenter().AlignMiddle().Text("NMB").FontColor(Colors.White).Bold().FontSize(14);row.RelativeItem().PaddingLeft(12).Column(c=>{c.Item().Text("NMB-HLabSys").Bold().FontSize(17).FontColor("0C3D82");c.Item().Text("Technician completed-test report").FontColor("176FE4").SemiBold();c.Item().Text($"Reporting period: {data.StartDate:dd MMM yyyy} - {data.EndDate:dd MMM yyyy}").FontSize(8).FontColor(Colors.Grey.Darken1);});row.ConstantItem(120).AlignRight().Column(c=>{c.Item().Text("GENERATED").FontSize(7).FontColor(Colors.Grey.Medium);c.Item().Text(DateTime.Now.ToString("dd MMM yyyy HH:mm")).FontSize(8);c.Item().PaddingTop(5).Text("CONFIDENTIAL").FontSize(7).Bold().FontColor("C55600");});});page.Content().PaddingVertical(16).Column(col=>{col.Item().Background("EFF6FF").Padding(10).Row(r=>{r.RelativeItem().Text($"Technician: {data.Technician.Name}").Bold().FontSize(10);r.RelativeItem().AlignRight().Text($"Employee no.: {data.Technician.EmployeeNumber}").FontSize(9);});col.Item().PaddingTop(12).Row(r=>{r.RelativeItem().Element(c=>c.Border(1).BorderColor("BFDBFE").Padding(8).Column(x=>{x.Item().Text(data.TotalCompleted.ToString()).FontSize(18).Bold().FontColor("0D5AC6");x.Item().Text("COMPLETED TESTS").FontSize(7).SemiBold();}));r.ConstantItem(8);r.RelativeItem().Element(c=>c.Border(1).BorderColor("FDE68A").Padding(8).Column(x=>{x.Item().Text(data.StatCount.ToString()).FontSize(18).Bold().FontColor("B45309");x.Item().Text("STAT TESTS").FontSize(7).SemiBold();}));r.ConstantItem(8);r.RelativeItem().Element(c=>c.Border(1).BorderColor("FECACA").Padding(8).Column(x=>{x.Item().Text(data.OverdueCompleted.ToString()).FontSize(18).Bold().FontColor("B91C1C");x.Item().Text("OVERDUE AT COMPLETION").FontSize(7).SemiBold();}));});if(!data.Categories.Any())col.Item().PaddingTop(28).AlignCenter().Text("No completed tests were found in the selected period.").Italic().FontColor(Colors.Grey.Medium);foreach(var category in data.Categories){col.Item().PaddingTop(16).Text(category.Category).Bold().FontSize(12).FontColor("0C3D82");col.Item().PaddingTop(5).Table(t=>{t.ColumnsDefinition(c=>{c.RelativeColumn(2);c.RelativeColumn(3);c.RelativeColumn(1);c.RelativeColumn(2);c.RelativeColumn(2);});t.Header(h=>{foreach(var head in new[]{"Request","Test","Urgency","Completed","Quality"})h.Cell().Background("0D5AC6").Padding(5).Text(head).FontColor(Colors.White).Bold().FontSize(8);});foreach(var test in category.Tests){t.Cell().BorderBottom(1).BorderColor("E5E7EB").Padding(5).Text(test.RequestNumber);t.Cell().BorderBottom(1).BorderColor("E5E7EB").Padding(5).Text(test.TestName);t.Cell().BorderBottom(1).BorderColor("E5E7EB").Padding(5).Text(test.Urgency.ToUpperInvariant());t.Cell().BorderBottom(1).BorderColor("E5E7EB").Padding(5).Text(test.CompletedAt.ToString("dd MMM HH:mm"));t.Cell().BorderBottom(1).BorderColor("E5E7EB").Padding(5).Text(test.IsOverdue?"Overdue":test.IsAbnormal?"Abnormal":"Within target").FontColor(test.IsOverdue?"B91C1C":test.IsAbnormal?"B45309":"15803D");}});}});page.Footer().AlignCenter().Text(x=>{x.Span("NMB-HLabSys - Technician report | Page ").FontSize(8).FontColor(Colors.Grey.Medium);x.CurrentPageNumber();x.Span(" of ");x.TotalPages();});})).GeneratePdf();
    public async Task<ReceiveSamplesViewModel> GetReceiveSamplesAsync()
    { var tech=await TechAsync(); var samples=await _db.Samples.Include(x=>x.TestRequest).ThenInclude(x=>x.Patient).Include(x=>x.SampleType).Where(x=>x.Status=="Pending").OrderBy(x=>x.TestRequest!.RequestDate).ToListAsync(); return new(){Technician=Identity(tech),Samples=samples.Select(x=>new SampleQueueItemViewModel{SampleId=x.SampleID,RequestNumber=x.TestRequest!.RequestNumber,Patient=$"{x.TestRequest.Patient!.FirstName} {x.TestRequest.Patient.LastName}",Barcode=x.Barcode,SampleType=x.SampleType!.SampleTypeName,Status=x.Status,RequestedAt=x.TestRequest.RequestDate}).ToList()}; }
    public async Task ReceiveSampleAsync(int sampleId)
    { var tech=await TechAsync(); var sample=await _db.Samples.Include(x=>x.TestRequest).SingleOrDefaultAsync(x=>x.SampleID==sampleId) ?? throw new ArgumentException("Sample was not found."); if(sample.Status!="Pending") throw new InvalidOperationException("This sample has already been received."); await using var tx=await _db.Database.BeginTransactionAsync(); sample.Status="Received"; sample.DateReceived=DateTime.Now; sample.ReceivedByTechnicianID=tech.TechnicianID; if(await _db.Samples.Where(x=>x.RequestID==sample.RequestID).AllAsync(x=>x.Status=="Received")) sample.TestRequest!.Status="Samples Received"; await _db.SaveChangesAsync(); await tx.CommitAsync(); }
    public async Task<PerformTestsViewModel> GetPerformTestsAsync()
    { var tech=await TechAsync(); var rows=await Assigned(tech.TechnicianID).Where(x=>x.Status=="Submitted"||x.Status=="In Progress"||x.Status=="To Be Reviewed").ToListAsync(); return new(){Technician=Identity(tech),Tests=rows.Where(x=>x.AssignedTechnicianID==null||x.AssignedTechnicianID==tech.TechnicianID).Select(Map).ToList()}; }
    public async Task SelectTestAsync(int requestTestId)
    { var tech=await TechAsync(); var test=await Assigned(tech.TechnicianID).Include(x=>x.TestRequest).SingleOrDefaultAsync(x=>x.RequestTestID==requestTestId) ?? throw new UnauthorizedAccessException("You are not assigned to this test type."); if(test.AssignedTechnicianID.HasValue && test.AssignedTechnicianID!=tech.TechnicianID) throw new InvalidOperationException("This test is already selected by another technician."); if(test.Status!="Submitted" && test.Status!="To Be Reviewed") throw new InvalidOperationException("This test cannot be selected in its current state."); if(!await _db.Samples.AnyAsync(x=>x.RequestID==test.RequestID && x.Status=="Received")) throw new InvalidOperationException("Receive the request sample before starting a test."); var consumables=await _db.TestConsumables.Include(x=>x.Consumable).Where(x=>x.TestID==test.TestTypeID).ToListAsync(); var shortage=consumables.FirstOrDefault(x=>x.Consumable.QuantityOnHand<x.QuantityRequired); if(shortage is not null) throw new InvalidOperationException($"Insufficient stock for {shortage.Consumable.ConsumableName}."); await using var tx=await _db.Database.BeginTransactionAsync(); foreach(var item in consumables){var previous=item.Consumable.QuantityOnHand;item.Consumable.QuantityOnHand-=item.QuantityRequired;_db.StockAdjustmentLogs.Add(new StockAdjustmentLog{ConsumableID=item.ConsumableID,PreviousQuantity=previous,NewQuantity=item.Consumable.QuantityOnHand,AdjustmentAmount=-item.QuantityRequired,AdjustmentType="Test Usage",Reason=$"Used for request {test.TestRequest!.RequestNumber}, test {test.TestType!.TestName}",AdjustedBy=tech.UserID,AdjustmentDate=DateTime.Now,RelatedTestID=test.TestTypeID});} test.AssignedTechnicianID=tech.TechnicianID; test.StartDateTime=DateTime.Now; test.Status="In Progress"; test.TestRequest!.Status="In Progress"; await _db.SaveChangesAsync(); await tx.CommitAsync(); }
    public async Task CaptureResultAsync(CaptureResultViewModel model)
    { var tech=await TechAsync(); var test=await Assigned(tech.TechnicianID).Include(x=>x.TestType).Include(x=>x.TestRequest).SingleOrDefaultAsync(x=>x.RequestTestID==model.RequestTestId) ?? throw new UnauthorizedAccessException("You are not assigned to this test type."); if(test.AssignedTechnicianID!=tech.TechnicianID || (test.Status!="In Progress"&&test.Status!="To Be Reviewed")) throw new InvalidOperationException("Select this test before recording its result."); if(string.IsNullOrWhiteSpace(model.ResultValue)) throw new ArgumentException("A result value is required."); var abnormal=model.NumericValue.HasValue && ((test.TestType!.NormalRangeMin.HasValue&&model.NumericValue<test.TestType.NormalRangeMin)||(test.TestType.NormalRangeMax.HasValue&&model.NumericValue>test.TestType.NormalRangeMax)); await using var tx=await _db.Database.BeginTransactionAsync(); var result=await _db.TestResults.Where(x=>x.RequestTestID==test.RequestTestID).OrderByDescending(x=>x.ResultDate).FirstOrDefaultAsync(); if(result is null){ result=new TestResult{RequestTestID=test.RequestTestID}; _db.TestResults.Add(result); } result.ResultValue=model.ResultValue.Trim(); result.NumericValue=model.NumericValue; result.Notes=model.Notes?.Trim(); result.IsAbnormal=abnormal; result.ResultDate=DateTime.Now; test.Status="Completed"; test.CompletedDateTime=DateTime.Now; if(await _db.RequestTestTypes.Where(x=>x.RequestID==test.RequestID).AllAsync(x=>x.Status=="Completed"||x.Status=="Verified")) test.TestRequest!.Status="Completed"; await _db.SaveChangesAsync(); await tx.CommitAsync(); }
    public async Task<VerificationQueueViewModel> GetVerificationQueueAsync()
    { var tech=await TechAsync(); var rows=await Assigned(tech.TechnicianID).Include(x=>x.TestResults!).ThenInclude(x=>x.Verifications).Where(x=>x.Status=="Completed"&&x.AssignedTechnicianID!=tech.TechnicianID).ToListAsync(); return new(){Technician=Identity(tech),Results=rows.SelectMany(x=>x.TestResults!.OrderByDescending(r=>r.ResultDate).Take(1).Select(r=>new VerificationItemViewModel{ResultId=r.ResultID,RequestTestId=x.RequestTestID,RequestNumber=x.TestRequest!.RequestNumber,TestName=x.TestType!.TestName,PerformedBy=x.AssignedTechnicianID?.ToString()??"",ResultValue=r.ResultValue??"",IsAbnormal=r.IsAbnormal,Status=x.Status})).ToList()}; }
    public async Task VerifyResultAsync(VerifyResultViewModel model,bool approve)
    {
        var tech = await TechAsync();
        var result = await _db.TestResults
            .Include(x => x.RequestTestType)
            .ThenInclude(x => x!.TestRequest)
            .SingleOrDefaultAsync(x => x.ResultID == model.ResultId)
            ?? throw new ArgumentException("Result was not found.");

        var test = result.RequestTestType!;
        var assigned = await _db.TechnicianTestAssignments.AnyAsync(x =>
            x.TechnicianID == tech.TechnicianID && x.TestID == test.TestTypeID && x.IsActive);

        if (!assigned) throw new UnauthorizedAccessException("You are not assigned to verify this test type.");
        if (test.AssignedTechnicianID == tech.TechnicianID) throw new InvalidOperationException("A technician cannot verify their own result.");
        if (test.Status != "Completed") throw new InvalidOperationException("Only completed results may be verified.");
        if (!approve && string.IsNullOrWhiteSpace(model.ReviewNotes)) throw new ArgumentException("A review note is required when returning a result.");

        var requestId = test.RequestID;
        var requestFullyVerified = false;

        await using (var tx = await _db.Database.BeginTransactionAsync())
        {
            _db.TestVerifications.Add(new TestVerification
            {
                ResultID = result.ResultID,
                VerifiedByTechnicianID = tech.TechnicianID,
                VerificationDate = DateTime.Now,
                Status = approve ? "Verified" : "Returned",
                ReviewNotes = model.ReviewNotes?.Trim()
            });

            test.Status = approve ? "Verified" : "To Be Reviewed";
            requestFullyVerified = approve && !await _db.RequestTestTypes.AnyAsync(x =>
                x.RequestID == requestId &&
                x.RequestTestID != test.RequestTestID &&
                x.Status != "Verified");

            if (requestFullyVerified)
                test.TestRequest!.Status = "Verified";

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        // Send after committing clinical data: a temporary SMTP outage must not undo a valid verification.
        if (requestFullyVerified)
            await SendVerifiedResultsToDoctorAsync(requestId);
    }

    private async Task SendVerifiedResultsToDoctorAsync(int requestId)
    {
        var request = await _db.TestRequests
            .Include(x => x.Doctor).ThenInclude(x => x!.User)
            .Include(x => x.Patient)
            .Include(x => x.RequestTestTypes!).ThenInclude(x => x.TestType)
            .Include(x => x.RequestTestTypes!).ThenInclude(x => x.TestResults)
            .SingleAsync(x => x.RequestID == requestId);

        var doctorEmail = request.Doctor?.User?.Email;
        if (string.IsNullOrWhiteSpace(doctorEmail))
            throw new InvalidOperationException("The results were verified, but the requesting doctor's email address is unavailable.");

        var report = BuildVerifiedResultsPdf(request);
        var doctorName = $"{request.Doctor!.User.Name} {request.Doctor.User.Surname}".Trim();
        var body = $"<p>Dear Dr. {System.Net.WebUtility.HtmlEncode(doctorName)},</p>" +
                   $"<p>All tests for request <strong>{System.Net.WebUtility.HtmlEncode(request.RequestNumber)}</strong> have been independently verified.</p>" +
                   "<p>The verified result report is attached.</p><p>Regards,<br/>NMB-HLabSys Laboratory</p>";

        await _emailService.SendEmailWithAttachmentAsync(
            doctorEmail,
            $"Verified laboratory results — {request.RequestNumber}",
            body,
            report,
            $"Verified-Results-{request.RequestNumber}.pdf");
    }

    private static byte[] BuildVerifiedResultsPdf(TestRequest request) => Document.Create(document => document.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(30);
        page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));
        page.Header().Column(column =>
        {
            column.Item().Text("NMB-HLabSys Laboratory").Bold().FontSize(18).FontColor("0C3D82");
            column.Item().Text("Verified laboratory result report").SemiBold().FontSize(12).FontColor("176FE4");
            column.Item().PaddingTop(6).Text($"Request: {request.RequestNumber} | Generated: {DateTime.Now:dd MMM yyyy HH:mm}");
        });
        page.Content().PaddingVertical(16).Column(column =>
        {
            column.Item().Text($"Patient: {request.Patient?.FirstName} {request.Patient?.LastName}").Bold();
            column.Item().Text($"Urgency: {request.Urgency.ToUpperInvariant()}");
            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor("CBD5E1");
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns => { columns.RelativeColumn(3); columns.RelativeColumn(2); columns.RelativeColumn(2); columns.RelativeColumn(2); });
                table.Header(header =>
                {
                    foreach (var heading in new[] { "Test", "Result", "Flag", "Completed" })
                        header.Cell().Background("0D5AC6").Padding(5).Text(heading).FontColor(Colors.White).Bold();
                });

                foreach (var test in request.RequestTestTypes ?? [])
                {
                    var result = test.TestResults?.OrderByDescending(x => x.ResultDate).FirstOrDefault();
                    table.Cell().BorderBottom(1).BorderColor("E5E7EB").Padding(5).Text(test.TestType?.TestName ?? "Test");
                    table.Cell().BorderBottom(1).BorderColor("E5E7EB").Padding(5).Text(result?.ResultValue ?? "—");
                    table.Cell().BorderBottom(1).BorderColor("E5E7EB").Padding(5).Text(result?.IsAbnormal == true ? "Abnormal" : "Normal");
                    table.Cell().BorderBottom(1).BorderColor("E5E7EB").Padding(5).Text(test.CompletedDateTime?.ToString("dd MMM yyyy HH:mm") ?? "—");
                }
            });
        });
        page.Footer().AlignCenter().Text("NMB-HLabSys • Confidential verified laboratory result report").FontSize(8).FontColor(Colors.Grey.Medium);
    })).GeneratePdf();
}


