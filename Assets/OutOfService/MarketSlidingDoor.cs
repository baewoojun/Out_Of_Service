using UnityEngine;

namespace OutOfService
{
    // The leaf, pull and collision shape translate together; the overhead track stays fixed.
    public sealed class MarketSlidingDoor:MonoBehaviour
    {
        public Vector3 openOffset=new Vector3(-3.2f,0,0);
        public float speed=2.4f;
        public bool isOpen;
        Vector3 closedPosition;
        BoxCollider leaf;
        readonly Collider[] overlaps=new Collider[24];
        public bool IsAtTarget=>Vector3.SqrMagnitude(transform.localPosition-(closedPosition+(isOpen?openOffset:Vector3.zero)))<.000001f;
        public float OpenFraction=>Mathf.Clamp01(Vector3.Distance(transform.localPosition,closedPosition)/openOffset.magnitude);
        void Awake(){closedPosition=transform.localPosition;leaf=GetComponentInChildren<BoxCollider>();}
        public void Toggle(){isOpen=!isOpen;}
        void Update()
        {
            if(IsAtTarget)return;
            Vector3 target=closedPosition+(isOpen?openOffset:Vector3.zero);
            Vector3 next=Vector3.MoveTowards(transform.localPosition,target,speed*Time.deltaTime);
            if(!isOpen&&leaf){
                Vector3 delta=transform.parent?transform.parent.TransformVector(next-transform.localPosition):next-transform.localPosition;
                Vector3 center=leaf.transform.TransformPoint(leaf.center)+delta;
                Vector3 half=Vector3.Scale(leaf.size,leaf.transform.lossyScale)*.5f+Vector3.one*.025f;
                int count=Physics.OverlapBoxNonAlloc(center,half,overlaps,leaf.transform.rotation,~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<count;i++){
                    Collider hit=overlaps[i];
                    if(hit.transform.IsChildOf(transform))continue;
                    if(hit.GetComponentInParent<CharacterController>()||hit.GetComponentInParent<MarketThreat>()||hit.attachedRigidbody){isOpen=true;return;}
                }
                // A saturated query cannot prove the closing space is clear.
                if(count==overlaps.Length){isOpen=true;return;}
            }
            transform.localPosition=next;
        }
    }
}
