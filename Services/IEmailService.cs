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
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly ApplicationDbContext _context;  // Added this

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger,
            ApplicationDbContext context)  // Added context parameter
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

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
            var subject = "Welcome to NMBHDL - Patient Portal Account Created";
            var body = $@"
                <h2>Welcome to Nelson Mandela Bay Haematology Diagnostic Laboratories</h2>
                <p>Dear {name},</p>
                <p>Your patient portal account has been created. Please find your login details below:</p>
                <p><strong>Email:</strong> {email}</p>
                <p><strong>Password:</strong> {password}</p>
                <p><strong>Important:</strong> Please change your password on your first login.</p>
                <p><a href='{GetBaseUrl()}/Account/Login'>Click here to login</a></p>
                <p>Regards,<br/>NMBHDL Management</p>
            ";

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

                // Send to all managers and admins
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

        private string GetBaseUrl()
        {
            return _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
        }
    }
}