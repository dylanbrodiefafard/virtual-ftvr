using UnityEngine;

public class SelectorHandPairer : MonoBehaviour
{
    public Transform LeftHand;
    public Transform RightHand;

    public void PairWithLeftHand()
    {
        var isLeftFound = LeftHand != null;
        gameObject.SetActive(isLeftFound);

        if (!isLeftFound)
        {
            Debug.LogWarning($"No {nameof(isLeftFound)} was set. Unable to {nameof(PairWithLeftHand)}.");
            return;
        }

        transform.parent = LeftHand;
        transform.localPosition = Vector3.zero;
    }

    public void PairWithRightHand()
    {
        var isRightFound = RightHand != null;
        gameObject.SetActive(isRightFound);

        if (!isRightFound)
        {
            Debug.LogWarning($"No {nameof(RightHand)} was set. Unable to {nameof(PairWithRightHand)}.");
            return;
        }

        transform.parent = RightHand;
        transform.localPosition = Vector3.zero;
    }
}
