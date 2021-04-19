using Andja.Utility;
using UnityEngine;

namespace Andja.Model {

    public class MoistureGenerator {

        internal static float[,] Generate(int width, int height, float[,] heights, ThreadRandom Random, float maxProgress, ref float progress) {
            float[,] moisture = new float[width, height];
            FastNoise noise = new FastNoise(Random.Range(0, int.MaxValue));
            noise.SetFrequency(0.01f);
            noise.SetFractalGain(1);
            progress += .1f * maxProgress;
            float sum = 0;
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    moisture[x, y] = Mathf.Abs(noise.GetCubic(x, y));
                    moisture[x, y] += heights[x, y] / 2;
                    sum += moisture[x, y];
                }
            }
            float avg = sum / (height * width);
            //Debug.Log(avg);
            progress += .9f * maxProgress;
            return moisture;
        }
    }
}