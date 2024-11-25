using Bkl.Dst.Interfaces;
using Bkl.Infrastructure;
using Bkl.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace TestProject2
{
    public class HKCameraTest1
    {
        private ITestOutputHelper _log;

        public HKCameraTest1(ITestOutputHelper console)
        {
            _log = console;
        }


        [Fact]
        public async void TestThermalHumi()
        {
            var thermal = new ThermalCameraISAPI("192.168.31.164", 80, "admin", "bkl666666");
            ThermalCameraISAPI.SetSmallNamespace();
            var response = await thermal.GetDeviceThermalTemp();
            _log.WriteLine(response);
            Assert.NotEmpty(response);
        }

        [Fact]
        public async void ThermalSetThermalRule()
        {
            var thermal = new ThermalCameraISAPI("192.168.31.242", 5122, "admin", "bkl666666");
            var response = await thermal.SetThermalRule(new ThermalMeasureRule
            {
                ruleId = 1,
                ruleName = "test1",
                enabled = 1,
                regionType = 0,
                regionPoints = new List<double[]> { new double[] { 0, 0, } }
            });
            Assert.True(response.statusCode == 1);
        }

        [Fact]
        public void TestThermalXmlSerial()
        {
            var re = new ThermalXmlObject.ThermometryRegion
            {
                id = 1,
                enabled = true,
                name = "test1",
                emissivity = "0.95",
                distance = "2",
                reflectiveEnable = false,
                reflectiveTemperature = "1",
                type = "region",
                distanceUnit = "meter",
                emissivityMode = "customsettings",
                //Point=new ThermalObject.Point {
                //    CalibratingCoordinates=new ThermalObject.Coordinates
                //    {
                //        positionX=0,positionY=1
                //    }
                //},
                Region = new ThermalXmlObject.Region
                {
                    RegionCoordinatesList = new ThermalXmlObject.Coordinates[]
                                {
                                    new ThermalXmlObject.Coordinates
                                    {
                                        positionX=100,positionY=100
                                    },
                                    new ThermalXmlObject.Coordinates
                                    {
                                        positionX=200,positionY=100
                                    },
                                    new ThermalXmlObject.Coordinates
                                    {
                                        positionX=300,positionY=300
                                    }
                                }
                },
            };
            ThermalXmlObject.ThermometryRegionList lis = new ThermalXmlObject.ThermometryRegionList
            {
                version = "2.0",
                ThermometryRegion = new ThermalXmlObject.ThermometryRegion[]
                {
                    re,re,re
                }
            };
            var xml = new XmlSerializer(typeof(ThermalXmlObject.ThermometryRegionList));
            MemoryStream ms = new MemoryStream();
            xml.Serialize(ms, lis);
            _log.WriteLine($"{Encoding.UTF8.GetString(ms.ToArray())}  ");
        }
        [Fact]
        public void TestThermalDeserialzie()
        {
            string str = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<ThermometryRegionList version=\"2.0\" xmlns=\"http://www.std-cgi.com/ver20/XMLSchema\">\r\n<ThermometryRegion>\r\n<id>1</id>\r\n<enabled>true</enabled>\r\n<name>二二</name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>region</type>\r\n<Region>\r\n<RegionCoordinatesList>\r\n<RegionCoordinates>\r\n<positionX>228</positionX>\r\n<positionY>705</positionY>\r\n</RegionCoordinates>\r\n<RegionCoordinates>\r\n<positionX>760</positionX>\r\n<positionY>688</positionY>\r\n</RegionCoordinates>\r\n<RegionCoordinates>\r\n<positionX>767</positionX>\r\n<positionY>206</positionY>\r\n</RegionCoordinates>\r\n<RegionCoordinates>\r\n<positionX>280</positionX>\r\n<positionY>265</positionY>\r\n</RegionCoordinates>\r\n<RegionCoordinates>\r\n<positionX>277</positionX>\r\n<positionY>271</positionY>\r\n</RegionCoordinates>\r\n</RegionCoordinatesList>\r\n</Region>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>2</id>\r\n<enabled>true</enabled>\r\n<name>test</name>\r\n<emissivity>0.95</emissivity>\r\n<distance>200</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>100</positionX>\r\n<positionY>100</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>3</id>\r\n<enabled>true</enabled>\r\n<name>test01</name>\r\n<emissivity>0.95</emissivity>\r\n<distance>200</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>region</type>\r\n<Region>\r\n<RegionCoordinatesList>\r\n<RegionCoordinates>\r\n<positionX>100</positionX>\r\n<positionY>99</positionY>\r\n</RegionCoordinates>\r\n<RegionCoordinates>\r\n<positionX>200</positionX>\r\n<positionY>99</positionY>\r\n</RegionCoordinates>\r\n<RegionCoordinates>\r\n<positionX>300</positionX>\r\n<positionY>199</positionY>\r\n</RegionCoordinates>\r\n</RegionCoordinatesList>\r\n</Region>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>4</id>\r\n<enabled>true</enabled>\r\n<name>line1</name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>line</type>\r\n<Region>\r\n<RegionCoordinatesList>\r\n<RegionCoordinates>\r\n<positionX>251</positionX>\r\n<positionY>743</positionY>\r\n</RegionCoordinates>\r\n<RegionCoordinates>\r\n<positionX>610</positionX>\r\n<positionY>172</positionY>\r\n</RegionCoordinates>\r\n</RegionCoordinatesList>\r\n</Region>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>5</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>6</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>7</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>8</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>9</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>10</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>11</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>12</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>13</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>14</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>15</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>16</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>17</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>18</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>19</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>20</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n<ThermometryRegion>\r\n<id>21</id>\r\n<enabled>false</enabled>\r\n<name></name>\r\n<emissivity>0.95</emissivity>\r\n<distance>20</distance>\r\n<reflectiveEnable>false</reflectiveEnable>\r\n<reflectiveTemperature>20.0</reflectiveTemperature>\r\n<type>point</type>\r\n<Point>\r\n<CalibratingCoordinates>\r\n<positionX>500</positionX>\r\n<positionY>500</positionY>\r\n</CalibratingCoordinates>\r\n</Point>\r\n<distanceUnit>centimeter</distanceUnit>\r\n<emissivityMode>customsettings</emissivityMode>\r\n</ThermometryRegion>\r\n</ThermometryRegionList>\r\n";
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(str));
            XmlSerializer xml = new XmlSerializer(typeof(ThermalXmlObject.ThermometryRegionList));
            var obj = xml.Deserialize(ms);
            Console.WriteLine(obj);
        }

        [Fact]
        public async void TestThermalGetRules()
        {
            var thermal = new ThermalCameraISAPI("192.168.31.164", 80, "admin", "bkl666666");
            ThermalCameraISAPI.SetSmallNamespace();
            var rules = await thermal.GetThermalRules();
            Assert.True(rules.Count != 0);
            Assert.True(rules[0].regionPoints != null);
            Assert.True(rules[0].regionPoints.Count != 0);
        }

        [Fact]
        public async void TestThermalGetTempOnce()
        {
            ThermalCameraISAPI thermal = new ThermalCameraISAPI("192.168.31.164", 80, "admin", "bkl666666");
            var temp = await thermal.ReadThermalMetryOnceAsync(); 
            Assert.NotNull(temp);
            Assert.NotNull(temp.Data);
            Assert.NotNull(temp.Data.TempRules);
            Assert.True(temp.Data.TempRules.Length != 0);
        }
        [Fact]
        public async void TestThermalJpegWithData()
        {
            ThermalCameraISAPI thermal = new ThermalCameraISAPI("192.168.1.100", 5122, "admin", "bkl666666");
            var boundaries = await thermal.GetThermalJpeg();
            var json = boundaries.FirstOrDefault(s => s.IsJsonData);
            var str = json.ReadData() as string;
            _log.WriteLine(str);
            int i = 0;
            foreach (var value in boundaries.Where(s => s.IsJpegData))
            {
                File.WriteAllBytes($"d:/thermal{i++}.jpg", value.Content);
            }
            foreach (var value in boundaries.Where(s => s.IsTempratureData))
            {
                var bmp = value.ReadAsBitmap();
                bmp.Save($"d:/thermal-temp{i++}.bmp");
            }
        }
        [Fact]
        public async void TestThermalJpeg()
        {
            ThermalCameraISAPI thermal = new ThermalCameraISAPI("192.168.31.164", 80, "admin", "bkl666666");
            var boundaries = await thermal.GetThermalJpeg();

            var da = boundaries[1];
            var da1 = boundaries[0].ReadAsJsonObject< ThermalJpegResponse>();
            var temps = da.ReadAsTemperature();
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
            var map1 = new Bitmap(1600, 1200);
            using (Graphics g = Graphics.FromImage(map1))
            {
                g.DrawImage(map, 0, 0, 1600, 1200);
                g.Save();
            }
            map1.Save("d:/123.bmp");

        }

    }
}
