using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class Avoider : MonoBehaviour
{
    private NavMeshAgent agent;
    private PoissonDiscSampler poissonDiscSampler;

    [SerializeField]
    [Tooltip("Object the avoider will avoid.")]
    private GameObject avoidee;
    [SerializeField]
    [Tooltip("The avoider will avoid the avoidee if it is closer than this distance from it.")]
    private float avoidRange;
    [SerializeField]
    [Tooltip("Speed the avoider will run away from the avoidee.")]
    private float runAwaySpeed;
    [SerializeField]
    [Tooltip("Show debug rays of avoider sight.")]
    private bool visualizeSightRays;

    private float minDistanceBetweenPoints = 0.1f;

    private int tickRate = 2; // ticks per second
    private float tickTimer;

    private Vector3 pointToMoveTo;
    private bool moving;

    private IEnumerable<Vector2> points;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = runAwaySpeed;

        poissonDiscSampler = new PoissonDiscSampler(avoidRange, avoidRange, minDistanceBetweenPoints);
        points = poissonDiscSampler.Samples();

        tickTimer = 60f / tickRate;
    }

    void OnValidate()
    {
        if (!TryGetComponent<NavMeshAgent>(out agent))
        {
            Debug.LogWarning("Avoider requires NavMeshAgent on GameObject. Please add a NavMeshAgent to " + name + " and bake a NavMeshSurface.", this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // only check on tick timer
        if (CheckTick())
        {
            if (IsAvoideeInRange())
            {
                // move if visible to avoidee
                if (IsVisibleByAvoidee())
                {
                    Debug.Log("is visible");
                    // check if valid point is available
                    if (TryGetClosestNonVisiblePoint(GetPointsFromPoissonDisc(), out Vector3 closestViablePoint))
                    {
                        pointToMoveTo = closestViablePoint;
                        agent.SetDestination(pointToMoveTo);
                        Debug.Log("move!");
                        //moving = true;
                    }
                }
            }
            
        }

        /*// move away
        if (moving)
        {
            MoveTowardsViablePoint();
        }
        */
        UpdateTickTimer();
    }

    private bool IsVisibleByAvoidee()
    {
        return IsPointVisibleByPlayer(transform.position, gameObject);
    }

    private Vector3[] GetPointsFromPoissonDisc()
    {
        List<Vector3> localConvertedPoints = new List<Vector3>();

        // get initial set of points
        foreach (var point in points)
        {
            Vector3 centeredPoint = new Vector3(point.x, 0, point.y);

            // center point around 0
            centeredPoint.x -= avoidRange / 2;
            centeredPoint.z -= avoidRange / 2;

            // add to player local space
            centeredPoint += transform.position;

            // add to list
            localConvertedPoints.Add(centeredPoint);
        }

        Debug.Log(localConvertedPoints.Count);

        return localConvertedPoints.ToArray();
    }

    private void DrawDebugRayFromAvoider(Vector3 endPoint, Color color)
    {   
        Debug.DrawLine(transform.position, endPoint, color);
    }

    private bool IsPointVisibleByPlayer(Vector3 point, GameObject excludedGameObject = null)
    { 
        Vector3 distanceToAvoidee = avoidee.transform.position - transform.position;

        if (Physics.Raycast(avoidee.transform.position, distanceToAvoidee.normalized, out RaycastHit hitInfo))
        {
            if (excludedGameObject != null && hitInfo.collider.gameObject == gameObject)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        return true;       
    }

    private bool TryGetClosestNonVisiblePoint(Vector3[] candidatePoints, out Vector3 closestViablePoint)
    {
        // start furthest point at max distance from player
        closestViablePoint = transform.position + (Vector3.one * avoidRange * 2);

        bool viablePointFound = false;

        foreach (Vector3 point in candidatePoints)
        {
            Color rayColor = Color.green;

            if (!IsPointVisibleByPlayer(point))
            {
                if (GetDistanceFromAvoider(point) < GetDistanceFromAvoider(closestViablePoint))
                {
                    closestViablePoint = point;
                    viablePointFound = true;
                }
            }
            else
            {
                rayColor = Color.red;
            }

            //DrawDebugRayFromAvoider(point, rayColor);
        }

        return viablePointFound;
    }

    private float GetDistanceFromAvoider(Vector3 point)
    {
        return (point - transform.position).magnitude;
    }

    private void UpdateTickTimer()
    {
        tickTimer -= Time.deltaTime;
    }

    private bool CheckTick()
    {
        if (tickTimer <= 0)
        {
            tickTimer = 60f / tickRate;
            return true;
        }

        return false;
    }

    private void MoveTowardsViablePoint()
    {
        Vector3.Lerp(transform.position, pointToMoveTo, Time.deltaTime * runAwaySpeed);

        float stopMoveTolerance = 0.1f;
        if ((transform.position - pointToMoveTo).sqrMagnitude < 0.1f)
        {
            moving = false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        if (poissonDiscSampler == null || points == null)
        {
            poissonDiscSampler = new PoissonDiscSampler(avoidRange, avoidRange, minDistanceBetweenPoints);
            points = poissonDiscSampler.Samples();
        }
        Vector3[] pointsArray = GetPointsFromPoissonDisc();

        foreach (Vector3 point in pointsArray)
        {
            if (IsPointVisibleByPlayer(point))
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.red;
            }

            Gizmos.DrawLine(transform.position, point);
        }
    }

    private bool IsAvoideeInRange()
    {
        return ((transform.position - avoidee.transform.position).sqrMagnitude < avoidRange * avoidRange);
    }
}
