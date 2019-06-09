using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK;
using SkiaSharp;

namespace SkiaSharpMemoryIssue
{
    public partial class MainWindow : Window
    {
        private GRContext _grContext;
        private SKSurface _surface;
        private SKImageInfo _imageInfo;
        private WriteableBitmap _wb;
        private SKImage _rasterImg;
        private List<SKImage> _textureImages = new List<SKImage>();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            //this is just a code to init everything and render image on a window to make sure everything is working as expected
            new GameWindow();
            _grContext = GRContext.Create(GRBackend.OpenGL);
            _imageInfo = new SKImageInfo(5996, 4003, SKColorType.Bgra8888, SKAlphaType.Premul);
            _surface = SKSurface.Create(_grContext, false, _imageInfo, 1, GRSurfaceOrigin.TopLeft);
            _rasterImg = SKImage.FromEncodedData(File.ReadAllBytes("hires.jpg"));
            _surface.Canvas.DrawImage(_rasterImg, 0, 0);
            _surface.Canvas.Flush();

            Img.Source = _wb = new WriteableBitmap(_imageInfo.Width, _imageInfo.Height, 1000, 1000, PixelFormats.Bgra32,
                BitmapPalettes.Halftone256Transparent);

            _surface.ReadPixels(_imageInfo, _wb.BackBuffer, _imageInfo.Width * 4, 0, 0);
            _wb.Lock();
            _wb.AddDirtyRect(new Int32Rect(0,0,_wb.PixelWidth, _wb.PixelHeight));
            _wb.Unlock();
        }

        //here we are creating a texture backed image snapshot every second
        //on x86 this code allocates only GPU memory, but on x64 also allocates RAM memory somehow
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            for (;;)
            {
                await Task.Delay(1000);
                using (var surf = SKSurface.Create(_grContext, false, _imageInfo, 1, GRSurfaceOrigin.TopLeft))
                {
                    surf.Canvas.DrawImage(_rasterImg, 0, 0);
                    surf.Canvas.Flush();
                    _textureImages.Add(surf.Snapshot());
                }
            }
        }

        //Dispose everything to not get memory exceptions
        protected override void OnClosing(CancelEventArgs e)
        {
            _rasterImg.Dispose();
            _surface.Dispose();
            foreach (var img in _textureImages)
            {
                img.Dispose();
            }
            base.OnClosing(e);
        }
    }
}
