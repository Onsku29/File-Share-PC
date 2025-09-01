using FileShare.Networking;
using QRCoder;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace FileShare.Scripts
{
    public class PairingService
    {
        private readonly PairingServer _pairingServer;

        public PairingService(PairingServer pairingServer)
        {
            _pairingServer = pairingServer;
        }

        public PairingInfo GetPairingInfo()
        {
            return _pairingServer.Info;
        }

        public Stream GenerateQrCodeStream()
        {
            string json = JsonSerializer.Serialize(GetPairingInfo());

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(json, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrData);
            using Bitmap qrBitmap = qrCode.GetGraphic(20);
            var ms = new MemoryStream();
            qrBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            return ms;
        }
    }
}