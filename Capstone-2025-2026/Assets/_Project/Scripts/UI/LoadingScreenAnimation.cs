using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreenAnimation : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private RectTransform movingObject;
    [SerializeField] private Image loadingText;
    [SerializeField] private Sprite[] loadingSprites;

    [Header("Settings")]
    [SerializeField] private float timeBetweenLoadingText = 0.5f;

    private float targetProgress = 0f;
    private float displayedProgress = 0f;
    private float loadingTextTimer = 0f;
    private int loadingTextCounter = 0;
    private bool isLoading = false;
    private Vector2 originalPos;

    private MySceneManager sceneManager;

    private void Awake()
    {
        sceneManager = FindAnyObjectByType<MySceneManager>();
        originalPos = movingObject.anchoredPosition;
    }

    public void StartTracking(AsyncOperation operation)
    {
        if (operation == null) return;
        isLoading = true;
        targetProgress = 0f;
        displayedProgress = 0f;
        loadingTextTimer = 0f;
        loadingTextCounter = 0;
        StartCoroutine(TrackProgress(operation));
    }

    private IEnumerator TrackProgress(AsyncOperation operation)
    {
        float elapsed = 0f;
        float minimumDuration = sceneManager.loadMinimumTime;

        //real progress
        while (operation.progress < 0.9f)
        {
            elapsed += Time.deltaTime;
            float timedProgress = Mathf.Clamp01(elapsed / minimumDuration);

            //asyncOperation caps at 0.9f until allowSceneActivation = true
            //remap 0–0.9 to 0–1 for a clean 0–100% display
            targetProgress = timedProgress;
            yield return null;
        }

        //if loading is faster than the minimum duration, use that instead
        while (elapsed < minimumDuration)
        {
            elapsed += Time.deltaTime;
            targetProgress = Mathf.Clamp01(elapsed / minimumDuration);
            yield return null;
        }

        //snap target to 100% once the actual loading is done
        targetProgress = 1f;

        //notify sceneManager the animation has finished and it's safe to switch
        yield return new WaitUntil(() => displayedProgress >= 0.99f);
        isLoading = false;
        sceneManager?.SetReadyToLoad(true);
    }

    private void Update()
    {
        if (!isLoading) return;
        UpdateVisuals();
        UpdateLoadingSprite();
    }

    private void UpdateVisuals()
    {
        float smoothT = Mathf.SmoothStep(0f, 1f, targetProgress);
        displayedProgress = smoothT;
        MoveObject(displayedProgress);
    }

    private void MoveObject(float progress)
    {
        if (movingObject == null) return;

        Vector2 from = new Vector2(-Screen.width / 2f, originalPos.y);
        Vector2 to = new Vector2(Screen.width / 2f, originalPos.y);

        movingObject.anchoredPosition = Vector2.Lerp(from, to, progress);
    }

    private void UpdateLoadingSprite()
    {
        loadingTextTimer += Time.deltaTime;
        if(loadingTextTimer > timeBetweenLoadingText)
        {
            loadingTextTimer = 0;
            loadingTextCounter++;

            if(loadingTextCounter > loadingSprites.Length - 1)
            {
                loadingTextCounter = 0;
            }
            loadingText.sprite = loadingSprites[loadingTextCounter];
        }
    }
    public void ShowLoadingScreen()
    {
        gameObject.SetActive(true);
        AsyncOperation op = sceneManager.GetPendingLoad();
        if (op != null) StartTracking(op);
    }

    public void Reset()
    {
        displayedProgress = 0f;
        targetProgress = 0f;
        isLoading = false;
        MoveObject(0f);
    }
}