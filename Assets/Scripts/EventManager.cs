using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class CustomEvent
{
    public UnityEvent unityEvent;
    public float interval;

    public CustomEvent(UnityEvent unityEvent, float interval)
    {
        this.unityEvent = unityEvent;
        this.interval = interval;
    }
}

public class EventManager : MonoBehaviour
{
    public List<CustomEvent> events = new List<CustomEvent>();
    private Coroutine eventCoroutine;
    private bool isPaused = false;
    private int currentEventIndex = 0;

    // 开始播放事件
    public void StartEvents()
    {
        if (eventCoroutine == null)
        {
            eventCoroutine = StartCoroutine(PlayEvents());
        }
    }

    // 暂停事件
    public void PauseEvents()
    {
        isPaused = true;
    }

    // 恢复事件
    public void ResumeEvents()
    {
        isPaused = false;
    }

    // 播放事件的协程
    private IEnumerator PlayEvents()
    {
        while (currentEventIndex < events.Count)
        {
            if (!isPaused)
            {
                CustomEvent currentEvent = events[currentEventIndex];
                currentEvent.unityEvent.Invoke();
                yield return new WaitForSeconds(currentEvent.interval);
                currentEventIndex++;
            }
            else
            {
                yield return null; // 等待下一帧，直到取消暂停
            }
        }

        // 重置
        eventCoroutine = null;
        currentEventIndex = 0;
    }
}