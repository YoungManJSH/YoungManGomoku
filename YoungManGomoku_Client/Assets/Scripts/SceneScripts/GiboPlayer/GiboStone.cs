using DG.Tweening;
using UnityEngine;

public class GiboStone : MonoBehaviour
{
    [SerializeField] private GameObject circle;

    public void SetCircleActive(bool active)
        => circle.SetActive(active);
}
