using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NMB_HLabSys.Models.ViewModels;
using NMB_HLabSys.Models;
using NMB_HLabSys.Data;

namespace NMB_HLabSys.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
        Task SendOrderEmailAsync(PurchaseOrder order);
        Task SendOrderCancellationEmailAsync(PurchaseOrder order, string reason);
        Task SendTechnicianWelcomeEmailAsync(string email, string name, string password);
        Task SendDoctorWelcomeEmailAsync(string email, string name, string password);
        Task SendPatientWelcomeEmailAsync(string email, string name, string password);
        Task SendTestResultEmailAsync(string toEmail, string patientName, string testResults);
        Task SendLowStockAlertEmailAsync(List<LowStockAlertViewModel> alerts);
        Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink);

        // ═══════════════ DOCTOR SUBSYSTEM EMAILS ═══════════════

        /// <summary>Sent when a doctor submits a new test request.</summary>
        Task SendTestRequestSubmittedEmailAsync(
            string toEmail,
            string patientName,
            string requestNumber,
            string doctorName,
            DateTime requestDate,
            string urgency,
            IEnumerable<string> testNames,
            string clinicalNotes);

        /// <summary>Sent when a doctor releases results (email delivery).</summary>
        Task SendResultReleasedEmailAsync(
            string toEmail,
            string patientName,
            string doctorName,
            string requestNumber,
            string note,
            IEnumerable<ReleasedTestEmailItem> testResults);

        /// <summary>Sent when a doctor requests the patient book an appointment.</summary>
        Task SendAppointmentRequestEmailAsync(
            string toEmail,
            string patientName,
            string doctorName,
            string requestNumber,
            string note);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
             _httpContextAccessor = httpContextAccessor;
        }

        // ═══════════════════════════════════════════════════════════
        // CORE SMTP SENDER
        // ═══════════════════════════════════════════════════════════

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("EmailSettings");
                var smtpClient = new SmtpClient(smtpSettings["Host"])
                {
                    Port = int.Parse(smtpSettings["Port"] ?? "587"),
                    Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                    EnableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true")
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpSettings["FromEmail"], smtpSettings["FromName"]),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent to {toEmail}: {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {toEmail}: {subject}");
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ORDER / INVENTORY EMAILS
        // ═══════════════════════════════════════════════════════════

        public async Task SendOrderEmailAsync(PurchaseOrder order)
        {
            var subject = $"Purchase Order #{order.OrderNumber} Created";
            var body = $@"
                <h2>Purchase Order #{order.OrderNumber}</h2>
                <p><strong>Supplier:</strong> {order.Supplier?.SupplierName}</p>
                <p><strong>Order Date:</strong> {order.OrderDate:dd/MM/yyyy}</p>
                <p><strong>Expected Delivery:</strong> {order.ExpectedDeliveryDate?.ToString("dd/MM/yyyy") ?? "Not specified"}</p>
                <h3>Items Ordered:</h3>
                <table border='1' cellpadding='5'>
                    <tr><th>Item</th><th>Quantity</th><th>Unit Price</th><th>Total</th></tr>
                    {string.Join("", order.OrderItems.Select(oi => $@"
                        <tr>
                            <td>{oi.Consumable?.ConsumableName}</td>
                            <td>{oi.QuantityOrdered}</td>
                            <td>R{oi.UnitPrice:N2}</td>
                            <td>R{(oi.QuantityOrdered * (oi.UnitPrice ?? 0)):N2}</td>
                        </tr>"))}
                </table>
                <p><strong>Total Amount:</strong> R{order.TotalAmount:N2}</p>
                <p>Please confirm receipt of this order.</p>
            ";

            if (!string.IsNullOrEmpty(order.Supplier?.EmailAddress))
            {
                await SendEmailAsync(order.Supplier.EmailAddress, subject, body);
            }
            else
            {
                _logger.LogWarning($"No email address for supplier: {order.Supplier?.SupplierName}");
            }
        }

        public async Task SendOrderCancellationEmailAsync(PurchaseOrder order, string reason)
        {
            var subject = $"Purchase Order #{order.OrderNumber} Cancelled";
            var body = $@"
                <h2>Purchase Order #{order.OrderNumber} Cancelled</h2>
                <p><strong>Supplier:</strong> {order.Supplier?.SupplierName}</p>
                <p><strong>Cancellation Date:</strong> {DateTime.Now:dd/MM/yyyy}</p>
                <p><strong>Reason:</strong> {reason}</p>
                <p>Please disregard the previous order.</p>
            ";

            if (!string.IsNullOrEmpty(order.Supplier?.EmailAddress))
            {
                await SendEmailAsync(order.Supplier.EmailAddress, subject, body);
            }
            else
            {
                _logger.LogWarning($"No email address for supplier: {order.Supplier?.SupplierName}");
            }
        }

        public async Task SendLowStockAlertEmailAsync(List<LowStockAlertViewModel> alerts)
        {
            if (alerts == null || !alerts.Any())
            {
                _logger.LogInformation("No low stock alerts to send.");
                return;
            }

            try
            {
                var subject = "⚠️ Low Stock Alert - NMBHDL Inventory";
                var body = $@"
                    <h2>Low Stock Alert</h2>
                    <p>The following items are below reorder level:</p>
                    <table border='1' cellpadding='5'>
                        <tr><th>Item</th><th>Current Stock</th><th>Reorder Level</th><th>Supplier</th></tr>
                        {string.Join("", alerts.Select(a => $@"
                            <tr>
                                <td>{a.ConsumableName}</td>
                                <td style='color: {(a.AlertLevel == "Critical" ? "red" : "orange")};'>{a.QuantityOnHand}</td>
                                <td>{a.ReorderLevel}</td>
                                <td>{a.SupplierName}</td>
                            </tr>"))}
                    </table>
                    <p>Please take immediate action to replenish stock.</p>
                    <p><a href='{GetBaseUrl()}/Manager/StockManagement'>View Stock Management</a></p>
                ";

                var recipients = await _context.Users
                    .Where(u => (u.UserRole == "Manager" || u.UserRole == "Admin") && u.IsActive)
                    .Select(u => u.Email)
                    .ToListAsync();

                if (recipients.Any())
                {
                    foreach (var recipient in recipients)
                    {
                        if (!string.IsNullOrEmpty(recipient))
                        {
                            await SendEmailAsync(recipient, subject, body);
                        }
                    }
                    _logger.LogInformation($"Low stock alert sent to {recipients.Count} recipients");
                }
                else
                {
                    _logger.LogWarning("No active managers or admins found to send low stock alerts.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending low stock alert emails");
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ACCOUNT / WELCOME EMAILS
        // ═══════════════════════════════════════════════════════════

        public async Task SendTechnicianWelcomeEmailAsync(string email, string name, string password)
        {
            var subject = "Welcome to NMBHDL - Technician Account Created";
            var body = $@"
                <h2>Welcome to Nelson Mandela Bay Haematology Diagnostic Laboratories</h2>
                <p>Dear {name},</p>
                <p>Your technician account has been created. Please find your login details below:</p>
                <p><strong>Email:</strong> {email}</p>
                <p><strong>Password:</strong> {password}</p>
                <p><strong>Important:</strong> Please change your password on your first login.</p>
                <p><a href='{GetBaseUrl()}/Account/Login'>Click here to login</a></p>
                <p>Regards,<br/>NMBHDL Management</p>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendDoctorWelcomeEmailAsync(string email, string name, string password)
        {
            var subject = "Welcome to NMBHDL - Doctor Account Created";
            var body = $@"
                <h2>Welcome to Nelson Mandela Bay Haematology Diagnostic Laboratories</h2>
                <p>Dear Dr. {name},</p>
                <p>Your doctor account has been created. Please find your login details below:</p>
                <p><strong>Email:</strong> {email}</p>
                <p><strong>Password:</strong> {password}</p>
                <p><strong>Important:</strong> Please change your password on your first login.</p>
                <p><a href='{GetBaseUrl()}/Account/Login'>Click here to login</a></p>
                <p>Regards,<br/>NMBHDL Management</p>
            ";

            await SendEmailAsync(email, subject, body);
        }
        public async Task SendPatientWelcomeEmailAsync(string email, string name, string password)
        {
            var subject = "Welcome to NMBHDL — Patient Portal Account Created";

            var body = $@"
<html>
<body style='font-family: Arial, Helvetica, sans-serif; color:#1A2C3A; max-width:640px; margin:0 auto; background:#F1F5F9; padding:1rem;'>

  <div style='background:linear-gradient(135deg,#0841C9,#1D97F1); color:white; padding:1.6rem; border-radius:12px 12px 0 0;'>
    <h2 style='margin:0; font-size:1.4rem;'>Welcome to the Patient Portal</h2>
    <p style='margin:0.4rem 0 0; font-size:0.88rem; opacity:0.92;'>
      Nelson Mandela Bay Haematology Diagnostic Laboratories
    </p>
  </div>

  <div style='background:#FFFFFF; padding:1.6rem; border:1px solid #E2E8F0; border-top:none;'>

    <p>Dear <strong>{WebUtility.HtmlEncode(name)}</strong>,</p>

    <p>
      Your patient portal account has been created. From the portal you will be able to
      track test requests, download signed results as PDFs, and keep your medical history
      up to date.
    </p>

    <p style='margin-bottom:0.4rem;'><strong>Your login details</strong></p>

    <table style='width:100%; border-collapse:collapse; background:#F8FAFE; border-radius:10px; overflow:hidden; margin:0.6rem 0 1rem;'>
      <tr>
        <td style='padding:0.7rem 1rem; background:#F1F5F9; font-weight:600; width:38%; color:#5A6E7A;'>Email (username)</td>
        <td style='padding:0.7rem 1rem; font-family:monospace; font-weight:700;'>{WebUtility.HtmlEncode(email)}</td>
      </tr>
      <tr>
        <td style='padding:0.7rem 1rem; background:#F1F5F9; font-weight:600; color:#5A6E7A;'>Temporary password</td>
        <td style='padding:0.7rem 1rem; font-family:monospace; font-weight:700; color:#032C7D;'>{WebUtility.HtmlEncode(password)}</td>
      </tr>
    </table>

    <div style='background:#FEF3C7; border-left:4px solid #F59E0B; padding:0.85rem 1rem; border-radius:6px; font-size:0.88rem; margin:1rem 0;'>
      <strong>⚠ Important</strong><br/>
      You will be asked to change your password the first time you log in.
      Your new password must be at least 8 characters and contain one uppercase letter,
      one lowercase letter, one number and one special character
      (for example <span style='font-family:monospace;'>Nmb#Pass2025</span>).
    </div>

    <p style='margin-bottom:0.4rem;'><strong>How to log in</strong></p>
    <ol style='margin-top:0; padding-left:1.2rem; font-size:0.9rem;'>
      <li>Open the NMB Haematology Patient Portal in your browser.</li>
      <li>Enter the email address above as your username.</li>
      <li>Enter the temporary password shown above.</li>
      <li>When prompted, choose a new password that only you know.</li>
    </ol>

    <p style='font-size:0.85rem; color:#6c757d;'>
      If you lose this email, please contact the practice or doctor who registered
      you so they can re-issue your temporary password.
    </p>

    <p style='margin-top:1.5rem; font-size:0.85rem; color:#6c757d;'>
      Kind regards,<br/>
      <strong>NMB Haematology Diagnostic Laboratories</strong>
    </p>
  </div>

  <div style='background:#020D2F; color:#CBD5E1; padding:1rem 1.6rem; border-radius:0 0 12px 12px; font-size:0.75rem; text-align:center;'>
    Aligned with UN SDG 3 — Good Health and Well-Being<br/>
    This is an automated message. Please do not reply to this email.
  </div>

</body>
</html>";

            await SendEmailAsync(email, subject, body);
        }


        public async Task SendTestResultEmailAsync(string toEmail, string patientName, string testResults)
        {
            var subject = $"Test Results Available for {patientName}";
            var body = $@"
                <h2>Test Results Available</h2>
                <p>Dear Patient,</p>
                <p>Your test results are now available. Please log in to your patient portal to view them.</p>
                <p><strong>Patient:</strong> {patientName}</p>
                <p><a href='{GetBaseUrl()}/Patient/Results'>View Results</a></p>
                <p>Regards,<br/>NMBHDL Team</p>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink)
        {
            var subject = "Password Reset - NMB-HLabSys";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Password Reset Request</h2>
                    <p>Dear {userName},</p>
                    <p>We received a request to reset your password for the NMB Haematology Laboratory Test Request Management System.</p>
                    <p>Click the link below to reset your password (valid for 24 hours):</p>
                    <p><a href='{resetLink}' style='background: #0d6efd; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                    <p>If you did not request this, please ignore this email.</p>
                    <hr />
                    <p style='color: #6c757d; font-size: 12px;'>Nelson Mandela Bay Haematology Diagnostic Laboratories</p>
                    <p style='color: #6c757d; font-size: 12px;'>UN SDG 3: Good Health and Well-Being</p>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        // ═══════════════════════════════════════════════════════════
        // DOCTOR SUBSYSTEM EMAILS
        // ═══════════════════════════════════════════════════════════

        public async Task SendTestRequestSubmittedEmailAsync(
     string toEmail,
     string patientName,
     string requestNumber,
     string doctorName,
     DateTime requestDate,
     string urgency,
     IEnumerable<string> testNames,
     string clinicalNotes)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("No patient email for request {RequestNumber}", requestNumber);
                return;
            }

            var urgencyColor = urgency?.ToLower() switch
            {
                "stat" => "#DC2626",
                "urgent" => "#F59E0B",
                _ => "#10B981"
            };
            var urgencyLabel = (urgency ?? "routine").ToUpper();

            var subject = $"Test Request {requestNumber} Submitted — NMB Haematology";

            var testList = testNames?.ToList() ?? new List<string>();
            var testsHtml = testList.Count == 0
                ? "<li style='color:#6c757d; font-style:italic;'>No tests listed.</li>"
                : string.Join("", testList.Select(t =>
                    $"<li style='margin:0.25rem 0;'>{WebUtility.HtmlEncode(t)}</li>"));

            var notesHtml = string.IsNullOrWhiteSpace(clinicalNotes)
                ? "<em style='color:#6c757d;'>No clinical notes were provided for this request.</em>"
                : WebUtility.HtmlEncode(clinicalNotes);

            var body = $@"
<html>
<body style='font-family: Arial, Helvetica, sans-serif; color:#1A2C3A; max-width:640px; margin:0 auto; background:#F1F5F9; padding:1rem;'>

  <div style='background:linear-gradient(135deg,#0841C9,#1D97F1); color:white; padding:1.6rem; border-radius:12px 12px 0 0;'>
    <h2 style='margin:0; font-size:1.4rem;'>Test Request Submitted</h2>
    <p style='margin:0.4rem 0 0; font-size:0.88rem; opacity:0.92;'>
      Nelson Mandela Bay Haematology Diagnostic Laboratories
    </p>
  </div>

  <div style='background:#FFFFFF; padding:1.6rem; border:1px solid #E2E8F0; border-top:none;'>

    <p>Dear <strong>{WebUtility.HtmlEncode(patientName)}</strong>,</p>

    <p>
      A test request has been submitted on your behalf by
      <strong>Dr.&nbsp;{WebUtility.HtmlEncode(doctorName)}</strong>.
      The laboratory has been notified and will begin processing your request shortly.
    </p>

    <table style='width:100%; border-collapse:collapse; margin:1.2rem 0; background:#F8FAFE; border-radius:10px; overflow:hidden;'>
      <tr>
        <td style='padding:0.7rem 1rem; font-weight:600; width:38%; color:#5A6E7A;'>Request Number</td>
        <td style='padding:0.7rem 1rem; font-family:monospace; font-weight:700; color:#032C7D;'>
          {WebUtility.HtmlEncode(requestNumber)}
        </td>
      </tr>
      <tr>
        <td style='padding:0.7rem 1rem; font-weight:600; color:#5A6E7A;'>Request Date</td>
        <td style='padding:0.7rem 1rem;'>{requestDate:dd MMM yyyy, HH:mm}</td>
      </tr>
      <tr>
        <td style='padding:0.7rem 1rem; font-weight:600; color:#5A6E7A;'>Urgency</td>
        <td style='padding:0.7rem 1rem;'>
          <span style='display:inline-block; padding:0.22rem 0.75rem; border-radius:30px; font-size:0.75rem; font-weight:700; color:white; background:{urgencyColor};'>
            {urgencyLabel}
          </span>
        </td>
      </tr>
    </table>

    <p style='margin-bottom:0.4rem;'><strong>Tests Requested</strong></p>
    <ul style='margin-top:0; padding-left:1.2rem;'>{testsHtml}</ul>

    <p style='margin-bottom:0.4rem;'><strong>Clinical Notes from Your Doctor</strong></p>
    <div style='background:#FEF9C3; border-left:4px solid #F59E0B; padding:0.75rem 1rem; border-radius:6px; font-size:0.88rem; margin-bottom:1.2rem;'>
      {notesHtml}
    </div>

    <p style='margin-bottom:0.4rem;'><strong>What happens next?</strong></p>
    <ol style='margin-top:0; padding-left:1.2rem; font-size:0.9rem;'>
      <li>The laboratory will receive your sample(s) and confirm receipt.</li>
      <li>A qualified technician will process the requested test(s).</li>
      <li>Your results will be verified and reviewed by your doctor.</li>
      <li>Once released, the results will appear in your patient portal.</li>
    </ol>

    <div style='background:#EFF6FF; border-left:4px solid #0841C9; padding:0.85rem 1rem; border-radius:6px; font-size:0.88rem; margin:1.2rem 0;'>
      <strong>Need to check progress?</strong><br/>
      Log in to the NMB Haematology Patient Portal with your email address and password,
      and open the <em>Dashboard</em> to see the live status of this request.
      If you have not yet received your portal login details, please contact the practice
      that registered you.
    </div>

    <p style='font-size:0.85rem; color:#6c757d;'>
      If you did not expect this test request or believe it was raised in error,
      please contact Dr.&nbsp;{WebUtility.HtmlEncode(doctorName)} directly so it can be
      cancelled before the sample is processed.
    </p>

    <p style='margin-top:1.5rem; font-size:0.85rem; color:#6c757d;'>
      Kind regards,<br/>
      <strong>NMB Haematology Diagnostic Laboratories</strong>
    </p>
  </div>

  <div style='background:#020D2F; color:#CBD5E1; padding:1rem 1.6rem; border-radius:0 0 12px 12px; font-size:0.75rem; text-align:center;'>
    Aligned with UN SDG 3 — Good Health and Well-Being<br/>
    This is an automated message. Please do not reply to this email.
  </div>

</body>
</html>";

            try
            {
                await SendEmailAsync(toEmail, subject, body);
                _logger.LogInformation("Test request submitted email sent to {Email} for {RequestNumber}", toEmail, requestNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test request submitted email to {Email}", toEmail);
            }
        }

        public async Task SendResultReleasedEmailAsync(
     string toEmail,
     string patientName,
     string doctorName,
     string requestNumber,
     string note,
     IEnumerable<ReleasedTestEmailItem> testResults)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("No patient email for released result on {RequestNumber}", requestNumber);
                return;
            }

            var resultsList = testResults?.ToList() ?? new List<ReleasedTestEmailItem>();
            var abnormalCount = resultsList.Count(r => r.IsAbnormal);

            var rowsHtml = resultsList.Count == 0
                ? "<tr><td colspan='3' style='padding:0.7rem; text-align:center; color:#6c757d; font-style:italic;'>No individual results attached.</td></tr>"
                : string.Join("", resultsList.Select(r =>
                {
                    var rowBg = r.IsAbnormal ? "#FEF2F2" : "#FFFFFF";
                    var valueColor = r.IsAbnormal ? "#DC2626" : "#059669";
                    var flag = r.IsAbnormal
                        ? "<span style='color:#DC2626; font-weight:700;'> &nbsp;⚠</span>"
                        : "";

                    return $@"
              <tr style='background:{rowBg};'>
                <td style='padding:0.6rem 0.8rem; border-bottom:1px solid #E2E8F0; font-weight:600;'>
                  {WebUtility.HtmlEncode(r.TestName)}
                </td>
                <td style='padding:0.6rem 0.8rem; border-bottom:1px solid #E2E8F0; font-family:monospace; font-weight:700; color:{valueColor};'>
                  {WebUtility.HtmlEncode(r.ResultValue)} {WebUtility.HtmlEncode(r.Unit)}{flag}
                </td>
                <td style='padding:0.6rem 0.8rem; border-bottom:1px solid #E2E8F0; font-size:0.8rem; color:#5A6E7A;'>
                  {WebUtility.HtmlEncode(r.NormalRange)}
                </td>
              </tr>";
                }));

            var noteHtml = string.IsNullOrWhiteSpace(note)
                ? ""
                : $@"
          <p style='margin:1.2rem 0 0.4rem;'><strong>Note from Dr.&nbsp;{WebUtility.HtmlEncode(doctorName)}</strong></p>
          <div style='background:#EFF6FF; border-left:4px solid #0841C9; padding:0.85rem 1rem; border-radius:6px; font-size:0.9rem;'>
            {WebUtility.HtmlEncode(note)}
          </div>";

            var abnormalBanner = abnormalCount > 0
                ? $@"
          <div style='background:#FEE2E2; border-left:4px solid #DC2626; padding:0.9rem 1rem; border-radius:6px; margin:1.2rem 0; font-size:0.88rem; color:#991B1B;'>
            <strong>⚠ Attention needed</strong><br/>
            {abnormalCount} of your result{(abnormalCount == 1 ? " is" : "s are")} outside the normal reference range.
            Please arrange a consultation with Dr.&nbsp;{WebUtility.HtmlEncode(doctorName)} to discuss
            {(abnormalCount == 1 ? "this finding" : "these findings")} in context.
          </div>"
                : "";

            var subject = $"Your Test Results Are Ready — {requestNumber}";

            var body = $@"
<html>
<body style='font-family: Arial, Helvetica, sans-serif; color:#1A2C3A; max-width:640px; margin:0 auto; background:#F1F5F9; padding:1rem;'>

  <div style='background:linear-gradient(135deg,#10B981,#059669); color:white; padding:1.6rem; border-radius:12px 12px 0 0;'>
    <h2 style='margin:0; font-size:1.4rem;'>Test Results Released</h2>
    <p style='margin:0.4rem 0 0; font-size:0.88rem; opacity:0.92;'>
      Nelson Mandela Bay Haematology Diagnostic Laboratories
    </p>
  </div>

  <div style='background:#FFFFFF; padding:1.6rem; border:1px solid #E2E8F0; border-top:none;'>

    <p>Dear <strong>{WebUtility.HtmlEncode(patientName)}</strong>,</p>

    <p>
      Dr.&nbsp;<strong>{WebUtility.HtmlEncode(doctorName)}</strong> has reviewed your recent
      laboratory results for request <strong>{WebUtility.HtmlEncode(requestNumber)}</strong>
      and has released them to you.
    </p>

    {noteHtml}
    {abnormalBanner}

    <p style='margin:1.2rem 0 0.4rem;'><strong>Summary of Results</strong></p>

    <table style='width:100%; border-collapse:collapse; background:white; border:1px solid #E2E8F0; border-radius:10px; overflow:hidden;'>
      <thead>
        <tr style='background:#020D2F; color:white;'>
          <th style='padding:0.65rem 0.8rem; text-align:left; font-size:0.78rem;'>Test</th>
          <th style='padding:0.65rem 0.8rem; text-align:left; font-size:0.78rem;'>Result</th>
          <th style='padding:0.65rem 0.8rem; text-align:left; font-size:0.78rem;'>Normal Range</th>
        </tr>
      </thead>
      <tbody>{rowsHtml}</tbody>
    </table>

    <div style='background:#FEF3C7; border-left:4px solid #F59E0B; padding:0.85rem 1rem; border-radius:6px; font-size:0.85rem; margin:1.2rem 0;'>
      <strong>Please note</strong><br/>
      The summary above is for information only and does not replace a professional consultation.
      The complete, signed PDF report is available on your patient portal. Log in with your
      registered email address and password, then open the <em>Dashboard</em> to download it.
    </div>

    <p style='margin-bottom:0.4rem;'><strong>What you can do next</strong></p>
    <ul style='margin-top:0; padding-left:1.2rem; font-size:0.9rem;'>
      <li>Review the full results in your patient portal.</li>
      <li>Download a signed PDF copy for your records.</li>
      <li>Contact Dr.&nbsp;{WebUtility.HtmlEncode(doctorName)} if you have any questions or concerns about the results.</li>
    </ul>

    <p style='font-size:0.85rem; color:#6c757d; margin-top:1.2rem;'>
      If you cannot access your portal account, please contact the practice that registered
      you so they can help you regain access.
    </p>

    <p style='margin-top:1.5rem; font-size:0.85rem; color:#6c757d;'>
      Kind regards,<br/>
      <strong>NMB Haematology Diagnostic Laboratories</strong>
    </p>
  </div>

  <div style='background:#020D2F; color:#CBD5E1; padding:1rem 1.6rem; border-radius:0 0 12px 12px; font-size:0.75rem; text-align:center;'>
    Aligned with UN SDG 3 — Good Health and Well-Being<br/>
    This is an automated message. Please do not reply to this email.
  </div>

</body>
</html>";

            try
            {
                await SendEmailAsync(toEmail, subject, body);
                _logger.LogInformation("Result released email sent to {Email} for {RequestNumber}", toEmail, requestNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send result released email to {Email}", toEmail);
            }
        }
        public async Task SendAppointmentRequestEmailAsync(
     string toEmail,
     string patientName,
     string doctorName,
     string requestNumber,
     string note)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("No patient email for appointment request on {RequestNumber}", requestNumber);
                return;
            }

            var noteHtml = string.IsNullOrWhiteSpace(note)
                ? ""
                : $@"
          <p style='margin:1.2rem 0 0.4rem;'><strong>Message from Dr.&nbsp;{WebUtility.HtmlEncode(doctorName)}</strong></p>
          <div style='background:#EFF6FF; border-left:4px solid #0841C9; padding:0.85rem 1rem; border-radius:6px; font-size:0.9rem;'>
            {WebUtility.HtmlEncode(note)}
          </div>";

            var subject = $"Please Book an Appointment to Discuss Your Results — {requestNumber}";

            var body = $@"
<html>
<body style='font-family: Arial, Helvetica, sans-serif; color:#1A2C3A; max-width:640px; margin:0 auto; background:#F1F5F9; padding:1rem;'>

  <div style='background:linear-gradient(135deg,#F59E0B,#D97706); color:white; padding:1.6rem; border-radius:12px 12px 0 0;'>
    <h2 style='margin:0; font-size:1.4rem;'>Appointment Requested</h2>
    <p style='margin:0.4rem 0 0; font-size:0.88rem; opacity:0.92;'>
      Nelson Mandela Bay Haematology Diagnostic Laboratories
    </p>
  </div>

  <div style='background:#FFFFFF; padding:1.6rem; border:1px solid #E2E8F0; border-top:none;'>

    <p>Dear <strong>{WebUtility.HtmlEncode(patientName)}</strong>,</p>

    <p>
      Dr.&nbsp;<strong>{WebUtility.HtmlEncode(doctorName)}</strong> has reviewed your test results
      for request <strong>{WebUtility.HtmlEncode(requestNumber)}</strong> and would like to
      see you in person to discuss them.
    </p>

    {noteHtml}

    <div style='background:#FEF3C7; border-left:4px solid #F59E0B; padding:0.9rem 1rem; border-radius:6px; font-size:0.88rem; margin:1.2rem 0;'>
      <strong>Please contact Dr.&nbsp;{WebUtility.HtmlEncode(doctorName)}'s practice to arrange an appointment.</strong><br/>
      Your results will be formally released to you once you have been seen.
    </div>

    <p style='margin-bottom:0.4rem;'><strong>Why am I being asked to come in?</strong></p>
    <p style='font-size:0.9rem; margin-top:0;'>
      Some laboratory results are best explained in a face-to-face consultation, where
      your doctor can discuss the findings in context, answer any questions you may have,
      and, where necessary, plan the next steps with you.
    </p>

    <p style='margin-bottom:0.4rem;'><strong>What to bring</strong></p>
    <ul style='margin-top:0; padding-left:1.2rem; font-size:0.9rem;'>
      <li>Your ID document or driver's licence.</li>
      <li>Any current medication you are taking.</li>
      <li>A list of questions you may want to ask.</li>
    </ul>

    <p style='font-size:0.85rem; color:#6c757d; margin-top:1.2rem;'>
      If you are unable to attend in person, please contact the practice to make an
      alternative arrangement.
    </p>

    <p style='margin-top:1.5rem; font-size:0.85rem; color:#6c757d;'>
      Kind regards,<br/>
      <strong>NMB Haematology Diagnostic Laboratories</strong>
    </p>
  </div>

  <div style='background:#020D2F; color:#CBD5E1; padding:1rem 1.6rem; border-radius:0 0 12px 12px; font-size:0.75rem; text-align:center;'>
    Aligned with UN SDG 3 — Good Health and Well-Being<br/>
    This is an automated message. Please do not reply to this email.
  </div>

</body>
</html>";

            try
            {
                await SendEmailAsync(toEmail, subject, body);
                _logger.LogInformation("Appointment request email sent to {Email} for {RequestNumber}", toEmail, requestNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send appointment request email to {Email}", toEmail);
            }
        }
        // ═══════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════
        private string GetBaseUrl()
        {
            // 1. Config override wins (use this in production)
            var configured = _configuration["AppSettings:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(configured))
                return configured.TrimEnd('/');

            // 2. Otherwise, derive from the current HTTP request
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null && request.Host.HasValue)
            {
                return $"{request.Scheme}://{request.Host}".TrimEnd('/');
            }

            // 3. Last-resort fallback (should never be reached from a request pipeline)
            return "https://localhost:5001";
        }

    }

    // ═══════════════════════════════════════════════════════════
    // DTO — namespace level, NOT nested in EmailService
    // ═══════════════════════════════════════════════════════════
    public class ReleasedTestEmailItem
    {
        public string TestName { get; set; } = string.Empty;
        public string ResultValue { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string NormalRange { get; set; } = string.Empty;
        public bool IsAbnormal { get; set; }
    }
}