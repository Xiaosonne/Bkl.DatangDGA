using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Bkl.Infrastructure
{
    public class UniviewHelper
    {
        public class SetThermalRuleRequest
        {
            public int id { get; set; }
            public string name { get; set; }
            public int enabled { get; set; }
            public int shape { get; set; }
            /// <summary>
            /// 0-4 最高 最低 平均 温度差 温度变化率
            /// </summary>
            public int type { get; set; }
            /// <summary>
            /// 0-2 低于 高于 匹配
            /// </summary>
            public int condition { get; set; }
            public float threshold { get; set; }
            /// <summary>
            /// 误差
            /// </summary>
            public float range { get; set; }
            /// <summary>
            /// 时间
            /// </summary>
            public int duration { get; set; }
            public int[] points { get; set; }
            public int compareRuleId { get; set; }

            public static Dictionary<string, int> ConditionDic = new Dictionary<string, int>
            {
                //max min  average increment rate   compare.max,compare.min
                {"<",0 },
                {">",1 },
                {"=",2 },
            };
            public static Dictionary<int, string> ConditionIntDic = new Dictionary<int, string>
            {
                //max min  average increment rate   compare.max,compare.min
                {0,"<" },
                {1,">" },
                {2,"=" },
            };
            public static Dictionary<string, int> TypeDic = new Dictionary<string, int>
            {
                //max min  average increment rate   compare.max,compare.min
                {"max",0 },
                {"min",1 },
                {"average",2 },
                {"increment",3 },
                {"rate",4 },
                {"compare.max",5 },
                {"compare.min",6 },
            };

            public static Dictionary<int, string> TypeIntDic = new Dictionary<int, string>
            {
                //max min  average increment rate   compare.max,compare.min
                {0,"max"},
                {1,"min"},
                {2,"average"},
                {3,"increment"},
                {4,"rate"},
                {5,"compare.max"},
                {6,"compare.min"},
            };


        }
        public class TemperatureDetectionBasicInfo
        {
            /// <summary>
            /// 0-4 最高 最低 平均 温度差 温度变化率
            /// </summary>
            public int Type { get; set; }
            /// <summary>
            /// 0-2 低于 高于 匹配
            /// </summary>
            public int Condition { get; set; }
            /// <summary>
            /// 温度阈值 
            /// </summary>
            public float Threshold { get; set; }
            /// <summary>
            /// 温度变化率
            /// </summary>
            public float ChangeRate { get; set; }
            /// <summary>
            /// 误差
            /// </summary>
            public float Range { get; set; }
            /// <summary>
            /// 时间
            /// </summary>
            public int Duration { get; set; }

        }
        public class TypedConfigListInfo<T>
        {
            public int Num { get; set; }
            public T ConfigList { get; set; }
        }

        public class RuleDetectionConfig
        {
            public int ID { get; set; }
            public int Enabled { get; set; }
            public string Name { get; set; }
        }
        public class CompareRuleDetectionConfig : RuleDetectionConfig
        {
            public int FirstCompareRuleID { get; set; }
            public int SecondCompareRuleID { get; set; }
        }

        public class TemperatureDetectionCompareRuleInfo : CompareRuleDetectionConfig
        {
            public TemperatureDetectionBasicInfo BasicRule { get; set; }
        }
        public class TemperatureDetectionCompareRuleList : TypedConfigListInfo<TemperatureDetectionCompareRuleInfo[]>
        {

        }

        public class TemperatureDetectionCompareRule
        {
            /// <summary>
            /// 温度对比信息
            /// </summary>
            public CompareRuleDetectionConfig CompareRuleDetectionConfig { get; set; }
            /// <summary>
            /// 温度告警规则
            /// </summary>
            public TemperatureDetectionBasicInfo TemperatureDetectionBasicInfo { get; set; }
        }

        public class TemperatureDetectionParamInfo
        {
            public TemperatureDetectionParamInfo()
            {
                Emissivity = 96;
                Distance = 5;
                EnvironmentTemperature = 20;
                Compensation = 10;
            }
            public int Emissivity { get; set; }
            public float Distance { get; set; }
            public float EnvironmentTemperature { get; set; }
            public float Compensation { get; set; }
            public static TemperatureDetectionParamInfo Single = new TemperatureDetectionParamInfo();
        }
        public class Point
        {
            public Point() { }
            public Point(int x, int y) { X = x; Y = y; }
            public int X { get; set; }
            public int Y { get; set; }
        }
        public class PolygonInfo
        {
            public int Num { get; set; }
            public Point[] PointList { get; set; }
        }
        public class SingleRuleDetectionConfig : RuleDetectionConfig
        {
            /// <summary>
            /// 红外测温规则
            /// </summary>
            public PolygonInfo PolygonInfo { get; set; }
            /// <summary>
            /// 红外设备参数
            /// </summary>
            public TemperatureDetectionParamInfo TemperatureDetectionParam { get; set; }
        }

        public class TemperatureDetectionSingleRule
        {
            public SingleRuleDetectionConfig SingleRuleDetectionConfig { get; set; }
            public TemperatureDetectionBasicInfo TemperatureDetectionBasicInfo { get; set; }
        }



        public class TemperatureValueInfo
        {
            public int ID { get; set; }
            public float MaxTemperature { get; set; }
            public float MinTemperature { get; set; }
            public float AverageTemperature { get; set; }
        }
        public class TemperatureValueList
        {
            public int Num { get; set; }
            public TemperatureValueInfo[] TemperatureValueInfoList { get; set; }
        }

        public class PresetInfoList
        {
            public class PresetInfo
            {
                public int ID { get; set; }
                public string Name { get; set; }

            }
            public int Num { get; set; }

            public PresetInfo[] PresetInfos { get; set; }
        }


        public class DetectionConfigInfo
        {
            public TypedConfigListInfo<SingleRuleDetectionConfig[]> SingleRuleDetectionConfigList { get; set; }
            public TypedConfigListInfo<CompareRuleDetectionConfig[]> CompareRuleDetectionConfigList { get; set; }
        }
        public class AlarmConfigInfo
        {
            public TypedConfigListInfo<TemperatureDetectionBasicInfo[]> SingleRuleAlarmConfigList { get; set; }
            public TypedConfigListInfo<TemperatureDetectionBasicInfo[]> CompareRuleAlarmConfigList { get; set; }
        }
        public class TemperatureDetectionRule
        {

            public DetectionConfigInfo DetectionConfig { get; set; }

            public AlarmConfigInfo AlarmConfig { get; set; }

        }


        public class PtzPosInfo
        {
            public float Longitude { get; set; }
            public float Latitude { get; set; }
            public float ZoomRatio { get; set; }
        }
        public class ResponseData<T>
        {
            public string ResponseURL { get; set; }
            public int ResponseCode { get; set; }
            public int SubResponseCode { get; set; }
            public string ResponseString { get; set; }
            public int StatusCode { get; set; }
            public T Data { get; set; }
        }
        public class UniviewResponse
        {

        }

        public class UniviewResponse<T> : UniviewResponse
        {
            public ResponseData<T> Response { get; set; }
        }

        static HttpClient _client;
        private string _host;
        private int _port;
        private string _username;
        private string _password;

        public UniviewHelper(string host, int port, string username, string password)
        {
            _host = host;
            _port = port;
            _username = username;
            _password = password;
            _client = new HttpClient(new HttpClientHandler
            {
                Credentials = new NetworkCredential(username, password)
            });
        }


        public async Task<UniviewResponse> UniSetTempRuleAsync(SetThermalRuleRequest request)
        {
            var pts = new List<UniviewHelper.Point>();
            for (int i = 0; i < request.points.Length / 2; i++)
            {
                pts.Add(new UniviewHelper.Point(request.points[2 * i], request.points[2 * i + 1]));
            }


            var resp = await this.BatchSetTemperatureRule(new TemperatureDetectionRule
            {
                DetectionConfig = new DetectionConfigInfo
                {
                    SingleRuleDetectionConfigList = new TypedConfigListInfo<SingleRuleDetectionConfig[]>
                    {
                        Num = 1,
                        ConfigList = new SingleRuleDetectionConfig[]
                        {
                          new SingleRuleDetectionConfig   {
                                ID = request.id,
                                Name = request.name,
                                Enabled = request.enabled,
                                PolygonInfo = new PolygonInfo
                                {
                                    Num = request.points.Length / 2,
                                    PointList = pts.ToArray(),
                                },
                                TemperatureDetectionParam = TemperatureDetectionParamInfo.Single,
                            }
                        }
                    },
                    CompareRuleDetectionConfigList = new TypedConfigListInfo<CompareRuleDetectionConfig[]>
                    {
                        Num = 0,
                    }
                }
               ,
                AlarmConfig = new AlarmConfigInfo
                {
                    SingleRuleAlarmConfigList = new TypedConfigListInfo<TemperatureDetectionBasicInfo[]>
                    {
                        Num = 1,
                        ConfigList = new TemperatureDetectionBasicInfo[]
                        {
                            new TemperatureDetectionBasicInfo
                            {
                                Type = request.type,
                                Condition = request.condition,
                                Range = request.range,
                                Duration = request.duration,
                                ChangeRate = request.type == 4 ? request.threshold : 0,
                                Threshold = request.type == 4 ? 0 : request.threshold
                            }
                        }
                    },
                    CompareRuleAlarmConfigList = new TypedConfigListInfo<TemperatureDetectionBasicInfo[]>
                    {
                        Num = 0,
                    }
                },
            });
            return resp;
        }


        public async Task<byte[]> Snapshop(int channel, int stream)
        {
            try
            {
                var resp = await _client.GetAsync($"http://{_host}:{_port}/LAPI/V1.0/Channels/{channel}/Media/Video/Streams/{stream}/Snapshot");
                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine($"{_host} {_port} {_username} {_password} error " + resp.Content.ReadAsStringAsync().Result);
                    return null;
                }
                var imgpic = await resp.Content.ReadAsByteArrayAsync();
                return imgpic;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }


        public async Task<PtzPosInfo> GetPtzCurrentPos()
        {
            try
            {
                PtzPosInfo ret = new PtzPosInfo();

                var resp1 = await _client.GetAsync($"http://{_host}:{_port}/LAPI/V1.0/Channels/1/PTZ/AbsoluteMove");
                if (resp1.StatusCode == HttpStatusCode.OK)
                {
                    var str1 = await resp1.Content.ReadAsStringAsync();
                    var info = JsonSerializer.Deserialize<UniviewResponse<PtzPosInfo>>(str1);
                    ret.Latitude = info.Response.Data.Latitude;
                    ret.Longitude = info.Response.Data.Longitude;
                }
                var resp2 = await _client.GetAsync($"http://{_host}:{_port}/LAPI/V1.0/Channels/1/PTZ/AbsoluteZoom");
                if (resp2.StatusCode == HttpStatusCode.OK)
                {
                    var str1 = await resp2.Content.ReadAsStringAsync();
                    var info = JsonSerializer.Deserialize<UniviewResponse<PtzPosInfo>>(str1);
                    ret.ZoomRatio = info.Response.Data.ZoomRatio;
                }
                return ret;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }


        public async Task<PtzPosInfo> SetPtzCurrentPos(PtzPosInfo posInfo)
        {
            try
            {
                PtzPosInfo ret = new PtzPosInfo();

                var text = JsonSerializer.Serialize(posInfo, new JsonSerializerOptions { PropertyNamingPolicy = null });
                var httpcontent = new StringContent(text, Encoding.UTF8, "application/json");

                var resp1 = await _client.PutAsync($"http://{_host}:{_port}/LAPI/V1.0/Channels/1/PTZ/AbsoluteMove", httpcontent);

                if (resp1.StatusCode == HttpStatusCode.OK)
                {
                    var str1 = await resp1.Content.ReadAsStringAsync();
                    Console.WriteLine(str1);
                    var info = JsonSerializer.Deserialize<UniviewResponse>(str1);
                    ret.Latitude = posInfo.Latitude;
                    ret.Longitude = posInfo.Longitude;
                }
                var resp2 = await _client.PutAsync($"http://{_host}:{_port}/LAPI/V1.0/Channels/1/PTZ/AbsoluteZoom", httpcontent);
                if (resp2.StatusCode == HttpStatusCode.OK)
                {
                    var str1 = await resp2.Content.ReadAsStringAsync();
                    Console.WriteLine(str1);

                    var info = JsonSerializer.Deserialize<UniviewResponse>(str1);
                    ret.ZoomRatio = posInfo.ZoomRatio;
                }
                return ret;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public async Task<PresetInfoList> GetPTZPresetList()
        {
            try
            {
                var resp = await _client.GetAsync($"http://{_host}:{_port}/LAPI/V1.0/Channels/1/PTZ/Presets");
                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine($"{_host} {_port} {_username} {_password} error " + resp.Content.ReadAsStringAsync().Result);
                    return null;
                }
                var imgpic = await resp.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<UniviewResponse<PresetInfoList>>(imgpic);

                return data.Response.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        internal async Task<string> PutPTZPreset(int id, string name)
        {
            try
            {
                var resp = await _client.PutAsync($"http://{_host}:{_port}/LAPI/V1.0/Channels/1/PTZ/Presets/{id}",
                    new StringContent(JsonSerializer.Serialize(new { ID = id, Name = name }, new JsonSerializerOptions { PropertyNamingPolicy = null })));
                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine($"{_host} {_port} {_username} {_password} error " + resp.Content.ReadAsStringAsync().Result);
                    return name;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        public async Task<int> GotoPreset(int id)
        {
            try
            {
                var resp = await _client.PutAsync($"http://{_host}:{_port}/LAPI/V1.0/Channels/1/PTZ/Presets/{id}/Goto", new StringContent(""));
                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine($"{_host} {_port} {_username} {_password} error " + resp.Content.ReadAsStringAsync().Result);
                    return id;
                }
                Console.WriteLine(" response " + resp.Content.ReadAsStringAsync().Result);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
        }


        public async Task<string> AddPTZPreset(string name)
        {
            try
            {
                var resp = await _client.PostAsync($"http://{_host}:{_port}/LAPI/V1.0/Channels/2/PTZ/Presets", new StringContent(JsonSerializer.Serialize(new { Name = name }, new JsonSerializerOptions { PropertyNamingPolicy = null })));
                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine($"{_host} {_port} {_username} {_password} error " + resp.Content.ReadAsStringAsync().Result);
                    return name;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }



        public async Task<UniviewResponse> SingleSetCompareTemperatureRule(TemperatureDetectionCompareRule compareRule, int ruleId)
        {

            return await Task.FromResult<UniviewResponse>(null);
        }
        public async Task<UniviewResponse> SingleSetTemperatureRule(TemperatureDetectionSingleRule singleRule, int ruleId)
        {
            try
            {
                var text = JsonSerializer.Serialize(singleRule, new JsonSerializerOptions { PropertyNamingPolicy = null });
                var httpcontent = new StringContent(text, Encoding.UTF8, "application/json");
                var resp = await _client.PutAsync($"http://{_host}:{_port}/LAPI/V1.0/Channels/2/Alarm/TemperatureDetection/Rule/{ruleId}", httpcontent);
                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine($"{_host} {_port} {_username} {_password} error " + resp.Content.ReadAsStringAsync().Result);
                    return null;
                }
                var imgpic = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UniviewResponse<TemperatureDetectionRule>>(imgpic);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        public async Task<UniviewResponse<Dictionary<string, int>>> NewTemperatureRule()
        {
            try
            {

                var resp = await _client.PostAsync($"http://{_host}:{_port}/LAPI/V1.0/Channels/2/Alarm/TemperatureDetection/Rule", new StringContent(""));
                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine($"{_host} {_port} {_username} {_password} error " + resp.Content.ReadAsStringAsync().Result);
                    return null;
                }
                var imgpic = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UniviewResponse<Dictionary<string, int>>>(imgpic);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        public async Task<UniviewResponse<string>> DeleteTemperatureRule(int ruleId)
        {
            try
            {

                var resp = await _client.DeleteAsync($"http://{_host}:{_port}/LAPI/V1.0/Channels/2/Alarm/TemperatureDetection/Rule/{ruleId}");
                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine($"{_host} {_port} {_username} {_password} error " + resp.Content.ReadAsStringAsync().Result);
                    return null;
                }
                var imgpic = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UniviewResponse<string>>(imgpic);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        public async Task<UniviewResponse> BatchSetTemperatureRule(TemperatureDetectionRule rule)
        {
            try
            {
                var text = JsonSerializer.Serialize(rule, new JsonSerializerOptions { PropertyNamingPolicy = null });
                var httpcontent = new StringContent(text, Encoding.UTF8, "application/json");
                var resp = await _client.PutAsync($"http://{_host}:{_port}/LAPI/V1.0/Channels/2/Alarm/TemperatureDetection/Rule", httpcontent);
                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine($"{_host} {_port} {_username} {_password} error " + resp.Content.ReadAsStringAsync().Result);
                    return null;
                }
                var imgpic = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UniviewResponse<string>>(imgpic);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
            return await Task.FromResult<UniviewResponse>(null);
        }

        public async Task<UniviewResponse<TemperatureDetectionRule>> GetTemperatureRules()
        {
            try
            {
                var resp = await _client.GetAsync($"http://{_host}:{_port}/LAPI/V1.0/Channels/2/Alarm/TemperatureDetection/Rule");
                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine($"{_host} {_port} {_username} {_password} error " + resp.Content.ReadAsStringAsync().Result);
                    return null;
                }
                var imgpic = await resp.Content.ReadAsStringAsync();
                Console.WriteLine(imgpic);
                return JsonSerializer.Deserialize<UniviewResponse<TemperatureDetectionRule>>(imgpic);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        public async Task<UniviewResponse<TemperatureValueList>> GetTemperatureValues()
        {
            try
            {
                var resp = await _client.GetAsync($"http://{_host}:{_port}/LAPI/V1.0/Channels/2/Alarm/TemperatureDetection/Temperature");
                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine($"{_host} {_port} {_username} {_password} error " + resp.Content.ReadAsStringAsync().Result);
                    return null;
                }
                var imgpic = await resp.Content.ReadAsStringAsync();
                var data = TryCatchExtention.TryCatch(str => JsonSerializer.Deserialize<UniviewResponse<TemperatureValueList>>(str), imgpic);
                if (data == null)
                {
                    throw new Exception(imgpic + " Not JSON");
                }
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }


    }
}