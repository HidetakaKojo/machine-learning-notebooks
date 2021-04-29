using UnityEngine;
using Unity.Barracuda;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class ImageClassification : MonoBehaviour
{
    public NNModel modelFile;
    public TextAsset labelsFile;

    public const int IMAGE_SIZE = 224;
    private const int IMAGE_MEAN = 117;
    private const float IMAGE_STD = 127.5f;
    private const string INPUT_NAME = "input";
    private const string OUTPUT_NAME = "MobilenetV2/Predictions/Reshape_1";
    //private const string OUTPUT_NAME = "Identity";

    private IWorker worker;
    private string[] labels;
    private int waitIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        this.labels = Regex.Split(this.labelsFile.text, "\n|\r|\r\n")
            .Where(s => !String.IsNullOrEmpty(s)).ToArray();
        var model = ModelLoader.Load(this.modelFile);

        this.worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
    }

    public IEnumerator Predict(Color32[] picture, System.Action<List<KeyValuePair<string, float>>> callback)
    {
        var map = new List<KeyValuePair<string, float>>();

        using (var tensor = TransformInput(picture, IMAGE_SIZE, IMAGE_SIZE))
        {
            var inputs = new Dictionary<string, Tensor>();
            inputs.Add(INPUT_NAME, tensor);

            var enumerator = this.worker.StartManualSchedule(inputs);

            while (enumerator.MoveNext())
            {
                waitIndex++;
                if (waitIndex >= 20)
                {
                    waitIndex = 0;
                    yield return null;
                }
            };

            var output = worker.PeekOutput(OUTPUT_NAME);
            for (int i = 0; i < labels.Length; i++) {
                map.Add(new KeyValuePair<string, float>(labels[i], output[i] * 100));
            }

        }

        callback(map.OrderByDescending(x => x.Value).ToList());
    }

    public static Tensor TransformInput(Color32[] picture, int width, int height)
    {
        var input = new Tensor(1, height, width, 3);
        float[] floatValues = new float[width * height * 3];
        for (int i = 0; i < picture.Length; ++i)
        {
            var color = picture[i];
            var y = i % IMAGE_SIZE;
            var x = i / IMAGE_SIZE;
            input[0, (IMAGE_SIZE-1)-y, x, 0] = (color.r - IMAGE_MEAN) / IMAGE_STD;
            input[0, (IMAGE_SIZE-1)-y, x, 1] = (color.g - IMAGE_MEAN) / IMAGE_STD;
            input[0, (IMAGE_SIZE-1)-y, x, 2] = (color.b - IMAGE_MEAN) / IMAGE_STD;
        }
        return input;
    }
}