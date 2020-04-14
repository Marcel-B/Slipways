using System.IO;
using com.b_velop.Slipways.DataProvider.Contracts;
using Microsoft.Extensions.FileProviders;

namespace com.b_velop.Slipways.DataProvider.Infrastructure
{
    public class SecretProvider : ISecretProvider
    {

        public string GetSecret(
              string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "";

            const string DOCKER_SECRET_PATH = "/run/secrets/";
            if (Directory.Exists(DOCKER_SECRET_PATH))
            {
                using var provider = new PhysicalFileProvider(DOCKER_SECRET_PATH);
                var fileInfo = provider.GetFileInfo(key);
                if (fileInfo.Exists)
                {
                    using var stream = fileInfo.CreateReadStream();
                    using var streamReader = new StreamReader(stream);
                    return streamReader.ReadToEnd();
                }
            }
            return System.Environment.GetEnvironmentVariable(key);
        }
    }
}