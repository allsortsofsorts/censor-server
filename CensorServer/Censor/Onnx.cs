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

    public CensorService()
    {
        //TODO this whole file is junk right now, need to fix soon.
        const string modelPath = "C://Users/x/Downloads/best.onnx";
        //var deviceId = 0;
        //todo the below is replaced to do the hw i think

        // is  this just an issue with the hardware sucking?
        var hwOpts = new SessionOptions()
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
        };
       // var hwOpts = SessionOptions.MakeSessionOptionWithCudaProvider(0);
        //hwOpts.AppendExecutionProvider_DML(deviceId);
        this._session = new InferenceSession(modelPath, hwOpts);

        //todo this should be functions
        using Image<Rgba32> image = Image.Load<Rgba32>("C:/Users/x/Pictures/Screenshots/6fyt1bystawc1.jpeg");
        // Resize image, todo is this needed or desired? should i copy more from: https://github.com/notAI-tech/NudeNet/blob/v3/nudenet/nudenet.py
        //https://github.com/silveredgold/censor-core/blob/33a60eb08943d20a8ad115ec919b95754b5f8f04/src/CensorCore/ImageSharpHandler.cs#L132 ???
        // https://github.com/microsoft/onnxruntime/blob/main/csharp/sample/Microsoft.ML.OnnxRuntime.FasterRcnnSample/Program.cs most from?
        // https://github.com/silveredgold/censor-core/blob/33a60eb08943d20a8ad115ec919b95754b5f8f04/src/CensorCore/TensorLoadOptions.cs#L67 what is this

        float aspect = (float)image.Width / (float)image.Height;
        int newHeight, newWidth;
        int targetSize = 320; // something to do with model width idk.
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
        //image.Mutate(x => x.Resize((int)(aspect * image.Width), (int)(aspect * image.Height)));
        //resize_factor = math.sqrt(
        //(img_width * *2 + img_height * *2) / (new_width * *2 + new_height * *2)
        //)
        image.Mutate(x => x.Resize(newWidth, newHeight));
        image.Mutate(x => x.Pad(targetSize, targetSize));
        image.Save("C:/Users/x/Pictures/Screenshots/resize.png");
        // lets use opencv so its 1:1.
 
        Tensor<float> input = new DenseTensor<float>(new[] { 1, 3, image.Height, image.Width });
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                //todo mess with this?
                Span<Rgba32> pixelSpan = accessor.GetRowSpan(y);
                for (int x = 0; x < pixelSpan.Length; x++)
                {
                    //todo mess around with this.
                    input[0, 0, y, x] = pixelSpan[x].B;// - 103.939F;
                    input[0, 1, y, x] = pixelSpan[x].G;//- 116.779F;
                    input[0, 2, y, x] = pixelSpan[x].R;// - 123.68F;

                }
            }
        });

        Console.WriteLine(this._session.InputMetadata);
        // Setup inputs and outputs
        var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("images", input)
            };

        // Run inference
        var results = this._session.Run(inputs);

        // Postprocess to get predictions
        var resultsArray = results.ToList()[0].AsTensor<float>();
        var nd = new NDArray(resultsArray.ToArray(), new Shape(1, 22, 2100));
        var outputs = np.transpose(np.squeeze(nd));
        var rows = outputs.shape[0];
        for (int i = 0; i < rows; i++)
        {
            //TODO what is this python equivalent?
            var classes_scores = outputs[i];
            var max_score = np.amax(classes_scores);
            if (max_score >= 0.2)
            {
                //TODO continue on from here.
                var class_id = np.argmax(classes_scores);
                var x = outputs[i][0];
                var y = outputs[i][1];
                var w = outputs[i][2];
                var h = outputs[i][3];

                /*
                left = int(round((x - w * 0.5 - pad_left) * resize_factor))
                top = int(round((y - h * 0.5 - pad_top) * resize_factor))
                width = int(round(w * resize_factor))
                height = int(round(h * resize_factor))
                class_ids.append(class_id)
                scores.append(max_score)
               boxes.append([left, top, width, height])
                */
        Console.WriteLine(ClassList[class_id]);
            }
        }


            Console.WriteLine("Tensor Output");
        Console.WriteLine("");
    }

    //todo fix the below should only have one image lib
    private void CaptureScreen()
    {
        //Creating a new Bitmap object
        Bitmap captureBitmap = new Bitmap(
           SystemInformation.VirtualScreen.Width,
           SystemInformation.VirtualScreen.Height,
        PixelFormat.Format32bppArgb
        );
        Rectangle captureRectangle = new Rectangle(0, 0, SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
        Graphics captureGraphics = Graphics.FromImage(captureBitmap);
        captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);
        captureBitmap.Save(@"C:\Users\x\Pictures\Screenshots\Capture.png", ImageFormat.Png);
    }
}
