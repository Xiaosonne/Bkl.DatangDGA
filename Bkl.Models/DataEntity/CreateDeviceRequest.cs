namespace Bkl.Models
{
    public class UpdateDeviceRequest
    {
        public long DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string DeviceMetadata { get; set; }
        public long FacilityId { get; set; }
    }
    public class CreateDeviceRequest
    {
        public long FactoryId { get; set; }
        public long FacilityId { get; set; }
        public long DeviceId { get; set; }
        /// <summary>
        /// WindPowerGenerator HeatPowerGenerator Transformer
        /// </summary>
        public string FacilityType { get; set; }
        public string FacilityName { get; set; }
        public string Position { get; set; }
        public string ProbeName { get; set; }
        /// <summary>
        /// BandageSensor ThermalCamera DGA PTDetector
        /// </summary>
        public string DeviceType { get; set; }
        public string IPaddress { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public byte BusId { get; set; }
        public string TransferType { get; set; }
        public int NodeIndex { get; set; }
        public string ReadType { get; set; } 
        public string ProtocolName { get; set; }

    }
}
