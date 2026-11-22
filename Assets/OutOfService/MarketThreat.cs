using UnityEngine;

namespace OutOfService
{
    public class MarketThreat : MonoBehaviour
    {
        Vector3 start;
        float nextHit;
        float patrolDirection = 1f;

        void Awake() { start = transform.position; }

        void Update()
        {
            MarketPlayer player = MarketPlayer.Instance;
            if (player == null || MarketGame.Instance == null || MarketGame.Instance.finished || MarketGame.Instance.failed) return;
            Vector3 flatTarget = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
            float distance = Vector3.Distance(transform.position, flatTarget);
            bool sameFloor = Mathf.Abs(player.transform.position.y - transform.position.y) < 1.8f;
            if (sameFloor && distance < 7f)
            {
                transform.position = Vector3.MoveTowards(transform.position, flatTarget, 2.1f * Time.deltaTime);
                transform.LookAt(flatTarget);
                if (distance < 1.7f && Time.time >= nextHit)
                {
                    nextHit = Time.time + 2f;
                    player.TakeDamage(18f);
                    MarketGame.Instance.Show("무언가가 바로 뒤에 있다!", 2f);
                }
            }
            else
            {
                if (Mathf.Abs(transform.position.z - start.z) > 8f) patrolDirection *= -1f;
                transform.position += Vector3.forward * patrolDirection * 1.2f * Time.deltaTime;
            }
        }
    }
}
