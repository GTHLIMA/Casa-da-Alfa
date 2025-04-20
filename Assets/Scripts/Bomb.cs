using UnityEngine;

public class Bomb : MonoBehaviour
{
    private bool isDestroyed = false;

    void Update()
    {
        if (Input.touchCount > 0 && !isDestroyed)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                Vector2 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                Collider2D hit = Physics2D.OverlapPoint(touchPosition);

                if (hit != null)
                {

                    if (hit.transform == transform)
                    {
                        Debug.Log("TOCOU NA BOMBA!");

                        Vibrar();

                        isDestroyed = true;
                        Destroy(gameObject);
                    }
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ground") && !isDestroyed)
        {
            isDestroyed = true;
            Destroy(gameObject);
        }
    }

    void Vibrar()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext"))
            using (AndroidJavaObject vibrator = context.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (vibrator != null)
                {
                    // Verifica se está em Android 26+ para usar VibrationEffect
                    AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION");
                    int sdkInt = version.GetStatic<int>("SDK_INT");

                    if (sdkInt >= 26)
                    {
                        AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                        AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                            "createOneShot", 100, vibrationEffectClass.GetStatic<int>("DEFAULT_AMPLITUDE"));
                        vibrator.Call("vibrate", effect);
                    }
                    else
                    {
                        vibrator.Call("vibrate", 100);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("Erro ao vibrar: " + e.Message);
        }
#else
        Handheld.Vibrate();
#endif
    }
}
