using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bkl.Models
{
    
    public partial class BklDeviceMetadata
    {
    

 
        public static DeviceConnectionString GetConnection(string connStr)
        {
            return JsonSerializer.Deserialize<DeviceConnectionString>(connStr);
        }
        public static List<BklDeviceMetadata> GetThermalCamera(IServiceProvider serviceProvider)
        {
            using (var dbcontex = serviceProvider.GetService<BklDbContext>())
            {
                var devices = dbcontex.BklDeviceMetadata.Where(p => p.DeviceType == "ThermalCamera" && p.ConnectionType == "rtsp").AsNoTracking().ToList();
                return devices;
            }
        }

        public static List<BklDeviceMetadata> GetBandageSensor(IServiceProvider serviceProvider)
        {
            using (var dbcontex = serviceProvider.GetService<BklDbContext>())
            {
                var devices = dbcontex.BklDeviceMetadata.Where(p => p.DeviceType == "BandageSensor" && (p.ConnectionType == "modbus" || p.ConnectionType == "modbusip")).AsNoTracking().ToList();
                return devices;
            }

        }
        public static List<BklDeviceMetadata> GetDGADevice(IServiceProvider serviceProvider)
        {
            using (var dbcontex = serviceProvider.GetService<BklDbContext>())
            {
                var devices = dbcontex.BklDeviceMetadata.Where(p => p.DeviceType == "DGA").AsNoTracking().ToList();
                return devices;
            }

        }
        public static List<BklDeviceMetadata> GetFDTDevice(IServiceProvider serviceProvider)
        {
            using (var dbcontex = serviceProvider.GetService<BklDbContext>())
            {
                var devices = dbcontex.BklDeviceMetadata.Where(p => p.DeviceType == "FDT").AsNoTracking().ToList();
                return devices;
            }

        }
    }
}
