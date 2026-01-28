using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinRace : MonoBehaviour
{
    [SerializeField] private TMP_Text winnerName;
    [SerializeField] private Image winnerImage;
    private Animator _animator;
    [SerializeField] private GameObject defaultUI;
    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    public void Win(RacerData winner)
    {
        defaultUI.SetActive(false);
        winnerName.text = winner.racerName + " wins!";
        winnerImage.sprite = winner.racerSprite;
        _animator.SetTrigger("Win");
        Cursor.lockState = CursorLockMode.None;
        
    }
    
}
