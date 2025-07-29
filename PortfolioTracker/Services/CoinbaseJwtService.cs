using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;

namespace PortfolioTracker.Services
{
    public class CoinbaseJwtService
    {
        private readonly string _keyName;
        private readonly string _privateKey;

        public CoinbaseJwtService(string keyName, string privateKey)
        {
            _keyName = keyName;
            _privateKey = privateKey;
        }

        public string GenerateJwtToken(string requestMethod, string requestPath)
        {
            var uri = $"{requestMethod} {requestPath}";
            var now = DateTimeOffset.UtcNow;

            // Načtěte privátní klíč a vytvořte podpis
            var ecPrivateKey = LoadEcPrivateKeyFromPem(_privateKey);
            var ecdsa = GetECDsaFromPrivateKey(ecPrivateKey);
            var securityKey = new ECDsaSecurityKey(ecdsa);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.EcdsaSha256);

            // Vytvořte JWT header
            var header = new JwtHeader(credentials);
            header["kid"] = _keyName;
            header["nonce"] = GenerateNonce();

            // Vytvořte JWT payload
            var payload = new JwtPayload
            {
                { "iss", "coinbase-cloud" },
                { "sub", _keyName },
                { "nbf", now.ToUnixTimeSeconds() },
                { "exp", now.AddMinutes(2).ToUnixTimeSeconds() },
                { "uri", uri }
            };

            var token = new JwtSecurityToken(header, payload);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateNonce(int length = 64)
        {
            byte[] nonceBytes = new byte[length / 2];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonceBytes);
            }
            return BitConverter.ToString(nonceBytes).Replace("-", "").ToLower();
        }

        private ECPrivateKeyParameters LoadEcPrivateKeyFromPem(string privateKeyInput)
        {
            string privateKeyPem;

            try
            {
                // Ověříme že je to validní base64
                var keyBytes = Convert.FromBase64String(privateKeyInput);

                // debug
                //if (keyBytes.Length > 10)
                //{
                //    // Pokud je to RSA klíč, vyhoď lepší chybovou zprávu
                //    throw new ArgumentException("Coinbase API klíč je pravděpodobně RSA typ. Potřebujete EC (Elliptic Curve) klíč. Vytvořte nový API klíč v Coinbase Developer Portal s typem 'EC'.");
                //}

                // Vytvoříme PEM formát pro BouncyCastle parser
                var base64Clean = privateKeyInput.Replace("\n", "").Replace("\r", "");

                // Rozdělíme na řádky po 64 znacích (PEM standard)
                var lines = new List<string>();
                for (int i = 0; i < base64Clean.Length; i += 64)
                {
                    int length = Math.Min(64, base64Clean.Length - i);
                    lines.Add(base64Clean.Substring(i, length));
                }

                privateKeyPem = "-----BEGIN EC PRIVATE KEY-----\n" +
                                string.Join("\n", lines) +
                                "\n-----END EC PRIVATE KEY-----";
            }
            catch (FormatException)
            {
                throw new ArgumentException("Private key musí být validní base64 string.");
            }

            // Nyní parsujeme PEM pomocí BouncyCastle
            using (var stringReader = new StringReader(privateKeyPem))
            {
                var pemReader = new PemReader(stringReader);
                var keyObject = pemReader.ReadObject();

                return keyObject switch
                {
                    AsymmetricCipherKeyPair keyPair => (ECPrivateKeyParameters)keyPair.Private,
                    ECPrivateKeyParameters privateKey => privateKey,
                    _ => throw new InvalidOperationException($"Neočekávaný typ klíče: {keyObject?.GetType()}")
                };
            }
        }

        private ECDsa GetECDsaFromPrivateKey(ECPrivateKeyParameters privateKey)
        {
            var q = privateKey.Parameters.G.Multiply(privateKey.D).Normalize();
            var qx = q.AffineXCoord.GetEncoded();
            var qy = q.AffineYCoord.GetEncoded();

            var ecdsaParams = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = { X = qx, Y = qy },
                D = privateKey.D.ToByteArrayUnsigned()
            };

            return ECDsa.Create(ecdsaParams);
        }
    }
}

