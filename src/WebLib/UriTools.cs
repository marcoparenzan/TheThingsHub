using Microsoft.Extensions.Primitives;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace WebLib;

public class UriTools(string signingKey)
{
    public bool Validate(IEnumerable<KeyValuePair<string, StringValues>> query)
    {
        var sig = query.SingleOrDefault(query => query.Key == "sig").Value.ToString();
        var se = query.SingleOrDefault(query => query.Key == "se").Value.ToString();
        // Validate required SAS parameters
        if (sig is null || se is null)
        {
            //return Results.BadRequest("Missing required SAS parameters");
            return false;
        }

        // Validate expiration time
        if (!DateTime.TryParse(se, out DateTime expiryTime) ||
            expiryTime < DateTime.UtcNow)
        {
            //return Results.BadRequest("SAS token expired or invalid expiry format");
            return false;
        }

        // Validate signature
        var expectedSignature = ComputeSignature(query);

        if (!string.Equals(sig, expectedSignature, StringComparison.OrdinalIgnoreCase))
        {
            //return Results.Unauthorized();
            return false;
        }

        //return Results.Ok(new { Success = true, Message = "SAS validation successful" });
        return true;
    }

    string ComputeSignature(IEnumerable<KeyValuePair<string, StringValues>> query)
    {
        // Get the signing key (in production, retrieve from secure storage)

        // Create string-to-sign from query parameters (excluding signature itself)
        var stringToSign = string.Join("&", query
            .Where(p => p.Key != "sig")
            .OrderBy(p => p.Key)
            .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));

        //
        //  I HAVE TO MATCH!
        //

        // Compute HMACSHA256 signature
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
        return Convert.ToBase64String(signatureBytes);
    }
    
    public string GenerateSasUrl(
        string baseUrl,
        string permissions = "r",
        DateTime? startTime = null,
        DateTime? expiryTime = null,
        string ipRange = null,
        string protocol = "https")
    {
        // Set default expiry time if not provided (1 hour from now)
        if (!expiryTime.HasValue)
            expiryTime = DateTime.UtcNow.AddHours(1);

        // Format the times in the required format
        string startTimeStr = startTime.HasValue ? startTime.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") : "";
        string expiryTimeStr = expiryTime.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

        // Build the canonicalized resource string
        string canonicalizedResource = $"/{new Uri(baseUrl).AbsolutePath}";

        // Build the string-to-sign
        var stringToSign = new StringBuilder();
        stringToSign.Append(permissions + "\n");
        stringToSign.Append(startTimeStr + "\n");
        stringToSign.Append(expiryTimeStr + "\n");
        stringToSign.Append(canonicalizedResource + "\n");
        stringToSign.Append(ipRange ?? "" + "\n");
        stringToSign.Append(protocol + "\n");
        stringToSign.Append("2020-10-02\n"); // API version

        // Generate signature
        byte[] keyBytes = Encoding.UTF8.GetBytes(signingKey);
        using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
        {
            byte[] dataToHmac = Encoding.UTF8.GetBytes(stringToSign.ToString());
            string signature = Convert.ToBase64String(hmac.ComputeHash(dataToHmac));

            // Build the SAS token
            var sasToken = new StringBuilder("?sv=2020-10-02");
            sasToken.Append("&sp=" + HttpUtility.UrlEncode(permissions));

            if (!string.IsNullOrEmpty(startTimeStr))
                sasToken.Append("&st=" + HttpUtility.UrlEncode(startTimeStr));

            sasToken.Append("&se=" + HttpUtility.UrlEncode(expiryTimeStr));

            if (!string.IsNullOrEmpty(ipRange))
                sasToken.Append("&sip=" + HttpUtility.UrlEncode(ipRange));

            sasToken.Append("&spr=" + protocol);
            sasToken.Append("&sig=" + HttpUtility.UrlEncode(signature));

            return baseUrl + sasToken.ToString();
        }
    }
}
