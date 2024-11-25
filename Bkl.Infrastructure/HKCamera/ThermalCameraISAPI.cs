using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Bkl.Infrastructure
{
    public class XmlIgnoreNSTextReader : XmlTextReader
    {
        public XmlIgnoreNSTextReader(Stream ms) : base(ms) { }
        public override string NamespaceURI => "";
    }
    public class ThermalCameraISAPI
    {
        static string DefaultNamespace = "http://www.std-cgi.com/ver20/XMLSchema";

        //"http://www.isapi.org/ver20/XMLSchema"

        public static void SetBigNamespace()
        {
            DefaultNamespace = "http://www.isapi.org/ver20/XMLSchema";

        }
        public static void SetSmallNamespace()
        {
            DefaultNamespace = "http://www.std-cgi.com/ver20/XMLSchema";
        }
        private string _thermalIp;
        private int _thermalPort;
        private string _userName;
        private string _passWord;
        private HttpClient _httpClient;

        public ThermalCameraISAPI(string thermalIp, int thermalPort, string userName, string passWord)
        {
            _thermalIp = thermalIp;
            //_thermalPort = thermalPort;
            _thermalPort = thermalPort;
            _userName = userName;
            _passWord = passWord;
            _httpClient = new HttpClient(new HttpClientHandler
            {
                Credentials = new NetworkCredential("admin", "bkl666666")
            });
        }
        public class HttpSegment
        {
            public string HeaderName { get; set; }
            public string HeaderValue { get; set; }
            public int ContentLength => int.TryParse(HeaderValue, out var v1) ? v1 : 0;

            public bool IsContentLength => HeaderName == "Content-Length";
            public bool IsContentType => HeaderName.ToLower() == "content-type";
            public bool IsTemperature => IsContentType && HeaderValue == "application/octet-stream";
            public bool IsJson => IsContentType && HeaderValue.Contains("application/json");
            public bool IsJPEG => IsContentType && (HeaderValue == "image/jpeg" || HeaderValue == "image/pjpeg");

            public static HttpSegment Parse(string line)
            {
                HttpSegment seg = new HttpSegment();
                var i = line.IndexOf(":");
                seg.HeaderName = line.Substring(0, i).Trim();
                seg.HeaderValue = line.Substring(i + 1, line.Length - i - 1).Trim();
                return seg;
            }
            public override string ToString()
            {
                return $"{HeaderName}:{HeaderValue}";
            }
        }
        public class HttpBoundaryData
        {
            public List<HttpSegment> Segments { get; set; }
            public byte[] Content { get; set; }
            private float[] _temps;

            public bool IsJpegData { get => Segments.Any(s => s.IsJPEG); }
            public bool IsTempratureData { get => Segments.Any(s => s.IsTemperature); }
            public bool IsJsonData { get => Segments.Any(s => s.IsJson); }

            public Object ReadData()
            {
                var content = Segments.Where(s => s.IsContentType).FirstOrDefault();
                switch (content.HeaderValue)
                {
                    case var str when content.HeaderValue.Contains("application/json"):
                        return Encoding.UTF8.GetString(Content);
                    case "image/jpeg":
                    case "image/pjpeg":
                        return Content;
                    case "application/octet-stream":
                        if (_temps == null)
                            _temps = ReadAsTemperature();
                        return _temps;
                    default:
                        return null;
                }
            }

            public T ReadAsJsonObject<T>()
            {
                string str = Encoding.UTF8.GetString(Content);
                return JsonSerializer.Deserialize<T>(str);
            }
            public float[] ReadAsTemperature()
            {
                float[] da = new float[160 * 120];
                for (int i = 0; i < 160 * 120; i++)
                {
                    byte[] num = new byte[] {
                        Convert.ToByte(255&Content[i*4+0]),
                        Convert.ToByte(255&Content[i * 4 + 1]),
                        Convert.ToByte(255&Content[i * 4 + 2]),
                        Convert.ToByte(255&Content[i*4+3]) };
                    var data2 = BitConverter.ToSingle(num, 0);
                    da[i] = data2;
                }
                return da;
            }
            public Bitmap ReadAsBitmap()
            {
                var temps = ReadAsTemperature();
                Bitmap map = new Bitmap(160, 120);
                var bitmapData = map.LockBits(new Rectangle(0, 0, 160, 120), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                var max = temps.Max();
                var min = temps.Min();
                byte[] rgb = new byte[bitmapData.Stride * 120];
                for (int i = 0; i < temps.Length; i++)
                {
                    rgb[i * 3 + 0] = (byte)(((temps[i] - min) / (max - min)) * 255);
                    rgb[i * 3 + 1] = (byte)(((temps[i] - min) / (max - min)) * 255);
                    rgb[i * 3 + 2] = (byte)(((temps[i] - min) / (max - min)) * 255);
                }
                Marshal.Copy(rgb, 0, bitmapData.Scan0, bitmapData.Stride * 120);
                map.UnlockBits(bitmapData);
                return map;
            }
        }


        public async Task<List<HttpBoundaryData>> GetThermalJpeg()
        {

            var uri = $"http://{_thermalIp}:{_thermalPort}/ISAPI/Thermal/channels/2/thermometry/jpegPicWithAppendData?format=json";
            var resp = await _httpClient.GetAsync(uri);
            var stream = await resp.Content.ReadAsStreamAsync();
            MemoryStream bufferStream = new MemoryStream();
            await stream.CopyToAsync(bufferStream);
            bufferStream.Seek(0, SeekOrigin.Begin);

            StreamReader sr = new StreamReader(bufferStream, Encoding.ASCII);
            string line = await sr.ReadLineAsync();

            List<HttpBoundaryData> httpBoundaryDatas = new List<HttpBoundaryData>();
            while (line != null && string.Compare(line, "--boundary--") != 0)
            {
                while (string.Compare(line, "--boundary") != 0 && string.Compare(line, "--boundary--") != 0)
                {
                    line = await sr.ReadLineAsync();
                }
                if (line == "--boundary--")
                    break;
                HttpBoundaryData data = new HttpBoundaryData();
                data.Segments = new List<HttpSegment>();
                line = await sr.ReadLineAsync();
                while (string.Compare(line, "") != 0)
                {
                    data.Segments.Add(HttpSegment.Parse(line));
                    line = await sr.ReadLineAsync();
                }
                var seg = data.Segments.Where(s => s.IsContentLength).FirstOrDefault();
                var readPos = ReadPosition(sr);

                bufferStream.Seek(readPos, SeekOrigin.Begin);
                BinaryReader br = new BinaryReader(bufferStream, Encoding.ASCII);
                byte[] chs = br.ReadBytes(seg.ContentLength);
                data.Content = chs;// chs.Select(s=>(char)s).ToArray();
                sr.DiscardBufferedData();
                sr.BaseStream.Seek(readPos + seg.ContentLength, SeekOrigin.Begin);
                httpBoundaryDatas.Add(data);
                line = await sr.ReadLineAsync();

            }
            return httpBoundaryDatas;
        }
        public async Task<List<ThermalMeasureRule>> GetThermalRules()
        {
            var uri = $"http://{_thermalIp}:{_thermalPort}/ISAPI/Thermal/channels/2/thermometry/1/regions";
            var stream = await _httpClient.GetStreamAsync(uri);

            var data = XmlDeserializeString<ThermalXmlObject.ThermometryRegionList>(stream);

            return data.ThermometryRegion.Select(s => new ThermalMeasureRule
            {
                ruleName = s.name,
                ruleId = s.id,
                enabled = (byte)(s.enabled ? 1 : 0),
                regionType = s.type == "region" ? 1 : (s.type == "point" ? 0 : 2),
                regionPoints = s.type == "point" ? new List<double[]>
                              {
                                new double[]{
                                    0,
                                    s.Point.CalibratingCoordinates.positionX/1000.0,
                                   (1000.0- s.Point.CalibratingCoordinates.positionY)/1000.0
                                }
                              } : s.Region.RegionCoordinatesList.Select(s => new double[]
                              {
                                  0,
                                  s.positionX/1000.0,
                                ( 1000.0- s.positionY)/1000.0
                              }).ToList()
            }).ToList();
        }
        public async Task<string> GetDeviceThermalTemp()
        {
            var uri = $"http://{_thermalIp}:{_thermalPort}/ISAPI/Thermal/TempHumi/channels/2";
            var response = await _httpClient.GetStringAsync(uri);
            return response;
        }
        public async Task<ThermalXmlObject.ResponseStatus> SetThermalBasic(ThermalXmlObject.ThermometryBasicParam param1 = null)
        {
            var uri = $"http://{_thermalIp}:{_thermalPort}/ISAPI/Thermal/channels/2/thermometry/basicParam";
            ThermalXmlObject.ThermometryBasicParam param = new ThermalXmlObject.ThermometryBasicParam
            {
                id = 2,
            };
            param.streamOverlay = param1 == null ? param.streamOverlay : param1.streamOverlay;
            param.pictureOverlay = param1 == null ? param.pictureOverlay : param1.pictureOverlay;

            var reqStr = XmlSerializeString(param);

            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Put, uri);
            req.Content = new StringContent(reqStr, Encoding.UTF8, "application/xml");
            var resp = await _httpClient.SendAsync(req);
            var text = await resp.Content.ReadAsStringAsync();
            return XmlDeserializeString<ThermalXmlObject.ResponseStatus>(text);
        }
        public async Task<ThermalXmlObject.ThermometryBasicParam> GetThermalBasic()
        {
            var uri = $"http://{_thermalIp}:{_thermalPort}/ISAPI/Thermal/channels/2/thermometry/basicParam";
            var text = await _httpClient.GetStringAsync(uri);
            return XmlDeserializeString<ThermalXmlObject.ThermometryBasicParam>(text);
        }
        string XmlSerializeString<T>(T obj)
        {
            MemoryStream ms = new MemoryStream();
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            serializer.Serialize(ms, obj);
            return Encoding.UTF8.GetString(ms.ToArray());
        }
        T XmlDeserializeString<T>(Stream ms)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (XmlTextReader xmlTextReader = new XmlTextReader(ms))
            {
                xmlTextReader.Namespaces = false;
                return (T)serializer.Deserialize(xmlTextReader);
            }
        }
        T XmlDeserializeString<T>(string ms)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (TextReader tr = new StringReader(ms))
            using (XmlTextReader xmlTextReader = new XmlTextReader(tr))
            {
                xmlTextReader.Namespaces = false;
                return (T)serializer.Deserialize(xmlTextReader);
            }
        }
        public async Task<ThermalXmlObject.ResponseStatus> SetThermalRule(ThermalMeasureRule rule)
        {
            var re = new ThermalXmlObject.ThermometryRegion
            {
                id = rule.ruleId,
                enabled = rule.enabled == 1,
                name = rule.ruleName,
                emissivity = "0.95",
                distance = "2",
                reflectiveEnable = false,
                reflectiveTemperature = "20",
                type = rule.regionType == 0 ? "point" : (rule.regionType == 1 ? "region" : "line"),
                distanceUnit = "meter",
                emissivityMode = "customsettings",
            };
            if (rule.regionType != 0)
                re.Region = new ThermalXmlObject.Region
                {
                    RegionCoordinatesList = rule.regionPoints.Select(s => new ThermalXmlObject.Coordinates
                    {
                        positionX = Convert.ToInt32(s[0] * 1000),
                        positionY = Convert.ToInt32(s[1] * 1000),
                    }).ToArray()
                };
            else
                re.Point = new ThermalXmlObject.Point
                {
                    CalibratingCoordinates = new ThermalXmlObject.Coordinates
                    {
                        positionX = Convert.ToInt32(rule.regionPoints[0][0] * 1000),
                        positionY = Convert.ToInt32(rule.regionPoints[0][1] * 1000),
                    }
                };
            ThermalXmlObject.ThermometryRegionList lis = new ThermalXmlObject.ThermometryRegionList
            {
                version = "2.0",
                ThermometryRegion = new ThermalXmlObject.ThermometryRegion[]
                {
                    re
                }
            };
            var uri = $"http://{_thermalIp}:{_thermalPort}/ISAPI/Thermal/channels/2/thermometry/1/regions";
            var str = XmlSerializeString(lis);
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Put, uri);
            req.Content = new StringContent(str, Encoding.UTF8, "application/xml");
            var resp = await _httpClient.SendAsync(req);
            var text = await resp.Content.ReadAsStringAsync();
            return XmlDeserializeString<ThermalXmlObject.ResponseStatus>(text);
        }
        public async Task<ThermalRuleTemperatureResponse> ReadThermalMetryOnceAsync()
        {
            var uri = $"http://{_thermalIp}:{_thermalPort}/ISAPI/Thermal/channels/2/thermometry/1/rulesTemperatureInfo?format=json";
            var resp = await _httpClient.GetAsync(uri);
            var str = await resp.Content.ReadAsStringAsync();
            var resp1 = TryCatchExtention.TryCatch(str1 => JsonSerializer.Deserialize<ThermalRuleTemperatureResponse>(str1), str.ToString());
            return resp1;
        }

        public async IAsyncEnumerable<ThermalRealtimeMetryResponse> ReadThermalMetryAsync([EnumeratorCancellation] CancellationToken token)
        {
            var uri = $"http://{_thermalIp}:{_thermalPort}/ISAPI/Thermal/channels/2/thermometry/realTimethermometry/rules?format=json";
            HttpWebRequest req = HttpWebRequest.CreateHttp(uri);
            var credentialCache = new CredentialCache();
            credentialCache.Add(new Uri(uri), "Digest", new NetworkCredential(_userName, _passWord));
            req.Credentials = credentialCache;
            req.Method = "GET";
            req.Timeout = 1000;
            var resp = await req.GetResponseAsync();
            var stream = resp.GetResponseStream();
            var sr = new StreamReader(stream);
            var line = await sr.ReadLineAsync();
            while (!sr.EndOfStream && !token.IsCancellationRequested)
            {
                while (string.Compare("--boundary", line) != 0)
                {
                    line = await sr.ReadLineAsync();
                }
                while (string.Compare("", line) != 0)
                {
                    line = await sr.ReadLineAsync();
                }
                // var contentType = await sr.ReadLineAsync();
                // var contentLength = await sr.ReadLineAsync();
                // var lineChange = await sr.ReadLineAsync();

                var sb = new StringBuilder();
                while (string.Compare("--boundary", line) != 0)
                {
                    sb.Append(line.Trim());
                    line = await sr.ReadLineAsync();
                }
                // var upload = JsonSerializer.Deserialize<ThermalUpload>(sb.ToString());
                // Console.WriteLine("ResponseBegin:" + sb.ToString() + ":ResponseEnd");
                // Console.WriteLine("lineBegin:" + line + "lineEnd");



                var resp1 = TryCatchExtention.TryCatch((str) => JsonSerializer.Deserialize<ThermalRealtimeMetryResponse>(str), sb.ToString());
                yield return resp1;
            }
            try
            {
                req.Abort();
            }
            catch
            {

            }
        }

        Int32 ReadPosition(StreamReader s)
        {
            Int32 charpos = (Int32)s.GetType().InvokeMember("_charPos",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField
                    , null, s, null);

            Int32 charlen = (Int32)s.GetType().InvokeMember("_charLen",
            BindingFlags.DeclaredOnly |
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.GetField
             , null, s, null);

            return (Int32)s.BaseStream.Position - charlen + charpos;
        }

    }



}
