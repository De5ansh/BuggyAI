using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : Agent
{
    [Header("Movement")]
    public float spd = 10f;
    public float turnSpeed = 180f;

    [Header("Camera Fall")]
    public Transform cam;
    public float camFallAcceleration = 20f;
    public float camTiltSpeed = 40f;

    [Header("Episode")]
    public float maxEpisodeTime = 180f;
    public Transform spawnPoint;

    [Header("Rewards")]
    public float newCellReward = 1.0f;
    public float revisitPenalty = -0.003f;
    public float movementReward = 0.02f;
    public float stepPenalty = -0.001f;
    public float turnPenalty = -0.001f;
    public float turnInPlacePenalty = -0.05f;
    public float stuckPenalty = -2.0f;
    public float pitPenalty = -3.0f;

    [Header("Stuck Detection")]
    public float stuckDistanceThreshold = 0.05f;
    public float stuckTimeThreshold = 2.0f;

    [Header("Coverage")]
    public GridCoverageTracker coverageTracker;

    private CharacterController charController;

    private bool isFalling = false;
    private float camFallSpeed = 0f;
    private float episodeTimer = 0f;
    private float stuckTimer = 0f;

    private float respawnGraceTimer = 0f;
    public float respawnGraceDuration = 0.5f;

    private Vector3 startCamLocalPos;
    private Quaternion startCamLocalRot;

    private Vector3 lastPosition;
    private float currentMoveIntent = 0f;
    private float currentTurnIntent = 0f;

    void Start()
    {
        Debug.Log("Sensors count: " +
GetComponents<Unity.MLAgents.Sensors.ISensor>().Length);
    }

    public override void Initialize()
    {
        Debug.Log("AGENT SCRIPT RUNNING");
        charController = GetComponent<CharacterController>();

        if (coverageTracker == null)
            coverageTracker = FindObjectOfType<GridCoverageTracker>();

        if (coverageTracker == null)
            Debug.LogError("PlayerMovement requires a GridCoverageTracker in the scene.");

        if (cam != null)
        {
            startCamLocalPos = cam.localPosition;
            startCamLocalRot = cam.localRotation;
        }

        lastPosition = transform.position;
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        // Do nothing -> disables masking completely
    }

    public void CheckWallProximity()
    {
        float rayDistance = 3f;

        // Forward ray
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, rayDistance))
        {
            if (hit.collider.CompareTag("Wall"))
            {
                float dist = hit.distance;

                // closer → bigger penalty
                float penalty = (rayDistance - dist) / rayDistance;
                AddReward(-0.01f * penalty);
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        CancelInvoke();

        episodeTimer = 0f;
        stuckTimer = 0f;
        isFalling = false;
        camFallSpeed = 0f;
        currentMoveIntent = 0f;
        currentTurnIntent = 0f;
        respawnGraceTimer = respawnGraceDuration;

        charController.enabled = false;

        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }
        else
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }

        charController.enabled = true;

        if (cam != null)
        {
            cam.localPosition = startCamLocalPos;
            cam.localRotation = startCamLocalRot;
        }

        if (coverageTracker != null)
        {
            coverageTracker.ResetEpisodeCoverage();
            coverageTracker.RegisterAgentPosition(transform.position);
        }

        lastPosition = transform.position;

        Debug.Log($"[EPISODE] Reset | Spawn Pos: {transform.position}");

        if (coverageTracker != null)
            coverageTracker.SaveCoverageToFile();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 pos = transform.position;

        if (coverageTracker == null)
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(isFalling ? 1f : 0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(1f);
            return;
        }

        float x = Mathf.InverseLerp(coverageTracker.worldMin.x,
coverageTracker.worldMax.x, pos.x);
        float z = Mathf.InverseLerp(coverageTracker.worldMin.z,
coverageTracker.worldMax.z, pos.z);

        x = x * 2f - 1f;
        z = z * 2f - 1f;

        float falling = isFalling ? 1f : 0f;
        Vector3 forward = transform.forward;

        Vector2 norm = coverageTracker.GetNormalizedPosition(pos);

        sensor.AddObservation(x);
        sensor.AddObservation(z);
        sensor.AddObservation(falling);
        sensor.AddObservation(norm.x);
        sensor.AddObservation(norm.y);
        sensor.AddObservation(forward.x);
        sensor.AddObservation(forward.z);

        Debug.Log($"OBS: {x}, {z}, {falling}, {norm.x}, {norm.y},{forward.x}, {forward.z}");
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (isFalling) return;

        float move = actions.ContinuousActions[0];
        float turn = actions.ContinuousActions[1];

        if (Mathf.Abs(turn) < 0.1f)
            turn = 0f;

        currentMoveIntent = move;
        currentTurnIntent = turn;

        transform.Rotate(Vector3.up * turn * turnSpeed * Time.deltaTime);

        Vector3 movement = transform.forward * move;
        movement.y = 0f;

        Vector3 before = transform.position;
        charController.Move(movement * spd * Time.deltaTime);
        Vector3 after = transform.position;

        float dist = Vector3.Distance(before, after);

        AddReward(stepPenalty);
        AddReward(Mathf.Abs(turn) * turnPenalty);

        if (Mathf.Abs(move) > 0.1f && dist > stuckDistanceThreshold)
        {
            AddReward(movementReward);
        }
        else if (Mathf.Abs(move) > 0.1f && dist < stuckDistanceThreshold)
        {
            AddReward(-0.01f);
        }

        if (Mathf.Abs(turn) > 0.3f && Mathf.Abs(move) < 0.1f)
        {
            AddReward(turnInPlacePenalty);
        }
        else if (Mathf.Abs(turn) > 0.5f && dist < 0.01f)
        {
            AddReward(-0.005f);
        }

        if (coverageTracker != null)
        {
            Vector2Int cell = coverageTracker.WorldToCell(transform.position);
            bool isNew =
coverageTracker.RegisterAgentPosition(transform.position);

            if (isNew)
            {
                AddReward(newCellReward);
                Debug.Log($"[COVERAGE] New Cell: {cell}");
            }
            else
            {
                AddReward(revisitPenalty);
            }
        }

        DetectStuck(dist);
        lastPosition = transform.position;
        CheckWallProximity();
    }

    private void Update()
    {
        if (isFalling)
        {
            if (cam == null)
            {
                EndEpisode();
                return;
            }

            camFallSpeed += camFallAcceleration * Time.deltaTime;
            cam.position += Vector3.down * camFallSpeed * Time.deltaTime;
            cam.Rotate(Vector3.forward * camTiltSpeed * Time.deltaTime);
            return;
        }

        episodeTimer += Time.deltaTime;

        if (respawnGraceTimer > 0f)
            respawnGraceTimer -= Time.deltaTime;

        if (episodeTimer >= maxEpisodeTime)
        {
            Debug.Log($"[EPISODE] Timeout | Pos: {transform.position}");
            EndEpisode();
        }
    }

    private void DetectStuck(float dist)
    {
        if (Mathf.Abs(currentMoveIntent) > 0.01f && dist <
stuckDistanceThreshold)
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }

        if (stuckTimer >= stuckTimeThreshold)
        {
            Debug.Log($"[BUG] StuckSpot | Pos: {transform.position}");
            if (coverageTracker != null)
                coverageTracker.RegisterBug(transform.position, "StuckSpot");
            AddReward(stuckPenalty);
            EndEpisode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isFalling || respawnGraceTimer > 0f) return;

        if (other.CompareTag("Pit"))
        {
            Debug.Log($"[BUG] Pit Trigger | Pos: {transform.position}");
            if (coverageTracker != null)
                coverageTracker.RegisterBug(transform.position, "PitTrap");
            AddReward(pitPenalty);

            isFalling = true;
            Invoke(nameof(EndEpisode), 1.5f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;

        c[0] = Input.GetKey(KeyCode.W) ? 1f :
               Input.GetKey(KeyCode.S) ? -1f : 0f;

        c[1] = Input.GetKey(KeyCode.D) ? 1f :
               Input.GetKey(KeyCode.A) ? -1f : 0f;
    }
}