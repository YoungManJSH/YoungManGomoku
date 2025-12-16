using UnityEngine;
using UnityEngine.UI;

public class BackgroundImageScale : MonoBehaviour
{
    private void Start()
    {
        FitByHeight();
    }

    // 배경 이미지에 보통 사용
    // 가로 세로 스트레치 상태일 때, 가로 비율이 원본 이미지에 비해 좁다면 비율을 맞춰줌
    private void FitByHeight()
    {
        var image = GetComponent<Image>();

        if (image == null || image.sprite == null)
        {
            return;
        }
        
        Sprite sp = image.sprite;
        RectTransform rt = GetComponent<RectTransform>();
        
        float screenRatio = (float)Screen.width / Screen.height; 
        float imageRatio = (float)sp.texture.width / sp.texture.height;

        if (screenRatio > imageRatio)
        {
            return;
        }
        
        float spriteRatio = (float)sp.texture.width / sp.texture.height;
        float height = rt.rect.height;
        float width = height * spriteRatio;
        
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }
}