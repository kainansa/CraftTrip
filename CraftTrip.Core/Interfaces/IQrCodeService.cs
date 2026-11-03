namespace CraftTrip.Core.Interfaces;

public interface IQrCodeService
{
    string GenerateQrCodeBase64(string text);
}
