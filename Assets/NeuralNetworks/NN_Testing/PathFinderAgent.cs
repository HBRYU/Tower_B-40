using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeuralNetworks.NN_Testing;
using Unity.Mathematics;
using UnityEngine;

public class PathFinderAgent : MonoBehaviour
{
    public NeuralNetwork brain;
    public Agent agent;
    public bool enableEvolution=true;
    private AgentInterface agentInterface;
    public float baseSpeed;

    public float computeClock;
    private float computeClockTimer;

    public Vector3 targetPos;
    public Transform targetTransform;
    private Rigidbody2D rb;
    private MotherNature motherNature;

    public LayerMask wallLayer;
    public float sensorRange;
    public int rayCount;
    public int residualCount;
    private float[] residual;
    public int historyCount = 20;
    public float historyInterval = 0.5f;
    private float historyIntervalTimer;

    private Queue<Vector2> dispositionHistory;
    private Vector3 initialPos;
    private Queue<float[]> sensorHistory;

    public float penalty;
    public float bonusReward;
    public float timeRewardMultiplier;

    private bool targetVisible;

    public float[] inputVector;
    public float[] outputVector;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        dispositionHistory  = new Queue<Vector2>(historyCount);
        sensorHistory = new Queue<float[]>(historyCount);
        for (int i = 0; i < historyCount; i++)
        {
            dispositionHistory.Enqueue(new Vector2(0f, 0f));
            sensorHistory.Enqueue(new float[rayCount]);
        }
        agentInterface = GetComponent<AgentInterface>();

        computeClockTimer = computeClock;
        historyIntervalTimer = historyInterval;
        initialPos = transform.position;
        residual = new float[residualCount];
        
        if (agentInterface.modelReceived)
        {
            brain = agentInterface.receivedModel;
            if(agentInterface.mutate)
                brain.Mutate(0.15f * agentInterface.mutationScale, 0.1f * agentInterface.mutationScale, 0.5f,
                    1/32f * agentInterface.reshuffleChanceScale, 1.5f, 1.5f, dropoutRate:0f);
        }
        else
        {
            // Construct Model
            
            // 1. Megabrain: perhaps more adequate for general pathfinding (randomized maps)
            /*
            // INPUT: 
            // disposition(2), deltax+y to target(2), ray data(rayCount), dispositionHistory(hc*2), sensorHistory(rayCount*hc), targetVisible(1), residual(rc)
            brain = new NeuralNetwork("Billy");
            brain.AddLayer(2+2+rayCount+historyCount*(2 + rayCount)+1+residualCount, NeuralNetwork.ActivationFunction.Linear);
            brain.AddLayer(128, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(64, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(32, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(32, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(16, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(16, NeuralNetwork.ActivationFunction.Sigmoid);
            // OUTPUT:
            // xy velocity(2), residual(rc)
            brain.AddLayer(2 + residualCount, NeuralNetwork.ActivationFunction.Sigmoid);
            brain.Compile(neuronDropoutRate:0.1f, weightDropoutRate:0.1f);
            */
            
            // 2. Simpleton: simple architecture with only raycasts and distance + direction to target + visibility
            brain = new NeuralNetwork("Simpleton");
            brain.AddLayer(rayCount + 2 + 1, NeuralNetwork.ActivationFunction.Linear);
            brain.AddLayer(32, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(32, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(16, NeuralNetwork.ActivationFunction.Sigmoid);
            brain.AddLayer(2, NeuralNetwork.ActivationFunction.Sigmoid);
            brain.Compile(neuronDropoutRate:0.1f, weightDropoutRate:0f);
        }

        // inputVector = new float[2 + 2 + rayCount + historyCount * (2 + rayCount) + 1 + residualCount];
        // outputVector = new float[2 + residualCount];
        inputVector = new float[rayCount + 2 + 1];
        outputVector = new float[2];

        agent = new Agent(gameObject, 0f, brain);
        
        motherNature = GameObject.FindGameObjectWithTag("GM").GetComponent<MotherNature>();
        if(enableEvolution)
            motherNature.agents.Add(agent);

        targetTransform = GameObject.FindGameObjectWithTag("Target").transform;
        targetPos = targetTransform.position;
    }
    
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        computeClockTimer -= dt;
        historyIntervalTimer -= dt;
        
        var position = transform.position;
        targetVisible = !Physics2D.Raycast(position, targetPos - position, 
            Vector3.Distance(position, targetPos), wallLayer);
        
        if (computeClockTimer <= 0f)
        {
            computeClockTimer = computeClock;
            HandleComputation();
        }

        agent.reward = FitnessFunction();
    }

    void HandleComputation()
    {
        
        /*float[] CalculateInputVector()
        {
            List<float> inputList = new List<float>();
            // 1. Disposition
            var position = transform.position;
            Vector3 disposition = (position - initialPos);
            inputList.Add(disposition.x);
            inputList.Add(disposition.y);
            
            // 2. Delta to target
            inputList.Add((targetPos - position).x);
            inputList.Add((targetPos - position).y);
            
            // 3. Sensor rays
            float theta = 0f;
            float[] distances = new float[rayCount];
            for (int i = 0; i < rayCount; i++)
            {
                Vector2 dir = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
                RaycastHit2D col = Physics2D.Raycast(transform.position, dir, sensorRange, wallLayer);

                if (col.collider != null)
                    distances[i] = Vector3.Distance(transform.position, col.point);
                else
                    distances[i] = -1f;
            
                theta += 2 * Mathf.PI / rayCount;
                inputList.Add(distances[i]);
            }
            
            // 4. Disposition history, 5. Sensor history
            if (historyIntervalTimer <= 0f)
            {
                // print("History updated");
                historyIntervalTimer = historyInterval;
                
                // 4. Disposition history
                dispositionHistory.Enqueue(disposition);
                dispositionHistory.Dequeue();
                // print("Disposition:" + dispositionHistory.Count);
                
                    // 5. Sensor history
                sensorHistory.Enqueue(distances);
                sensorHistory.Dequeue();
                // print("Sensor:" + sensorHistory.Count);
                
            }
            
            foreach (var thisDisposition in dispositionHistory)
            {
                inputList.Add(thisDisposition.x);
                inputList.Add(thisDisposition.y);
            }
            
            foreach (var thisSensorData in sensorHistory)
            {
                foreach (float d in thisSensorData)
                {
                    inputList.Add(d);
                }
            }
            
            // 6. Target visible
            inputList.Add(targetVisible ? 1f : 0f);
            
            // 7. Residual signals
            foreach (var res in residual)
            {
                inputList.Add(res);
            }
            
            return inputList.ToArray();
        }*/

        float[] CalculateInputVectorSimpleton()
        {
            List<float> inputList = new List<float>();
            var position = transform.position;
            var distance = Vector3.Distance(position, targetPos);
            
            // 1. Distance
            inputList.Add(distance);
            
            // 2. Direction
            inputList.Add(Mathf.Atan2(targetPos.y - position.y,targetPos.x - position.x));
            
            // 2. Sensor rays
            float theta = 0f;
            float[] distances = new float[rayCount];
            for (int i = 0; i < rayCount; i++)
            {
                Vector2 dir = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
                RaycastHit2D col = Physics2D.Raycast(transform.position, dir, sensorRange, wallLayer);

                if (col.collider != null)
                    distances[i] = Vector3.Distance(transform.position, col.point);
                else
                    distances[i] = sensorRange + 2f;
            
                theta += 2 * Mathf.PI / rayCount;
                inputList.Add(distances[i]);
            }
            
            // 6. Target visible
            inputList.Add(targetVisible ? 1f : 0f);
            
            return inputList.ToArray();
        }

        // inputVector = CalculateInputVector();
        inputVector = CalculateInputVectorSimpleton();
        outputVector = brain.Compute(inputVector);
        
        rb.velocity = new Vector2(outputVector[0] - 0.5f,outputVector[1] - 0.5f) * (2 * baseSpeed);
        /*for (int i = 0; i < residualCount; i++)
        {
            residual[i] = outputVector[i + 2];
        }*/
    }

    private bool targetReachedFlag;
    float FitnessFunction()
    {
        float reward;
        var position = transform.position;
        float sqrDist = (targetPos - position).sqrMagnitude;
        reward = -sqrDist;
        reward *= targetVisible ? 0.5f : 1f;

        if (!targetReachedFlag && sqrDist <= 1f)
        {
            // bonusReward += motherNature.genocideClockTimer * timeRewardMultiplier;
            bonusReward += 50f;
            targetReachedFlag = true;
        }

        if (targetPos != targetTransform.position)
        {
            targetReachedFlag = false;
            targetPos = targetTransform.position;
            bonusReward += reward;
        }
        
        
        return reward + bonusReward - penalty;
    }

    bool IsLayerInMask(GameObject obj, LayerMask mask) 
    {
        return (mask.value & (1 << obj.layer)) > 0;
    }
    
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (IsLayerInMask(other.gameObject, wallLayer))
        {
            penalty += 0.2f;
        }
    }
    
    private void OnCollisionStay2D(Collision2D other)
    {
        if (IsLayerInMask(other.gameObject, wallLayer))
        {
            penalty += 0.1f*Time.deltaTime;
        }
    }
}
