using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using DG.Tweening;
using Foundation;
using UnityEngine;
using World;

namespace UI
{
    public sealed class MinimapUI : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private RectTransform _mapContainer;

        [SerializeField] private GameObject _roomBlockPrefab;

        [Header("Settings")] 
        [Tooltip("Must be the same size as 'MinimapBlock_Prefab', minus 1 pixel")]
        [SerializeField] private Vector2 _blockSize = new(100f, 60f);
        [SerializeField] private float _transitionDuration = 0.25f;
        [SerializeField] private Ease _transitionEase = Ease.OutQuad;
        
        [Header("Icon Configuration")]
        [SerializeField] private RoomStyleConfig[] _roomIcons;
        [SerializeField] private Sprite _portalIcon;

        private Dictionary<int, MinimapBlockUI> _spawnedBlocks = new();

        private void OnEnable()
        {
            GameStateManager.RunState.OnFloorMapGenerated += BuildMap;
            GameStateManager.RunState.OnPlayerEnteredRoom += HandlePlayerMoved;
            GameStateManager.RunState.OnRoomStateChanged += HandleRoomStateChanged;

            if (GameStateManager.RunState.FloorMap.Count > 0) BuildMap();
        }

        private void OnDisable()
        {
            if (GameStateManager.RunState != null)
            {
                GameStateManager.RunState.OnFloorMapGenerated -= BuildMap;
                GameStateManager.RunState.OnPlayerEnteredRoom -= HandlePlayerMoved;
                GameStateManager.RunState.OnRoomStateChanged -= HandleRoomStateChanged;
            }
        }

        private void BuildMap()
        {
            foreach (var block in _spawnedBlocks.Values) Destroy(block.gameObject);
            _spawnedBlocks.Clear();

            foreach (var kvp in GameStateManager.RunState.FloorMap)
            {
                if (kvp.Value.IsDiscovered) SpawnBlock(kvp.Value);
            }

            CenterMapOnIndex(GameStateManager.RunState.CurrentRoomIndex, instant: true);
        }

        private void SpawnBlock(VolatileRunState.RoomMapData data)
        {
            GameObject go = Instantiate(_roomBlockPrefab, _mapContainer);
            RectTransform rect = go.GetComponent<RectTransform>();

            rect.anchoredPosition = new Vector2(data.X * _blockSize.x, -data.Y * _blockSize.y);

            if (go.TryGetComponent<MinimapBlockUI>(out var blockUI))
            {
                blockUI.Setup(data, GetStyleForRoom(data));
                _spawnedBlocks[data.Index] = blockUI;
            }
        }

        private void HandlePlayerMoved(int newIndex)
        {
            foreach (var kvp in GameStateManager.RunState.FloorMap)
            {
                if (kvp.Value.IsDiscovered && !_spawnedBlocks.ContainsKey(kvp.Key))
                {
                    SpawnBlock(kvp.Value);
                }
            }

            CenterMapOnIndex(newIndex, instant: false);

            foreach (var block in _spawnedBlocks)
            {
                block.Value.SetFocus(block.Key == newIndex);
            }
        }

        private void HandleRoomStateChanged(int index)
        {
            if (_spawnedBlocks.TryGetValue(index, out var block))
            {
                var data = GameStateManager.RunState.FloorMap[index];
                RoomStyleConfig style = GetStyleForRoom(data);
                
                block.UpdateVisuals(data, style);
                block.SetFocus(index == GameStateManager.RunState.CurrentRoomIndex);
            }
        }

        private void CenterMapOnIndex(int targetIndex, bool instant)
        {
            if (!GameStateManager.RunState.FloorMap.TryGetValue(targetIndex, out var data)) return;

            Vector2 targetPos = new Vector2(-data.X * _blockSize.x, data.Y * _blockSize.y);

            _mapContainer.DOKill();

            if (instant)
                _mapContainer.anchoredPosition = targetPos;
            else
                _mapContainer.DOAnchorPos(targetPos, _transitionDuration)
                    .SetEase(_transitionEase)
                    .SetUpdate(true);
        }

        private RoomStyleConfig GetStyleForRoom(VolatileRunState.RoomMapData data)
        {
            RoomStyleConfig config = default;

            foreach (var style in _roomIcons)
            {
                if (style.Type == data.Type)
                {
                    config = style;
                    break;
                }
            }

            if (data.Type == RoomType.Boss)
            {
                config.Icon = _portalIcon;
            }

            return config;
        }
    }

    [Serializable]
    public struct RoomStyleConfig
    {
        public RoomType Type;
        public Sprite Icon;
        public Color ActiveColor;
        public Color DimmedColor;
    }
}