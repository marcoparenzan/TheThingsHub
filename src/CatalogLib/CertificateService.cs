using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CatalogLib;

public class CertificateService
{
    public X509Certificate2 GenerateCACertificate(string subjectName, string password, int validityYears = 1)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(
            new X500DistinguishedName(subjectName),
            ecdsa,
            HashAlgorithmName.SHA256);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(true, true, 5, true));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.KeyCertSign,
                true));

        var enhancedKeyUsage = new OidCollection {
            new Oid("1.3.6.1.5.5.7.3.1"), // Server Authentication
            new Oid("1.3.6.1.5.5.7.3.2"), // Client Authentication
        };
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsage, true));

        // Add Subject Key Identifier
        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(validityYears));

        return certificate;
    }

    public X509Certificate2 GenerateIntermediateCertificate(X509Certificate2 caCert, string subjectName, string password, int validityYears = 1)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(
            new X500DistinguishedName(subjectName),
            ecdsa,
            HashAlgorithmName.SHA256);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(true, true, 2, true));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.KeyCertSign,
                true));

        var enhancedKeyUsage = new OidCollection {
            new Oid("1.3.6.1.5.5.7.3.1"), // Server Authentication
            new Oid("1.3.6.1.5.5.7.3.2"), // Client Authentication
        };
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsage, true));

        // Add Subject Key Identifier
        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        // Add Authority Key Identifier properly with manual encoding
        var caSubjectKeyIdentifier = caCert.Extensions["2.5.29.14"] as X509SubjectKeyIdentifierExtension;
        if (caSubjectKeyIdentifier != null)
        {
            // Get the key identifier value (without ASN.1 header)
            byte[] keyIdentifierValue;

            // Extract the actual key identifier value
            if (caSubjectKeyIdentifier.RawData.Length > 2)
            {
                int valueLength = caSubjectKeyIdentifier.RawData[1];
                keyIdentifierValue = new byte[valueLength];
                Buffer.BlockCopy(caSubjectKeyIdentifier.RawData, 2, keyIdentifierValue, 0, valueLength);
            }
            else
            {
                // Fallback if the length is unexpected
                keyIdentifierValue = caSubjectKeyIdentifier.RawData;
            }

            // Format Authority Key Identifier correctly
            using var ms = new MemoryStream();
            // Write SEQUENCE tag
            ms.WriteByte(0x30);

            // Calculate and write length for entire structure
            int totalLength = 2 + keyIdentifierValue.Length; // Tag(0x80) + Length + Content
            ms.WriteByte((byte)totalLength);

            // Write KeyIdentifier [0] context-specific tag
            ms.WriteByte(0x80);
            ms.WriteByte((byte)keyIdentifierValue.Length);
            ms.Write(keyIdentifierValue, 0, keyIdentifierValue.Length);

            request.CertificateExtensions.Add(new X509Extension("2.5.29.35", ms.ToArray(), false));
        }

        using var caPrivateKey = caCert.GetECDsaPrivateKey();
        var serial = new byte[8];
        RandomNumberGenerator.Fill(serial);

        var certificate = request.Create(
            caCert.SubjectName,
            X509SignatureGenerator.CreateForECDsa(caPrivateKey),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(validityYears),
            new ReadOnlySpan<byte>(serial));

        // Add private key
        var certWithPrivateKey = certificate.CopyWithPrivateKey(ecdsa);

        return certWithPrivateKey;
    }

    public X509Certificate2 GenerateDeviceCertificate(X509Certificate2 signingCert, string deviceId, string password, int validityYears = 1)
    {
        string subjectName = $"CN={deviceId}";

        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(
            new X500DistinguishedName(subjectName),
            ecdsa,
            HashAlgorithmName.SHA256);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, true));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                true));

        var enhancedKeyUsage = new OidCollection {
            new Oid("1.3.6.1.5.5.7.3.1"), // Server Authentication
            new Oid("1.3.6.1.5.5.7.3.2"), // Client Authentication
        };
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsage, true));

        // Add Subject Key Identifier
        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        // Add Authority Key Identifier properly with manual encoding
        var signingSubjectKeyIdentifier = signingCert.Extensions["2.5.29.14"] as X509SubjectKeyIdentifierExtension;
        if (signingSubjectKeyIdentifier != null)
        {
            // Get the key identifier value (without ASN.1 header)
            byte[] keyIdentifierValue;

            // Extract the actual key identifier value
            if (signingSubjectKeyIdentifier.RawData.Length > 2)
            {
                int valueLength = signingSubjectKeyIdentifier.RawData[1];
                keyIdentifierValue = new byte[valueLength];
                Buffer.BlockCopy(signingSubjectKeyIdentifier.RawData, 2, keyIdentifierValue, 0, valueLength);
            }
            else
            {
                // Fallback if the length is unexpected
                keyIdentifierValue = signingSubjectKeyIdentifier.RawData;
            }

            // Format Authority Key Identifier correctly
            using var ms = new MemoryStream();
            // Write SEQUENCE tag
            ms.WriteByte(0x30);

            // Calculate and write length for entire structure
            int totalLength = 2 + keyIdentifierValue.Length; // Tag(0x80) + Length + Content
            ms.WriteByte((byte)totalLength);

            // Write KeyIdentifier [0] context-specific tag
            ms.WriteByte(0x80);
            ms.WriteByte((byte)keyIdentifierValue.Length);
            ms.Write(keyIdentifierValue, 0, keyIdentifierValue.Length);

            request.CertificateExtensions.Add(new X509Extension("2.5.29.35", ms.ToArray(), false));
        }

        using var signingPrivateKey = signingCert.GetECDsaPrivateKey();
        var serial = new byte[8];
        RandomNumberGenerator.Fill(serial);

        var certificate = request.Create(
            signingCert.SubjectName,
            X509SignatureGenerator.CreateForECDsa(signingPrivateKey),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(validityYears),
            new ReadOnlySpan<byte>(serial));

        // Add private key
        var certWithPrivateKey = certificate.CopyWithPrivateKey(ecdsa);

        return certWithPrivateKey;
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
