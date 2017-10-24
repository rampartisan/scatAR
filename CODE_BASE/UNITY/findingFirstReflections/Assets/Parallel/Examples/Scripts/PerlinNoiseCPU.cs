using UnityEngine;

public class PerlinNoiseCPU : MonoBehaviour
{
    public int widthResolution = 1024;
    public int heightResolution = 1024;
    public float scale = 20f;
    public bool isParallel = true;

    void Start ()
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var texture = new Texture2D(widthResolution, heightResolution, TextureFormat.ARGB32, false);
        Color[] noise = new Color[widthResolution * heightResolution];
        FillNoise(noise);
        FillTexture(texture, noise);
        Debug.Log(string.Format("PerlinNoiseCPU ({0}) took {1}ms", isParallel ? "Parallel" : "Serial", stopwatch.ElapsedMilliseconds));
    }

    private void FillNoise(Color[] noise)
    {        
        if(isParallel)
        {
            FillNoise_Parallel(noise);
        }
        else
        {
            FillNoise_Serial(noise);
        }
    }

    private void FillNoise_Parallel(Color[] noise)
    {
        Parallel.For(0, widthResolution, (x) =>
        {
				print(x);
            for (int y = 0; y < heightResolution; y++)
            {
                noise[x * heightResolution + y] = GetNoiseAt(x, y);
            }
        });
    }

    private void FillNoise_Serial(Color[] noise)
    {
        for (int x = 0; x < widthResolution; x++)
        {
            for (int y = 0; y < heightResolution; y++)
            {
                noise[x * heightResolution + y] = GetNoiseAt(x, y);
            }
        }
    }

    private Color GetNoiseAt(int x, int y)
    {
        float noiseVal = Mathf.PerlinNoise((x / (float)widthResolution) * scale, (y / (float)heightResolution) * scale);
        return new Color(noiseVal, noiseVal, noiseVal);
    }

    private void FillTexture(Texture2D texture, Color[] noise)
    {
        //TODO: Compare time with/without parallel!
        texture.SetPixels(noise);

        // Apply all SetPixel calls
        texture.Apply();

        // connect texture to material of GameObject this script is attached to
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.mainTexture = texture;
        }
    }
}
