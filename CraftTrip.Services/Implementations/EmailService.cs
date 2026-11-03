using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CraftTrip.Core.Entities;
using CraftTrip.Services.Interfaces;

namespace CraftTrip.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendBookingConfirmationEmailAsync(Booking booking)
    {
        if (booking == null || string.IsNullOrWhiteSpace(booking.CustomerEmail))
        {
            _logger.LogWarning("Không thể gửi email xác nhận vì thông tin đơn hoặc email người nhận trống.");
            return false;
        }

        var slot = booking.Slot;
        var schedule = slot?.Schedule;
        var workshop = booking.Workshop ?? schedule?.Workshop;
        var village = workshop?.Village;

        var workshopTitle = workshop?.Title ?? "Trải nghiệm văn hóa làng nghề truyền thống";
        var villageName = village?.Name ?? "Làng nghề truyền thống";
        var sessionDate = schedule != null ? schedule.Date.ToString("dddd, dd/MM/yyyy", new System.Globalization.CultureInfo("vi-VN")) : "Lịch đã hẹn";
        var sessionTime = slot != null ? $"{slot.StartTime:hh\\:mm} - {slot.EndTime:hh\\:mm}" : "Giờ mở cửa";
        var ticketCode = booking.Ticket?.TicketCode ?? $"TCK-{booking.BookingCode}";
        var qrCodeData = booking.Ticket?.QrCodeData ?? string.Empty;

        var htmlBuilder = new StringBuilder();
        htmlBuilder.Append(@"
<!DOCTYPE html>
<html lang='vi'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>CraftTrip — Xác nhận đặt chỗ</title>
<style>
    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f7f4ed; color: #2c2523; margin: 0; padding: 20px; }
    .card { max-width: 620px; margin: 0 auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.06); border: 1px solid #ebd8c3; }
    .header { background: #6b3e26; color: #ffffff; padding: 28px 24px; text-align: center; }
    .header h1 { margin: 0; font-size: 24px; font-weight: 700; letter-spacing: 0.5px; }
    .badge { display: inline-block; background: #e07a5f; color: #ffffff; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 600; text-transform: uppercase; margin-top: 8px; }
    .content { padding: 28px 24px; }
    .ticket-box { background: #faf8f5; border: 2px dashed #d4c2b0; border-radius: 12px; padding: 20px; text-align: center; margin: 20px 0; }
    .ticket-code { font-family: monospace; font-size: 18px; font-weight: 700; color: #6b3e26; letter-spacing: 1px; }
    .qr-img { width: 160px; height: 160px; margin: 12px auto; display: block; }
    .info-table { width: 100%; border-collapse: collapse; margin: 16px 0; font-size: 14px; }
    .info-table td { padding: 10px 0; border-bottom: 1px solid #f0e9df; }
    .info-table td.label { color: #7d6b62; width: 40%; }
    .info-table td.val { font-weight: 600; text-align: right; color: #2c2523; }
    .total-row td { border-bottom: none; font-size: 16px; color: #e07a5f; font-weight: 700; padding-top: 14px; }
    .notice { background: #eef7f2; border-left: 4px solid #2a9d8f; padding: 12px 16px; border-radius: 6px; font-size: 13px; color: #1e5a52; margin: 20px 0; }
    .btn { display: inline-block; background: #6b3e26; color: #ffffff !important; text-decoration: none; padding: 14px 28px; border-radius: 30px; font-weight: 600; font-size: 15px; margin: 10px 0; }
    .footer { text-align: center; font-size: 12px; color: #9c8e85; padding: 20px; border-top: 1px solid #ebd8c3; background: #faf8f5; }
</style>
</head>
<body>
<div class='card'>
    <div class='header'>
        <h1>CraftTrip</h1>
        <div class='badge'>Xác nhận vé điện tử thành công</div>
    </div>
    <div class='content'>
        <p>Kính chào Quý khách <strong>" + WebUtility.HtmlEncode(booking.CustomerName) + @"</strong>,</p>
        <p>Cảm ơn bạn đã lựa chọn trải nghiệm cùng CraftTrip. Vé tham gia workshop của bạn đã được thanh toán thành công và có hiệu lực ngay bây giờ.</p>

        <div class='ticket-box'>
            <div style='font-size: 13px; color: #7d6b62; text-transform: uppercase;'>Mã vé tham gia</div>
            <div class='ticket-code'>" + WebUtility.HtmlEncode(ticketCode) + @"</div>");

        if (!string.IsNullOrEmpty(qrCodeData))
        {
            htmlBuilder.Append("<img src='" + qrCodeData + @"' alt='Mã QR vé' class='qr-img' />");
        }

        htmlBuilder.Append(@"
            <div style='font-size: 12px; color: #7d6b62; margin-top: 6px;'>Xuất trình mã QR này cho nghệ nhân tại xưởng khi đến tham gia</div>
        </div>

        <table class='info-table'>
            <tr><td class='label'>Mã đơn đặt chỗ</td><td class='val' style='font-family: monospace;'>" + WebUtility.HtmlEncode(booking.BookingCode) + @"</td></tr>
            <tr><td class='label'>Workshop</td><td class='val'>" + WebUtility.HtmlEncode(workshopTitle) + @"</td></tr>
            <tr><td class='label'>Làng nghề</td><td class='val'>" + WebUtility.HtmlEncode(villageName) + @"</td></tr>
            <tr><td class='label'>Ngày trải nghiệm</td><td class='val'>" + WebUtility.HtmlEncode(sessionDate) + @"</td></tr>
            <tr><td class='label'>Khung giờ ca</td><td class='val'>" + WebUtility.HtmlEncode(sessionTime) + @"</td></tr>
            <tr><td class='label'>Số lượng người tham gia</td><td class='val'>" + booking.Quantity + @" người</td></tr>
            <tr class='total-row'><td class='label'>Tổng số tiền đã thanh toán</td><td class='val'>" + booking.TotalAmount.ToString("N0") + @" đ</td></tr>
        </table>

        <div class='notice'>
            <strong>Chính sách hoàn hủy 100%:</strong> Bạn có thể chủ động hủy vé và nhận lại 100% số tiền nếu thực hiện hủy trước giờ trải nghiệm tối thiểu 48 tiếng trên hệ thống CraftTrip.
        </div>

        <div style='text-align: center; margin: 24px 0;'>
            <a href='http://localhost:5166/Booking/Details/" + booking.Id + @"' class='btn'>Xem vé và Mã QR trên hệ thống</a>
        </div>
    </div>
    <div class='footer'>
        <p style='margin: 0 0 6px 0;'><strong>CraftTrip — Nền tảng kết nối trải nghiệm Làng nghề truyền thống Việt Nam</strong></p>
        <p style='margin: 0;'>Hotline hỗ trợ: 1900 6868 • Email: hotro@crafttrip.vn • Địa chỉ: Hà Nội, Việt Nam</p>
    </div>
</div>
</body>
</html>");

        var subject = $"CraftTrip — Xác nhận đặt chỗ và vé trải nghiệm của bạn ({booking.BookingCode})";
        return await SendEmailAsync(booking.CustomerEmail, subject, htmlBuilder.ToString());
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? _configuration["SMTP_HOST"];
        var smtpPortStr = _configuration["Email:SmtpPort"] ?? _configuration["SMTP_PORT"] ?? "587";
        var smtpUser = _configuration["Email:SmtpUser"] ?? _configuration["SMTP_USERNAME"];
        var smtpPass = _configuration["Email:SmtpPass"] ?? _configuration["SMTP_PASSWORD"];
        var fromEmail = _configuration["Email:FromEmail"] ?? _configuration["SMTP_FROM_EMAIL"] ?? "no-reply@crafttrip.vn";
        var fromName = _configuration["Email:FromName"] ?? _configuration["SMTP_FROM_NAME"] ?? "CraftTrip";
        var enableSsl = _configuration.GetValue<bool>("Email:EnableSsl", true);

        // Check if SMTP is configured
        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser) || smtpUser.Contains("your-email"))
        {
            _logger.LogInformation(
                "SMTP credentials are not configured or in development placeholder mode. Email to '{ToEmail}' with subject '{Subject}' recorded as simulated/not-configured.",
                toEmail,
                subject);
            return true; // Safe fallback: does not crash, preserves payment and ticket
        }

        try
        {
            int.TryParse(smtpPortStr, out var smtpPort);
            if (smtpPort <= 0) smtpPort = 587;

            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail, fromName, Encoding.UTF8);
            message.To.Add(new MailAddress(toEmail));
            message.Subject = subject;
            message.SubjectEncoding = Encoding.UTF8;
            message.Body = htmlBody;
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(smtpHost, smtpPort);
            client.EnableSsl = enableSsl;
            client.Credentials = new NetworkCredential(smtpUser, smtpPass);
            client.Timeout = 10000;

            await client.SendMailAsync(message);
            _logger.LogInformation("Đã gửi email thành công đến {ToEmail} qua SMTP server {SmtpHost}", toEmail, smtpHost);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi gửi email đến {ToEmail}: {Message}", toEmail, ex.Message);
            return false;
        }
    }
}
