using QRCoder;
using CraftTrip.Core.Interfaces;

namespace CraftTrip.Infrastructure.Services;

public class QrCodeService : IQrCodeService
{
    public string GenerateQrCodeBase64(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var pngQrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = pngQrCode.GetGraphic(20);
        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
    }
}
