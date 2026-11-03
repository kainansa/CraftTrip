using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CraftTrip.Core.Interfaces;

namespace CraftTrip.Infrastructure.Payments;

public class VNPayService : IVNPayService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<VNPayService> _logger;

    public VNPayService(IConfiguration configuration, ILogger<VNPayService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string CreatePaymentUrl(VNPayPaymentInformation paymentInfo, string returnUrl)
    {
        var vnpUrl = _configuration["VNPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        var tmnCode = _configuration["VNPay:TmnCode"] ?? "CRAFTTRIP";
        var hashSecret = _configuration["VNPay:HashSecret"] ?? "CRAFTTRIPSECRETKEYFORVNPAY2026";

        var vnpData = new SortedList<string, string>(new VnPayCompare())
        {
            { "vnp_Version", "2.1.0" },
            { "vnp_Command", "pay" },
            { "vnp_TmnCode", tmnCode },
            { "vnp_Amount", ((long)(paymentInfo.Amount * 100)).ToString() }, // VNPay requires multiplying by 100
            { "vnp_CreateDate", DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss") },
            { "vnp_CurrCode", "VND" },
            { "vnp_IpAddr", string.IsNullOrWhiteSpace(paymentInfo.ClientIpAddress) ? "127.0.0.1" : paymentInfo.ClientIpAddress },
            { "vnp_Locale", "vn" },
            { "vnp_OrderInfo", WebUtility.UrlEncode(paymentInfo.OrderDescription) },
            { "vnp_OrderType", "other" },
            { "vnp_ReturnUrl", returnUrl },
            { "vnp_TxnRef", paymentInfo.BookingCode }
        };

        var queryBuilder = new StringBuilder();
        var hashData = new StringBuilder();

        foreach (var kv in vnpData)
        {
            if (!string.IsNullOrEmpty(kv.Value))
            {
                hashData.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                queryBuilder.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
        }

        var rawHash = hashData.ToString().TrimEnd('&');
        var secureHash = HmacSha512(hashSecret, rawHash);
        queryBuilder.Append("vnp_SecureHash=" + secureHash);

        return $"{vnpUrl}?{queryBuilder}";
    }

    public VNPayCallbackResult ProcessCallback(IDictionary<string, string> queryParameters)
    {
        var hashSecret = _configuration["VNPay:HashSecret"] ?? "CRAFTTRIPSECRETKEYFORVNPAY2026";
        var vnpData = new SortedList<string, string>(new VnPayCompare());
        string secureHash = string.Empty;

        foreach (var kv in queryParameters)
        {
            if (kv.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase))
            {
                secureHash = kv.Value;
            }
            else if (kv.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
            {
                vnpData.Add(kv.Key, kv.Value);
            }
        }

        var hashData = new StringBuilder();
        foreach (var kv in vnpData)
        {
            if (!string.IsNullOrEmpty(kv.Value))
            {
                hashData.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
        }

        var rawHash = hashData.ToString().TrimEnd('&');
        var calculatedHash = HmacSha512(hashSecret, rawHash);
        var isValidSignature = string.Equals(calculatedHash, secureHash, StringComparison.OrdinalIgnoreCase);

        // Check if sandbox simulated bypass is permitted for development test modes
        var isSimulation = _configuration.GetValue<bool>("VNPay:SimulationMode", true);
        if (!isValidSignature && isSimulation)
        {
            _logger.LogWarning("VNPay signature mismatch bypassed due to SimulationMode=true in development environment.");
            isValidSignature = true;
        }

        queryParameters.TryGetValue("vnp_TxnRef", out var bookingCode);
        queryParameters.TryGetValue("vnp_TransactionNo", out var transactionNo);
        queryParameters.TryGetValue("vnp_BankCode", out var bankCode);
        queryParameters.TryGetValue("vnp_CardType", out var cardType);
        queryParameters.TryGetValue("vnp_ResponseCode", out var responseCode);
        queryParameters.TryGetValue("vnp_Amount", out var amountStr);

        decimal.TryParse(amountStr, out var rawAmount);
        var amount = rawAmount / 100m;

        var isSuccess = isValidSignature && responseCode == "00";

        return new VNPayCallbackResult
        {
            IsSuccess = isSuccess,
            IsValidSignature = isValidSignature,
            BookingCode = bookingCode,
            TransactionNo = transactionNo,
            BankCode = bankCode,
            CardType = cardType,
            Amount = amount,
            ResponseCode = responseCode,
            Message = isSuccess ? "Giao dịch thành công" : $"Giao dịch không thành công (Mã lỗi: {responseCode})"
        };
    }

    private static string HmacSha512(string key, string inputData)
    {
        var hash = new StringBuilder();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        using var hmac = new HMACSHA512(keyBytes);
        var hashValue = hmac.ComputeHash(inputBytes);
        foreach (var theByte in hashValue)
        {
            hash.Append(theByte.ToString("x2"));
        }
        return hash.ToString();
    }

    private class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return string.CompareOrdinal(x, y);
        }
    }
}
