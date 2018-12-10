using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Visualizer : MonoBehaviour
{
    protected AudioHandler AudioHandler;

    public virtual void Start()
    {
        AudioHandler = AudioHandler.Instance;
        AudioHandler.BeatNotifier += OnBeat;
    }

    public abstract void OnBeat();
}
