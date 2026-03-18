using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Security.Credentials;
using Windows.Storage;
using Newtonsoft.Json;

namespace XboxRemoteSync.Services
{
    public sealed class CredentialProtector
    {
        private const string Resource = "XboxRemoteSync";
        private const string FallbackFileName = "credentials.json";

        public async Task StoreAsync(string key, string password)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Credential key is required.", nameof(key));
            }

            if (password == null)
            {
                password = string.Empty;
            }

            try
            {
                var vault = new PasswordVault();
                vault.Add(new PasswordCredential(Resource, key, password));
                return;
            }
            catch
            {
            }

            var encrypted = await ProtectAsync(password);
            var payload = await LoadFallbackAsync();
            payload[key] = encrypted;
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FallbackFileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(payload, Formatting.Indented));
        }

        public async Task<string> RetrieveAsync(string key)
        {
            try
            {
                var vault = new PasswordVault();
                var credential = vault.FindAllByResource(Resource).FirstOrDefault(item => item.UserName == key);
                if (credential != null)
                {
                    credential.RetrievePassword();
                    return credential.Password;
                }
            }
            catch
            {
            }

            var payload = await LoadFallbackAsync();
            if (!payload.TryGetValue(key, out var encrypted))
            {
                return string.Empty;
            }

            return await UnprotectAsync(encrypted);
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                var vault = new PasswordVault();
                var credential = vault.FindAllByResource(Resource).FirstOrDefault(item => item.UserName == key);
                if (credential != null)
                {
                    vault.Remove(credential);
                }
            }
            catch
            {
            }

            var payload = await LoadFallbackAsync();
            if (payload.Remove(key))
            {
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FallbackFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(payload, Formatting.Indented));
            }
        }

        private static async Task<string> ProtectAsync(string text)
        {
            var provider = new DataProtectionProvider("LOCAL=user");
            var input = CryptographicBuffer.ConvertStringToBinary(text, BinaryStringEncoding.Utf8);
            var output = await provider.ProtectAsync(input);
            return CryptographicBuffer.EncodeToBase64String(output);
        }

        private static async Task<string> UnprotectAsync(string text)
        {
            var provider = new DataProtectionProvider();
            var input = CryptographicBuffer.DecodeFromBase64String(text);
            var output = await provider.UnprotectAsync(input);
            return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, output);
        }

        private static async Task<System.Collections.Generic.Dictionary<string, string>> LoadFallbackAsync()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FallbackFileName, CreationCollisionOption.OpenIfExists);
                var json = await FileIO.ReadTextAsync(file);
                return string.IsNullOrWhiteSpace(json)
                    ? new System.Collections.Generic.Dictionary<string, string>()
                    : JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(json) ?? new System.Collections.Generic.Dictionary<string, string>();
            }
            catch
            {
                return new System.Collections.Generic.Dictionary<string, string>();
            }
        }
    }
}
