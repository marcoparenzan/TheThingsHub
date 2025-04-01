using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace DeviceCertificateGenerator
{
    class Program
    {
        static int Main(string[] args)
        {
            var rootCommand = new RootCommand("Generate device certificates for Azure Event Grid using a CA certificate")
            {
                new Option<string>(
                    "--ca-cert",
                    "Path to the CA certificate file (.pfx)"),
                
                new Option<string>(
                    "--ca-password",
                    "Password for the CA certificate file"),
                
                new Option<string>(
                    "--device-id",
                    "Device ID for which to generate a certificate"),
                
                new Option<string>(
                    "--output-dir",
                    () => ".",
                    "Directory where to save the generated files")
            };

            rootCommand.Handler = CommandHandler.Create<string, string, string, string>(GenerateDeviceCertificate);
            return rootCommand.InvokeAsync(args).Result;
        }

        static void GenerateDeviceCertificate(string caCert, string caPassword, string deviceId, string outputDir)
        {
            try
            {
                // Load the CA certificate
                var caCertificate = new X509Certificate2(caCert, caPassword, X509KeyStorageFlags.Exportable);

                // Create device certificate
                using (var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
                {
                    var deviceCertRequest = new CertificateRequest(
                        $"CN={deviceId}", ecdsa, HashAlgorithmName.SHA256);

                    // Add basic constraints extension
                    deviceCertRequest.CertificateExtensions.Add(
                        new X509BasicConstraintsExtension(false, false, 0, false));

                    // Add key usage extension
                    deviceCertRequest.CertificateExtensions.Add(
                        new X509KeyUsageExtension(
                            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                            false));

                    // Add enhanced key usage extension (Client Authentication)
                    deviceCertRequest.CertificateExtensions.Add(
                        new X509EnhancedKeyUsageExtension(
                            new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, // Client Authentication
                            false));

                    // Get the CA's private key
                    var caPrivateKey = caCertificate.GetRSAPrivateKey();
                    if (caPrivateKey == null)
                    {
                        throw new InvalidOperationException("Could not get private key from CA certificate");
                    }

                    // Create the certificate and sign it with the CA certificate
                    var notBefore = DateTime.UtcNow;
                    var notAfter = notBefore.AddYears(1); // 1-year validity
                    var deviceCert = deviceCertRequest.Create(
                        caCertificate.SubjectName,
                        X509SignatureGenerator.CreateForRSA(caPrivateKey, RSASignaturePadding.Pkcs1),
                        notBefore,
                        notAfter,
                        new byte[] { 1, 2, 3, 4 }); // Serial number - should be random and unique

                    // Create certificate with private key
                    var deviceCertWithKey = deviceCert.CopyWithPrivateKey(ecdsa);

                    // Save device certificate as PFX
                    var pfxPath = Path.Combine(outputDir, $"{deviceId}.pfx");
                    File.WriteAllBytes(pfxPath, deviceCertWithKey.Export(X509ContentType.Pfx, ""));
                    Console.WriteLine($"Device certificate saved to: {pfxPath}");

                    // Save device certificate as PEM
                    var pemPath = Path.Combine(outputDir, $"{deviceId}.pem");
                    File.WriteAllText(pemPath, ExportToPem(deviceCertWithKey));
                    Console.WriteLine($"Device certificate saved to: {pemPath}");

                    // Generate CA certificate public thumbprint
                    var caThumbprintPath = Path.Combine(outputDir, "ca-thumbprint.txt");
                    File.WriteAllText(caThumbprintPath, caCertificate.Thumbprint.ToLower());
                    Console.WriteLine($"CA thumbprint saved to: {caThumbprintPath}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error generating device certificate: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
            }
        }

        static string ExportToPem(X509Certificate2 cert)
        {
            StringBuilder builder = new StringBuilder();

            // Add certificate
            builder.AppendLine("-----BEGIN CERTIFICATE-----");
            builder.AppendLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine("-----END CERTIFICATE-----");

            // Add private key if available
            if (cert.HasPrivateKey)
            {
                var privateKey = cert.GetECDsaPrivateKey();
                if (privateKey != null)
                {
                    var parameters = privateKey.ExportParameters(true);
                    builder.AppendLine();
                    builder.AppendLine("-----BEGIN PRIVATE KEY-----");
                    builder.AppendLine(Convert.ToBase64String(privateKey.ExportPkcs8PrivateKey(), Base64FormattingOptions.InsertLineBreaks));
                    builder.AppendLine("-----END PRIVATE KEY-----");
                }
            }

            return builder.ToString();
        }
    }
}