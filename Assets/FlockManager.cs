using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class FlockManager : MonoBehaviour
{
    [SerializeField] private int m_BoidNumber;
    [SerializeField] private GameObject m_BoidPrefab;
    [SerializeField] private float m_FlockRadius;

    [SerializeField][Range(0f, 1f)] private float CohesionRuleWeight;
    [SerializeField][Range(0f, 1f)] private float AlignmentRuleWeight;
    [SerializeField][Range(0f, 1f)] private float SeperationRuleWeight;
    [SerializeField][Range(0f, 1f)] private float BorderAvoidanceRuleWeight;

    private Boids[] m_Boids;

    private Vector3 m_AverageHeading;
    private Vector3 m_CenterOfMass;

    private void Awake()
    {
        m_Boids = new Boids[m_BoidNumber];
    }

    void Start()
    {
        for (int i = 0; i < m_BoidNumber; i++)
        {
            var dposition = Random.insideUnitSphere * m_FlockRadius;
            var drotation = Random.insideUnitSphere * 20;
            var boid = Instantiate(m_BoidPrefab, dposition, Quaternion.Euler(drotation), transform).GetComponent<Boids>();
            boid.SetBelongingFlock(this);
            boid.SetWeights(SeperationRuleWeight, CohesionRuleWeight, AlignmentRuleWeight, BorderAvoidanceRuleWeight);
            boid.SetFlockOrigin(transform.position);
            boid.SetFlockRadius(m_FlockRadius);

            m_Boids[i] = boid;
        }
    }

    // Update is called once per frame
    void Update()
    {
        var sumBoidPosition = Vector3.zero;
        var sumHeading = Vector3.zero;

        foreach (var boid in m_Boids)
        {
            var dtransform = boid.transform;
            sumBoidPosition += dtransform.position;
            sumHeading += dtransform.forward.normalized;
        }

        m_CenterOfMass = sumBoidPosition / m_BoidNumber;
        m_AverageHeading = sumHeading / m_BoidNumber;
    }

    public Vector3 GetAverageHeading()
    {
        return m_AverageHeading;
    }

    public Vector3 GetCenterofMass()
    {
        return m_CenterOfMass;
    }
}