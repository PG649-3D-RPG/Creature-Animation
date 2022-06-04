using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using Unity.MLAgents.Sensors;
using Unity.MLAgentsExamples;
using System.Runtime;
using Unity.MLAgents;

public class MultiModelControl : MonoBehaviour{

    public NNModel walkerModel;
    public NNModel attackerModel;

    public Transform parentArena;

    private BarracudaModelRunner walkerRunner;
    private BarracudaModelRunner attackerRunner;

    JointDriveController m_JdController;

    ///////////////////////////// Walker observation stuff

    //The walking speed to try and achieve
    private float m_TargetWalkingSpeed = 4;

    public float MTargetWalkingSpeed // property
    {
        get { return m_TargetWalkingSpeed; }
        set { m_TargetWalkingSpeed = Mathf.Clamp(value, .1f, m_maxWalkingSpeed); }
    }
    public bool randomizeWalkSpeedEachEpisode = false;

    const float m_maxWalkingSpeed = 10; //The max walking speed

    [Header("Target To Walk Towards")] 
    public Transform target; //Target the agent will walk towards during training.

    [Header("Body Parts")] public Transform hips;
    public Transform chest;
    public Transform spine;
    public Transform head;
    public Transform thighL;
    public Transform shinL;
    public Transform footL;
    public Transform thighR;
    public Transform shinR;
    public Transform footR;
    public Transform armL;
    public Transform forearmL;
    public Transform handL;
    public Transform armR;
    public Transform forearmR;
    public Transform handR;

    //This will be used as a stabilized model space reference point for observations
    //Because ragdolls can move erratically during training, using a stabilized reference transform improves learning
    OrientationCubeController m_OrientationCube;

    EnvironmentParameters m_ResetParams;

    ////////////////////////////////////////////////
    

    public void Start(){
        walkerRunner = new BarracudaModelRunner(walkerModel, "action");
        attackerRunner = new BarracudaModelRunner(attackerModel, "continuous_actions");

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        m_OrientationCube = GetComponentInChildren<OrientationCubeController>();

        m_JdController = GetComponent<JointDriveController>();
        m_JdController.SetupBodyPart(hips);
        m_JdController.SetupBodyPart(chest);
        m_JdController.SetupBodyPart(spine);
        m_JdController.SetupBodyPart(head);
        m_JdController.SetupBodyPart(thighL);
        m_JdController.SetupBodyPart(shinL);
        m_JdController.SetupBodyPart(footL);
        m_JdController.SetupBodyPart(thighR);
        m_JdController.SetupBodyPart(shinR);
        m_JdController.SetupBodyPart(footR);
        m_JdController.SetupBodyPart(armL);
        m_JdController.SetupBodyPart(forearmL);
        m_JdController.SetupBodyPart(handL);
        m_JdController.SetupBodyPart(armR);
        m_JdController.SetupBodyPart(forearmR);
        m_JdController.SetupBodyPart(handR);


        Reset();

        
    }

    public void Reset(){
        // reset
        foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        //Random start rotation to help generalize
        hips.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0.0f, 360.0f), 0);

        UpdateOrientationObjects();

        //Set our goal walking speed
        MTargetWalkingSpeed =
            randomizeWalkSpeedEachEpisode ? UnityEngine.Random.Range(0.1f, m_maxWalkingSpeed) : MTargetWalkingSpeed;

        SetResetParameters();
    }

    private int updateInterval = 5;
    private int updateCount = 0;

    private float attackDistance = 3.8f;
    public void FixedUpdate(){
        if(updateCount != updateInterval){
            updateCount++;
            return;
        }

        var walkerObservation = GetWalkerObservations(true);
        Tensor walkerActions = null;
        if(walkerObservation !=null){
             walkerActions = walkerRunner.RunModel(walkerObservation);
             //Debug.Log("ran walker model");
        }

        var attackerObservation = GetAttackerObservations(true);
        Tensor attackerActions = null; 
        if(attackerObservation != null){
            attackerActions = attackerRunner.RunModel(attackerObservation);
            //Debug.Log("ran attacker model");
        }

        if(walkerActions != null && attackerActions != null) Debug.Log($"Walker Actions size: {walkerActions.shape} / Attacker Actions size: {attackerActions.shape}");

        ///
        // Actually perform some actions from the networks
        float currentDistance = Vector2.Distance( parentArena.InverseTransformPoint(m_OrientationCube.transform.position).Horizontal3dTo2d(), parentArena.InverseTransformPoint(target.position).Horizontal3dTo2d());
        if(currentDistance < attackDistance && attackerActions != null){
            Debug.Log($"Performing attacker actions / distance {currentDistance} ");
            PerformActions(attackerActions);
        }
        else if(walkerActions != null){
            Debug.Log($"Performing walker actions / distance {currentDistance}");
            PerformActions(walkerActions);
        }
        else{
            Debug.Log("Could not perform any actions since they were all null");
        }


        ///

        if(attackerActions != null) attackerActions.Dispose();
        if(walkerActions != null) walkerActions.Dispose();

        if(attackerObservation != null) attackerObservation.Dispose();
        if(walkerObservation != null) walkerObservation.Dispose();

        UpdateOrientationObjects();
        updateCount = 0;
    }

    public WalkerAgent walkerAgent;
    public Tensor GetWalkerObservations(){
        List<float> observations = new List<float>();
        foreach(float f in walkerAgent.GetObservations()){
            observations.Add(f);
        }
        //Debug.Log($"observation length:{observations.Count}");

        if(observations.Count == 243){
            return new Tensor(n: 1, h:1, w:1, c: 243, srcData: observations.ToArray());
        }
        else{
            return null;
        }
    }
    public Tensor GetWalkerObservations(bool use){       
        List<float> observations = new List<float>();

        var cubeForward = m_OrientationCube.transform.forward;

        //velocity we want to match
        var velGoal = cubeForward * MTargetWalkingSpeed;
        //ragdoll's avg vel
        var avgVel = GetAvgVelocity();

        observations.Add(Vector3.Distance(velGoal, avgVel));
        AddVectorToList(m_OrientationCube.transform.InverseTransformDirection(avgVel), observations);
        AddVectorToList(m_OrientationCube.transform.InverseTransformDirection(velGoal), observations);

        AddQuaternionToList(Quaternion.FromToRotation(hips.forward, cubeForward), observations);
        AddQuaternionToList(Quaternion.FromToRotation(head.forward, cubeForward), observations);

        AddVectorToList(m_OrientationCube.transform.InverseTransformPoint(target.transform.position.Horizontal3dTo2d().Horizontal2dTo3d(y:1)), observations);

        foreach(var bodyPart in m_JdController.bodyPartsList){
            CollectBodyPartObservation(bodyPart, observations);
        }

        Tensor observation = new Tensor(n: 1, h:1, w:1, c: 243, srcData: observations.ToArray());
        return observation;
    }

    public AttackAgent attackAgent;
    public Tensor GetAttackerObservations(){
        List<float> observations = new List<float>();
        foreach(float f in attackAgent.GetObservations()){
            observations.Add(f);
        }
        if(observations.Count == 242){
            return new Tensor(n: 1, h:1, w:1, c: 242, srcData: observations.ToArray());
        }
        else{
            return null;
        }
    }

    public Tensor GetAttackerObservations(bool use){
        List<float> observations = new List<float>();

        var cubeForward = m_OrientationCube.transform.forward;

        //ragdoll's avg vel
        var avgVel = GetAvgVelocity();

        AddVectorToList(avgVel, observations);
        AddVectorToList(m_OrientationCube.transform.InverseTransformDirection(avgVel), observations);

        AddQuaternionToList(Quaternion.FromToRotation(hips.forward, cubeForward), observations);
        AddQuaternionToList(Quaternion.FromToRotation(head.forward, cubeForward), observations);

        AddVectorToList(m_OrientationCube.transform.InverseTransformPoint(target.transform.position), observations);

        foreach(var bodyPart in m_JdController.bodyPartsList){
            CollectBodyPartObservation(bodyPart, observations);
        }

        Tensor observation = new Tensor(n: 1, h:1, w:1, c: 242, srcData: observations.ToArray());
        return observation;
    }

    private void CollectBodyPartObservation(BodyPart bp, List<float> observations){
        observations.Add(bp.groundContact.touchingGround ? 1f : 0f);

        AddVectorToList(m_OrientationCube.transform.InverseTransformDirection(bp.rb.velocity), observations);
        AddVectorToList(m_OrientationCube.transform.InverseTransformDirection(bp.rb.angularVelocity), observations);

        AddVectorToList(m_OrientationCube.transform.InverseTransformDirection(bp.rb.position - hips.position), observations);

        if (bp.rb.transform != hips && bp.rb.transform != handL && bp.rb.transform != handR)
        {
            AddQuaternionToList(bp.rb.transform.localRotation, observations);
            observations.Add(bp.currentStrength / m_JdController.maxJointForceLimit);
        }
    }

    private void AddVectorToList(Vector3 vec, List<float> list){
        list.Add(vec.x);
        list.Add(vec.y);
        list.Add(vec.z);
    }

    private void AddQuaternionToList(Quaternion quat, List<float> list){
        list.Add(quat.x);
        list.Add(quat.y);
        list.Add(quat.z);
        list.Add(quat.w);
    }

    //Returns the average velocity of all of the body parts
    //Using the velocity of the hips only has shown to result in more erratic movement from the limbs, so...
    //...using the average helps prevent this erratic movement
    Vector3 GetAvgVelocity()
    {
        Vector3 velSum = Vector3.zero;

        //ALL RBS
        int numOfRb = 0;
        foreach (var item in m_JdController.bodyPartsList)
        {
            numOfRb++;
            velSum += item.rb.velocity;
        }

        var avgVel = velSum / numOfRb;
        return avgVel;
    }

    public void PerformActions(Tensor actions){
        var bpDict = m_JdController.bodyPartsDict;
        var i = -1;

        float[] continuousActions = actions.ToReadOnlyArray();
        actions.Dispose();

        bpDict[chest].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        bpDict[spine].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);

        bpDict[thighL].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[thighR].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[shinL].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[shinR].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[footR].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        bpDict[footL].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);

        bpDict[armL].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[armR].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[forearmL].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[forearmR].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[head].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);

        //update joint strength settings
        bpDict[chest].SetJointStrength(continuousActions[++i]);
        bpDict[spine].SetJointStrength(continuousActions[++i]);
        bpDict[head].SetJointStrength(continuousActions[++i]);
        bpDict[thighL].SetJointStrength(continuousActions[++i]);
        bpDict[shinL].SetJointStrength(continuousActions[++i]);
        bpDict[footL].SetJointStrength(continuousActions[++i]);
        bpDict[thighR].SetJointStrength(continuousActions[++i]);
        bpDict[shinR].SetJointStrength(continuousActions[++i]);
        bpDict[footR].SetJointStrength(continuousActions[++i]);
        bpDict[armL].SetJointStrength(continuousActions[++i]);
        bpDict[forearmL].SetJointStrength(continuousActions[++i]);
        bpDict[armR].SetJointStrength(continuousActions[++i]);
        bpDict[forearmR].SetJointStrength(continuousActions[++i]);

    }

    private Vector3 m_WorldDirToWalk = Vector3.right;
    //Update OrientationCube and DirectionIndicator
    void UpdateOrientationObjects()
    {
        m_WorldDirToWalk = target.position - hips.position;
        m_OrientationCube.UpdateOrientation(hips, target);
        // if (m_DirectionIndicator)
        // {
        //     m_DirectionIndicator.MatchOrientation(m_OrientationCube.transform);
        // }
    }

    public void SetTorsoMass()
    {
        m_JdController.bodyPartsDict[chest].rb.mass = m_ResetParams.GetWithDefault("chest_mass", 8);
        m_JdController.bodyPartsDict[spine].rb.mass = m_ResetParams.GetWithDefault("spine_mass", 8);
        m_JdController.bodyPartsDict[hips].rb.mass = m_ResetParams.GetWithDefault("hip_mass", 8);
    }

    public void SetResetParameters()
    {
        SetTorsoMass();
    }
}