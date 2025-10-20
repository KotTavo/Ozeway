using UnityEngine;
using System.Collections.Generic;

public class SlimeNavigationSystem : MonoBehaviour
{
    [Header("=== УПРОЩЕННАЯ НАВИГАЦИЯ ===")]
    public bool enableOnlyForComplexCases = true;
    public float complexCaseThreshold = 2.5f;
    public float emergencyNavigationForce = 25f;

    private SlimeCharacterController slimeController;
    private List<Rigidbody2D> stuckNodes = new List<Rigidbody2D>();

    void Start()
    {
        slimeController = GetComponent<SlimeCharacterController>();
    }

    void FixedUpdate()
    {
        if (!enableOnlyForComplexCases) return;

        CheckForComplexCases();
        HandleComplexCases();
    }

    private void CheckForComplexCases()
    {
        stuckNodes.Clear();

        foreach (var surfaceNode in slimeController.surfaceNodes)
        {
            if (surfaceNode != null && IsNodeComplexStuck(surfaceNode))
            {
                stuckNodes.Add(surfaceNode);
            }
        }
    }

    private bool IsNodeComplexStuck(Rigidbody2D node)
    {
        float distanceToCenter = Vector2.Distance(node.position, transform.position);
        bool isStuck = distanceToCenter > complexCaseThreshold &&
                      node.linearVelocity.magnitude < 0.2f &&
                      slimeController.GetCenterVelocity().magnitude > 0.5f;

        return isStuck;
    }

    private void HandleComplexCases()
    {
        if (stuckNodes.Count == 0) return;

        foreach (var node in stuckNodes)
        {
            ApplyEmergencyNavigation(node);
        }
    }

    private void ApplyEmergencyNavigation(Rigidbody2D node)
    {
        Vector2 toCenter = ((Vector2)transform.position - node.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(node.position, toCenter, 1f, slimeController.obstacleLayers);
        if (hit.collider != null)
        {
            Vector2 avoidanceDir = new Vector2(-toCenter.y, toCenter.x);
            node.AddForce(avoidanceDir * emergencyNavigationForce);
        }
        else
        {
            node.AddForce(toCenter * emergencyNavigationForce);
        }

        Debug.DrawRay(node.position, toCenter * 0.8f, Color.white);
    }

    public void ForceNavigateNode(Rigidbody2D nodeRb)
    {
        if (!stuckNodes.Contains(nodeRb))
        {
            stuckNodes.Add(nodeRb);
        }
    }
}