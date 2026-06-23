using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Code.UI
{
    public class TitleScreen : MonoBehaviour
    {
        
        [SerializeField] private Image washingMachine;
        [SerializeField] private float timeToWait;
        [SerializeField] private float shakeSpeed;
        [SerializeField] private RectTransform shakepointA;
        [SerializeField] private RectTransform shakepointB;
        [SerializeField] private bool shaking;
        
        void Start()
        {
            shakepointA.position = washingMachine.rectTransform.position;
            
            StartCoroutine(ThrowItBack());
        }
        
        
        private IEnumerator ThrowItBack()
        {
            while (shaking)
            {
                yield return new WaitForSeconds(timeToWait);
                while (Vector2.Distance(washingMachine.rectTransform.position, shakepointB.position) > 0.01f)
                {
                    washingMachine.rectTransform.position =  Vector2.MoveTowards(washingMachine.rectTransform.position, shakepointB.position, shakeSpeed * Time.deltaTime);
                    yield return null;
                    //Debug.Log("moving");
                }
            
                yield return new WaitForSeconds(timeToWait);
                while (Vector2.Distance(washingMachine.rectTransform.position, shakepointA.position) > 0.01f)
                {
                    washingMachine.rectTransform.position =  Vector2.MoveTowards(washingMachine.rectTransform.position, shakepointA.position, shakeSpeed * Time.deltaTime);
                    yield return null;
                }
            }

            yield return null;
        }
    }
}
