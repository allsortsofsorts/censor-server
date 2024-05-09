using Microsoft.ML.OnnxRuntime;
using SessionOptions = Microsoft.ML.OnnxRuntime.SessionOptions;
using System.Drawing.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Rectangle = System.Drawing.Rectangle;
using Image = SixLabors.ImageSharp.Image;
using Microsoft.ML.OnnxRuntime.Tensors;
using NumSharp;
using OpenCvSharp;
using NumSharp.Backends.Unmanaged;
using OpenCvSharp.Dnn;
using System.Windows.Shapes;
using PointF = SixLabors.ImageSharp.PointF;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System.Drawing;
using Color = System.Drawing.Color;
using RectangleF = System.Drawing.RectangleF;
using Shape = NumSharp.Shape;
using Discord.WebSocket;
using System.Windows.Forms.Design;
using System.Diagnostics;
using System.Windows;
using System.Runtime.InteropServices;
using Path = System.IO.Path;
using System.Reflection;

public class CensorService
{

    private readonly InferenceSession _session;
    public static readonly string[] ClassList = new[] {
    "FEMALE_GENITALIA_COVERED",
    "FACE_FEMALE",
    "BUTTOCKS_EXPOSED",
    "FEMALE_BREAST_EXPOSED",
    "FEMALE_GENITALIA_EXPOSED",
    "MALE_BREAST_EXPOSED",
    "ANUS_EXPOSED",
    "FEET_EXPOSED",
    "BELLY_COVERED",
    "FEET_COVERED",
    "ARMPITS_COVERED",
    "ARMPITS_EXPOSED",
    "FACE_MALE",
    "BELLY_EXPOSED",
    "MALE_GENITALIA_EXPOSED",
    "ANUS_COVERED",
    "FEMALE_BREAST_COVERED",
    "BUTTOCKS_COVERED",
    };

    public static CensorService Create()
    {
        return new CensorService();
    }

    //TODO see if we can interpolate for better results?
    public CensorService()
    {
        // load the model
        const string modelPath = "Censor/best.onnx";
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        string root = Directory.GetCurrentDirectory();
        string fullFilePath = Path.Combine(root, modelPath);
       
        //var deviceId = 0;
        //todo the below is replaced to do the hw i think

        // is  this just an issue with the hardware sucking?
        var hwOpts = new SessionOptions()
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
        };
        // var hwOpts = SessionOptions.MakeSessionOptionWithCudaProvider(0);
        //hwOpts.AppendExecutionProvider_DML(deviceId);
        this._session = new InferenceSession(fullFilePath, hwOpts);
    }

    public (Bitmap, List<RectangleF> detections) ProduceCensoredDesktop(DiscordService.DiscordThread.OpaqueForm window, List<RectangleF> priorDetections)
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        Bitmap capture = CaptureScreen(window, priorDetections); //TODO do I need to make this a png?
        (DenseTensor<float> tensor, int padX, int padY, double resizeFactor )= PreprocessImage(capture);
        (Bitmap censoredImage, List<RectangleF> detections) = RunInferenceAndCensor(tensor, padX, padY, resizeFactor);
        stopwatch.Stop();
        return (censoredImage,detections);
    }
    private Bitmap CaptureScreen(DiscordService.DiscordThread.OpaqueForm window, List<RectangleF> priorDetections)
    {
 
        //Creating a new Bitmap object
        Bitmap captureBitmap = new Bitmap(
           SystemInformation.VirtualScreen.Width,
           SystemInformation.VirtualScreen.Height,
        PixelFormat.Format32bppArgb
        );

        Rectangle captureRectangle = new Rectangle(0, 0, SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
        Graphics captureGraphics = Graphics.FromImage(captureBitmap);
        //https://stackoverflow.com/questions/46590295/how-can-i-capture-the-screen-and-ignore-a-specific-window-in-the-image
        // It works but it flickers.
        //https://stackoverflow.com/questions/69145807/take-a-screenshot-behind-window-in-c-sharp-winform
        // todo set this in the thing
        //https://stackoverflow.com/questions/6812068/c-sharp-which-is-the-fastest-way-to-take-a-screen-shot
        //do i need to do this in fragments or something and flicker off ?
        //https://github.com/fffelix-jan/ScreenBlindfold/blob/main/ScreenBlindfold/BlindfoldForm.cs
        //https://www.youtube.com/watch?v=yh7Kd3o5Y3k
        //https://stackoverflow.com/questions/37931433/capture-screen-of-window-by-handle
        //https://stackoverflow.com/questions/17396000/is-there-any-way-to-hide-chrome-window-and-capture-a-screenshot-or-convert-the-c
        //https://stackoverflow.com/questions/1163761/capture-screenshot-of-active-window
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        // capture the screen with censors.
        captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);
        // still flickering a ton ):
        for(int i=0; i<priorDetections.Count; i++)
        {

            //Creating a new Bitmap object
            Bitmap subregionCapture = new Bitmap(
               (int)priorDetections[i].Width,
               (int)priorDetections[i].Height,
            PixelFormat.Format32bppArgb
            );

            Rectangle subRegionRectangle = new Rectangle((int)priorDetections[i].X, (int)priorDetections[i].Y, (int)priorDetections[i].Width, (int)priorDetections[i].Height);
            Graphics subregionCaptureGraphics = Graphics.FromImage(subregionCapture);
            window.Visible = false;
            subregionCaptureGraphics.CopyFromScreen(subRegionRectangle.Left, subRegionRectangle.Top, 0, 0, subRegionRectangle.Size);
            window.Visible = true;
            // now overlay the region onto the new one.
            captureGraphics.DrawImage(subregionCapture, (int)priorDetections[i].X, (int)priorDetections[i].Y);
        }
        stopwatch.Stop();
        return captureBitmap;
    }

    private (DenseTensor<float>, int, int, double) PreprocessImage(Bitmap incoming)
    {
        //TODO lets not use 800 image libraries at some point
        incoming.Save("screen", ImageFormat.Png);
        var image = Image.Load("screen");

        float aspect = (float)image.Width / (float)image.Height;
        int newHeight, newWidth;
        int targetSize = 320; // magic number is model width/height
        if (image.Height > image.Width)
        {
            newHeight = targetSize;
            newWidth = (int)(targetSize * aspect);
        }
        else
        {
            newWidth = targetSize;
            newHeight = (int)(targetSize / aspect);
        }

        var padX = targetSize - newWidth;
        var padY = targetSize - newHeight;
        var resizeFactor = Math.Sqrt((Math.Pow(image.Width, 2) + Math.Pow(image.Height, 2)) / (Math.Pow(newWidth, 2) + Math.Pow(newHeight, 2)));

        image.Mutate(x => x.Resize(newWidth, newHeight));
        image.Mutate(x => x.Pad(targetSize, targetSize)); //todo i might have messed this up. inspect padding later.
        image.Save("resize.png"); // mmm saving and loading to disk multiple times. why didn't i do this in python
        var cvIm = Cv2.ImRead("resize.png", ImreadModes.Unchanged);
        Cv2.CvtColor(cvIm, cvIm, ColorConversionCodes.RGB2BGR);

        var pnd = ToNDArray(cvIm);
        pnd = pnd.astype(typeof(float)) / 255.0;
        int[] transDir = new int[] { 2, 0, 1 };
        pnd = np.transpose(pnd, transDir);
        pnd = np.expand_dims(pnd, 0);
        var dimensions = new int[] { 1, 3, 320, 320 };
        return (new DenseTensor<float>(pnd.ToArray<float>(), dimensions, false), padX, padY, resizeFactor);
    }

    private (Bitmap, List<RectangleF>) RunInferenceAndCensor(DenseTensor<float> tensor, int padX, int padY, double resizeFactor)
    {
        var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("images", tensor)
            };

        // Run inference
        var results = this._session.Run(inputs);

        // Postprocess to get predictions
        var resultsArray = results.ToList()[0].AsTensor<float>();
        var nd = new NDArray(resultsArray.ToArray(), new Shape(1, 22, 2100));
        var outputs = np.transpose(np.squeeze(nd));
        var rows = outputs.shape[0];
        var nmsboxes = new List<Rect> { };
        var boxes = new List<RectangleF> { };
        var detections = new List<RectangleF> { };
        var scores = new List<float> { };
        var class_ids = new List<int> { };
        for (int i = 0; i < rows; i++)
        {
            var classes_scores = outputs[i][$"4:"];
            var max_score = np.amax(classes_scores);
            if (max_score >= 0.2f)
            {
                var class_id = np.argmax(classes_scores);
                float x = outputs[i][0];
                float y = outputs[i][1];
                float w = outputs[i][2];
                float h = outputs[i][3];
                int left = (int)Math.Round((x - w * 0.5 - padX / 2) * resizeFactor);
                int top = (int)Math.Round((y - h * 0.5 - padY / 2) * resizeFactor);
                int width = (int)Math.Round(w * resizeFactor);
                int height = (int)Math.Round(h * resizeFactor);
                class_ids.Add(class_id);
                scores.Add(max_score);
                nmsboxes.Add(new Rect(left, top, width, height));
                boxes.Add(new RectangleF(left, top, width, height));
            }
        }

        int[] indices;
        CvDnn.NMSBoxes(nmsboxes, scores, 0.25f, 0.45f, out indices);
        for (int i = 0; i < indices.Length; i++)
        {
            // these are all needed so we can make this configurable which we will do in a follow up diff
            var box = boxes[indices[i]];
            var score = scores[indices[i]];
            var class_id = class_ids[indices[i]];
            detections.Add(boxes[indices[i]]);
        }

        Bitmap bitmap = new Bitmap((int)SystemParameters.VirtualScreenWidth, (int)SystemParameters.VirtualScreenHeight, PixelFormat.Format32bppArgb);
        using (Graphics gfx = Graphics.FromImage(bitmap))
        using (SolidBrush shadowBrush = new SolidBrush(Color.Pink))
        using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 0, 0, 0)))
        {
            gfx.FillRectangle(brush, 0, 0, (int)SystemParameters.VirtualScreenWidth, (int)SystemParameters.VirtualScreenHeight);
            if (detections.Count > 0) {
                gfx.FillRectangles(shadowBrush, detections.ToArray());
            }
        }
        return (bitmap, detections);
    }
   
    static unsafe NDArray ToNDArray(Mat src)
    {
        var nd = new NDArray(NPTypeCode.Byte, (src.Height, src.Width, src.Type().Channels), fillZeros: false);
        new UnmanagedMemoryBlock<byte>(src.DataPointer, nd.size)
            .CopyTo(nd.Unsafe.Address);

        return nd;
    }
}

