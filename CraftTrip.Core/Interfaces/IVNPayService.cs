namespace CraftTrip.Core.Interfaces;

public class VNPayPaymentInformation
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string OrderDescription { get; set; } = string.Empty;
    public string ClientIpAddress { get; set; } = "127.0.0.1";
}

public class VNPayCallbackResult
{
    public bool IsSuccess { get; set; }
    public string? BookingCode { get; set; }
    public string? TransactionNo { get; set; }
    public string? BankCode { get; set; }
    public string? CardType { get; set; }
    public decimal Amount { get; set; }
    public string? ResponseCode { get; set; }
    public string? Message { get; set; }
    public bool IsValidSignature { get; set; }
}

public interface IVNPayService
{
    string CreatePaymentUrl(VNPayPaymentInformation paymentInfo, string returnUrl);
    VNPayCallbackResult ProcessCallback(IDictionary<string, string> queryParameters);
}
