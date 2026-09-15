using UnityEngine;
using UnityEngine.UI;

public class S05_MyMeshRenderer : MonoBehaviour
{
    [SerializeField] private int canvasWidth = 256;
    [SerializeField] private int canvasHeight = 256;
    [SerializeField] private Color bgColor = new Color(0f,0f,0f,1f);

    [Header("무늬 실습(줄무늬,체스판 공용)")]
    [SerializeField] private int patternSize = 16;
    [SerializeField] private Color colorA = new Color(1f,1f,1f,1f);
    [SerializeField] private Color colorB = new Color(0.3f,0.5f,0.8f,1f);

    private Texture2D canvasTexture;
    private RawImage targetImage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        targetImage = GetComponent<RawImage>();

        // 1. 빈 텍스처(Texture2D) 생성 — 아직 아무 색도 채워지지 않은 상태
        canvasTexture = new Texture2D(canvasWidth, canvasHeight);

        // 2. 픽셀 경계를 흐리지 않게(확대해도 네모난 픽셀 그대로 보이도록)
        canvasTexture.filterMode = FilterMode.Point;

        // 3. 픽셀 채우기 (실습 단계에 따라 아래 호출을 교체)
        //FillBackground(bgColor);
        //FillRandom();
        //FillVerticalStripes(patternSize, colorA, colorB);
        FillCheckerboard(patternSize, colorA, colorB);

        // 4. 지금까지의 SetPixel 변경 사항을 실제로 텍스처에 반영
        canvasTexture.Apply();

        // 5. 완성된 텍스처를 화면의 RawImage에 연결
        targetImage.texture = canvasTexture;

    }
    private void FillBackground(Color color)
    {
        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                canvasTexture.SetPixel(x, y, color);
            }
        }
    }
    private void FillRandom()
    {
        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                Color randomColor = new Color(Random.value, Random.value, Random.value, 1f);
                canvasTexture.SetPixel(x, y, randomColor);
            }
        }
    }
    private void FillVerticalStripes(int width, Color colorA, Color colorB)
    {
        for (int x = 0; x < canvasWidth; x++)
        {
            // TODO: x를 width로 나눈 몫이 짝수면 colorA, 홀수면 colorB가 되도록
            // isColorA를 올바른 조건식으로 바꾸세요.
            // 힌트: (x / width) % 2 == 0
            bool isColorA;
            if ((x/width)%2==0)
            {
                isColorA = true;
            }
            else
            {
                isColorA = false;
            }
            Color stripeColor = isColorA ? colorA : colorB;
            for (int y = 0; y < canvasHeight; y++)
                canvasTexture.SetPixel(x, y, stripeColor);
        }
    }
    private void FillCheckerboard(int size, Color colorA, Color colorB)
    {
        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                // TODO: 줄무늬는 x만 봤지만, 체스판은 x와 y를 함께 고려해야 합니다.
                // 힌트: (x / size) + (y / size) 의 결과를 활용해보세요.
                bool isColorA;
                if (((x/size)+(y/size))%2==0)
                {
                    isColorA = true;
                }
                else
                {
                    isColorA = false;
                }
                Color checkerboardColor = isColorA ? colorA : colorB;
                // 여기에 SetPixel 호출까지 직접 작성하세요.
                canvasTexture.SetPixel(x, y, checkerboardColor);
            }
        }
    }


}
