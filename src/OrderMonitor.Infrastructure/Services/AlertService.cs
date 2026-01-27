using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderMonitor.Core.Configuration;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Models;
using OrderMonitor.Infrastructure.Security;

namespace OrderMonitor.Infrastructure.Services;

/// <summary>
/// Service for sending stuck order alerts via email.
/// </summary>
public class AlertService : IAlertService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly AlertSettings _alertSettings;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        IOptions<SmtpSettings> smtpSettings,
        IOptions<AlertSettings> alertSettings,
        ILogger<AlertService> logger)
    {
        _smtpSettings = smtpSettings?.Value ?? throw new ArgumentNullException(nameof(smtpSettings));
        _alertSettings = alertSettings?.Value ?? throw new ArgumentNullException(nameof(alertSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SendStuckOrdersAlertAsync(
        StuckOrdersSummary summary,
        IEnumerable<StuckOrderDto> topOrders,
        CancellationToken cancellationToken = default)
    {
        if (!_alertSettings.Enabled)
        {
            _logger.LogDebug("Alerts are disabled, skipping email");
            return;
        }

        if (!_alertSettings.Recipients.Any())
        {
            _logger.LogWarning("No alert recipients configured, skipping email");
            return;
        }

        var subject = $"{_alertSettings.SubjectPrefix} {summary.TotalStuckOrders} Stuck Orders Detected";
        var body = BuildAlertEmailBody(summary, topOrders);

        await SendEmailAsync(subject, body, _alertSettings.Recipients, cancellationToken);

        _logger.LogInformation(
            "Sent stuck orders alert to {RecipientCount} recipients for {OrderCount} stuck orders",
            _alertSettings.Recipients.Count,
            summary.TotalStuckOrders);
    }

    /// <inheritdoc />
    public async Task SendTestAlertAsync(string recipientEmail, CancellationToken cancellationToken = default)
    {
        var subject = $"{_alertSettings.SubjectPrefix} Test Alert";
        var body = BuildTestEmailBody();

        await SendEmailAsync(subject, body, new[] { recipientEmail }, cancellationToken);

        _logger.LogInformation("Sent test alert to {Recipient}", recipientEmail);
    }

    private string BuildAlertEmailBody(StuckOrdersSummary summary, IEnumerable<StuckOrderDto> topOrders)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
        sb.AppendLine("<h2 style='color: #d9534f;'>⚠️ Stuck Orders Alert</h2>");
        sb.AppendLine($"<p><strong>Generated:</strong> {summary.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC</p>");

        // Summary section
        sb.AppendLine("<h3>Summary</h3>");
        sb.AppendLine($"<p><strong>Total Stuck Orders:</strong> <span style='color: #d9534f; font-size: 1.5em;'>{summary.TotalStuckOrders}</span></p>");

        // By threshold
        sb.AppendLine("<h4>By Threshold</h4>");
        sb.AppendLine("<table style='border-collapse: collapse; width: 100%; max-width: 500px;'>");
        sb.AppendLine("<tr style='background-color: #f5f5f5;'><th style='padding: 8px; text-align: left; border: 1px solid #ddd;'>Category</th><th style='padding: 8px; text-align: right; border: 1px solid #ddd;'>Count</th></tr>");
        foreach (var threshold in summary.ByThreshold)
        {
            sb.AppendLine($"<tr><td style='padding: 8px; border: 1px solid #ddd;'>{threshold.Key}</td><td style='padding: 8px; text-align: right; border: 1px solid #ddd;'>{threshold.Value}</td></tr>");
        }
        sb.AppendLine("</table>");

        // By Facility (Partner-wise breakdown for FacilityStatuses)
        if (summary.ByFacility.Any())
        {
            sb.AppendLine("<h4>By Facility/Partner (FacilityStatuses only)</h4>");
            sb.AppendLine("<table style='border-collapse: collapse; width: 100%; max-width: 500px;'>");
            sb.AppendLine("<tr style='background-color: #f5f5f5;'><th style='padding: 8px; text-align: left; border: 1px solid #ddd;'>Facility</th><th style='padding: 8px; text-align: right; border: 1px solid #ddd;'>Count</th></tr>");
            foreach (var facility in summary.ByFacility)
            {
                sb.AppendLine($"<tr><td style='padding: 8px; border: 1px solid #ddd;'>{facility.Key}</td><td style='padding: 8px; text-align: right; border: 1px solid #ddd;'>{facility.Value}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        // Top statuses
        if (summary.TopStatuses.Any())
        {
            sb.AppendLine("<h4>Top Statuses</h4>");
            sb.AppendLine("<table style='border-collapse: collapse; width: 100%; max-width: 600px;'>");
            sb.AppendLine("<tr style='background-color: #f5f5f5;'><th style='padding: 8px; text-align: left; border: 1px solid #ddd;'>Status ID</th><th style='padding: 8px; text-align: left; border: 1px solid #ddd;'>Status</th><th style='padding: 8px; text-align: right; border: 1px solid #ddd;'>Count</th></tr>");
            foreach (var status in summary.TopStatuses.Take(10))
            {
                sb.AppendLine($"<tr><td style='padding: 8px; border: 1px solid #ddd;'>{status.StatusId}</td><td style='padding: 8px; border: 1px solid #ddd;'>{status.Status}</td><td style='padding: 8px; text-align: right; border: 1px solid #ddd;'>{status.Count}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        // Top orders
        var ordersList = topOrders.Take(20).ToList();
        if (ordersList.Any())
        {
            sb.AppendLine("<h3>Sample Stuck Orders</h3>");
            sb.AppendLine("<table style='border-collapse: collapse; width: 100%;'>");
            sb.AppendLine("<tr style='background-color: #f5f5f5;'>");
            sb.AppendLine("<th style='padding: 8px; text-align: left; border: 1px solid #ddd;'>Order ID</th>");
            sb.AppendLine("<th style='padding: 8px; text-align: left; border: 1px solid #ddd;'>Status</th>");
            sb.AppendLine("<th style='padding: 8px; text-align: right; border: 1px solid #ddd;'>Hours Stuck</th>");
            sb.AppendLine("<th style='padding: 8px; text-align: left; border: 1px solid #ddd;'>Last Updated</th>");
            sb.AppendLine("</tr>");

            foreach (var order in ordersList)
            {
                var rowColor = order.HoursStuck > 48 ? "#ffebee" : (order.HoursStuck > 24 ? "#fff3e0" : "white");
                sb.AppendLine($"<tr style='background-color: {rowColor};'>");
                sb.AppendLine($"<td style='padding: 8px; border: 1px solid #ddd;'>{order.OrderId}</td>");
                sb.AppendLine($"<td style='padding: 8px; border: 1px solid #ddd;'>{order.Status} ({order.StatusId})</td>");
                sb.AppendLine($"<td style='padding: 8px; text-align: right; border: 1px solid #ddd;'>{order.HoursStuck}</td>");
                sb.AppendLine($"<td style='padding: 8px; border: 1px solid #ddd;'>{order.StuckSince:yyyy-MM-dd HH:mm}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine("<hr style='margin-top: 20px;'/>");
        sb.AppendLine("<p style='color: #888; font-size: 0.9em;'>This is an automated alert from the Order Monitor API.</p>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    private string BuildTestEmailBody()
    {
        var sb = new StringBuilder();

        sb.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
        sb.AppendLine("<h2 style='color: #5cb85c;'>✅ Test Alert Successful</h2>");
        sb.AppendLine($"<p><strong>Generated:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        sb.AppendLine("<p>If you received this email, the Order Monitor alert system is configured correctly.</p>");
        sb.AppendLine("<h3>Configuration</h3>");
        sb.AppendLine("<ul>");
        sb.AppendLine($"<li><strong>SMTP Host:</strong> {_smtpSettings.Host}</li>");
        sb.AppendLine($"<li><strong>SMTP Port:</strong> {_smtpSettings.Port}</li>");
        sb.AppendLine($"<li><strong>From:</strong> {_smtpSettings.FromEmail}</li>");
        sb.AppendLine($"<li><strong>SSL Enabled:</strong> {_smtpSettings.UseSsl}</li>");
        sb.AppendLine("</ul>");
        sb.AppendLine("<hr style='margin-top: 20px;'/>");
        sb.AppendLine("<p style='color: #888; font-size: 0.9em;'>This is a test alert from the Order Monitor API.</p>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    private async Task SendEmailAsync(
        string subject,
        string htmlBody,
        IEnumerable<string> recipients,
        CancellationToken cancellationToken)
    {
        // Get password from config or environment variable, decrypt if encrypted
        var encryptedPassword = _smtpSettings.Password ?? Environment.GetEnvironmentVariable("SMTP_PASSWORD");

        if (string.IsNullOrEmpty(encryptedPassword))
        {
            _logger.LogError("SMTP password not configured. Set SMTP_PASSWORD environment variable or SmtpSettings:Password in config.");
            throw new InvalidOperationException("SMTP password not configured");
        }

        // Decrypt password (if it's encrypted, otherwise returns as-is)
        var password = PasswordEncryptor.Decrypt(encryptedPassword);

        using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
        {
            EnableSsl = _smtpSettings.UseSsl,
            Credentials = new NetworkCredential(_smtpSettings.Username, password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000 // 30 seconds
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_smtpSettings.FromEmail, "Order Monitor"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        foreach (var recipient in recipients)
        {
            message.To.Add(recipient);
        }

        _logger.LogDebug("Sending email to {Recipients} via {Host}:{Port}",
            string.Join(", ", recipients),
            _smtpSettings.Host,
            _smtpSettings.Port);

        await client.SendMailAsync(message, cancellationToken);
    }
}
