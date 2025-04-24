using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace IoTMgmtApp.Services;

public class DeviceCertificateService
{
    string outputDir = @"D:\IoTHub\TheIoTThing\src\ConsoleApp2\bin\Debug\net9.0\certs";

    string password = "password";
    int validityYears = 1;

    public void GenerateCACertificate()
    {
        Console.Write("Enter subject name for CA certificate (e.g., 'CN=My Root CA'): ");
        string subjectName = Console.ReadLine() ?? "CN=Root CA";

        string baseName = Path.Combine(outputDir, "ca-cert");

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

        // Export as PFX
        File.WriteAllBytes($"{baseName}.pfx", certificate.Export(X509ContentType.Pfx, password));

        // Export as PEM certificate
        File.WriteAllText($"{baseName}.crt", ExportToPem(certificate));

        // Export private key
        File.WriteAllText($"{baseName}.key", ExportPrivateKeyToPem(ecdsa));

        Console.WriteLine($"CA Certificate created in directory: {outputDir}");
        Console.WriteLine($"Thumbprint: {certificate.Thumbprint}");
    }

    public void GenerateIntermediateCertificate()
    {
        Console.Write("Enter CA certificate path (.pfx): ");
        string caPath = Path.Combine(outputDir, "ca-cert.pfx");

        Console.Write("Enter subject name for Intermediate certificate (e.g., 'CN=My Intermediate CA'): ");
        string subjectName = Console.ReadLine() ?? "CN=Intermediate CA";

        string baseName = Path.Combine(outputDir, "intermediate-cert");

        var caCert = new X509Certificate2(caPath, password);

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

        // Export as PFX
        File.WriteAllBytes($"{baseName}.pfx", certWithPrivateKey.Export(X509ContentType.Pfx, password));

        // Export as PEM certificate
        File.WriteAllText($"{baseName}.crt", ExportToPem(certificate));

        // Export private key
        File.WriteAllText($"{baseName}.key", ExportPrivateKeyToPem(ecdsa));

        Console.WriteLine($"Intermediate Certificate created in directory: {outputDir}");
        Console.WriteLine($"Thumbprint: {certificate.Thumbprint}");
    }

    public void GenerateDeviceCertificate()
    {
        Console.Write("Enter signing certificate path (.pfx) [CA or Intermediate]: ");
        string signingCertPath = Path.Combine(outputDir, "intermediate-cert.pfx");

        Console.Write("Enter device name or ID for certificate: ");
        string deviceId = Console.ReadLine() ?? "device001";
        string subjectName = $"CN={deviceId}";

        string baseName = Path.Combine(outputDir, $"{deviceId}-cert");

        var signingCert = new X509Certificate2(signingCertPath, password);

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

        // Export as PFX
        File.WriteAllBytes($"{baseName}.pfx", certWithPrivateKey.Export(X509ContentType.Pfx, password));

        // Export as PEM certificate
        File.WriteAllText($"{baseName}.crt", ExportToPem(certificate));

        // Export private key
        File.WriteAllText($"{baseName}.key", ExportPrivateKeyToPem(ecdsa));

        Console.WriteLine($"Device Certificate created in directory: {outputDir}");
        Console.WriteLine($"Thumbprint: {certificate.Thumbprint}");
    }

    public void ReadCertificateThumbprint()
    {
        Console.Write("Enter certificate path (.pfx, .crt, or .pem): ");
        string certPath = Console.ReadLine();
        certPath = Path.Combine(outputDir, certPath + "-cert.pfx");

        if (!File.Exists(certPath))
        {
            Console.WriteLine($"Certificate file not found: {certPath}");
            return;
        }

        X509Certificate2 certificate;

        var extension = Path.GetExtension(certPath).ToLower();
        if (extension == ".pfx")
        {
            certificate = new X509Certificate2(certPath, password);
        }
        else if (extension == ".crt" || extension == ".pem")
        {
            // Load PEM formatted certificate
            string pemData = File.ReadAllText(certPath);
            certificate = LoadCertificateFromPem(pemData);
        }
        else
        {
            Console.WriteLine("Unsupported file format. Please use .pfx, .crt, or .pem");
            return;
        }

        Console.WriteLine("Certificate Information:");
        Console.WriteLine($"Subject: {certificate.Subject}");
        Console.WriteLine($"Issuer: {certificate.Issuer}");
        Console.WriteLine($"Valid From: {certificate.NotBefore}");
        Console.WriteLine($"Valid To: {certificate.NotAfter}");
        Console.WriteLine($"Serial Number: {certificate.SerialNumber}");
        Console.WriteLine($"Thumbprint: {certificate.Thumbprint}");

        var keyAlgorithm = certificate.GetKeyAlgorithm();
        Console.WriteLine($"Key Algorithm: {keyAlgorithm}");

        // Check for ECC public key and get curve info
        var publicKey = certificate.PublicKey.GetECDsaPublicKey();
        if (publicKey != null)
        {
            var curve = publicKey.ExportParameters(false).Curve.Oid.FriendlyName;
            Console.WriteLine($"ECC Curve: {curve}");
        }

        // Display key usage if available
        var keyUsage = certificate.Extensions["2.5.29.15"] as X509KeyUsageExtension;
        if (keyUsage != null)
        {
            Console.WriteLine($"Key Usage: {keyUsage.KeyUsages}");
        }

        // Check if it's a CA certificate
        var basicConstraints = certificate.Extensions["2.5.29.19"] as X509BasicConstraintsExtension;
        if (basicConstraints != null)
        {
            Console.WriteLine($"Is CA: {basicConstraints.CertificateAuthority}");
            if (basicConstraints.CertificateAuthority && basicConstraints.HasPathLengthConstraint)
            {
                Console.WriteLine($"Path Length Constraint: {basicConstraints.PathLengthConstraint}");
            }
        }

        // Check for Authority Key Identifier
        var authorityKeyIdentifier = certificate.Extensions["2.5.29.35"];
        if (authorityKeyIdentifier != null)
        {
            Console.WriteLine("Authority Key Identifier: Present");
        }
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
}
