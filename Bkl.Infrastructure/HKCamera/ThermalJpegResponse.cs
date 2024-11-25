namespace Bkl.Infrastructure
{
    public class ThermalJpegResponse
    {
        public class ThermalJpegData
        {
            public int channel { get; set; }
            public int jpegPicLen { get; set; }
            public int jpegPicWidth { get; set; }
            public int jpegPicHeight { get; set; }
            public int p2pDataLen { get; set; }
            public bool isFreezedata { get; set; }
            public int temperatureDataLength { get; set; }
        }
        public ThermalJpegData JpegPictureWithAppendData { get; set; }
    }
}
