using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Bkl.Models.DataEntity
{
    public struct YoloLabel
    {
        public static YoloLabel Zero = new YoloLabel()
        {
            ClassId = "none",
            CenterX = -1,
            CenterY = -1,
            W = -1,
            H = -1,
        };
        public double CenterX { get; set; }
        public double CenterY { get; set; }

        public double W { get; set; }
        public double H { get; set; }

        public string ClassId { get; set; }
        public override string ToString()
        {
            return $"{ClassId} {CenterX.ToString("0.000000")} {CenterY.ToString("0.000000")} {W.ToString("0.000000")} {H.ToString("0.000000")}";
        }

        public bool IsValid()
        {
            return CenterX <= 1 && CenterX >= 0 && W <= 1 && W >= 0 && H <= 1 && H >= 0;
        }
        public double[] Point(double w, double h)

        {
            var ww = w * W;
            var hh = h * H;
            var h2 = hh / 2;
            var w2 = ww / 2;
            var cx = CenterX * w;
            var cy = CenterY * h;
            return new double[] { 
                cx - w2, cy - h2,
                cx + w2, cy - h2,
                cx + w2, cy + h2,
                cx - w2, cy + h2,
            };
        }
        

    }
    public static class YoloHelper
    {
        public static YoloLabel Parse(string content)
        {
            var arr = content.Split(' ');
            return new YoloLabel
            {
                ClassId = arr[0],
                CenterX = double.Parse(arr[1]),
                CenterY = double.Parse(arr[2]),
                W = double.Parse(arr[3]),
                H = double.Parse(arr[4]),
            };
        }
        public static YoloLabel cxcywh2yolo(int cx, int cy, int w0, int h0, int W, int H, string key = "0")
        {
            var centralX = (cx * 1.0) / W;
            var centralY = (cy * 1.0) / H;
            var w = w0 * 1.0 / W;
            var h = h0 * 1.0 / H;

            return new YoloLabel
            {
                CenterX = centralX,
                CenterY = centralY,
                W = w,
                H = h,
                ClassId = key
            };
        }

        public static YoloLabel xywh2yolo(int x, int y, int w0, int h0, int W, int H, string key = "0")
        {
            var centralX = (x * 1.0 + w0 / 2) / W;
            var centralY = (y * 1.0 + h0 / 2) / H;
            var w = w0 * 1.0 / W;
            var h = h0 * 1.0 / H;

            return new YoloLabel
            {
                CenterX = centralX,
                CenterY = centralY,
                W = w,
                H = h,
                ClassId = key
            };
        }
        public static YoloLabel xyxy2yolo(Point[] xys, int W, int H, string key = "0")
        {
            int x = xys[0].X;
            int y = xys[0].Y;
            int w0 = Math.Abs(xys[2].X - xys[0].X);
            int h0 = Math.Abs(xys[2].Y - xys[0].Y);
            var centralX = (x * 1.0 + w0 / 2) / W;
            var centralY = (y * 1.0 + h0 / 2) / H;
            var w = w0 * 1.0 / W;
            var h = h0 * 1.0 / H;

            return new YoloLabel
            {
                CenterX = centralX,
                CenterY = centralY,
                W = w,
                H = h,
                ClassId = key
            };
        }
    }
}
