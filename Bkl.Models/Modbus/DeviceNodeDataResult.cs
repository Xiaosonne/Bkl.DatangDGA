using System;
using Bkl.Infrastructure;


namespace Bkl.Models.Std
{
    public struct DeviceNodeDataResult
    {
        public long DeviceId;
        public long nodeId;
        public HexString data;

        public DeviceNodeDataResult(long deviceId, long nodeId, HexString data)
        {
            DeviceId = deviceId;
            this.nodeId = nodeId;
            this.data = data;
        }
        public DeviceStatusItem GetDeviceStatus(ModbusNodeInfo node)
        {
            if (node.Id != this.nodeId)
                throw new ArgumentException($"node.Id {node.Id} not equal to this nodeId {nodeId}", nameof(node.Id));
            DeviceStatusItem statusItem = new DeviceStatusItem
            {
                name = node.StatusName,
                nameCN = node.StatusNameCN,
                type = node.DataType.ToString().Substring(3),
                unit = node.Unit,
                unitCN = node.UnitCN,
                value = ""
            };
            switch ((ModbusDataType)node.DataType)
            {
                case ModbusDataType.dt_float:
                    statusItem.value = (this.data.GetFloat((ModbusByteDataOrder)node.DataOrder) * ((node.Scale.Empty() || node.Scale == "1") ? 1.0f : float.Parse(node.Scale))).ToString();
                    break;
                case ModbusDataType.dt_int16:
                    statusItem.value = ((Int16)(this.data.GetInt16((ModbusByteDataOrder)node.DataOrder) * ((node.Scale.Empty() || node.Scale == "1") ? 1 : float.Parse(node.Scale)))).ToString();
                    break;
                case ModbusDataType.dt_uint16:
                    statusItem.value = ((UInt16)(this.data.GetUInt16((ModbusByteDataOrder)node.DataOrder) * ((node.Scale.Empty() || node.Scale == "1") ? 1 : float.Parse(node.Scale)))).ToString();
                    break;
                case ModbusDataType.dt_int32:
                    statusItem.value = ((Int32)(this.data.GetInt32((ModbusByteDataOrder)node.DataOrder) * ((node.Scale.Empty() || node.Scale == "1") ? 1 : float.Parse(node.Scale)))).ToString();
                    break;
                case ModbusDataType.dt_uint32:
                    statusItem.value = ((UInt32)(this.data.GetUInt32((ModbusByteDataOrder)node.DataOrder) * ((node.Scale.Empty() || node.Scale == "1") ? 1 : float.Parse(node.Scale)))).ToString();
                    break;
                default:
                    break;
            }
            return statusItem;
        }
        public override bool Equals(object obj)
        {
            return obj is DeviceNodeDataResult other &&
                   DeviceId == other.DeviceId &&
                   nodeId == other.nodeId &&
                   data == other.data;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DeviceId, nodeId, data);
        }

        public void Deconstruct(out long deviceId, out long nodeId, out HexString data)
        {
            deviceId = DeviceId;
            nodeId = this.nodeId;
            data = this.data;
        }

        public static implicit operator (long DeviceId, long nodeId, HexString data)(DeviceNodeDataResult value)
        {
            return (value.DeviceId, value.nodeId, value.data);
        }

        public static implicit operator DeviceNodeDataResult((long DeviceId, long nodeId, HexString data) value)
        {
            return new DeviceNodeDataResult(value.DeviceId, value.nodeId, value.data);
        }
        public static implicit operator DeviceNodeDataResult(string value)
        {
            var arr = value.Split('#');
            return new DeviceNodeDataResult(long.Parse(arr[0]), long.Parse(arr[1]), arr[2]);
        }
        public static implicit operator string(DeviceNodeDataResult value)
        {
            return $"{value.DeviceId}#{value.nodeId}#{value.data}";
        }
    }
}
