using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CatalogLib;

public class CertificateService
{
    const int KeySize = 2048;

    public X509Certificate2 GenerateCACertificate(string subjectName, string password, int validityYears = 1)
    {
        var rsa = RSA.Create(KeySize);
        var request = new CertificateRequest(
            $"CN={subjectName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add CA certificate capabilities
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(true, true, 2, true));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                true));

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.Now.AddDays(-1),
            DateTimeOffset.Now.AddYears(validityYears));  // CA has longer validity

        return certificate;
    }

    public X509Certificate2 GenerateIntermediateCertificate(X509Certificate2 caCert, string subjectName, string password, int validityYears = 1)
    {
        var rsa = RSA.Create(KeySize);
        var request = new CertificateRequest(
            $"CN={subjectName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add intermediate CA capabilities
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(true, true, 1, true));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                true));

        var serialNumber = new byte[8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(serialNumber);
        }

        // Use the issuer's private key to sign this certificate
        using var issuerPrivateKey = caCert.GetRSAPrivateKey();
        var certificate = request.Create(
            caCert.SubjectName,
            X509SignatureGenerator.CreateForRSA(issuerPrivateKey, RSASignaturePadding.Pkcs1),
            DateTimeOffset.Now.AddDays(-1),
            DateTimeOffset.Now.AddYears(validityYears),
            serialNumber);

        // Combine with the private key
        return certificate.CopyWithPrivateKey(rsa);
    }

    public X509Certificate2 GenerateDeviceCertificate(X509Certificate2 signingCert, string deviceId, string password, int validityYears = 1)
    {
        var rsa = RSA.Create(KeySize);
        var request = new CertificateRequest(
            $"CN={deviceId}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add device certificate capabilities (no CA capabilities)
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, true));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                true));

        // Add enhanced key usage for client authentication
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, // Client Authentication
                true));

        var serialNumber = new byte[8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(serialNumber);
        }

        // Use the issuer's private key to sign this certificate
        using var issuerPrivateKey = signingCert.GetRSAPrivateKey();
        var certificate = request.Create(
            signingCert.SubjectName,
            X509SignatureGenerator.CreateForRSA(issuerPrivateKey, RSASignaturePadding.Pkcs1),
            DateTimeOffset.Now.AddDays(-1),
            DateTimeOffset.Now.AddYears(validityYears),
            serialNumber);

        // Combine with the private key
        return certificate.CopyWithPrivateKey(rsa);
    }

    public string ExportToPem(X509Certificate2 cert)
    {
        StringBuilder builder = new();
        builder.AppendLine("-----BEGIN CERTIFICATE-----");
        builder.AppendLine(Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks));
        builder.AppendLine("-----END CERTIFICATE-----");
        return builder.ToString();
    }

    public string ExportPrivateKeyToPem(ECDsa ecdsa)
    {
        var privateKeyBytes = ecdsa.ExportECPrivateKey();
        StringBuilder builder = new();
        builder.AppendLine("-----BEGIN PRIVATE KEY-----");
        builder.AppendLine(Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks));
        builder.AppendLine("-----END PRIVATE KEY-----");
        return builder.ToString();
    }

    public X509Certificate2 LoadCertificateFromPem(string pemData)
    {
        const string BEGIN_CERTIFICATE = "-----BEGIN CERTIFICATE-----";
        const string END_CERTIFICATE = "-----END CERTIFICATE-----";

        int startIndex = pemData.IndexOf(BEGIN_CERTIFICATE);
        if (startIndex < 0)
            throw new FormatException("PEM data does not contain a certificate");

        startIndex += BEGIN_CERTIFICATE.Length;
        int endIndex = pemData.IndexOf(END_CERTIFICATE, startIndex);
        if (endIndex < 0)
            throw new FormatException("PEM data does not contain a valid certificate");

        string base64 = pemData[startIndex..endIndex].Replace("\r", "").Replace("\n", "");
        byte[] rawData = Convert.FromBase64String(base64);

        return new X509Certificate2(rawData);
    }

    public string ToPfxBase64(X509Certificate2 certificate, string password)
    {
        var data = certificate.Export(X509ContentType.Pfx, password);
        return Convert.ToBase64String(data);
    }

    public X509Certificate2 FromPfxBase64(string certificateString, string password)
    {
        var data = Convert.FromBase64String(certificateString);
        return X509CertificateLoader.LoadPkcs12(data, password);
    }
}
