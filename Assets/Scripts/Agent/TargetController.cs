using UnityEngine;
using Random = UnityEngine.Random;
using Unity.MLAgents;
using UnityEngine.Events;

    public class TargetController : MonoBehaviour
    {

        private float spawnRadius; //The radius in which a target can be randomly spawned.
        public bool respawnIfTouched; //Should the target respawn to a different position when touched


        private Vector3 m_startingPos; //the starting position of the target
        private Agent m_agentTouching; //the agent currently touching the target


        [System.Serializable]
        public class CollisionEvent : UnityEvent<Collision>
        {
            
        }

        [Header("Collision Callbacks")]
        public CollisionEvent onCollisionEnterEvent = new CollisionEvent();

        private Vector3[] targetPositions;
        public int vertexCount = 40;
        public float radius = 3.5f;
        public Transform agentTransform;

        public void OnEnable(){
            targetPositions = new Vector3[vertexCount];

            float deltaTheta = (2f * Mathf.PI)/vertexCount;
            float theta = 0f;

            for (int i = 0; i< vertexCount; i++){
                Vector3 pos = new Vector3(agentTransform.position.x + radius* Mathf.Cos(theta),transform.position.y,agentTransform.position.z +radius*Mathf.Sin(theta));
                theta +=deltaTheta;
                targetPositions[i]=pos;
            }

            if (respawnIfTouched)
            {
                //TODO change to fixed Position
                MoveTargetToRandomPosition();
            }
        }

        /*
        public CollisionEvent onCollisionStayEvent = new CollisionEvent();
        public CollisionEvent onCollisionExitEvent = new CollisionEvent();
        */
        // Start is called before the first frame update
        // void OnEnable()
        // {

        //     m_startingPos = transform.position;
        //     if (respawnIfTouched)
        //     {
        //         //TODO change to fixed Position
        //         MoveTargetToRandomPosition();
        //     }
        // }

        public void MoveTargetToRandomPosition()
        {
            //TODO Advanced: Move on circle
            int choice = Random.Range(0, 40);
            transform.position = targetPositions[choice];
        }

        private void OnCollisionEnter(Collision col)
        {
            onCollisionEnterEvent.Invoke(col);
            MoveTargetToRandomPosition();
        }
        /*
        private void OnCollisionStay(Collision col)
        {
            if (col.transform.CompareTag(nameToDetect))
            {
                onCollisionStayEvent.Invoke(col);
            }
        }

        private void OnCollisionExit(Collision col)
        {
            if (col.transform.CompareTag(nameToDetect))
            {
                onCollisionExitEvent.Invoke(col);
            }
        }*/
    }
