using EMenu.Application.Abstractions.Configurations;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EMenu.Application.Services
{
    public class VNPayService
    {
        private static readonly Regex TxnRefPattern = new(
            "^S(?<session>\\d+)O(?<order>\\d+)T(?<timestamp>\\d+)R(?<random>\\d+)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly VNPayConfig _config;

        public VNPayService(VNPayConfig config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(
            int sessionId,
            int orderId,
            decimal amount,
            string? clientIpAddress)
        {
            EnsureConfigOrThrow();
            var tmnCode = _config.TmnCode.Trim();
            var hashSecret = _config.HashSecret.Trim();
            var url = _config.Url.Trim();
            var returnUrl = _config.ReturnUrl.Trim();

            if (sessionId <= 0)
                throw new InvalidOperationException("Session reference is invalid.");

            if (orderId <= 0)
                throw new InvalidOperationException("Order reference is invalid.");

            if (amount <= 0)
                throw new InvalidOperationException("Payment amount must be greater than zero.");

            var txnRef = BuildTxnRef(sessionId, orderId);
            var normalizedIpAddress = NormalizeIpAddress(clientIpAddress);
            var createDate = DateTime.Now;
            var expireDate = createDate.AddMinutes(15);

            var vnp = new SortedDictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = tmnCode,
                ["vnp_Amount"] = ((int)(amount * 100)).ToString(CultureInfo.InvariantCulture),
                ["vnp_CreateDate"] = createDate.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = normalizedIpAddress,
                ["vnp_Locale"] = "vn",
                ["vnp_OrderInfo"] = $"Payment session {sessionId} order {orderId}",
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_ExpireDate"] = expireDate.ToString("yyyyMMddHHmmss"),
                ["vnp_TxnRef"] = txnRef
            };

            var signData = BuildSignData(vnp);
            var secureHash = ComputeHmacSha512(hashSecret, signData);
            var queryString = BuildQueryString(vnp);

            queryString += "&vnp_SecureHash=" + secureHash;

            return $"{url}?{queryString}";
        }

        public bool ValidateSignature(
            IReadOnlyDictionary<string, string> parameters,
            string? secureHash)
        {
            EnsureConfigOrThrow();
            var hashSecret = _config.HashSecret.Trim();

            if (parameters == null || parameters.Count == 0)
                return false;

            if (string.IsNullOrWhiteSpace(secureHash))
                return false;

            var signData = BuildSignData(parameters);
            var expectedHash = ComputeHmacSha512(hashSecret, signData);

            return secureHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        public bool TryParseTxnRef(
            string? txnRef,
            out int sessionId,
            out int orderId)
        {
            sessionId = 0;
            orderId = 0;

            if (string.IsNullOrWhiteSpace(txnRef))
                return false;

            var match = TxnRefPattern.Match(txnRef.Trim());

            if (!match.Success)
                return false;

            var parsedSessionId = int.TryParse(
                match.Groups["session"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out sessionId);

            var parsedOrderId = int.TryParse(
                match.Groups["order"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out orderId);

            return parsedSessionId && parsedOrderId && sessionId > 0 && orderId > 0;
        }

        public static bool IsSuccessfulTransaction(
            string? responseCode,
            string? transactionStatus)
        {
            return string.Equals(responseCode, "00", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(transactionStatus, "00", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildTxnRef(int sessionId, int orderId)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var randomValue = RandomNumberGenerator.GetInt32(1000, 9999);

            return $"S{sessionId}O{orderId}T{timestamp}R{randomValue}";
        }

        private static string NormalizeIpAddress(string? clientIpAddress)
        {
            if (string.IsNullOrWhiteSpace(clientIpAddress))
            {
                return "127.0.0.1";
            }

            var rawAddress = clientIpAddress.Trim();
            if (!IPAddress.TryParse(rawAddress, out var parsedIp))
            {
                return "127.0.0.1";
            }

            if (IPAddress.IsLoopback(parsedIp))
            {
                return "127.0.0.1";
            }

            if (parsedIp.AddressFamily == AddressFamily.InterNetworkV6 && parsedIp.IsIPv4MappedToIPv6)
            {
                return parsedIp.MapToIPv4().ToString();
            }

            return parsedIp.ToString();
        }

        private static string BuildQueryString(IReadOnlyDictionary<string, string> parameters)
        {
            var query = new StringBuilder();

            foreach (var item in parameters.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                query.Append(WebUtility.UrlEncode(item.Key))
                    .Append('=')
                    .Append(WebUtility.UrlEncode(item.Value))
                    .Append('&');
            }

            return query.ToString().TrimEnd('&');
        }

        private static string BuildSignData(IReadOnlyDictionary<string, string> parameters)
        {
            var hashData = new StringBuilder();

            foreach (var item in parameters.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                hashData.Append(WebUtility.UrlEncode(item.Key))
                    .Append('=')
                    .Append(WebUtility.UrlEncode(item.Value))
                    .Append('&');
            }

            return hashData.ToString().TrimEnd('&');
        }

        private static string ComputeHmacSha512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));

            return BitConverter.ToString(hash)
                .Replace("-", "")
                .ToLowerInvariant();
        }

        private void EnsureConfigOrThrow()
        {
            if (string.IsNullOrWhiteSpace(_config.TmnCode))
            {
                throw new InvalidOperationException("VNPay configuration is missing TmnCode.");
            }

            if (string.IsNullOrWhiteSpace(_config.HashSecret))
            {
                throw new InvalidOperationException("VNPay configuration is missing HashSecret.");
            }

            if (string.IsNullOrWhiteSpace(_config.Url))
            {
                throw new InvalidOperationException("VNPay configuration is missing Url.");
            }

            if (string.IsNullOrWhiteSpace(_config.ReturnUrl))
            {
                throw new InvalidOperationException("VNPay configuration is missing ReturnUrl.");
            }
        }
    }
}
