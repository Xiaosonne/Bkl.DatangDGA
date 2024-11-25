using Bkl.Models;
using System;

namespace Bkl.Dst.Interfaces
{
    public class ThermalCameraGrainId
    {
        private long _camId;
        public ThermalCameraGrainId(BklThermalCamera bklThermalCamera)
        {
            _camId = bklThermalCamera.Id;
        }
        private ThermalCameraGrainId(long camId)
        {
            _camId = camId;
        }
        public long CamId { get => _camId; set => _camId = value; }

        public static implicit operator String(ThermalCameraGrainId id)
        {
            return $"cameraId:{id.CamId}";
        }
        public static implicit operator ThermalCameraGrainId(String str)
        {
            var arr = str.Split(':');
            return new ThermalCameraGrainId(long.Parse(arr[1]));
        }
    }
}
