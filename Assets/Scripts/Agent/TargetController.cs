using UnityEngine;
using Random = UnityEngine.Random;
using Unity.MLAgents;
using UnityEngine.Events;

    public class TargetController : MonoBehaviour
    {

        private string nameToDetect = "hand";

        private float spawnRadius; //The radius in which a target can be randomly spawned.
        private bool respawnIfTouched; //Should the target respawn to a different position when touched


        private Vector3 m_startingPos; //the starting position of the target
        private Agent m_agentTouching; //the agent currently touching the target


        [System.Serializable]
        public class CollisionEvent : UnityEvent<Collision>
        {
            
        }

        [Header("Collision Callbacks")]
        public CollisionEvent onCollisionEnterEvent = new CollisionEvent();

        /*
        public CollisionEvent onCollisionStayEvent = new CollisionEvent();
        public CollisionEvent onCollisionExitEvent = new CollisionEvent();
        */
        // Start is called before the first frame update
        void OnEnable()
        {
            m_startingPos = transform.position;
            if (respawnIfTouched)
            {
                //TODO change to fixed Position
                MoveTargetToRandomPosition();
            }
        }

        public void MoveTargetToRandomPosition()
        {
            //TODO Advanced: Move on circle
            var newTargetPos = m_startingPos + (Random.insideUnitSphere * spawnRadius);
            newTargetPos.y = m_startingPos.y;
            transform.position = newTargetPos;
        }

        private void OnCollisionEnter(Collision col)
        {
            onCollisionEnterEvent.Invoke(col);
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
