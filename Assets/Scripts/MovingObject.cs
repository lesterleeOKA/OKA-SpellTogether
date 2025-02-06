using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class MovingObject : MonoBehaviour
{
    public int sortLayer = 0;
    public Canvas canvas;
    public Texture[] objectTextures;
    public float startPosX;
    public RawImage objectImage; // Reference to the RawImage UI element
    public float minSpeed = 1f; // Minimum speed
    public float maxSpeed = 5f; // Maximum speed
    private RectTransform rectTransform = null;
    private Tween currentTween = null;
    // Start is called before the first frame update
    void Start()
    {
        if (this.objectImage == null) this.objectImage = this.GetComponent<RawImage>();
        if (this.rectTransform == null) this.rectTransform = this.GetComponent<RectTransform>();
    }

    public enum MovingDirection
    {
        None,
        Left,
        Right
    }

    public int SortLayer
    {
        set { 
            this.sortLayer = value;
            if (this.canvas != null)
            {
                this.canvas.sortingOrder = this.sortLayer;
            }
        }
        get
        {
            return this.sortLayer;
        }
    }

    public void StartNewMovement(int roadId=-1)
    {
        if(GameController.Instance.playing) { 
            if(this.objectImage != null && roadId > -1)
            {
                this.objectImage.texture = this.objectTextures[roadId];
            }
            else
            {
                this.objectImage.texture = this.randomObjectTex;
            }
            // Randomize speed
            this.minSpeed = (LoaderConfig.Instance.gameSetup.objectAverageSpeed * 2.5f) - 1f;
            this.maxSpeed = (LoaderConfig.Instance.gameSetup.objectAverageSpeed * 2.5f) + 1f;
            float speed = UnityEngine.Random.Range(minSpeed, maxSpeed);

            // Determine the target position
            Vector2 targetPosition = Vector2.zero;
            this.rectTransform.anchoredPosition = new Vector2(this.startPosX, this.rectTransform.anchoredPosition.y);
            targetPosition = new Vector2(-(this.startPosX), this.rectTransform.anchoredPosition.y);
            // Use DOTween to move the car
            this.currentTween = this.rectTransform.DOAnchorPos(targetPosition, speed).SetEase(Ease.Linear).OnComplete(()=> this.StartNewMovement());
        }
        else
        {
            this.StopMovement();
        }
    }

    public void StopMovement()
    {
        // Stop the current tween
        if (this.currentTween != null)
        {
            this.currentTween.Kill();
            this.currentTween = null;
        }
    }


    public Texture randomObjectTex
    {
        get
        {
            if (this.objectTextures != null && this.objectTextures.Length > 0)
            {
                int randomId =UnityEngine.Random.Range(0, this.objectTextures.Length);
                return this.objectTextures[randomId];
            }
            else return null;
        }   
    }

}
