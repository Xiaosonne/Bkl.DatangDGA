//using Bkl.Dst.Interfaces;
//using Orleans;
//using Orleans.Runtime;
//using System.Threading.Tasks;
//using Bkl.Models;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Bkl.Models.Std;
//using System.Linq;
//using System;
//using Bkl.Infrastructure;
//using Microsoft.Extensions.Logging;

//namespace Bkl.Dst.Grains
//{
//    public class ModbusDecoderGrain : Grain, IModbusDecoderGrain
//    {
//        public class State
//        {
//            public State()
//            {
//            }
//            public ModbusNodeInfo[] NodeInfo { get; set; }
//        }

//        private ILogger<ModbusDecoderGrain> _logger;
//        private IPersistentState<State> _state;
//        public ModbusDecoderGrain([PersistentState("modbusDecoder", BklConstants.RedisProvider)] IPersistentState<State> state,
//            ILogger<ModbusDecoderGrain> logg)
//        {
//            _logger = logg;
//            _state = state;
//        }
//        public override async Task OnActivateAsync()
//        {
//            using (BklDbContext db = new BklDbContext(this.ServiceProvider.GetService<DbContextOptionsBuilder<BklDbContext>>().Options))
//            {
//                var pname = this.GetPrimaryKeyString();
//                _state.State.NodeInfo = db.ModbusNodeInfo.Where(s => s.ProtocolName == pname).AsNoTracking().ToArray();
//            }
//            await _state.WriteStateAsync();
//            await base.OnActivateAsync();
//        }
//        public async Task Decode(GrainReference grainReference, ReadDeviceStatusRequest readDeviceStatus)
//        {
//            byte busId = readDeviceStatus.BusId;
//            string protocolName = readDeviceStatus.ProtocolName;
//            DeviceReadBundle[] readBundles = readDeviceStatus.Bundles;
//            var modbusGrain = grainReference.AsReference<IModbusGrain>();
//            foreach (var readBundle in readBundles)
//            {
//                var node = _state.State.NodeInfo.First(s => s.Id == readBundle.AttributeId);
//                ModbusReadType type = (ModbusReadType)node.ReadType;
//                HexString bytesHex = "";

//                _logger.LogInformation($"BeginRead {readBundle.DeviceId} {busId} {(ushort)(readBundle.NodeIndex + node.StartAddress)} {node.Id} {node.ProtocolName} {node.StatusName} {node.DataOrder} ");
//                switch (type)
//                {

//                    case ModbusReadType.ReadCoils:
//                        bytesHex = await modbusGrain.ReadCoils(busId, (ushort)(readBundle.NodeIndex + node.StartAddress), (ushort)node.DataSize);
//                        break;
//                    case ModbusReadType.ReadInputs:
//                        bytesHex = await modbusGrain.ReadInputs(busId, (ushort)(readBundle.NodeIndex + node.StartAddress), (ushort)node.DataSize);
//                        break;
//                    case ModbusReadType.ReadHoldingRegister:
//                        bytesHex = await modbusGrain.ReadHoldingRegister(busId, (ushort)(readBundle.NodeIndex + node.StartAddress), (ushort)node.DataSize);
//                        break;
//                    case ModbusReadType.ReadInputRegister:
//                        bytesHex = await modbusGrain.ReadInputRegister(busId, (ushort)(readBundle.NodeIndex + node.StartAddress), (ushort)node.DataSize);
//                        break;
//                    default:
//                        continue;
//                }

//                var val = GetDeviceStatus(readBundle, bytesHex, node);
//                val.ConnUuid = grainReference.GetPrimaryKeyString();
//                val.AttributeId = readBundle.AttributeId;
//                val.PairId = readBundle.PairId;
//                _logger.LogInformation($"EndRead {readBundle.DeviceId} {busId} {(ushort)(readBundle.NodeIndex + node.StartAddress)} {node.Id} {node.ProtocolName} {node.StatusName} {node.DataOrder} {bytesHex} {val.Value}");
//                IDeviceGrain device = this.GrainFactory.GetGrain<IDeviceGrain>(new DeviceGrainId(new BklDeviceMetadataRef { Id = readBundle.DeviceId }));
//                await device.UpdateStatus(val);
//            }
//        }

//        private DeviceUpdateStatus GetDeviceStatus(DeviceReadBundle bundle, HexString data, ModbusNodeInfo node)
//        {
//            DeviceUpdateStatus statusItem = new DeviceUpdateStatus
//            {
//                DeviceId = bundle.DeviceId, 
//                Name = node.StatusName,
//                NameCN = node.StatusNameCN,
//                Type = node.DataType.ToString().Substring(3),
//                Unit = node.Unit,
//                UnitCN = node.UnitCN,
//                Value = "",
//                Index = node.Id,
//            };
//            switch ((ModbusDataType)node.DataType)
//            {
//                case ModbusDataType.dt_float:
//                    statusItem.Value = (data.GetFloat((ModbusByteDataOrder)node.DataOrder) * ((node.Scale.Empty() || node.Scale == "1") ? 1.0f : float.Parse(node.Scale))).ToString();
//                    break;
//                case ModbusDataType.dt_int16:
//                    statusItem.Value = ((Int16)(data.GetInt16((ModbusByteDataOrder)node.DataOrder) * ((node.Scale.Empty() || node.Scale == "1") ? 1 : float.Parse(node.Scale)))).ToString();
//                    break;
//                case ModbusDataType.dt_uint16:
//                    statusItem.Value = ((UInt16)(data.GetUInt16((ModbusByteDataOrder)node.DataOrder) * ((node.Scale.Empty() || node.Scale == "1") ? 1 : float.Parse(node.Scale)))).ToString();
//                    break;
//                case ModbusDataType.dt_int32:
//                    statusItem.Value = ((Int32)(data.GetInt32((ModbusByteDataOrder)node.DataOrder) * ((node.Scale.Empty() || node.Scale == "1") ? 1 : float.Parse(node.Scale)))).ToString();
//                    break;
//                case ModbusDataType.dt_uint32:
//                    statusItem.Value = ((UInt32)(data.GetUInt32((ModbusByteDataOrder)node.DataOrder) * ((node.Scale.Empty() || node.Scale == "1") ? 1 : float.Parse(node.Scale)))).ToString();
//                    break;
//                default:
//                    break;
//            }
//            return statusItem;
//        }

//        public async Task Write(string grainId, WriteDeviceStatusRequest writeRequest)
//        {
//            var modbusGrain = this.GrainFactory.GetGrain<IModbusGrain>(grainId);
//            var node = _state.State.NodeInfo.First(s => s.Id == writeRequest.AttributeId);
//            switch (node.ReadType)
//            {

//                case ModbusReadType.WriteSingleCoil:
//                    await modbusGrain.WriteSingleCoil(writeRequest.BusId, (ushort)(node.StartAddress), BitConverter.ToBoolean(writeRequest.Data, 0));
//                    break;
//                case ModbusReadType.WriteSingleInput:
//                    await modbusGrain.WriteSingleRegister(writeRequest.BusId, (ushort)(node.StartAddress), BitConverter.ToUInt16(writeRequest.Data, 0));
//                    break;
//                case ModbusReadType.WriteCoils:
//                    var data = writeRequest.Data.Select(s => s == 0 ? false : true).ToArray();
//                    await modbusGrain.WriteMultipleCoils(writeRequest.BusId, (ushort)(node.StartAddress), data);
//                    break;
//                case ModbusReadType.WriteInputs:
//                    var data1 = Enumerable.Range(0, writeRequest.Data.Length / 2)
//                          .Select(s => BitConverter.ToUInt16(writeRequest.Data, s * 2))
//                          .ToArray();
//                    await modbusGrain.WriteMultipleRegisters(writeRequest.BusId, (ushort)(node.StartAddress), data1);
//                    break;
//                default:/**/
//                    break;
//            }
//        }
//    }
//}
