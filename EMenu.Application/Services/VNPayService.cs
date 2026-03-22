using EMenu.Application.Abstractions.Configurations;
using System.Text;
using System.Web;

namespace EMenu.Application.Services
{
    public class VNPayService
    {
        private readonly VNPayConfig _config;

        public VNPayService(VNPayConfig config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(int sessionId, decimal amount)
        {
            var vnp = new SortedDictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _config.TmnCode,
                ["vnp_Amount"] = ((int)(amount * 100)).ToString(),
                ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = "127.0.0.1",
                ["vnp_Locale"] = "vn",
                ["vnp_OrderInfo"] = "Payment session " + sessionId,
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = _config.ReturnUrl,
                ["vnp_TxnRef"] = sessionId.ToString()
            };

            var query = new StringBuilder();
            var hashData = new StringBuilder();

            foreach (var item in vnp)
            {
                query.Append(HttpUtility.UrlEncode(item.Key))
                    .Append('=')
                    .Append(HttpUtility.UrlEncode(item.Value))
                    .Append('&');

                hashData.Append(item.Key)
                    .Append('=')
                    .Append(item.Value)
                    .Append('&');
            }

            var queryString = query.ToString().TrimEnd('&');
            var signData = hashData.ToString().TrimEnd('&');
            var secureHash = HmacSHA512(_config.HashSecret, signData);

            queryString += "&vnp_SecureHash=" + secureHash;

            return _config.Url + "?" + queryString;
        }

        private static string HmacSHA512(string key, string data)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA512(
                Encoding.UTF8.GetBytes(key));

            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));

            return BitConverter.ToString(hash)
                .Replace("-", "")
                .ToLowerInvariant();
        }
    }
}
