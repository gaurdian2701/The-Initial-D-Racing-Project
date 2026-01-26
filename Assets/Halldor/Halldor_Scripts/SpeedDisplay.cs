using TMPro;
using UnityEngine;

public class SpeedDisplay : MonoBehaviour
{
    [SerializeField]
    private Rigidbody _carRB;
    [SerializeField]
    private TMP_Text _speedText;
    void Update()
    {
        _speedText.text = _carRB.linearVelocity.magnitude.ToString();
    }
}
