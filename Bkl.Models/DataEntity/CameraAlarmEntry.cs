public class CameraAlarmEntry
{
    // 注意: 生成的代码可能至少需要 .NET Framework 4.5 或 .NET Core/Standard 2.0。
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class EventNotificationAlert
    {

        private string ipAddressField;

        private ushort portNoField;

        private string protocolField;

        private string macAddressField;

        private byte dynChannelIDField;

        private byte channelIDField;

        private System.DateTime dateTimeField;

        private byte activePostCountField;

        private string eventTypeField;

        private string eventStateField;

        private string eventDescriptionField;

        private DetectionRegionEntry[] detectionRegionListField;

        private string channelNameField;

        private byte detectionPicturesNumberField;

        private string uRLCertificationTypeField;

        /// <remarks/>
        public string ipAddress
        {
            get
            {
                return this.ipAddressField;
            }
            set
            {
                this.ipAddressField = value;
            }
        }

        /// <remarks/>
        public ushort portNo
        {
            get
            {
                return this.portNoField;
            }
            set
            {
                this.portNoField = value;
            }
        }

        /// <remarks/>
        public string protocol
        {
            get
            {
                return this.protocolField;
            }
            set
            {
                this.protocolField = value;
            }
        }

        /// <remarks/>
        public string macAddress
        {
            get
            {
                return this.macAddressField;
            }
            set
            {
                this.macAddressField = value;
            }
        }

        /// <remarks/>
        public byte dynChannelID
        {
            get
            {
                return this.dynChannelIDField;
            }
            set
            {
                this.dynChannelIDField = value;
            }
        }

        /// <remarks/>
        public byte channelID
        {
            get
            {
                return this.channelIDField;
            }
            set
            {
                this.channelIDField = value;
            }
        }

        /// <remarks/>
        public System.DateTime dateTime
        {
            get
            {
                return this.dateTimeField;
            }
            set
            {
                this.dateTimeField = value;
            }
        }

        /// <remarks/>
        public byte activePostCount
        {
            get
            {
                return this.activePostCountField;
            }
            set
            {
                this.activePostCountField = value;
            }
        }

        /// <remarks/>
        public string eventType
        {
            get
            {
                return this.eventTypeField;
            }
            set
            {
                this.eventTypeField = value;
            }
        }

        /// <remarks/>
        public string eventState
        {
            get
            {
                return this.eventStateField;
            }
            set
            {
                this.eventStateField = value;
            }
        }

        /// <remarks/>
        public string eventDescription
        {
            get
            {
                return this.eventDescriptionField;
            }
            set
            {
                this.eventDescriptionField = value;
            }
        }

        /// <remarks/>
        public DetectionRegionEntry[] DetectionRegionList
        {
            get
            {
                return this.detectionRegionListField;
            }
            set
            {
                this.detectionRegionListField = value;
            }
        }

        /// <remarks/>
        public string channelName
        {
            get
            {
                return this.channelNameField;
            }
            set
            {
                this.channelNameField = value;
            }
        }

        /// <remarks/>
        public byte detectionPicturesNumber
        {
            get
            {
                return this.detectionPicturesNumberField;
            }
            set
            {
                this.detectionPicturesNumberField = value;
            }
        }

        /// <remarks/>
        public string URLCertificationType
        {
            get
            {
                return this.uRLCertificationTypeField;
            }
            set
            {
                this.uRLCertificationTypeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class DetectionRegionEntry
    {

        private byte regionIDField;

        private RegionCoordinates[] regionCoordinatesListField;

        private RegionEntryTMA tMAField;

        /// <remarks/>
        public byte regionID
        {
            get
            {
                return this.regionIDField;
            }
            set
            {
                this.regionIDField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("RegionCoordinates", IsNullable = false)]
        public RegionCoordinates[] RegionCoordinatesList
        {
            get
            {
                return this.regionCoordinatesListField;
            }
            set
            {
                this.regionCoordinatesListField = value;
            }
        }

        /// <remarks/>
        public RegionEntryTMA TMA
        {
            get
            {
                return this.tMAField;
            }
            set
            {
                this.tMAField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class RegionCoordinates
    {

        private ushort positionXField;

        private ushort positionYField;

        /// <remarks/>
        public ushort positionX
        {
            get
            {
                return this.positionXField;
            }
            set
            {
                this.positionXField = value;
            }
        }

        /// <remarks/>
        public ushort positionY
        {
            get
            {
                return this.positionYField;
            }
            set
            {
                this.positionYField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class RegionEntryTMA
    {

        private string thermometryUnitField;

        private decimal ruleTemperatureField;

        private decimal currTemperatureField;

        private string ruleCalibTypeField;

        private string ruleTypeField;

        private TMAMaximumTemperaturePoint maximumTemperaturePointField;

        private TMAAbsoluteHigh absoluteHighField;

        private byte presetNoField;

        /// <remarks/>
        public string thermometryUnit
        {
            get
            {
                return this.thermometryUnitField;
            }
            set
            {
                this.thermometryUnitField = value;
            }
        }

        /// <remarks/>
        public decimal ruleTemperature
        {
            get
            {
                return this.ruleTemperatureField;
            }
            set
            {
                this.ruleTemperatureField = value;
            }
        }

        /// <remarks/>
        public decimal currTemperature
        {
            get
            {
                return this.currTemperatureField;
            }
            set
            {
                this.currTemperatureField = value;
            }
        }

        /// <remarks/>
        public string ruleCalibType
        {
            get
            {
                return this.ruleCalibTypeField;
            }
            set
            {
                this.ruleCalibTypeField = value;
            }
        }

        /// <remarks/>
        public string ruleType
        {
            get
            {
                return this.ruleTypeField;
            }
            set
            {
                this.ruleTypeField = value;
            }
        }

        /// <remarks/>
        public TMAMaximumTemperaturePoint MaximumTemperaturePoint
        {
            get
            {
                return this.maximumTemperaturePointField;
            }
            set
            {
                this.maximumTemperaturePointField = value;
            }
        }

        /// <remarks/>
        public TMAAbsoluteHigh AbsoluteHigh
        {
            get
            {
                return this.absoluteHighField;
            }
            set
            {
                this.absoluteHighField = value;
            }
        }

        /// <remarks/>
        public byte presetNo
        {
            get
            {
                return this.presetNoField;
            }
            set
            {
                this.presetNoField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class TMAMaximumTemperaturePoint
    {

        private RegionCoordinates regionCoordinatesField;

        /// <remarks/>
        public RegionCoordinates RegionCoordinates
        {
            get
            {
                return this.regionCoordinatesField;
            }
            set
            {
                this.regionCoordinatesField = value;
            }
        }
    }
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class TMAAbsoluteHigh
    {

        private decimal elevationField;

        private decimal azimuthField;

        private decimal absoluteZoomField;

        /// <remarks/>
        public decimal elevation
        {
            get
            {
                return this.elevationField;
            }
            set
            {
                this.elevationField = value;
            }
        }

        /// <remarks/>
        public decimal azimuth
        {
            get
            {
                return this.azimuthField;
            }
            set
            {
                this.azimuthField = value;
            }
        }

        /// <remarks/>
        public decimal absoluteZoom
        {
            get
            {
                return this.absoluteZoomField;
            }
            set
            {
                this.absoluteZoomField = value;
            }
        }
    }


}
