#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace OutOfService
{
    // Editor-only visual review. It runs against the real scene and CharacterController.
    public class MarketReviewProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            var player=MarketPlayer.Instance;
            var cc=player.GetComponent<CharacterController>();
            Vector3 initialPosition=player.transform.position;
            Quaternion initialRotation=player.transform.rotation;
            bool torchWasOn=player.flashlight.enabled;
            bool bareWasActive=player.bareHandsModel.activeSelf;
            bool torchWasActive=player.flashlightModel.activeSelf;
            Quaternion cameraRotation=player.viewCamera.transform.localRotation;
            player.bareHandsModel.SetActive(false);player.flashlightModel.SetActive(true);
            player.enabled=false;
            foreach(var threat in FindObjectsByType<MarketThreat>()) threat.enabled=false;
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Review"));Directory.CreateDirectory(folder);
            var report=new StringBuilder("Market V7 editor play review\n");
            report.AppendLine("Renderers: "+FindObjectsByType<Renderer>().Length);
            report.AppendLine("Colliders: "+FindObjectsByType<Collider>().Length);
            string[] names={"entrance","ramen","meat","seafood","escalator","clothing","shoes","ramen_reverse","detergent","paper_goods","produce","sign_front","sign_back","dairy","cooler","apple_carton","tomato_pack","greens_wall","pepper_promo","frozen_aisle","apple_slope","king_oyster_pack","enoki_pack","cut_vegetables"};
            Vector3[] positions={new Vector3(0,.9f,-38),new Vector3(-5.5f,.9f,-12),new Vector3(-42,.9f,25.4f),
                new Vector3(-27,.9f,25.4f),new Vector3(34.5f,.9f,-8),new Vector3(-39.5f,5.9f,-31),new Vector3(-10,5.9f,1),new Vector3(-10.5f,.9f,-12),new Vector3(28,.9f,8),new Vector3(28,.9f,17),new Vector3(-35,.9f,-18),new Vector3(-8,.9f,-20),new Vector3(-5.5f,.9f,-13),new Vector3(-17,.9f,38.8f),new Vector3(7,.9f,38.8f),new Vector3(-35.55f,.9f,-14),new Vector3(-19.55f,.9f,-14),new Vector3(-43.4f,.9f,22),new Vector3(-30,.9f,-10.6f),new Vector3(-14,.9f,36),new Vector3(-35.4f,.9f,-16.7f),new Vector3(-44.35f,.9f,-12.9f),new Vector3(-44.35f,.9f,-7.1f),new Vector3(-44.35f,.9f,0)};
            Vector3[] targets={new Vector3(0,1.7f,-43),new Vector3(-8,1.5f,-12),new Vector3(-42,1.10f,27.7f),
                new Vector3(-27,1.10f,27.7f),new Vector3(34.5f,3f,0),new Vector3(-37.5f,6.35f,-29),new Vector3(-10,5.9f,4),new Vector3(-8,1.5f,-12),new Vector3(26,1.35f,8),new Vector3(26,1.35f,17),new Vector3(-38,1.1f,-15),new Vector3(-8,3.8f,-16),new Vector3(-8,3.8f,-16),new Vector3(-17,1.45f,42),new Vector3(7,1.45f,42),new Vector3(-36.95f,1.00f,-14),new Vector3(-20.95f,1.0f,-14),new Vector3(-45.4f,1.63f,22),new Vector3(-30,1.09f,-8),new Vector3(4,1.55f,36.7f),new Vector3(-37.3f,1.16f,-16),new Vector3(-45.12f,1.58f,-12.9f),new Vector3(-45.12f,1.58f,-7.1f),new Vector3(-45.12f,1.58f,0)};
            player.flashlight.enabled=true;
            for(int view=0;view<names.Length;view++)
            {
                cc.enabled=false;player.transform.position=positions[view];player.transform.rotation=Quaternion.identity;
                player.viewCamera.transform.LookAt(targets[view]);cc.enabled=true;
                foreach(var visibility in FindObjectsByType<MarketDetailVisibility>())visibility.Refresh();
                for(int frame=0;frame<12;frame++)yield return null;
                while(ShaderUtil.anythingCompiling)yield return null;
                for(int frame=0;frame<5;frame++)yield return null;
                float elapsed=0;
                for(int frame=0;frame<45;frame++){elapsed+=Time.unscaledDeltaTime;yield return null;}
                report.AppendLine(names[view]+": "+(45f/elapsed).ToString("F1")+" editor FPS; "+UnityStats.drawCalls+" draw calls; "+UnityStats.triangles+" triangles");
                int hidden=0,visibilityErrors=0;
                foreach(var visibility in FindObjectsByType<MarketDetailVisibility>()) foreach(var detail in visibility.details) if(detail!=null) {
                    float d=detail.bounds.SqrDistance(player.viewCamera.transform.position);if(!detail.enabled)hidden++;
                    if((d<34f*34f&&!detail.enabled)||(d>37f*37f&&detail.enabled))visibilityErrors++; }
                report.AppendLine(names[view]+" detail culling: "+hidden+" hidden; "+visibilityErrors+" incorrect near/far states");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,names[view]+".png"));
                if(names[view]=="detergent")
                {
                    foreach(var visibility in FindObjectsByType<MarketDetailVisibility>())visibility.enabled=false;
                    for(int wait=0;wait<15;wait++)yield return null;
                    report.AppendLine("detergent unculled comparison: "+UnityStats.drawCalls+" draw calls; "+UnityStats.triangles+" triangles");
                    yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"detergent_unculled.png"));
                    var diagnostics=new StringBuilder();
                    foreach(var r in FindObjectsByType<MeshRenderer>())if(r.name.StartsWith("Small props sector"))
                    {
                        var mesh=r.GetComponent<MeshFilter>().sharedMesh;
                        if(r.bounds.SqrDistance(new Vector3(26,1.5f,8))<10 || mesh.bounds.SqrDistance(new Vector3(26,1.5f,8))<10)
                            diagnostics.AppendLine(r.name+" world="+r.bounds+" mesh="+mesh.bounds+" enabled="+r.enabled);
                    }
                    File.WriteAllText(Path.Combine(folder,"bounds-review.txt"),diagnostics.ToString());
                    foreach(var visibility in FindObjectsByType<MarketDetailVisibility>()){visibility.enabled=true;visibility.Refresh();}
                }
            }
            var door=GameObject.Find("생수 · 탄산 냉장고 문 3 - E to open").GetComponent<MarketSwingDoor>();
            Quaternion closed=door.transform.localRotation;door.Toggle();yield return new WaitForSeconds(1f);
            float opening=Quaternion.Angle(closed,door.transform.localRotation);
            report.AppendLine("Cooler door open angle: "+opening.ToString("F1"));
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"cooler_open.png"));
            door.Toggle();yield return new WaitForSeconds(1f);
            report.AppendLine("Cooler door closed error: "+Quaternion.Angle(closed,door.transform.localRotation).ToString("F2"));
            var staff=GameObject.Find("정육 staff door - E to open").GetComponent<MarketSwingDoor>();
            Quaternion staffClosed=staff.transform.localRotation;staff.Toggle();yield return new WaitForSeconds(1f);
            report.AppendLine("Prep door open angle: "+Quaternion.Angle(staffClosed,staff.transform.localRotation).ToString("F1"));
            staff.Toggle();yield return new WaitForSeconds(1f);
            foreach(float lane in new[]{34.5f,41.5f})
            {
                cc.enabled=false;player.transform.position=new Vector3(lane,.95f,-6.2f);cc.enabled=true;Physics.SyncTransforms();
                for(int frame=0;frame<180;frame++){cc.Move(new Vector3(0,-.08f,.08f));yield return null;}
                Vector3 top=player.transform.position;
                report.AppendLine("Escalator "+lane+" up: "+top+"; "+(top.z>6f && top.y>5.7f ? "PASS" : "FAIL"));
                for(int frame=0;frame<180;frame++){cc.Move(new Vector3(0,-.08f,-.08f));yield return null;}
                Vector3 bottom=player.transform.position;
                report.AppendLine("Escalator "+lane+" down: "+bottom+"; "+(bottom.z< -6f && bottom.y<1.1f ? "PASS" : "FAIL"));
            }
            cc.enabled=false;player.transform.position=initialPosition;player.transform.rotation=initialRotation;
            player.viewCamera.transform.localRotation=cameraRotation;cc.enabled=true;player.enabled=true;
            player.flashlight.enabled=torchWasOn;player.bareHandsModel.SetActive(bareWasActive);player.flashlightModel.SetActive(torchWasActive);
            foreach(var threat in FindObjectsByType<MarketThreat>()) threat.enabled=true;
            File.WriteAllText(Path.Combine(folder,"play-review.txt"),report.ToString());
            Debug.Log("Market review finished: "+Path.Combine(folder,"play-review.txt"));Destroy(this);
        }
    }
}
#endif
