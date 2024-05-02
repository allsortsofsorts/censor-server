using System.Windows;
using Application = System.Windows.Forms.Application;
using System.Drawing.Imaging;

namespace Microsoft.Extensions.DependencyInjection
{
    //TODO this file is also not used at the momement

    public static class Overlay
    {
        public partial class OpaqueForm : CSWinFormLayeredWindow.PerPixelAlphaForm
        {
            public OpaqueForm()
            {
                Bitmap bitmap = new Bitmap((int)SystemParameters.VirtualScreenWidth, (int)SystemParameters.VirtualScreenHeight, PixelFormat.Format32bppArgb);
                using (Graphics gfx = Graphics.FromImage(bitmap))
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(128, 245, 39, 143)))
                {
                    gfx.FillRectangle(brush, 0, 0, (int)SystemParameters.VirtualScreenWidth, (int)SystemParameters.VirtualScreenHeight);
                }

                this.SelectBitmap(bitmap);
            }
        }

        public static WebApplication StartOverlay(this WebApplication app)
        {
            var logger = app.Logger;
            logger.LogDebug($"Starting Overlay...");
            var overlayThread = new Thread(() => Application.Run(new OpaqueForm()));
            overlayThread.SetApartmentState(ApartmentState.STA);
            overlayThread.IsBackground = true;
            overlayThread.Start();
            logger.LogInformation($"Loaded Overlay");
            return app;
        }
    }
}