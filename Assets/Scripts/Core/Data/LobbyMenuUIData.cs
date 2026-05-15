using UnityEngine;
using System.Collections.Generic;

namespace TDF.Core.Data
{
    public enum ButtonActionType { OpenPage, StartGame, CloseCurrentPage, Custom }

    [System.Serializable]
    public class LobbyButtonData
    {
        public string buttonName = "New Button";
        public Sprite buttonImage;
        public string buttonText = "Button";
        public Rect buttonRect = new Rect(100, 100, 200, 50);
        public ButtonActionType actionType = ButtonActionType.OpenPage;
        public PageData targetPageAsset;
        
        // Optional styling
        public Color textColor = Color.white;
        public int fontSize = 48;
        public FontStyle fontStyle = FontStyle.Normal;
        public Font fontAsset;
    }

    [CreateAssetMenu(fileName = "NewLobbyMenuUIData", menuName = "TDF/Data/LobbyMenuUIData")]
    public class LobbyMenuUIData : ScriptableObject
    {
        [Header("Background Settings")]
        public Sprite backgroundImage;
        
        [Header("Top Banner Settings")]
        public Sprite topBannerImage;
        public Rect topBannerRect = new Rect(10, 10, 400, 100);
        
        [Header("Buttons Configuration")]
        public List<LobbyButtonData> buttons = new List<LobbyButtonData>();
    }
}
