using UnityEngine;

public class HandBehavior : MonoBehaviour
{
    private GameObject hand;

    private GameObject pauseMenuPanel;
    private GameObject endPhasePanel;

    private float inactivityTimer = 0f;
    private float maxInactivity = 1f;

    void Start()
    {
        hand = FindIncludingInactive("Animation");
        pauseMenuPanel = FindIncludingInactive("PauseMenu");
        endPhasePanel = FindIncludingInactive("EndPhase");
    }

    void Update()
    {
        bool isTouching = false;


        if (Input.GetMouseButtonDown(0))
        {
            DeactivateHand();
            isTouching = true;
        }

        if ((pauseMenuPanel != null && pauseMenuPanel.activeInHierarchy) ||
            (endPhasePanel != null && endPhasePanel.activeInHierarchy))
        {
            return;
        }

        if (!isTouching)
        {
            inactivityTimer += Time.unscaledDeltaTime;

            if (inactivityTimer >= maxInactivity)
            {
                ActivateHand();
                inactivityTimer = 0f;
            }
        }
    }

    private void DeactivateHand()
    {
        if (hand == null) hand = FindIncludingInactive("Animation");
        if (hand != null)
        {
            hand.SetActive(false);
        }
        inactivityTimer = 0f;
    }

    private void ActivateHand()
    {
        if (hand == null) hand = FindIncludingInactive("Animation");
        if (hand != null)
        {
            hand.SetActive(true);
        }
    }

    private GameObject FindIncludingInactive(string name)
    {
        var objs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in objs)
        {
            if (obj.name == name)
                return obj;
        }
        return null;
    }
}
