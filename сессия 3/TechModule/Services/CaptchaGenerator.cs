using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace TechModule.Services
{
    public static class CaptchaGenerator
    {
        private static readonly Random _random = new Random();

        public static (byte[] ImageBytes, string Code) GenerateCaptcha(int width = 400, int height = 150)
        {
            var code = GenerateRandomCode(6);
            var bitmap = new Bitmap(width, height);
            
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                for (int i = 0; i < 20; i++)
                {
                    var pen = new Pen(Color.FromArgb(_random.Next(100, 200), _random.Next(100, 200), _random.Next(100, 200)), 2);
                    g.DrawLine(pen, _random.Next(0, width), _random.Next(0, height), _random.Next(0, width), _random.Next(0, height));
                }
                
                for (int i = 0; i < 800; i++)
                {
                    bitmap.SetPixel(_random.Next(0, width), _random.Next(0, height), 
                        Color.FromArgb(_random.Next(100, 200), _random.Next(100, 200), _random.Next(100, 200)));
                }
                
                var font = new Font("Arial", 42, FontStyle.Bold | FontStyle.Italic);
                
                for (int i = 0; i < code.Length; i++)
                {
                    var angle = _random.Next(-15, 15);
                    var x = 35 + i * 55;
                    var y = 50 + _random.Next(-10, 10);
                    
                    using (var path = new GraphicsPath())
                    {
                        path.AddString(code[i].ToString(), font.FontFamily, (int)font.Style, font.Size, new Point(x, y), StringFormat.GenericDefault);
                        
                        var matrix = new Matrix();
                        matrix.RotateAt(angle, new PointF(x + 20, y + 30));
                        g.Transform = matrix;
                        
                        var brush = new LinearGradientBrush(new Rectangle(0, 0, width, height), 
                            Color.FromArgb(_random.Next(20, 100), _random.Next(20, 100), _random.Next(20, 100)),
                            Color.FromArgb(_random.Next(100, 200), _random.Next(100, 200), _random.Next(100, 200)), 
                            LinearGradientMode.Horizontal);
                        
                        g.FillPath(brush, path);
                        g.ResetTransform();
                    }
                }
                
                using (var pen = new Pen(Color.Gray, 2))
                {
                    g.DrawRectangle(pen, 0, 0, width - 1, height - 1);
                }
                
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    return (ms.ToArray(), code.ToUpper());
                }
            }
        }
        
        private static string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ0123456789";
            var code = new char[length];
            for (int i = 0; i < length; i++)
            {
                code[i] = chars[_random.Next(chars.Length)];
            }
            return new string(code);
        }
    }
}