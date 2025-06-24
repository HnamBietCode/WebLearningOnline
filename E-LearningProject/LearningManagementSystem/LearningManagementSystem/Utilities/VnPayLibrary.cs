using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace LearningManagementSystem.Utilities
{
    public class VnPayLibrary
    {
        public const string VERSION = "2.1.0";

        public static string GetIpAddress(HttpContext context)
        {
            string ipAddress;
            try
            {
                ipAddress = context.Connection.RemoteIpAddress.ToString();

                if (string.IsNullOrEmpty(ipAddress) || ipAddress.ToLower() == "unknown" || ipAddress.Length > 45)
                    ipAddress = "127.0.0.1";
            }
            catch (Exception ex)
            {
                ipAddress = "Invalid IP:" + ex.Message;
            }

            return ipAddress;
        }

        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }

        public static string CreateRequestUrl(string baseUrl, SortedList<string, string> requestData, string vnp_HashSecret)
        {
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            string queryString = data.ToString();
            queryString = queryString.Remove(queryString.Length - 1, 1); // Remove last '&'

            string signData = queryString;
            string vnp_SecureHash = HmacSHA512(vnp_HashSecret, signData);

            baseUrl += "?" + queryString + "&vnp_SecureHash=" + vnp_SecureHash;

            return baseUrl;
        }

        public static bool ValidateSignature(string inputHash, string secretKey, SortedList<string, string> requestData)
        {
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            string queryString = data.ToString();
            queryString = queryString.Remove(queryString.Length - 1, 1); // Remove last '&'

            string signData = queryString;
            string calHash = HmacSHA512(secretKey, signData);

            return calHash.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}