using Bkl.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Bkl.ESPS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExportController : Controller
    {
        private BklConfig config;

        public ExportController(BklConfig config)
        {
            this.config = config;
        }
        [AllowAnonymous]
        [HttpGet("DownloadVideo")]
        public Object DownloadVideoFile([FromServices] BklDbContext context, [FromQuery] string file)
        {
            return new FileStreamResult(System.IO.File.OpenRead(Path.Combine(config.AlarmConfig.VideoFileBasePath, file)), "video/mp4");
        }
        [AllowAnonymous]
        [HttpGet("DownloadFile")]
        public Object DownloadFile([FromServices] BklDbContext context,

            [FromServices] LogonUser user, [FromQuery] string file)
        {
            var dir = Path.Combine(config.FileBasePath, "TempExportFile");
            //return new FileStreamResult(new FileStream(Path.Combine(dir, $"{file}.xlsx"), FileMode.Open), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            return new PhysicalFileResult(Path.Combine(dir, $"{file}.xlsx"), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }


        [HttpGet("bianyaqi/GetData/{deviceId}")]
        public Object GetExportData(
            [FromServices] BklDbContext context,
            [FromServices] LogonUser user,
            [FromRoute] int deviceId,
                   string startTime,
                   string endTime,
                   string timeType,
                   int needFile = 0,
                   int maxIntervalTime = 0)
        {
            var datestart = DateTime.Parse(startTime);
            var dateend = DateTime.Parse(endTime);
            long start = long.Parse(datestart.ToString("yyyyMMddHHmmss"));
            long end = long.Parse(dateend.ToString("yyyyMMddHHmmss"));
            var lis = context.BklDeviceStatus
            .Where(s => s.FactoryRelId == user.factoryId && (deviceId == 0 || s.DeviceRelId == deviceId) && s.Time >= start && s.Time <= end)
            .ToList();
            if (maxIntervalTime <= 0)
                maxIntervalTime = 3600;
            int total = (int)(dateend - datestart).TotalSeconds;
            int startIndex = (datestart.UnixEpoch() / maxIntervalTime);
            int endIndex = (dateend.UnixEpoch() / maxIntervalTime);
            int totalCount = endIndex - startIndex + 1;
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            Func<BklDeviceStatus, object> keyMaker = q => DateTime.ParseExact(q.Time.ToString(), "yyyyMMddHHmmss", CultureInfo.InvariantCulture).UnixEpoch() / maxIntervalTime;

            foreach (var item in lis.GroupBy(q => q.DeviceRelId))
            {
                //DateTime.ParseExact(q.Time.ToString(), "yyyyMMddHHmmss", CultureInfo.InvariantCulture).UnixEpoch() / maxIntervalTime

                foreach (var sameTime in item.GroupBy(keyMaker))
                {
                    Dictionary<string, object> dic = new Dictionary<string, object>();
                    dic.Add("time", (maxIntervalTime * (int)sameTime.Key).UnixEpochBack().ToString("yyyy-MM-dd HH:mm"));
                    foreach (var sameGroup in sameTime.GroupBy(q => q.GroupName))
                    {
                        var status = sameGroup.First();
                        dic.TryAdd(status.StatusName, sameGroup.Average(q => q.StatusValue));
                    }
                    // foreach (var status in sameTime)
                    // {
                    //     dic.TryAdd(status.StatusName, status.StatusValue);
                    // }
                    list.Add(dic);
                }
            }
            if (needFile == 0)
                return list;
            else
            {
                var first = list.FirstOrDefault();
                var header = first.Keys.ToArray();
                List<string[]> lis1 = new List<string[]>();
                foreach (var item in list)
                {
                    List<string> row = new List<string>();
                    foreach (var col in header)
                    {
                        row.Add(item[col].ToString());
                    }
                    lis1.Add(row.ToArray());
                }
                string filename = Guid.NewGuid().ToString();
                var dir = Path.Combine(config.FileBasePath, "TempExportFile");
                Directory.CreateDirectory(dir);
                var filename1 = Path.Combine(dir, filename + ".xlsx");
                CommonSaveExcelFile(filename1, header, lis1);
                return new
                {
                    data = list,
                    filename = filename
                };
            }
        }
        [HttpGet("GetTemperatureData/{deviceId}")]
        public Object GetTemperatureData(
            [FromServices] BklDbContext context,
            [FromServices] LogonUser user,
            [FromRoute] int deviceId,
                   string startTime,
                   string endTime,
                   string timeType,
                   int needFile = 0,
                   int maxIntervalTime = 0)
        {
            var datestart = DateTime.Parse(startTime);
            var dateend = DateTime.Parse(endTime);
            long start = long.Parse(datestart.ToString("yyyyMMddHHmmss"));
            long end = long.Parse(dateend.ToString("yyyyMMddHHmmss"));
            var lis = context.BklDeviceStatus
            .Where(s => s.FactoryRelId == user.factoryId && (deviceId == 0 || s.DeviceRelId == deviceId) && s.Time >= start && s.Time <= end)
            .ToList();
            if (maxIntervalTime <= 0)
                maxIntervalTime = 3600;
            int total = (int)(dateend - datestart).TotalSeconds;
            int startIndex = (datestart.UnixEpoch() / maxIntervalTime);
            int endIndex = (dateend.UnixEpoch() / maxIntervalTime);
            int totalCount = endIndex - startIndex + 1;
            List<DeviceTemperatureExportData> list = new List<DeviceTemperatureExportData>();
            foreach (var item in lis.GroupBy(q => q.DeviceRelId))
            {
                var ret = item.GroupBy(q => DateTime.ParseExact(q.Time.ToString(), "yyyyMMddHHmmss", CultureInfo.InvariantCulture).UnixEpoch() / maxIntervalTime)
                .Select(q => new DeviceTemperatureExportData
                {
                    DeviceId = item.Key,
                    Key = q.Key,
                    Time = (q.Key * maxIntervalTime).UnixEpochBack().ToString("HH:mm"),
                    Max = q.Max(s => s.StatusValue),
                    Min = q.Min(s => s.StatusValue),
                    Average = q.Average(s => s.StatusValue),
                }).ToList();
                foreach (var item1 in Enumerable.Range(startIndex, totalCount))
                {
                    if (ret.Any(s => s.Key == item1))
                        continue;
                    ret.Add(new DeviceTemperatureExportData
                    {
                        DeviceId = item.Key,
                        Time = (item1 * maxIntervalTime).UnixEpochBack().ToString("HH:mm"),
                        Key = (long)item1,
                        Max = 0.0,
                        Min = 0.0,
                        Average = 0.0
                    });
                }
                list.AddRange(ret.OrderBy(s => s.Key).ToList());
            }
            string str = needFile == 1 ? linZhouTanShuaSaveExcelFile(context, user, list) : "";
            return new { data = list, fileName = str };
        }
        private static void CommonSaveExcelFile(string filename1, string[] columns, List<string[]> values)
        {
           
            using (FileStream ms = new FileStream(filename1, FileMode.OpenOrCreate))
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(ms, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = doc.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    var sheetdata = new SheetData();
                    worksheetPart.Worksheet = new Worksheet(sheetdata);

                    Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
                    Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" };
                    sheets.Append(sheet);

                    Row headerRow = new Row();
                    foreach (var columnName in columns)
                    {
                        headerRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue(columnName) });
                    }
                    sheetdata.AppendChild(headerRow);
                    try
                    {

                        foreach (var row in values)
                        {
                            Row headerRow1 = new Row();
                            foreach (var value in row)
                            {
                                headerRow1.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue(value) });
                            }
                            sheetdata.AppendChild(headerRow1);

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    workbookPart.Workbook.Save();
                }
            }
            Console.WriteLine("saveto " + filename1);
        }
        private  string linZhouTanShuaSaveExcelFile(BklDbContext context, LogonUser user, List<DeviceTemperatureExportData> list)
        {
            var strName = Guid.NewGuid().ToString();
            var dir = Path.Combine(config.FileBasePath, "TempExportFile");
            Directory.CreateDirectory(dir);
            FileStream ms = new FileStream(Path.Combine(dir, strName + ".xlsx"), FileMode.OpenOrCreate);
            using (SpreadsheetDocument doc = SpreadsheetDocument.Create(ms, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = doc.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetdata = new SheetData();
                worksheetPart.Worksheet = new Worksheet(sheetdata);

                Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" };
                sheets.Append(sheet);
                var devices = context.BklDeviceMetadata.Where(q => q.FactoryId == user.factoryId).ToList();
                Row headerRow = new Row();
                headerRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("时间") });
                headerRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("机组") });
                headerRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("WS-最低温") });
                headerRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("WS-最高温") });
                headerRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("WN-最低温") });
                headerRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("WN-最高温") });
                headerRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("ES-最低温") });
                headerRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("ES-最高温") });
                headerRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("EN-最低温") });
                headerRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("EN-最高温") });
                sheetdata.AppendChild(headerRow);
                var ws = devices.Where(q => q.ProbeName == "WS").FirstOrDefault();
                var wn = devices.Where(q => q.ProbeName == "WN").FirstOrDefault();
                var es = devices.Where(q => q.ProbeName == "ES").FirstOrDefault();
                var en = devices.Where(q => q.ProbeName == "EN").FirstOrDefault();

                var enumDevice = new BklDeviceMetadata[] { ws, wn, es, en };
                foreach (var item in list.GroupBy(q => q.Time))
                {
                    Row headerRow1 = new Row();
                    headerRow1.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue(item.Key.ToString()) });
                    headerRow1.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("机组") });

                    foreach (var dev in enumDevice)
                    {
                        var temp = item.FirstOrDefault(q => q.DeviceId == dev.Id);
                        if (temp != null)
                        {
                            headerRow1.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue($"{temp.Min}") });
                            headerRow1.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue($"{temp.Max}") });
                        }
                        else
                        {
                            headerRow1.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue($"0") });
                            headerRow1.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue($"0") });
                        }
                    }
                    sheetdata.AppendChild(headerRow1);

                }
                workbookPart.Workbook.Save();
            }
            return strName;
        }

        [HttpGet("GetAlarmData/{datetype}/{date}/{returnType}")]
        public async Task<Object> GetAlarmData([FromServices] BklDbContext context,
                [FromServices] LogonUser user,
                [FromRoute] string returnType,
                [FromRoute] string datetype,
                [FromRoute] string date,
                [FromQuery] int needVideo = 0,
                [FromQuery] long facilityId = 0,
                [FromQuery] long deviceId = 0,
                [FromQuery] int year = 2021)
        {
            int totalCount = 0;
            switch (datetype)
            {
                case "day":
                    date = DateTime.Parse(date).DayOfYear.ToString();
                    totalCount = 24;
                    break;
                case "month":
                    var monthDate = DateTime.Parse(date);
                    date = monthDate.Month.ToString();
                    totalCount = DateTime.DaysInMonth(monthDate.Year, monthDate.Month);
                    break;
                case "week":
                    date = date.Split('-')[1];
                    date = date.Substring(0, date.Length - 2);
                    totalCount = 7;
                    break;
            }
            if (needVideo == 1)
            {
                var logs = await BklAnalysisLog.QueryAnalysisLogs(context, year,
                     factoryId: user.factoryId,
                     facilityId: facilityId,
                     deviceId: deviceId,
                     datetype: datetype,
                     date: date,
                     needVideo: needVideo);
                return logs;
            }
            else
            {
                var groupViews = await BklAnalysisLog.CountGroupByProbeNameDetail(context, year, datetype, date, user.factoryId, facilityId, deviceId);
                return BklAnalysisLog.JsonDataView(groupViews, totalCount);
            }

        }
    }

    internal class DeviceTemperatureExportData
    {
        public long DeviceId { get; set; }
        public long Key { get; set; }
        public double Max { get; set; }
        public double Min { get; set; }
        public double Average { get; set; }
        public string Time { get; set; }
    }
}//namespace
