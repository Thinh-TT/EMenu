using EMenu.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace EMenu.Application.Services
{
    public class VNPayService
    {
        private readonly VNPayConfig _config;

        public VNPayService(IOptions<VNPayConfig> config)
        {
            _config = config.Value;
        }

        public string CreatePaymentUrl(int sessionId, decimal amount)
        {
            var vnp = new SortedDictionary<string, string>();

            vnp.Add("vnp_Version", "2.1.0");
            vnp.Add("vnp_Command", "pay");
            vnp.Add("vnp_TmnCode", _config.TmnCode);
            vnp.Add("vnp_Amount", ((int)(amount * 100)).ToString());
            vnp.Add("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnp.Add("vnp_CurrCode", "VND");
            vnp.Add("vnp_IpAddr", "127.0.0.1");
            vnp.Add("vnp_Locale", "vn");
            vnp.Add("vnp_OrderInfo", "Payment session " + sessionId);
            vnp.Add("vnp_OrderType", "other");
            vnp.Add("vnp_ReturnUrl", _config.ReturnUrl);
            vnp.Add("vnp_TxnRef", sessionId.ToString());

            var query = new StringBuilder();
            var hashData = new StringBuilder();

            foreach (var item in vnp)
            {
                query.Append(HttpUtility.UrlEncode(item.Key) + "=" + HttpUtility.UrlEncode(item.Value) + "&");

                // SIGN DATA KHÔNG ENCODE
                hashData.Append(item.Key + "=" + item.Value + "&");
            }

            string queryString = query.ToString().TrimEnd('&');
            string signData = hashData.ToString().TrimEnd('&');

            Console.WriteLine("SIGN DATA:");
            Console.WriteLine(signData);

            Console.WriteLine("HASH SECRET:");
            Console.WriteLine(_config.HashSecret);

            
            string secureHash = HmacSHA512(_config.HashSecret, signData);

            Console.WriteLine("SECURE HASH:");
            Console.WriteLine(secureHash);

            queryString += "&vnp_SecureHash=" + secureHash;

            return _config.Url + "?" + queryString;
        }

        private string HmacSHA512(string key, string data)
        {
            var hmac = new System.Security.Cryptography.HMACSHA512(
                System.Text.Encoding.UTF8.GetBytes(key));

            var hash = hmac.ComputeHash(
                System.Text.Encoding.UTF8.GetBytes(data));

            return BitConverter.ToString(hash)
                .Replace("-", "")
                .ToLower();
        }
    }
}