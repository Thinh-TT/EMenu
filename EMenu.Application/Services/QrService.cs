using QRCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Application.Services
{
    public class QrService
    {
        public byte[] Generate(string url)
        {
            using QRCodeGenerator qrGenerator = new QRCodeGenerator();

            var data = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);

            var qrCode = new PngByteQRCode(data);

            return qrCode.GetGraphic(20);
        }
    }
}
