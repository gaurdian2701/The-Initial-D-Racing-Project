using TMPro;
using UnityEngine;

public class SpeedDisplay : MonoBehaviour
{
    [HideInInspector]
    public Rigidbody _carRB;
    [SerializeField]
    private TMP_Text _speedText;
    void Update()
    {
        _speedText.text = Mathf.RoundToInt(_carRB.linearVelocity.magnitude * 50f) +  " km/h";
    }
}
