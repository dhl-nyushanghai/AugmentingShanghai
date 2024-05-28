using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

[Serializable]
public class CustomEvent
{
    [Space(7)]
    public UnityEvent unityEvent;
    [Space(7)]
    public float interval;
    
    public CustomEvent(UnityEvent unityEvent, float interval)
    {
        this.unityEvent = unityEvent;
        this.interval = interval;
    }
}

public class SequentialPlayManager : MonoBehaviour
{
    public List<CustomEvent> events = new List<CustomEvent>();
    private Coroutine _eventCoroutine;
    private bool _isPaused = false;
    private int _currentEventIndex = 0;

    // 开始播放事件
    public void StartEvents()
    {
        if (_eventCoroutine == null)
        {
            _eventCoroutine = StartCoroutine(PlayEvents());
        }
    }

    // 暂停事件
    public void PauseEvents()
    {
        _isPaused = true;
    }

    // 恢复事件
    public void ResumeEvents()
    {
        _isPaused = false;
    }

    // 播放事件的协程
    private IEnumerator PlayEvents()
    {
        while (_currentEventIndex < events.Count)
        {
            if (!_isPaused)
            {
                CustomEvent currentEvent = events[_currentEventIndex];
                currentEvent.unityEvent.Invoke();
                yield return new WaitForSeconds(currentEvent.interval);
                _currentEventIndex++; //TODO:　change to Queue
            }
            else
            {
                yield return null; // 等待下一帧，直到取消暂停
            }
        }

        // 重置
        _eventCoroutine = null;
        _currentEventIndex = 0;
    }
}