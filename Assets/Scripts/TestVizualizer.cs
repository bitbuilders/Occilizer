using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestVizualizer : Visualizer
{
    [SerializeField] GameObject m_cube = null;
    [SerializeField] [Range(0.0f, 10.0f)] float m_radius = 3.0f;
    [SerializeField] bool m_useBuffer;
    [SerializeField] [Range(0.0f, 20.0f)] float m_maxCubeScale = 5.0f;

    List<GameObject> m_cubes;

    public override void Start()
    {
        base.Start();
        m_cubes = new List<GameObject>();
        CreateCubes();
    }

    void CreateCubes()
    {
        m_cubes.Clear();
        int size = (int)AudioHandler.FrequencyBandSize;
        float rotationAmount = 360.0f / size;
        for (int i = 0; i < size; i++)
        {
            GameObject go = Instantiate(m_cube, transform);
            go.transform.localPosition = Vector3.zero;
            go.name = "Visualizer Cube_" + i;
            go.transform.localEulerAngles = new Vector3(0.0f, rotationAmount * i, 0.0f);
            go.transform.localPosition += go.transform.forward * m_radius;
            m_cubes.Add(go);
        }
    }

    private void Update()
    {
        int size = (int)AudioHandler.FrequencyBandSize;
        for (int i = 0; i < size; i++)
        {
            GameObject cube = m_cubes[i];
            if (m_useBuffer)
                cube.transform.localScale = new Vector3(1.0f, AudioHandler.m_audioBandBuffer[i] * m_maxCubeScale, 1.0f);
            else
                cube.transform.localScale = new Vector3(1.0f, AudioHandler.m_audioBand[i] * m_maxCubeScale, 1.0f);
        }
    }

    public override void OnBeat()
    {
        print("beat");
    }
}
