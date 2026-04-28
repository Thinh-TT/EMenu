using EMenu.Application.Abstractions.Configurations;
using EMenu.Application.Services;
using Microsoft.AspNetCore.WebUtilities;

namespace EMenu.Tests.Services;

public class VNPayServiceTests
{
    [Fact]
    public void CreatePaymentUrl_ValidInput_GeneratesSignedUrlWithParseableTxnRef()
    {
        var service = CreateService();

        var paymentUrl = service.CreatePaymentUrl(
            sessionId: 12,
            orderId: 34,
            amount: 120000m,
            clientIpAddress: "10.0.0.5");

        var uri = new Uri(paymentUrl);
        var query = QueryHelpers.ParseQuery(uri.Query);
        var secureHash = query["vnp_SecureHash"].ToString();

        Assert.Equal("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html", $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}");
        Assert.Equal("12000000", query["vnp_Amount"].ToString());
        Assert.Equal("10.0.0.5", query["vnp_IpAddr"].ToString());
        Assert.False(string.IsNullOrWhiteSpace(secureHash));

        var signParams = query
            .Where(x =>
                !string.Equals(x.Key, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(x.Key, "vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.Ordinal);

        Assert.True(service.ValidateSignature(signParams, secureHash));
        Assert.True(service.TryParseTxnRef(query["vnp_TxnRef"].ToString(), out var sessionId, out var orderId));
        Assert.Equal(12, sessionId);
        Assert.Equal(34, orderId);
    }

    [Fact]
    public void ValidateSignature_InvalidHash_ReturnsFalse()
    {
        var service = CreateService();
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Amount"] = "10000",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = "TESTCODE",
            ["vnp_TxnRef"] = "S1O2T123R1234"
        };

        var valid = service.ValidateSignature(parameters, "invalidhash");

        Assert.False(valid);
    }

    [Fact]
    public void TryParseTxnRef_InvalidFormat_ReturnsFalse()
    {
        var service = CreateService();

        var valid = service.TryParseTxnRef("12345", out var sessionId, out var orderId);

        Assert.False(valid);
        Assert.Equal(0, sessionId);
        Assert.Equal(0, orderId);
    }

    [Fact]
    public void CreatePaymentUrl_MissingConfig_Throws()
    {
        var config = new VNPayConfig
        {
            TmnCode = string.Empty,
            HashSecret = "TESTSECRET",
            Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
            ReturnUrl = "https://localhost/Payment/VNPayReturn"
        };

        var service = new VNPayService(config);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            service.CreatePaymentUrl(1, 2, 1000m, "127.0.0.1"));

        Assert.Equal("VNPay configuration is missing TmnCode.", ex.Message);
    }

    private static VNPayService CreateService()
    {
        var config = new VNPayConfig
        {
            TmnCode = "TESTCODE",
            HashSecret = "TESTSECRET",
            Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
            ReturnUrl = "https://localhost/Payment/VNPayReturn"
        };

        return new VNPayService(config);
    }
}
