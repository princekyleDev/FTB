using UnityEngine;

public class DayNightController : MonoBehaviour
{
    [Header("Day Objects")]
    [SerializeField] private GameObject[] dayObjects;

    [Header("Night Objects")]
    [SerializeField] private GameObject[] nightObjects;

    public void SetDay()
    {
        SetObjectsActive(dayObjects, true);
        SetObjectsActive(nightObjects, false);
    }

    public void SetNight()
    {
        SetObjectsActive(dayObjects, false);
        SetObjectsActive(nightObjects, true);
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }

    [ContextMenu("Test Day")]
    private void TestDay()
    {
        SetDay();
    }

    [ContextMenu("Test Night")]
    private void TestNight()
    {
        SetNight();
    }
}
