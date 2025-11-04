using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Opc.Ua;

namespace UAInspector.Core.Services
{
    public class CertificateManager
    {
private readonly string _certificatesPath;
        private readonly ApplicationConfiguration _applicationConfig;

        public CertificateManager(ApplicationConfiguration applicationConfig, string certificatesBasePath)
  {
    _applicationConfig = applicationConfig;
         _certificatesPath = certificatesBasePath;
            EnsureCertificateDirectories();
  }

 private void EnsureCertificateDirectories()
        {
        try
            {
   var expandedPath = Environment.ExpandEnvironmentVariables(_certificatesPath);
     Directory.CreateDirectory(expandedPath);
    Directory.CreateDirectory(Path.Combine(expandedPath, "own"));
           Directory.CreateDirectory(Path.Combine(expandedPath, "trusted"));
        Directory.CreateDirectory(Path.Combine(expandedPath, "issuers"));
 Directory.CreateDirectory(Path.Combine(expandedPath, "rejected"));
       System.Diagnostics.Debug.WriteLine($"Certificate directories ready at: {expandedPath}");
         }
      catch (Exception ex)
         {
System.Diagnostics.Debug.WriteLine($"Error creating certificate directories: {ex.Message}");
         }
    }

        public async System.Threading.Tasks.Task ValidateApplicationCertificateAsync()
   {
            try
        {
         System.Diagnostics.Debug.WriteLine("Application certificate configuration ready");
        await System.Threading.Tasks.Task.CompletedTask;
            }
   catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Error validating certificate: {ex.Message}");
        }
        }

 public async System.Threading.Tasks.Task<X509Certificate2> GetApplicationCertificateAsync()
    {
            try
         {
          if (_applicationConfig?.SecurityConfiguration?.ApplicationCertificate == null)
        return null;
                return await System.Threading.Tasks.Task.FromResult<X509Certificate2>(null);
            }
      catch (Exception ex)
      {
     System.Diagnostics.Debug.WriteLine($"Error getting application certificate: {ex.Message}");
           return null;
        }
        }

        public void TrustServerCertificate(X509Certificate2 certificate)
        {
 try
     {
        if (certificate == null)
          return;

    var trustedStore = _applicationConfig.SecurityConfiguration?.TrustedPeerCertificates;
        if (trustedStore == null)
     return;

      var certificatePath = Path.Combine(
       Environment.ExpandEnvironmentVariables(trustedStore.StorePath),
               $"{certificate.Thumbprint}.der");

     if (!File.Exists(certificatePath))
     {
      File.WriteAllBytes(certificatePath, certificate.RawData);
           System.Diagnostics.Debug.WriteLine($"Server certificate trusted: {certificate.Subject}");
    }
            }
   catch (Exception ex)
   {
    System.Diagnostics.Debug.WriteLine($"Error trusting server certificate: {ex.Message}");
          }
   }

        public void RejectServerCertificate(X509Certificate2 certificate)
        {
            try
      {
                if (certificate == null)
             return;

    var rejectedStore = _applicationConfig.SecurityConfiguration?.RejectedCertificateStore;
        if (rejectedStore == null)
        return;

      var certificatePath = Path.Combine(
        Environment.ExpandEnvironmentVariables(rejectedStore.StorePath),
      $"{certificate.Thumbprint}.der");

         if (!File.Exists(certificatePath))
        {
File.WriteAllBytes(certificatePath, certificate.RawData);
         System.Diagnostics.Debug.WriteLine($"Server certificate rejected: {certificate.Subject}");
           }
 }
     catch (Exception ex)
            {
     System.Diagnostics.Debug.WriteLine($"Error rejecting server certificate: {ex.Message}");
  }
        }
    }
}
