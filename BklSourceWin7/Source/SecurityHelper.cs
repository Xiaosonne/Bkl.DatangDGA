using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Bkl.Infrastructure
{
    public static class SecurityHelper
    {
        public static class RSAHelper
        {

            static public byte[] RSAEncrypt(byte[] DataToEncrypt, RSAParameters RSAKeyInfo, bool DoOAEPPadding)
            {
                try
                {
                    byte[] encryptedData;
                    //Create a new instance of RSACryptoServiceProvider.
                    using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                    {


                        //Import the RSA Key information. This only needs
                        //toinclude the public key information.
                        RSA.ImportParameters(RSAKeyInfo);


                        //Encrypt the passed byte array and specify OAEP padding. 
                        //OAEP padding is only available on Microsoft Windows XP or
                        //later. 
                        encryptedData = RSA.Encrypt(DataToEncrypt, DoOAEPPadding);
                    }
                    return encryptedData;
                }
                //Catch and display a CryptographicException 
                //to the console.
                catch (CryptographicException e)
                {
                    Console.WriteLine(e.Message);


                    return null;
                }


            }


            static public byte[] RSADecrypt(byte[] DataToDecrypt, RSAParameters RSAKeyInfo, bool DoOAEPPadding)
            {
                try
                {
                    byte[] decryptedData;
                    //Create a new instance of RSACryptoServiceProvider.
                    using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                    {
                        //Import the RSA Key information. This needs
                        //to include the private key information.
                        RSA.ImportParameters(RSAKeyInfo);


                        //Decrypt the passed byte array and specify OAEP padding. 
                        //OAEP padding is only available on Microsoft Windows XP or
                        //later. 
                        decryptedData = RSA.Decrypt(DataToDecrypt, DoOAEPPadding);
                    }
                    return decryptedData;
                }
                //Catch and display a CryptographicException 
                //to the console.
                catch (CryptographicException e)
                {
                    Console.WriteLine(e.ToString());


                    return null;
                }


            }


            public static String Encrypt(String plaintext, X509Certificate2 pubcrt)
            {
                X509Certificate2 _X509Certificate2 = pubcrt;
                using (RSACryptoServiceProvider RSACryptography = _X509Certificate2.PublicKey.Key as RSACryptoServiceProvider)
                {
                    Byte[] PlaintextData = Encoding.UTF8.GetBytes(plaintext);
                    int MaxBlockSize = RSACryptography.KeySize / 8 - 11;    //加密块最大长度限制


                    if (PlaintextData.Length <= MaxBlockSize)
                        return Convert.ToBase64String(RSACryptography.Encrypt(PlaintextData, false));


                    using (MemoryStream PlaiStream = new MemoryStream(PlaintextData))
                    using (MemoryStream CrypStream = new MemoryStream())
                    {
                        Byte[] Buffer = new Byte[MaxBlockSize];
                        int BlockSize = PlaiStream.Read(Buffer, 0, MaxBlockSize);


                        while (BlockSize > 0)
                        {
                            Byte[] ToEncrypt = new Byte[BlockSize];
                            Array.Copy(Buffer, 0, ToEncrypt, 0, BlockSize);


                            Byte[] Cryptograph = RSACryptography.Encrypt(ToEncrypt, false);
                            CrypStream.Write(Cryptograph, 0, Cryptograph.Length);


                            BlockSize = PlaiStream.Read(Buffer, 0, MaxBlockSize);
                        }


                        return Convert.ToBase64String(CrypStream.ToArray(), Base64FormattingOptions.None);
                    }
                }
            }


            public static String Decrypt(String ciphertext, X509Certificate2 prvpfx)
            {
                X509Certificate2 _X509Certificate2 = prvpfx;
                using (RSACryptoServiceProvider RSACryptography = _X509Certificate2.PrivateKey as RSACryptoServiceProvider)
                {
                    Byte[] CiphertextData = Convert.FromBase64String(ciphertext);
                    int MaxBlockSize = RSACryptography.KeySize / 8;    //解密块最大长度限制


                    if (CiphertextData.Length <= MaxBlockSize)
                        return Encoding.UTF8.GetString(RSACryptography.Decrypt(CiphertextData, false));


                    using (MemoryStream CrypStream = new MemoryStream(CiphertextData))
                    using (MemoryStream PlaiStream = new MemoryStream())
                    {
                        Byte[] Buffer = new Byte[MaxBlockSize];
                        int BlockSize = CrypStream.Read(Buffer, 0, MaxBlockSize);


                        while (BlockSize > 0)
                        {
                            Byte[] ToDecrypt = new Byte[BlockSize];
                            Array.Copy(Buffer, 0, ToDecrypt, 0, BlockSize);


                            Byte[] Plaintext = RSACryptography.Decrypt(ToDecrypt, false);
                            PlaiStream.Write(Plaintext, 0, Plaintext.Length);


                            BlockSize = CrypStream.Read(Buffer, 0, MaxBlockSize);
                        }


                        return Encoding.UTF8.GetString(PlaiStream.ToArray());
                    }
                }
            }


            private static X509Certificate2 RetrieveX509Certificate()
            {
                return null;    //检索用于 RSA 加密的 X509Certificate2 证书
            }

            //调用方法

            public static void doit()
            {

                //Create a UnicodeEncoder to convert between byte array and string.
                UnicodeEncoding ByteConverter = new UnicodeEncoding();

                //Create byte arrays to hold original, encrypted, and decrypted data.
                byte[] dataToEncrypt = ByteConverter.GetBytes("310991");
                byte[] encryptedData;

                X509Certificate2 pubcrt = new X509Certificate2(AppDomain.CurrentDomain.BaseDirectory + "cmb.cer");
                RSACryptoServiceProvider pubkey = (RSACryptoServiceProvider)pubcrt.PublicKey.Key;

                //X509Certificate2 prvcrt = new X509Certificate2(AppDomain.CurrentDomain.BaseDirectory + "bfkey.pfx", "123456789", X509KeyStorageFlags.Exportable);
                //RSACryptoServiceProvider prvkey = (RSACryptoServiceProvider)prvcrt.PrivateKey;

                encryptedData = RSAEncrypt(dataToEncrypt, pubkey.ExportParameters(false), false);
                string encryptedDataStr = Convert.ToBase64String(encryptedData);
                Console.WriteLine("Encrypted plaintext: {0}", Convert.ToBase64String(encryptedData));

                //decryptedData = EncrypHelp.RSADecrypt(encryptedData, prvkey.ExportParameters(true), false);
                //Console.WriteLine("Decrypted plaintext: {0}", ByteConverter.GetString(decryptedData));

                //加密长内容
                String data = @"RSA 是常用的非对称加密算法。最近使用时却出现了“不正确的长度”的异常，研究发现是由于待加密的数据超长所致。
                  .NET Framework 中提供的 RSA 算法规定：
                  待加密的字节数不能超过密钥的长度值除以 8 再减去 11（即：RSACryptoServiceProvider.KeySize / 8 - 11），而加密后得到密文的字节数，正好是密钥的长度值除以 8（即：RSACryptoServiceProvider.KeySize / 8）。
                  所以，如果要加密较长的数据，则可以采用分段加解密的方式，实现方式如下：";

                string encrypt = Encrypt(data, pubcrt);
                Console.WriteLine("Encrypted plaintext: {0}", encrypt);
                //string decrypt = EncrypHelp.Decrypt(encrypt, prvcrt);
                //Console.WriteLine("Decrypted plaintext: {0}", decrypt);

                //prvkey.Clear();
                pubkey.Clear();
                Console.Read();
            }
        }

        public static bool NotEmpty(this string str)
        {
            return !string.IsNullOrEmpty(str) && !string.IsNullOrWhiteSpace(str);
        }
        public static bool Empty(this string str)
        {
            return string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str);
        }
        public static byte[] HMACSHA1(string key, string dataToSign)
        {
            Byte[] secretBytes = UTF8Encoding.UTF8.GetBytes(key);
            HMACSHA1 hmac = new HMACSHA1(secretBytes);
            Byte[] dataBytes = UTF8Encoding.UTF8.GetBytes(dataToSign);
            return hmac.ComputeHash(dataBytes);
        }
        public static byte[] HMACSHA256(string dataToSign, string key)
        {
            Byte[] secretBytes = UTF8Encoding.UTF8.GetBytes(key);
            HMACSHA256 hmac = new HMACSHA256(secretBytes);
            Byte[] dataBytes = UTF8Encoding.UTF8.GetBytes(dataToSign);
            return hmac.ComputeHash(dataBytes);
        }
        public static uint MD5Hash(this string str)
        {
            var strToHash = str.Get32MD5();
            uint hash = 0;
            foreach (var item in strToHash.ToCharArray())
            {
                hash = hash * 131313 + item;
            }
            return hash;
        }
        public static uint BKDRHash(this string str)
        {
            uint hash = 0;
            foreach (var item in str.ToCharArray())
            {
                hash = hash * 131313 + item;
            }
            return hash;
        }
        public static byte[] Random(int len)
        {
            byte[] bts = new byte[len];
            using (RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                rNGCryptoServiceProvider.GetNonZeroBytes(bts);
                return bts;
            }
        }

        public static byte[] Sha256(this byte[] input)
        {
            if (input == null)
            {
                return null;
            }
            byte[] result;
            using (SHA256 sHA = SHA256.Create())
            {
                result = sHA.ComputeHash(input);
            }
            return result;
        }
        public static int Int(this string input) => int.Parse(input);
        public static long Int64(this string input) => long.Parse(input);
        public static byte[] Bytes(this string input) => Encoding.UTF8.GetBytes(input);
        public static string HexString(this byte[] input) => input.Aggregate(new StringBuilder(), (sb, a) => sb.Append(a.ToString("x2"))).ToString();
        public static string HexString(this ushort[] input) => input.Aggregate(new StringBuilder(), (sb, a) => sb.Append(a.ToString("x4"))).ToString();
        public static string String(this byte[] input) => Encoding.UTF8.GetString(input);
        public static byte[] HexStringDecode(this string input)
        {
            try
            {
                if (0 != (input.Length % 2))
                {
                    return null;
                }
                var arr = input.ToCharArray();
                byte[] data = new byte[arr.Length / 2];
                for (int i = 0; i < arr.Length; i += 2)
                {
                    data[i / 2] = byte.Parse(new string(arr, i, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                }
                return data;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static string HexSha256(this string raw)
        {
            return Encoding.UTF8.GetBytes(raw).Sha256().Aggregate(new StringBuilder(), (sb, a) => sb.Append(a.ToString("x2"))).ToString();
        }
        public static string Sha256(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            string result;
            using (SHA256 sHA = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                result = Convert.ToBase64String(sHA.ComputeHash(bytes));
            }
            return result;
        }

        //// <summary>
        /// 获取16位md5加密
        /// </summary>
        /// <param name="strSource">需要加密的明文</param>
        /// <returns>返回16位加密结果，该结果取32位加密结果的第9位到25位</returns>
        public static string Get16MD5(string source)
        {
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            //获取密文字节数组
            byte[] bytResult = md5.ComputeHash(System.Text.Encoding.Default.GetBytes(source));
            //转换成字符串，并取9到25位
            string strResult = BitConverter.ToString(bytResult, 4, 8);
            //BitConverter转换出来的字符串会在每个字符中间产生一个分隔符，需要去除掉
            strResult = strResult.Replace("-", "");
            return strResult.ToUpper();
        }
        public static string GUID32()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static string GUID16(string code = "")
        {
            return Get16MD5(string.IsNullOrEmpty(code) ? Guid.NewGuid().ToString("N") : code).ToLower();
        }

        public static string GetMd5(byte[] bytes)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            string t2 = BitConverter.ToString(md5.ComputeHash(bytes));
            t2 = t2.Replace("-", "");
            t2 = t2.ToLower();
            return t2;
        }

        //// <summary>
        /// 获取32位md5加密
        /// </summary>
        /// <param name="strSource">需要加密的明文</param>
        /// <returns>返回32位加密结果</returns>
        public static string Get32MD5(this string source)
        {
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            //获取密文字节数组
            byte[] bytResult = md5.ComputeHash(System.Text.Encoding.Default.GetBytes(source));
            //转换成字符串，32位
            string strResult = BitConverter.ToString(bytResult);
            //BitConverter转换出来的字符串会在每个字符中间产生一个分隔符，需要去除掉
            strResult = strResult.Replace("-", "");
            return strResult.ToUpper();
        }

        public static string Get32MD5Hex(this string source)
        {
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            //获取密文字节数组
            byte[] bytResult = md5.ComputeHash(System.Text.Encoding.Default.GetBytes(source));
            StringBuilder builder = new StringBuilder();
            foreach (var item in bytResult)
            {
                builder.Append(item.ToString("x2"));//再把加密后的密码转换为16进制,防止暴力破解
            }
            return builder.ToString();
        }
        //// <summary>
        /// 获取加盐32位md5加密
        /// </summary>
        /// <param name="strSource">需要加密的明文</param>
        /// <returns>返回32位加密结果</returns>
        public static string MD5SHA1Salt(string source, Func<string, string> salt = null)
        {
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            SHA1 sha1 = SHA1.Create();
            //加盐，以D和R做盐,进行加盐加密,这样就算你的原始密码泄露,加密后的密码也无法被破解
            byte[] data = Encoding.Default.GetBytes(salt == null ? "D" + source + "R" : salt(source));
            //先进行MD5加密,获取密文字节数组
            byte[] bytResult_md5 = md5.ComputeHash(data);
            //再进行SHA1二次加密
            byte[] bytResult_sha1 = sha1.ComputeHash(bytResult_md5);

            StringBuilder builder = new StringBuilder();
            foreach (var item in bytResult_sha1)
            {
                builder.Append(item.ToString("x2"));//再把加密后的密码转换为16进制,防止暴力破解
            }
            string password_with_encrypted = builder.ToString();//得到加密后的新密码
            md5.Clear();
            sha1.Clear();
            return password_with_encrypted.ToUpper();
        }
        public static string MD5Salt(string source, Func<string, string> salt = null)
        {
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            //加盐，以D和R做盐,进行加盐加密,这样就算你的原始密码泄露,加密后的密码也无法被破解
            byte[] data = Encoding.Default.GetBytes(salt == null ? "D" + source + "R" : salt(source));
            //先进行MD5加密,获取密文字节数组
            byte[] bytResult_md5 = md5.ComputeHash(data);
            StringBuilder builder = new StringBuilder();
            foreach (var item in bytResult_md5)
            {
                builder.Append(item.ToString("x2"));//再把加密后的密码转换为16进制,防止暴力破解
            }
            string password_with_encrypted = builder.ToString();//得到加密后的新密码
            md5.Clear();
            return password_with_encrypted.ToLower();
        }
        public static bool MD5SaltCompare(this string input, string compared, Func<string, string> salt = null)
        {
            var val = MD5SHA1Salt(input, salt);
            return string.Compare(val, compared) == 0;
        }
        public static bool MD5SaltCompare(this string input, string compared, Func<string, Func<string, string>, string>
          calc, Func<string, string> salt = null)
        {
            var val = calc(input, salt);
            return string.Compare(val, compared) == 0;
        }
        public static string MD5SaltEncrypt(this string input, Func<string, string> salt = null) =>
                MD5SHA1Salt(input, salt);

        public class AESHelper
        {
            /// <summary>
            /// 默认密钥-密钥的长度必须是32
            /// </summary>
            internal const string PublicKey = "1234567890123456";

            /// <summary>
            /// 默认向量
            /// </summary>
            private const string Iv = "abcdefghijklmnop";
            /// <summary>  
            /// AES加密  
            /// </summary>  
            /// <param name="str">需要加密字符串</param>  
            /// <returns>加密后字符串</returns>  
            public static String Encrypt(string str)
            {
                return Encrypt(str, PublicKey);
            }

            /// <summary>  
            /// AES解密  
            /// </summary>  
            /// <param name="str">需要解密字符串</param>  
            /// <returns>解密后字符串</returns>  
            public static String Decrypt(string str)
            {
                return Decrypt(str, PublicKey);
            }
            /// <summary>
            /// AES加密
            /// </summary>
            /// <param name="str">需要加密的字符串</param>
            /// <param name="key">32位密钥</param>
            /// <returns>加密后的字符串</returns>
            public static string Encrypt(string str, string key)
            {
                Byte[] keyArray = System.Text.Encoding.UTF8.GetBytes(key);
                Byte[] toEncryptArray = System.Text.Encoding.UTF8.GetBytes(str);
                var rijndael = new System.Security.Cryptography.RijndaelManaged();
                rijndael.Key = keyArray;
                rijndael.Mode = System.Security.Cryptography.CipherMode.ECB;
                rijndael.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
                rijndael.IV = System.Text.Encoding.UTF8.GetBytes(Iv);
                System.Security.Cryptography.ICryptoTransform cTransform = rijndael.CreateEncryptor();
                Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
                return Convert.ToBase64String(resultArray, 0, resultArray.Length);
            }
            /// <summary>
            /// AES解密
            /// </summary>
            /// <param name="str">需要解密的字符串</param>
            /// <param name="key">32位密钥</param>
            /// <returns>解密后的字符串</returns>
            public static string Decrypt(string str, string key)
            {
                Byte[] keyArray = System.Text.Encoding.UTF8.GetBytes(key);
                Byte[] toEncryptArray = Convert.FromBase64String(str);
                var rijndael = new System.Security.Cryptography.RijndaelManaged();
                rijndael.Key = keyArray;
                rijndael.Mode = System.Security.Cryptography.CipherMode.ECB;
                rijndael.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
                rijndael.IV = System.Text.Encoding.UTF8.GetBytes(Iv);
                System.Security.Cryptography.ICryptoTransform cTransform = rijndael.CreateDecryptor();
                Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
                return System.Text.Encoding.UTF8.GetString(resultArray);
            }
        }
        public static string AESEncrypt(this string input, string key1 = null)
        {
            try
            {
                return AESHelper.Encrypt(input, key1 ?? AESHelper.PublicKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
        public static string AESDecrypt(this string input, string key1 = null)
        {
            try
            {
                return AESHelper.Decrypt(input, key1 ?? AESHelper.PublicKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
    }
}
