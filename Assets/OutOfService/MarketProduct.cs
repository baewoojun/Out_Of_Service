using UnityEngine;

namespace OutOfService
{
    [RequireComponent(typeof(Rigidbody))]
    public class MarketProduct : MonoBehaviour
    {
        Rigidbody body;
        Collider itemCollider;
        bool inspecting;
        Vector3 restingCenter;
        float normalFieldOfView;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            itemCollider = GetComponent<Collider>();
            body.isKinematic = true;
        }

        public void PickUp(Camera camera)
        {
            if (camera == null) return;
            body.isKinematic = true;
            itemCollider.enabled = false;
            transform.SetParent(camera.transform, false);
            transform.localPosition = new Vector3(0.28f, -0.29f, 0.82f);
            transform.localRotation = Quaternion.Euler(0, 20f, 0);
            restingCenter=camera.transform.InverseTransformPoint(HeldCenter());
        }

        Vector3 HeldCenter()=>transform.TransformPoint(itemCollider is BoxCollider box?box.center:Vector3.zero);

        public void SetInspecting(Camera camera,bool active)
        {
            if(inspecting==active || transform.parent!=camera.transform)return;
            inspecting=active;
            Vector3 center=restingCenter;
            if(active){
                normalFieldOfView=camera.fieldOfView;
                camera.fieldOfView=Mathf.Min(29,normalFieldOfView);
                float radius=itemCollider is BoxCollider box?Vector3.Scale(box.size,transform.lossyScale).magnitude*.5f:.15f;
                float distance=Mathf.Max(camera.nearClipPlane+radius+.025f,
                    1.14f*radius/Mathf.Sin(camera.fieldOfView*.5f*Mathf.Deg2Rad));
                center=Vector3.forward*distance;
            }else camera.fieldOfView=normalFieldOfView;
            transform.position+=camera.transform.TransformPoint(center)-HeldCenter();
        }

        public void RotateHeld(float yaw,float pitch)
        {
            if(!transform.parent || itemCollider.enabled)return;
            var pivot=HeldCenter();
            transform.RotateAround(pivot,transform.parent.up,yaw);
            transform.RotateAround(pivot,transform.parent.right,pitch);
        }

        public void Drop(Camera camera)
        {
            SetInspecting(camera,false);
            transform.SetParent(null, true);
            itemCollider.enabled = true;
            body.isKinematic = false;
            body.linearVelocity = camera.transform.forward * 2.3f + Vector3.up * 0.8f;
        }
    }
}
