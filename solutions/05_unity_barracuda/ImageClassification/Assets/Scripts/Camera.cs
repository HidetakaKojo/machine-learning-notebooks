using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Webカメラ
public class Camera : MonoBehaviour
{
    // カメラ
    RawImage rawImage; // RawImage
    WebCamTexture webCamTexture; //Webカメラテクスチャ

    // 推論
    public ImageClassification classification; // 分類
    public Text uiText; // テキスト
    private bool isWorking = false; // 処理中

    // スタート時に呼ばれる
    void Start ()
    {
        // Webカメラの開始
        this.rawImage = GetComponent<RawImage>();
        this.webCamTexture = new WebCamTexture(
                        ImageClassification.IMAGE_SIZE, ImageClassification.IMAGE_SIZE, 30);
        this.rawImage.texture = this.webCamTexture;
        this.webCamTexture.Play();
    }

    // フレーム毎に呼ばれる
    private void Update()
    {
        // 画像分類
        TFClassify();
    }

    // 画像分類
    private void TFClassify()
    {
        if (this.isWorking)
        {
            return;
        }

        this.isWorking = true;

        // 画像の前処理
        StartCoroutine(ProcessImage(result =>
        {
            // 推論の実行
            StartCoroutine(this.classification.Predict(result, probabilities =>
            {
                // 推論結果の表示
                this.uiText.text = "";
                for (int i = 0; i < 5; i++)
                {
                    this.uiText.text += probabilities[i].Key + ": " +
                        string.Format("{0:0.000}%", probabilities[i].Value) + "\n";
                }

                // 未使用のアセットをアンロード
                Resources.UnloadUnusedAssets();
                this.isWorking = false;
            }));
        }));
    }

    // 画像の前処理
    private IEnumerator ProcessImage(System.Action<Color32[]> callback)
    {
        // 画像のクロップ（WebCamTexture → Texture2D）
        yield return StartCoroutine(CropSquare(webCamTexture, texture =>
            {
                // 画像のスケール（Texture2D → Texture2D）
                var scaled = Scaled(texture,
                    ImageClassification.IMAGE_SIZE,
                    ImageClassification.IMAGE_SIZE);
 
                // コールバックを返す
                callback(scaled.GetPixels32());
            }));
    }

    public static IEnumerator CropSquare(WebCamTexture texture, System.Action<Texture2D> callback)
    {
        var smallest = texture.width < texture.height ? texture.width : texture.height;
        var rect = new Rect(0, 0, smallest, smallest);
        Texture2D result = new Texture2D((int)rect.width, (int)rect.height);

        if (rect.width != 0 && rect.height != 0)
        {
            result.SetPixels(texture.GetPixels(
                Mathf.FloorToInt((texture.width - rect.width) / 2),
                Mathf.FloorToInt((texture.height - rect.height) / 2),
                Mathf.FloorToInt(rect.width),
                Mathf.FloorToInt(rect.height)));
            yield return null;
            result.Apply();
        }

        yield return null;
        callback(result);
    }

    public static Texture2D Scaled(Texture2D texture, int width, int height)
    {
        var rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(texture, rt);

        var preRT = RenderTexture.active;
        RenderTexture.active = rt;
        var ret = new Texture2D(width, height);
        ret.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        ret.Apply();
        RenderTexture.active = preRT;
        RenderTexture.ReleaseTemporary(rt);
        return ret;
    }
}