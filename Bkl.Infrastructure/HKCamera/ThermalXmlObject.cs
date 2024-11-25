using System.ComponentModel;
using System.Xml.Serialization;

namespace Bkl.Infrastructure
{

    public class ThermalXmlObject
    {

        ////const string Namespace = "http://www.std-cgi.com/ver20/XMLSchema";
        //const string Namespace = "";
        ////const string Namespace = "http://www.isapi.org/ver20/XMLSchema";

        // 注意: 生成的代码可能至少需要 .NET Framework 4.5 或 .NET Core/Standard 2.0。
        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        //[System.Xml.Serialization.XmlTypeAttribute(Namespace = Namespace)]
        //[System.Xml.Serialization.XmlRootAttribute(Namespace = Namespace)]
        public partial class ThermometryBasicParam
        {

            public ThermometryBasicParam()
            {
                version = "2.0";
                enabled = true;
                streamOverlay = true;
                pictureOverlay = false;
                showTempStripEnable = true;
                id = 2;

            }
            /// <remarks/>
            public byte id { get; set; }

            /// <remarks/>
            public bool enabled { get; set; }

            /// <remarks/>
            public bool streamOverlay { get; set; }

            /// <remarks/>
            public bool pictureOverlay { get; set; }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string version { get; set; }




            public string temperatureRange { get; set; } = "-20~150";
            public string temperatureUnit { get; set; } = "degreeCentigrade";
            public string emissivity { get; set; } = "0.95";
            public string distanceUnit { get; set; } = "centimeter";
            public string specialPointThermType { get; set; } = "centerPoint";
            public string distance { get; set; } = "20";
            public string reflectiveEnable { get; set; } = "false";
            public string alert { get; set; } = "45.0";
            public string alarm { get; set; } = "55.0";

            public bool showTempStripEnable { get; set; }

            public bool displayMaxTemperatureEnabled { get; set; } = true;
            public bool displayMinTemperatureEnabled { get; set; } = true;
            public bool displayAverageTemperatureEnabled { get; set; } = true;


        }


        // 注意: 生成的代码可能至少需要 .NET Framework 4.5 或 .NET Core/Standard 2.0。
        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        //[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = Namespace)]
        //[System.Xml.Serialization.XmlRootAttribute(Namespace = Namespace, IsNullable = false)]
        public partial class ResponseStatus
        {

            /// <remarks/>
            public string requestURL { get; set; }

            /// <remarks/>
            public byte statusCode { get; set; }

            /// <remarks/>
            public string statusString { get; set; }

            /// <remarks/>
            public string subStatusCode { get; set; }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string version { get; set; }
        }




        // 注意: 生成的代码可能至少需要 .NET Framework 4.5 或 .NET Core/Standard 2.0。
        /// <remarks/>
        [System.Serializable()]
        [DesignerCategory("code")]
        //[XmlType(AnonymousType = true, Namespace = Namespace)]
        //[XmlRoot(IsNullable = false, Namespace = Namespace)]
        public partial class ThermometryRegionList
        {

            [XmlElement("ThermometryRegion", IsNullable = false)]
            //[XmlArray("ThermometryRegion", IsNullable = false)]
            /// <remarks/>
            public ThermometryRegion[] ThermometryRegion { get; set; }

            /// <remarks/>
            [XmlAttribute()]
            public string version { get; set; }


        }

        /// <remarks/>
        [System.Serializable()]
        [DesignerCategory("code")]
        //[XmlType(AnonymousType = true, Namespace = Namespace)]
        public partial class ThermometryRegion
        {

            /// <remarks/>
            public int id { get; set; }

            /// <remarks/>
            public bool enabled { get; set; }

            /// <remarks/>
            public string name { get; set; }

            /// <remarks/>
            public string emissivity { get; set; }

            /// <remarks/>
            public string distance { get; set; }

            /// <remarks/>
            public bool reflectiveEnable { get; set; }

            /// <remarks/>
            public string reflectiveTemperature { get; set; }

            /// <remarks/>
            public string type { get; set; }

            /// <remarks/>
            public Region Region { get; set; }

            public Point Point { get; set; }

            /// <remarks/>
            public string distanceUnit { get; set; }

            /// <remarks/>
            public string emissivityMode { get; set; }
        }

        /// <remarks/>
        [System.Serializable()]
        [DesignerCategory("code")]
        //[XmlType(AnonymousType = true, Namespace = Namespace)]
        public partial class Region
        {
            /// <remarks/>
            [XmlArrayItem("RegionCoordinates", IsNullable = false)]
            public Coordinates[] RegionCoordinatesList { get; set; }
        }

        [System.Serializable()]
        [DesignerCategory("code")]
        //[XmlType(AnonymousType = true, Namespace = Namespace)]
        public partial class Point
        {
            /// <remarks/>
            [XmlElement(IsNullable = false)]
            public Coordinates CalibratingCoordinates { get; set; }
        }

        /// <remarks/>
        [System.Serializable()]
        [DesignerCategory("code")]
        //[XmlType(AnonymousType = true, Namespace = Namespace)]
        public partial class Coordinates
        {

            /// <remarks/>
            public int positionX { get; set; }

            /// <remarks/>
            public int positionY { get; set; }
        }
    }
}
