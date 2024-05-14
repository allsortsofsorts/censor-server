using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Discord.WebSocket;
using Discord;
using Microsoft.VisualBasic.ApplicationServices;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Windows;
using Color = System.Drawing.Color;
using System.Threading;
using System.Windows.Interop;
using System.Xml.Linq;
using System.Reflection.Metadata;
using static System.Net.Mime.MediaTypeNames;
using System.Net;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.Formats.Png;
using System.Drawing;
using ColorMatrix = System.Drawing.Imaging.ColorMatrix;
using Rectangle = System.Drawing.Rectangle;
using System.Drawing.Drawing2D;
using Microsoft.VisualBasic.Logging;
using SixLabors.ImageSharp.PixelFormats;
using Accessibility;
using System.Net.NetworkInformation;
using RectangleF = System.Drawing.RectangleF;
using OpenCvSharp;
using System.Numerics;
using OpenCvSharp.Aruco;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DiscordService
    {

        //move these
        public static byte[] ToArray(this SixLabors.ImageSharp.Image imageIn)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                imageIn.Save(ms, PngFormat.Instance);
                return ms.ToArray();
            }
        }

        public static global::System.Drawing.Image ToNetImage(this byte[] byteArrayIn)
        {
            using (MemoryStream ms = new MemoryStream(byteArrayIn))
            {
                global::System.Drawing.Image returnImage = global::System.Drawing.Image.FromStream(ms);
                return returnImage;
            }
        }

        public class DiscordOverrides
        {
            //todo use me
            public ConcurrentDictionary<string, Dictionary<string, string>> Overrides { get; } = new ConcurrentDictionary<string, Dictionary<string, string>>();
        }

        public static WebApplication StartDiscord(this WebApplication app)
        {
            var logger = app.Logger;
            logger.LogDebug($"Starting Discord...");
            var discordThread = new Thread(() => DiscordThread.DoWork());
            discordThread.SetApartmentState(ApartmentState.STA);
            discordThread.Start();
            logger.LogInformation($"Loaded Discord");
            return app;
        }

        public class DiscordThread
        {

            //TODO make an init to populate these things so we don't have warnings
            private static DiscordSocketClient? _client;
            private static HttpClient _httpClient = new HttpClient();
            private static Random _random = new Random();
            private static CensorService _service = new CensorService();
            private static bool _censorEnabled = false;
            private static Dictionary<string, bool> _censorStyle = new Dictionary<string, bool>() {
            { "FEMALE_GENITALIA_COVERED", true },
            { "BUTTOCKS_EXPOSED", true },
            { "FEMALE_BREAST_EXPOSED", true },
            { "FEMALE_GENITALIA_EXPOSED", true },
            { "ANUS_EXPOSED", true },
            { "ANUS_COVERED", true },
            { "FEMALE_BREAST_COVERED", true },
            { "BUTTOCKS_COVERED", true },
            };



            private static void InitCommands()
            {
               // Subscribe a handler to see if a message invokes a command.
               _client.MessageReceived += HandleCommandAsync;
                _client.MessageUpdated += HandleUpdateAsync;
                _client.Ready += HandleClientReady;
                return;
            }

            public async static void DoWork()
            {

                var config = new DiscordSocketConfig()
                {
                    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
                };

                _client = new DiscordSocketClient(config);

                //_discordOverrides = discordOverrides;

                var dir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
                string fullFilePath = Path.Combine(dir, "token.txt");
                if (!File.Exists(fullFilePath))
                {
                    Console.WriteLine($"Failed to load Discord token from {fullFilePath} skipping Discord Loading");
                    return;
                }
                var token = File.ReadAllText(fullFilePath);

                // Centralize the logic for commands into a separate method.
                InitCommands();

                // Login and connect.
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();
                Console.WriteLine("Discord Bot Started");

                // Wait infinitely so your bot actually stays connected.
                await Task.Delay(Timeout.Infinite);
            }

            private static async Task HandleUpdateAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
            {
                await HandleCommandAsync(after);
            }
            private static async Task HandleCommandAsync(SocketMessage arg)
            {

                // Bail out if it's a System Message.
                var msg = arg as SocketUserMessage;
                if (msg == null) return;


                if (msg.Content == "!test")
                {
                    handleTestCommand(msg);
                    return;
                }
                //This is the line to change if you want to give someone else control.
                //if (msg.Author.Id != x) return;

                // We don't want the bot to respond to itself or other bots.
                if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot) return;
                var content = msg.Content;
                if (msg.Content.StartsWith("!"))
                {
                    var split = content.Split(' ', 2);
                    switch (split[0])
                    {

                        case "!censor-start":
                            handleCensorStartCommand(msg);
                            break;
                        case "!censor-stop":
                            handleCensorStopCommand(msg);
                            break;
                        case "!censor-tune":
                            handleCensorTuneCommand(msg);
                            break;
                        case "!shutdown":
                            handleShutdownCommand(msg);
                            break;
                        case "!set-background":
                            handleSetBackgroundCommand(msg);
                            break;
                        case "!pink":
                            handlePinkCommand(msg);
                            break;
                        case "!image-overlay":
                            handleImageOverlayCommand(msg);
                            break;
                        default:
                            // unknown command don't do anything
                            break;
                    }
                }
            }

            private static async Task HandleClientReady()
            {
                //TODO fix this later
            }

            private static async void handleTestCommand(IMessage msg)
            {
                await msg.Channel.SendMessageAsync("Can see and respond to messages");
            }

            private static async void handleSetBackgroundCommand(IMessage msg)
            {
                //https://stackoverflow.com/questions/1061678/change-desktop-wallpaper-using-code-in-net
            }

            private static void makeBitmapOpaque(Bitmap b, System.Drawing.Image image, int opacity)
            {
                BitmapData l = b.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                IntPtr ptr = l.Scan0;
                int numBytes = b.Width * b.Height * 4;
                byte[] argbValues = new byte[numBytes];
                Marshal.Copy(ptr, argbValues, 0, numBytes);

                // Manipulate the bitmap, such as changing the
                // RGB values for all pixels in the the bitmap.
                for (int counter = 0; counter < argbValues.Length; counter += 4)
                {
                    // argbValues is in format BGRA (Blue, Green, Red, Alpha)
                    // If 100% transparent, skip pixel
                    if (argbValues[counter + 4 - 1] == 0)
                        continue;
                    int pos = 0;
                    pos++; // B
                    pos++; // G
                    pos++; // R
                    argbValues[counter + pos] = (byte)opacity;
                }
                // Copy the ARGB values back to the bitmap
                Marshal.Copy(argbValues, 0, ptr, numBytes);
                b.UnlockBits(l);
            }
            private static (Bitmap,SixLabors.ImageSharp.Point) processImage(Image image, int opacity, string mode, SixLabors.ImageSharp.Point existingPoint )
            {


                //preprocess the image so it will fit on the screen. should be a helper
                if (image.Width > SystemParameters.VirtualScreenWidth || image.Height > SystemParameters.VirtualScreenHeight)
                {
                    var newHeight = SystemParameters.VirtualScreenHeight;
                    var newWidth = SystemParameters.VirtualScreenWidth;
                    // naively check height first
                    if (image.Height > SystemParameters.VirtualScreenHeight)
                    {
                        // calculate what new width would be
                        var heightReductionRatio = SystemParameters.VirtualScreenHeight / image.Height;
                        newWidth = image.Width * heightReductionRatio;
                    }

                    if (newWidth > SystemParameters.VirtualScreenWidth)
                    {
                        // if so, re-calculate height to fit for maxWidth
                        var widthReductionRatio = SystemParameters.VirtualScreenWidth / newWidth; // ratio of maxWidth:newWidth (height reduction ratio may have been applied)
                        newHeight = SystemParameters.VirtualScreenHeight * widthReductionRatio; // apply new ratio to maxHeight to get final height
                        newWidth = SystemParameters.VirtualScreenWidth;
                    }
                    image.Mutate(x => x.Resize((int)newWidth, (int)newHeight));
                }

                if (mode == "maximize")
                {
                    double widthRatio = (double)image.Width / (double)SystemParameters.VirtualScreenWidth;
                    double heightRatio = (double)image.Height / (double)SystemParameters.VirtualScreenHeight;
                    double ratio = Math.Max(widthRatio, heightRatio);
                    int newWidth = (int)(image.Width / ratio);
                    int newHeight = (int)(image.Height / ratio);
                    image.Mutate(x => x.Resize(newWidth, newHeight));
                    image.Mutate(x => x.Pad((int)SystemParameters.VirtualScreenWidth, (int)SystemParameters.VirtualScreenHeight, SixLabors.ImageSharp.Color.Transparent));

                }
                else if (mode == "stretch")
                {
                    image.Mutate(x => x.Resize((int)SystemParameters.VirtualScreenWidth, (int)SystemParameters.VirtualScreenHeight));
                } else
                {
                    //todo randomly place this on the screen need to take screen size - image size use that as upper bounds.
                    // gifs need to get the same frame each time or it will be weird.
                    var xRange = SystemParameters.VirtualScreenWidth - image.Width;
                    var yRange = SystemParameters.VirtualScreenHeight - image.Height;
                    if (existingPoint == new SixLabors.ImageSharp.Point(0, 0))
                    {
                        existingPoint = new SixLabors.ImageSharp.Point(_random.Next(0, (int)xRange), _random.Next(0, (int)yRange));
                    }
                    var newImage = new Image<Rgba32>((int)SystemParameters.VirtualScreenWidth, (int)SystemParameters.VirtualScreenHeight); 
                    newImage.Mutate(x => x.DrawImage(image, existingPoint, 1));
                    image = newImage;
                }


                // is there a way that doesn't convert between these about 800 times
                var im = image.ToArray().ToNetImage();
                Bitmap b = new Bitmap(im).Clone(new System.Drawing.Rectangle(0, 0, image.Width, image.Height), PixelFormat.Format32bppArgb);
                if (opacity >= 0)
                {
                    makeBitmapOpaque(b, im, opacity);
                }
                return (b, existingPoint);

            }

            private static async void handleCensorTuneCommand(IMessage msg)
            {
                parseCensorTuneCommand(msg.Content);
                await msg.Channel.SendMessageAsync("Censoring styles updated");
            }

            private static void parseCensorTuneCommand(string content)
            {
                //TODO look into a CLI lib to do something like this, otherwise lift it ourselves, for now doing this as a one off...
                var startOfLine = 0; // magic number is chars in "censor-tune "
                var endOfLine = content.Length;
                if (startOfLine < 0 || endOfLine < 0)
                {
                    //TODO return an errors
                    return;
                }

                string line = content.Substring(startOfLine + 1, endOfLine - 1 - startOfLine);
                string[] splitOnSpaces = content.Split(' ');
                foreach (string piece in splitOnSpaces)
                {
                    if (piece.Contains("="))
                    {
                        string[] splitOnEqual = piece.Split('=');
                        if (splitOnEqual.Length == 2)
                        {
                            _censorStyle[splitOnEqual[0]] = bool.Parse(splitOnEqual[1]);
                        }
                    }
                }
            }

            private static async void handleCensorStopCommand(IMessage msg)
            {
                _censorEnabled = false;
                await msg.Channel.SendMessageAsync("Censoring stopped if it was running");
            }

                private static async void handleCensorStartCommand(IMessage msg)
            {

                if (_censorEnabled)
                {
                    await msg.Channel.SendMessageAsync("Cannot start two censors at a time");
                    return;
                }
                await msg.Channel.SendMessageAsync("Censoring starting");
                _censorEnabled = true;
                Thread thread = new Thread(() =>
                {
                    OpaqueForm window = null;
                    window = new OpaqueForm(0,0,0,0);
                    window.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    window.ShowInTaskbar = false;
                    window.Visible = true;
                    window.TopMost = true;
                    window.Show();
                    List<RectangleF> detections = new List<RectangleF>();
                    while (_censorEnabled)
                    {
                        // just go as fast as we can its like a frame anyway ._.
                        window.SelectBitmap(_service.ProduceCensoredDesktop(_censorStyle));
                    }
                    window.Visible = false;
                    window.Dispose();
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }


                private static async void handleImageOverlayCommand(IMessage msg)
            {
                //https://stackoverflow.com/questions/1061678/change-desktop-wallpaper-using-code-in-net
                if (msg.Attachments.Count != 1)
                {
                    await msg.Channel.SendMessageAsync("No attachments found");
                    return;
                }
                

                (int opacity, int active_duration, string mode) = parseImageOverlayCommand(msg.Content);

                //todo dispose of all of these?
                //todo handle different types eg gifs
                //todo tile, stretch, center, maybe a random location? theres got to be a cleaner way.
                string url = msg.Attachments.ElementAt(0).Url;
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                Stream stream= await response.Content.ReadAsStreamAsync();

                var processedImages = new List<Bitmap>();
                var image = Image.Load(stream);
                int delay = 1;
                if (image?.Metadata?.DecodedImageFormat?.Name == "GIF") {
                    delay = image.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay;
                    var existingPoint = new SixLabors.ImageSharp.Point(0, 0);
                    for (int i = 0; i < image.Frames.Count; i++)
                    {
                        (Bitmap b, existingPoint) = processImage(image.Frames.CloneFrame(i), opacity, mode, existingPoint);
                        processedImages.Add(b);
                    }
                } else
                {
                    (Bitmap b, var _) = processImage(image, opacity, mode, new SixLabors.ImageSharp.Point(0, 0));
                    processedImages.Add(b);
                }


                Thread thread = new Thread(() =>
                {
                    OpaqueForm window = null;
                    if (processedImages.Count == 1)
                    {
                        window = new OpaqueForm(processedImages[0]);
                        window.Visible = true;
                        window.TopMost = true;
                        window.ShowInTaskbar = false;
                        window.Show();
                        Thread.Sleep(active_duration * 1000);
                        window.Visible = false;
                        window.Dispose();
                    } else
                    {

                            DateTime startTime = DateTime.UtcNow;
                            window = new OpaqueForm(processedImages[0]);
                            window.Visible = true;
                            window.TopMost = true;
                            window.ShowInTaskbar = false;
                            window.Show();
                            for (int i = 0; i < processedImages.Count; i++)
                            {
                                window.SelectBitmap(processedImages[i]);
                                Thread.Sleep(delay *10); // delay multiplied by 10 since GIF spec is in centiseconds
                                if( i == processedImages.Count - 1 )
                                {
                                    //make this loop forever and wait for our parent to kill us.
                                    i =0;
                                }

                                if ((DateTime.UtcNow - startTime).TotalSeconds > active_duration)
                                {
                                    window.Dispose();
                                    break;
                                }
                            }
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();


            }

            private static async void handleShutdownCommand(IMessage msg)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    await msg.Channel.SendMessageAsync("Unsupported OS");
                    return;
                }
                await msg.Channel.SendMessageAsync("Shutdown is imminent");
                var psi = new ProcessStartInfo("shutdown", "/s /t 0");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
            }

            public partial class OpaqueForm : CSWinFormLayeredWindow.PerPixelAlphaForm
            {
                public OpaqueForm(int opacity, int r, int g, int b)
                {
                    Bitmap bitmap = new Bitmap((int)SystemParameters.VirtualScreenWidth, (int)SystemParameters.VirtualScreenHeight, PixelFormat.Format32bppArgb);
                    using (Graphics gfx = Graphics.FromImage(bitmap))
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(opacity, r, g, b)))
                    {
                        gfx.FillRectangle(brush, 0, 0, (int)SystemParameters.VirtualScreenWidth, (int)SystemParameters.VirtualScreenHeight);
                    }
                    this.SelectBitmap(bitmap);
                }

                public OpaqueForm(Bitmap b)
                {
                    //todo make this more interesting
                    this.SelectBitmap(b);
                }
            }

            private static Tuple<int, int, string> parseImageOverlayCommand(string content)
            {
                //TODO look into a CLI lib to do something like this, otherwise lift it ourselves, for now doing this as a one off...
                int opacity = -1;
                int active_duration = 30;
                string mode = "random"; //todo make modes an enum.
                var startOfLine = 0;
                var endOfLine = content.Length;
                if (startOfLine < 0 || endOfLine < 0)
                {
  
                    return Tuple.Create(opacity, active_duration, mode);
                }
                string line = content.Substring(startOfLine + 1, endOfLine - 1 - startOfLine);
                string[] splitOnSpaces = content.Split(' ');
                foreach (string piece in splitOnSpaces)
                {
                    if (piece.StartsWith("opacity="))
                    {
                        opacity = Int32.Parse(piece.Split('=')[1]);
                    }

                    if (piece.StartsWith("active_duration="))
                    {
                        active_duration = Int32.Parse(piece.Split('=')[1]);
                    }

                    if (piece.StartsWith("mode="))
                    {
                        mode = piece.Split('=')[1];
                    }
                }
                    

                return Tuple.Create(opacity, active_duration, mode);
            }

            private static Tuple<int, int, int, int, int, int, int> parsePinkCommand(string content)
            {
                //TODO look into a CLI lib to do something like this, otherwise lift it ourselves, for now doing this as a one off...
                int times = 1;
                int active_duration = 1;
                int inactive_duration = 1;
                int r = 0;
                int g = 0;
                int b = 0;
                int opacity = 0;

                var startOfLine = 0;
                var endOfLine = content.Length;
                if (startOfLine < 0 || endOfLine < 0) {
                    //TODO return an errors
                    return Tuple.Create(times, active_duration, inactive_duration, r, g, b, opacity);
                }
                string line = content.Substring(startOfLine + 1, endOfLine - 1 - startOfLine);
                string[] splitOnSpaces = content.Split(' ');
                foreach (string piece in splitOnSpaces)
                {
                    if (piece.StartsWith("times="))
                    {
                        times = Int32.Parse(piece.Split('=')[1]);
                    }

                    if (piece.StartsWith("active_duration="))
                    {
                        active_duration = Int32.Parse(piece.Split('=')[1]);
                    }

                    if (piece.StartsWith("inactive_duration="))
                    {
                        inactive_duration = Int32.Parse(piece.Split('=')[1]);
                    }

                   if (piece.StartsWith("color_r="))
                    {
                       r = Int32.Parse(piece.Split('=')[1]);
                        if (r < 0)
                        {
                            r = 0;
                        }
                        else if (r > 255)
                        {
                            r = 255;
                        }
                    }

                    if (piece.StartsWith("color_g="))
                    {
                        g = Int32.Parse(piece.Split('=')[1]);
                        if (g< 0)
                        {
                            g = 0;
                        }
                        else if (g > 255)
                        {
                            g = 255;
                        }
                    }

                    if (piece.StartsWith("color_b="))
                    {
                        b = Int32.Parse(piece.Split('=')[1]);
                        if (b < 0)
                        {
                            b = 0;
                        }
                        else if (b > 255)
                        {
                           b = 255;
                        }
                    }
                   
                    if (piece.StartsWith("opacity="))
                    {
                        opacity = Int32.Parse(piece.Split('=')[1]);
                        if (opacity < 0 )
                        {
                            opacity = 0;
                        } else if (opacity > 255) {
                            opacity = 255;
                        }
                    }
                }

                return Tuple.Create(times, active_duration, inactive_duration, r, g, b, opacity);
            }

            [DllImport("User32.dll")]
            public static extern Int32 SetForegroundWindow(int hWnd);

            private static async void handlePinkCommand(IMessage msg)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    await msg.Channel.SendMessageAsync("Unsupported OS");
                    return;
                }
                await msg.Channel.SendMessageAsync("PINKED");

                Func<Task> callback = async () => { await msg.Channel.SendMessageAsync($"PINKED OVER"); };
                Thread thread = new Thread(() =>
                {
                    (int times, int active_duration, int inactive_duration, int r, int g, int b, int opacity) = parsePinkCommand(msg.Content);
                    var window = new OpaqueForm(opacity, r, g,b);
                    for (int i = 0; i < times; i = i + 1)
                    {
                        window.Visible = true;
                        window.TopMost = true;
                        window.ShowInTaskbar = false;
                        window.Show();
                        Thread.Sleep(active_duration * 1000);
                        window.Visible = false;
                        Thread.Sleep(inactive_duration * 1000);
                    }
                    window.Dispose();
                    callback();
                });
                thread.SetApartmentState(ApartmentState.STA);

                thread.Start();
            }

        }
    }
}