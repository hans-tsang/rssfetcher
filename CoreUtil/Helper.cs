using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
//using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.Linq;
using System.Security.Cryptography;

namespace ZTAMPZ_EMAIL_SWF.CoreUtil
{
    public class Helper
    {
        public const string YEAR_TO_DAY_DATETIME_FORMAT_FOR_DOTNET = "yyyy'-'MM'-'dd";
        public const string YEAR_TO_SECOND_DATETIME_FORMAT_FOR_DOTNET = "yyyy'-'MM'-'dd HH':'mm':'ss";
        public const string SCHEDULE_TYPE_NAME =
       "ExtensibleSchedulerApplication.TableDataCopySchedulerLibrary.TransactedDataCopyScheduler,TableDataCopySchedulerLibrary";

        //private static JavaScriptSerializer DefaultJavaScriptSerializer = new JavaScriptSerializer();

        private static HashSet<byte> PercentEncodeSafeBytes;

        static Helper()
        {
            PercentEncodeSafeBytes = new HashSet<byte>();
            for (char c = 'A'; c <= 'Z'; c++)
            {
                PercentEncodeSafeBytes.Add((byte)c);
            }
            for (char c = 'a'; c <= 'z'; c++)
            {
                PercentEncodeSafeBytes.Add((byte)c);
            }
            for (char c = '0'; c <= '9'; c++)
            {
                PercentEncodeSafeBytes.Add((byte)c);
            }
            PercentEncodeSafeBytes.Add((byte)'-');
            PercentEncodeSafeBytes.Add((byte)'_');
            PercentEncodeSafeBytes.Add((byte)'.');
            PercentEncodeSafeBytes.Add((byte)'~');
        }

        public class ListEqualityComparer<T> : IEqualityComparer, IEqualityComparer<IList<T>>
        {
            private static readonly int[] MULTIPLIERS = new int[] { 1, 3, 5, 7, 11, 13, 17, 19, 23, 27, 31 };

            public bool Equals(IList<T> x, IList<T> y)
            {
                if ((x == null) && (y == null))
                {
                    return true;
                }
                if ((x == null) ^ (y == null))
                {
                    return false;
                }
                // x and y should not be null at this point

                if (x.Count != y.Count)
                {
                    return false;
                }

                for (int i = 0; i < x.Count; i++)
                {
                    if (!object.Equals(x[i], y[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            public int GetHashCode(IList<T> obj)
            {
                if (obj == null)
                {
                    return 0;
                }
                int hash = 0;
                int count = Math.Min(MULTIPLIERS.Length, obj.Count);
                unchecked
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (obj[i] != null)
                        {
                            hash += obj[i].GetHashCode() * MULTIPLIERS[i];
                        }
                    }
                }
                return hash;
            }

            bool IEqualityComparer.Equals(object x, object y)
            {
                if ((x != null && !(x is IList<T>)) || (y != null && !(y is IList<T>)))
                {
                    throw new ArgumentException();
                }
                return this.Equals((IList<T>)x, (IList<T>)y);
            }

            int IEqualityComparer.GetHashCode(object obj)
            {
                if (obj != null && !(obj is IList<T>))
                {
                    return obj.GetHashCode();
                }
                return this.GetHashCode((IList<T>)obj);
            }
        }

        private class Lookup<TKey, TElement> : ILookup<TKey, TElement>
        {
            private class Grouping : IGrouping<TKey, TElement>
            {
                private TKey key;
                private IEnumerable<TElement> elements;
                public Grouping(TKey key, IEnumerable<TElement> elements)
                {
                    this.key = key;
                    this.elements = elements;
                }

                public TKey Key
                {
                    get
                    {
                        return key;
                    }
                }

                public IEnumerator<TElement> GetEnumerator()
                {
                    return elements.GetEnumerator();
                }

                IEnumerator System.Collections.IEnumerable.GetEnumerator()
                {
                    return ((IEnumerable)elements).GetEnumerator();
                }
            }

            private IList<TKey> keysArray;
            private Dictionary<TKey, IList<TElement>> keysToValues;

            public Lookup(IList<TKey> keysArray, Dictionary<TKey, IList<TElement>> keysToValues)
            {
                this.keysArray = keysArray;
                this.keysToValues = keysToValues;
            }

            public bool Contains(TKey key)
            {
                return keysToValues.ContainsKey(key);
            }

            public int Count
            {
                get
                {
                    return this.keysArray.Count;
                }
            }

            public IEnumerable<TElement> this[TKey key]
            {
                get
                {
                    if (!keysToValues.ContainsKey(key))
                    {
                        return new TElement[0];
                    }
                    else
                    {
                        return keysToValues[key];
                    }
                }
            }

            public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
            {
                foreach (var key in this.keysArray)
                {
                    yield return new Grouping(key, this.keysToValues[key]);
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        public class RESTRequestParameters
        {
            public string Url { get; set; }
            public string XHttpMethod { get; set; }
            public object RequestBody { get; set; }
            public bool ParseJsonResponse { get; set; }
            public string LoginId { get; set; }
            public string LoginPassword { get; set; }
            public string LoginDomain { get; set; }

            private Dictionary<string, string> requestHeaderOverride = new Dictionary<string, string>();
            public Dictionary<string, string> RequestHeaderOverride
            {
                get
                {
                    return requestHeaderOverride;
                }
            }
        }

        public static string ConcatStringList(IEnumerable<string> stringList, string separator = ", ", string quote = "")
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string s in stringList)
            {
                stringBuilder.Append(quote).Append(s).Append(quote).Append(separator);
            }

            if (stringBuilder.Length > 0)
            {
                stringBuilder.Length -= separator.Length;
            }
            return stringBuilder.ToString();
        }

        public static string ConcatStringListEscaped(IEnumerable<string> stringList, string separator = ",", char quote = '"')
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string s in stringList)
            {
                if (s.Contains(quote) || s.Contains(separator))
                {
                    stringBuilder.Append(quote).Append(s.Replace(quote.ToString(), quote.ToString() + quote.ToString())).Append(quote);
                }
                else
                {
                    stringBuilder.Append(s);
                }
                stringBuilder.Append(separator);
            }

            if (stringBuilder.Length > 0)
            {
                stringBuilder.Length -= separator.Length;
            }
            return stringBuilder.ToString();
        }

        public static string[] SplitString(string s, char separator, char? quote)
        {
            // state = 0: Ready to read next token
            // state = 1: Reading a token, and not inside a qoute
            // state = 2: Reading a token, and inside a qoute
            // state = 3: Reading a token, and inside a qoute, and just parsed a qoute(expecting another qoute or a separator at next)
            int state = 0;
            var result = new List<string>();
            var temp = new StringBuilder();
            foreach (char c in s)
            {
                if (state <= 1)
                {
                    if (state == 0 && c == quote)
                    {
                        state = 2;
                    }
                    else if (c == separator)
                    {
                        result.Add(temp.ToString());
                        temp.Length = 0;
                        state = 0;
                    }
                    else
                    {
                        temp.Append(c);
                        state = 1;
                    }
                }
                else
                {
                    if (state == 2)
                    {
                        if (c == quote)
                        {
                            state = 3;
                        }
                        else
                        {
                            temp.Append(c);
                        }
                    }
                    else
                    {
                        if (c == quote)
                        {
                            temp.Append(c);
                            state = 2;
                        }
                        else if (c == separator)
                        {
                            result.Add(temp.ToString());
                            temp.Length = 0;
                            state = 0;
                        }
                        else
                        {
                            throw new ArgumentException();
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(s))
            {
                result.Add(temp.ToString());
            }
            return result.ToArray();
        }

        public static int FirstIndex<T>(IList<T> list, Func<T, bool> predicate)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public static int LastIndex<T>(IList<T> list, Func<T, bool> predicate)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (predicate(list[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public static ILookup<TKey, TSource> ToStableLookup<TSource, TKey>(
            IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return ToStableLookup<TSource, TKey, TSource>(source, keySelector, s => s);
        }

        public static ILookup<TKey, TSource> ToStableLookup<TSource, TKey>(
            IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return ToStableLookup<TSource, TKey, TSource>(source, keySelector, s => s, comparer);
        }

        public static ILookup<TKey, TElement> ToStableLookup<TSource, TKey, TElement>(
            IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return ToStableLookup<TSource, TKey, TElement>(source, keySelector, elementSelector, EqualityComparer<TKey>.Default);
        }

        public static ILookup<TKey, TElement> ToStableLookup<TSource, TKey, TElement>(
            IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            var keysArray = new List<TKey>();
            var dict = new Dictionary<TKey, IList<TElement>>(comparer);
            foreach (var s in source)
            {
                var key = keySelector(s);
                if (!dict.ContainsKey(key))
                {
                    dict[key] = new List<TElement>();
                    keysArray.Add(key);
                }
                dict[key].Add(elementSelector(s));
            }

            return new Lookup<TKey, TElement>(keysArray, dict);
        }

        public static IEnumerable<T> StableDistinct<T>(IEnumerable<T> source)
        {
            return StableDistinct(source, EqualityComparer<T>.Default);
        }

        public static IEnumerable<T> StableDistinct<T>(IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            HashSet<T> set = new HashSet<T>(comparer);
            foreach (var s in source)
            {
                if (!set.Contains(s))
                {
                    set.Add(s);
                    yield return s;
                }
            }
        }

        public static string PercentEncode(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                if (PercentEncodeSafeBytes.Contains(b))
                {
                    sb.Append((char)b);
                }
                else
                {
                    sb.AppendFormat("%{0:X2}", b);
                }
            }
            return sb.ToString();
        }

        public static TValue TryGetValueFromDictionary<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            dictionary.TryGetValue(key, out value);
            return value;
        }

        /// <summary>
        /// Validate the inputStream is of the specified MIME type.
        /// The inputStream will be fully read and stored in a temporary file.
        /// After disposing the returned stream, the temporary file will be deleted.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="fileTypeMime"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static Stream ValidateFileType(Stream inputStream, string fileTypeMime, out bool result)
        {
            // 512 KiB buffer
            int bufferSize = (512 << 10);
            byte[] buffer = new byte[bufferSize];

            string tempPath = Path.GetTempPath();
            string tempFilePath;
            do
            {
                tempFilePath = tempPath + (tempPath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? "" : Path.DirectorySeparatorChar.ToString()) + Guid.NewGuid().ToString() + ".tmp";
            }
            while (File.Exists(tempFilePath));

            Stream fileStream = File.Create(tempFilePath, bufferSize, FileOptions.DeleteOnClose | FileOptions.RandomAccess);

            int read;
            while ((read = inputStream.Read(buffer, 0, bufferSize)) > 0)
            {
                fileStream.Write(buffer, 0, read);
            }
            inputStream.Dispose();

            fileStream.Seek(0, SeekOrigin.Begin);
            int totalRead = 0;
            while ((read = fileStream.Read(buffer, totalRead, bufferSize - totalRead)) > 0)
            {
                totalRead += read;
            }
            fileStream.Seek(0, SeekOrigin.Begin);

            var equalityComparer = new ListEqualityComparer<byte>();
            byte[] startingMagicNumberBytes;

            unchecked
            {
                switch (fileTypeMime)
                {
                    case "application/pdf":
                    case "application/x-pdf":
                        startingMagicNumberBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
                        break;
                    default:
                        throw new ArgumentException("MIME " + fileTypeMime + " is not supported.");
                }
            }
            result = equalityComparer.Equals(startingMagicNumberBytes, buffer.Take(startingMagicNumberBytes.Length).ToList());
            return fileStream;
        }

        public static XElement GenerateInlineScriptOrStyleBlock(XName tagName, string text, string commentStart = "/*", string commentEnd = "*/")
        {
            return
                new XElement(
                    tagName,
                    commentStart,
                    new XCData(commentEnd + text + commentStart),
                    commentEnd
                );
        }

        public static void SendMail(SmtpClient smtpClient, string subject, string body, string senderAddress, IEnumerable<string> toAddresses, out HashSet<string> invalidToAddresses, string encodingName = "UTF-8")
        {
            invalidToAddresses = new HashSet<string>();
            var toAddressesMod = toAddresses.Select(t => (t ?? "").Trim()).Distinct().Where(t => !string.IsNullOrEmpty(t)).ToList();
            if (toAddressesMod.Count == 0)
            {
                throw new Exception("No recipients");
            }

            try
            {
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(senderAddress);
                foreach (string toAddress in toAddressesMod)
                {
                    try
                    {
                        mailMessage.To.Add(new MailAddress(toAddress));
                    }
                    catch (Exception e2)
                    {
                        invalidToAddresses.Add(toAddress);
                    }
                }

                mailMessage.SubjectEncoding = Encoding.GetEncoding(encodingName);
                mailMessage.BodyEncoding = Encoding.GetEncoding(encodingName);

                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = true;
                smtpClient.Send(mailMessage);
            }
            catch (SmtpFailedRecipientException e3)
            {
                HashSet<string> invalidToAddresses2 = invalidToAddresses;

                Action<SmtpFailedRecipientException> handleSmtpFailedRecipientException = (e) =>
                {
                    string failedTo = e.FailedRecipient;
                    if (failedTo.StartsWith("<") && failedTo.EndsWith(">"))
                    {
                        failedTo = failedTo.Substring(1, failedTo.Length - 2);
                    }

                    invalidToAddresses2.Add(failedTo);
                };
                SmtpFailedRecipientsException es = e3 as SmtpFailedRecipientsException;
                if (es != null)
                {
                    foreach (SmtpFailedRecipientException e in es.InnerExceptions)
                    {
                        handleSmtpFailedRecipientException(e);
                    }
                }
                else
                {
                    handleSmtpFailedRecipientException(e3);
                }
            }

            if (toAddressesMod.Count == invalidToAddresses.Count)
            {
                throw new Exception("Cannot be sent to any recipients");
            }
        }

        public static string XsltTransform(string rawXmlString, string rawXsltString, XsltArgumentList argumentList = null, XmlWriterSettings xmlWriterSettings = null)
        {
            using (var xsltStringReader = new StringReader(rawXsltString))
            {
                using (var xsltReader = XmlReader.Create(xsltStringReader))
                {
                    XslCompiledTransform xslt = new XslCompiledTransform();
                    xslt.Load(xsltReader);

                    using (var xmlStringReader = new StringReader(rawXmlString))
                    {
                        using (var xmlReader = XmlReader.Create(xmlStringReader))
                        {
                            using (var resultStringWriter = new StringWriter())
                            {
                                using (var resultXmlWriter = XmlWriter.Create(resultStringWriter, xmlWriterSettings))
                                {
                                    if (argumentList != null)
                                    {
                                        xslt.Transform(xmlReader, argumentList, resultXmlWriter);
                                    }
                                    else
                                    {
                                        xslt.Transform(xmlReader, resultXmlWriter);
                                    }

                                    return resultStringWriter.ToString();
                                }
                            }
                        }
                    }
                }
            }
        }

        public static T GetXElementValue<T>(XElement xElement)
        {
            if (xElement == null || (xElement.Value == "" && typeof(T) != typeof(string)))
            {
                return (T)(object)null;
            }

            return DatabaseHelper.ConvertFromDatabaseDataObject<T>(xElement.Value);
        }

        public static void AddAll<TKey, TValue>(IDictionary<TKey, TValue> destination,
            IDictionary<TKey, TValue> newValues)
        {
            foreach (KeyValuePair<TKey, TValue> pair in newValues)
            {
                destination[pair.Key] = pair.Value;
            }
        }

        public static void AddAll<E1, E2>(ICollection<E1> destination,
            IEnumerable<E2> newValues) where E2 : E1
        {
            foreach (E2 e in newValues)
            {
                destination.Add(e);
            }
        }

        public static class Crypto
        {
            public static byte[] _salt = Encoding.ASCII.GetBytes("aw;?1d??2xh3!vxse=3a,=6gzai5@`u&lnq$1+p;");

            public static string Encrypt(string plainText, string sharedSecret)
            {
                if (string.IsNullOrEmpty(plainText)) return "";

                //            throw new ArgumentNullException("plainText");
                if (string.IsNullOrEmpty(sharedSecret))
                    throw new ArgumentNullException("sharedSecret");

                string outStr = null;                       // Encrypted string to return
                RijndaelManaged aesAlg = null;              // RijndaelManaged object used to encrypt the data.

                try
                {
                    Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, Crypto._salt);
                    aesAlg = new RijndaelManaged();
                    aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                        msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(plainText);
                            }
                        }
                        outStr = Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
                finally
                {
                    if (aesAlg != null)
                        aesAlg.Clear();
                }

                return outStr;
            }

            public static string shared = "$d9yoodrs8";

            public static string Decrypt(string cipherText, string sharedSecret)
            {
                if (string.IsNullOrEmpty(cipherText)) return "";

                //            throw new ArgumentNullException("cipherText");
                if (string.IsNullOrEmpty(sharedSecret))
                    throw new ArgumentNullException("sharedSecret");

                RijndaelManaged aesAlg = null;
                string plaintext = null;

                try
                {
                    Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, Crypto._salt);
                    byte[] bytes = Convert.FromBase64String(cipherText);
                    using (MemoryStream msDecrypt = new MemoryStream(bytes))
                    {
                        aesAlg = new RijndaelManaged();
                        aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                        aesAlg.IV = Crypto.ReadByteArray(msDecrypt);
                        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                                plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
                catch
                {
                    return cipherText;
                }
                finally
                {
                    if (aesAlg != null)
                        aesAlg.Clear();
                }

                return plaintext;
            }

            private static byte[] ReadByteArray(Stream s)
            {
                byte[] rawLength = new byte[sizeof(int)];
                if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
                {
                    return null;
                    //            throw new SystemException("Invalid string");
                }

                byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
                if (s.Read(buffer, 0, buffer.Length) != buffer.Length)
                {
                    return null;
                    //            throw new SystemException("Did not read byte array properly");
                }

                return buffer;
            }
        }
    }

}