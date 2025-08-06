//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IdentityModel.Tokens.Jwt;
//using System.IO;
//using System.Security.Cryptography;
//using Microsoft.IdentityModel.Tokens;
//using Org.BouncyCastle.Crypto;
//using Org.BouncyCastle.Crypto.Parameters;
//using Org.BouncyCastle.OpenSsl;

//namespace PortfolioTracker.Services
//{
//    public class CoinbaseJwtService
//    {
//        private readonly string _keyName;
//        private readonly string _privateKey;

//        public CoinbaseJwtService(string keyName, string privateKey)
//        {
//            _keyName = keyName;
//            _privateKey = privateKey;
//        }

//        public string GenerateJwtToken(string requestMethod, string requestPath)
//        {
//            var uri = $"{requestMethod} {requestPath}";
//            var now = DateTimeOffset.UtcNow;
//            var exp = now.AddMinutes(2);

//            // Načtěte privátní klíč přímo jako ECDsa
//            var ecdsa = LoadEcPrivateKeyFromRaw(_privateKey);
//            var securityKey = new ECDsaSecurityKey(ecdsa);
//            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.EcdsaSha256);

//            // Vytvořte JWT header
//            var header = new JwtHeader(credentials);
//            header["kid"] = _keyName;
//            header["nonce"] = GenerateNonce();

//            // Vytvořte JWT payload
//            var payload = new JwtPayload
//            {
//                { "iss", "coinbase-cloud" },
//                { "sub", _keyName },
//                { "aud", new[] { "retail_rest_api_proxy" } },
//                { "nbf", now.ToUnixTimeSeconds() },
//                { "exp", exp.ToUnixTimeSeconds() },
//                { "uri", requestPath }
//            };

//            var token = new JwtSecurityToken(header, payload);
//            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

//            Debug.WriteLine($"Generated JWT Token: {tokenString}");

//            return tokenString;
//        }

//        private string GenerateNonce(int length = 64)
//        {
//            byte[] nonceBytes = new byte[length / 2];
//            using (var rng = RandomNumberGenerator.Create())
//            {
//                rng.GetBytes(nonceBytes);
//            }
//            return BitConverter.ToString(nonceBytes).Replace("-", "").ToLower();
//        }

//        private ECDsa LoadEcPrivateKeyFromRaw(string privateKeyInput)
//        {
//            try
//            {
//                // Parse PEM format (remove headers and decode)
//                var pemContent = privateKeyInput
//                    .Replace("-----BEGIN EC PRIVATE KEY-----", "")
//                    .Replace("-----END EC PRIVATE KEY-----", "")
//                    .Replace("\n", "")
//                    .Replace("\r", "");

//                // Convert base64 string to byte array
//                var privateKeyBytes = Convert.FromBase64String(pemContent);

//                // Import as ECDsa key using .NET's built-in method
//                var ecdsa = ECDsa.Create();
//                ecdsa.ImportECPrivateKey(privateKeyBytes, out _);

//                return ecdsa;
//            }
//            catch (FormatException)
//            {
//                throw new ArgumentException("Private key musí být validní base64 string.");
//            }
//            catch (Exception ex)
//            {
//                throw new ArgumentException($"Nepodařilo se načíst EC private key: {ex.Message}");
//            }
//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Security.Cryptography;
using Jose;

namespace PortfolioTracker.Services
{
    public class CoinbaseJwtService
    {
        private readonly string _keyName;
        private readonly string _privateKey;
        private static readonly Random random = new Random();

        public CoinbaseJwtService(string keyName, string privateKey)
        {
            _keyName = keyName;
            _privateKey = privateKey;
        }

        public string GenerateJwtToken(string requestMethod, string requestPath)
        {
            var uri = $"{requestMethod} {requestPath}";

            var secret = ParseKey(_privateKey);
            var token = GenerateToken(_keyName, secret, uri);

            Debug.WriteLine($"Generated JWT Token: {token}");
            Debug.WriteLine($"URI: {uri}");
            Debug.WriteLine($"Key Name: {_keyName}");

            return token; 
        }

        private string GenerateToken(string name, string secret, string uri)
        {
            var privateKeyBytes = Convert.FromBase64String(secret);
            using var key = ECDsa.Create();
            key.ImportECPrivateKey(privateKeyBytes, out _);

            var payload = new Dictionary<string, object>
            {
                { "iss", "cdp" },
                { "nbf", Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) },
                { "exp", Convert.ToInt64((DateTime.UtcNow.AddMinutes(1) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) },
                { "sub", name },
                { "uri", uri }
            };

            var extraHeaders = new Dictionary<string, object>
            {
                { "typ", "JWT"},
                { "alg", "ES256"},
                { "kid", name },
                { "nonce", randomHex(16) }
            };

            var encodedToken = JWT.Encode(payload, key, JwsAlgorithm.ES256, extraHeaders);
            return encodedToken;
        }

        private string ParseKey(string key)
        {
            List<string> keyLines = new List<string>();
            keyLines.AddRange(key.Split('\n', StringSplitOptions.RemoveEmptyEntries));

            keyLines.RemoveAt(0); // Remove BEGIN line
            keyLines.RemoveAt(keyLines.Count - 1); // Remove END line

            return String.Join("", keyLines);
        }

        private string randomHex(int digits)
        {
            byte[] buffer = new byte[digits / 2];
            random.NextBytes(buffer);

            string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (digits % 2 == 0)
                return result;

            return result + random.Next(16).ToString("X");
        }
    }
}