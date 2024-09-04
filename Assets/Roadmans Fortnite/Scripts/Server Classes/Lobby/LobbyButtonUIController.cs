using System;
using UnityEngine;
using UnityEngine.UI;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Lobby
{
    public class LobbyButtonUIController : MonoBehaviour
    {
        [SerializeField] private Sprite _notReady;
        [SerializeField] private Sprite _ready;

        [SerializeField] private Image _currentImage;


        public void ToggleReadyImage()
        {
            _currentImage.sprite = _currentImage.sprite == _notReady ? _ready : _notReady;
        }
    }
}
