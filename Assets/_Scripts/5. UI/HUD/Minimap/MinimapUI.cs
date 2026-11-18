using System;
using System.Collections.Generic;
using DG.Tweening;
using Foundation;
using UnityEngine;
using World;

namespace UI
{
    public sealed class MinimapUI : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private RectTransform _mapContainer;
        [SerializeField] private GameObject _roomBlockPrefab;
        [SerializeField] private RectTransform _rootContainer; 

        [Header("Settings")] 
        [SerializeField] private Vector2 _blockSize = new(97f, 58.5f);
        [SerializeField] private float _transitionDuration = 0.25f;
        [SerializeField] private Ease _transitionEase = Ease.OutQuad;
        [SerializeField] private Vector2 _defaultRootSize = new(300f, 300f);
        [SerializeField] private Vector2 _framePadding = new(100f, 100f);
        
        [Header("Currency Interaction")]
        [Tooltip("Assign the Currency Display RectTransform here so it can be moved out of the way.")]
        [SerializeField] private RectTransform _currencyContainer;

        [Header("Icon Configuration")]
        [SerializeField] private RoomStyleConfig[] _roomIcons;
        [SerializeField] private Sprite _portalIcon;

        private Dictionary<int, MinimapBlockUI> _spawnedBlocks = new();
        private bool _hasCenteredOnce;
        private bool _isExpanded;
        private float _currencyDefaultY;

        private void Start()
        {
            if (_currencyContainer != null)
            {
                _currencyDefaultY = _currencyContainer.anchoredPosition.y;
            }
        }

        private void OnEnable()
        {
            GameStateManager.RunState.OnFloorMapGenerated += BuildMap;
            GameStateManager.RunState.OnPlayerEnteredRoom += HandlePlayerMoved;
            GameStateManager.RunState.OnRoomStateChanged += HandleRoomStateChanged;

            Helpers.Input.OnExpandMapStarted += HandleMapExpandStarted;
            Helpers.Input.OnExpandMapCanceled += HandleMapExpandCanceled;

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

            Helpers.Input.OnExpandMapStarted -= HandleMapExpandStarted;
            Helpers.Input.OnExpandMapCanceled -= HandleMapExpandCanceled;
        }

        private void HandleMapExpandStarted() => SetExpanded(true);
        
        private void HandleMapExpandCanceled() => SetExpanded(false);

        public void SetExpanded(bool expanded)
        {
            if (_isExpanded == expanded) return;
            _isExpanded = expanded;

            _rootContainer.DOKill();
            _mapContainer.DOKill();
            if (_currencyContainer != null) _currencyContainer.DOKill();

            if (_isExpanded)
            {
                CalculateFullMapSize(out Vector2 targetSize, out Vector2 targetCenterPos);
                
                _rootContainer.DOSizeDelta(targetSize, _transitionDuration)
                    .SetEase(_transitionEase)
                    .SetUpdate(true);

                _mapContainer.DOAnchorPos(targetCenterPos, _transitionDuration)
                    .SetEase(_transitionEase)
                    .SetUpdate(true);

                if (_currencyContainer != null)
                {
                    float addedHeight = targetSize.y - _defaultRootSize.y;
                    
                    _currencyContainer.DOAnchorPosY(_currencyDefaultY - addedHeight, _transitionDuration)
                        .SetEase(_transitionEase)
                        .SetUpdate(true);
                }
            }
            else
            {
                _rootContainer.DOSizeDelta(_defaultRootSize, _transitionDuration)
                    .SetEase(_transitionEase)
                    .SetUpdate(true);

                CenterMapOnIndex(GameStateManager.RunState.CurrentRoomIndex, instant: false);

                if (_currencyContainer != null)
                {
                    _currencyContainer.DOAnchorPosY(_currencyDefaultY, _transitionDuration)
                        .SetEase(_transitionEase)
                        .SetUpdate(true);
                }
            }
        }

        private void BuildMap()
        {
            foreach (var block in _spawnedBlocks.Values) Destroy(block.gameObject);
            _spawnedBlocks.Clear();
            _hasCenteredOnce = false;

            foreach (var kvp in GameStateManager.RunState.FloorMap)
            {
                if (kvp.Value.IsDiscovered) 
                    SpawnBlock(kvp.Value);
            }
            
            if (_isExpanded)
            {
                SetExpanded(true); 
            }
        }

        private void SpawnBlock(VolatileRunState.RoomMapData data)
        {
            GameObject go = Instantiate(_roomBlockPrefab, _mapContainer);
            RectTransform rect = go.GetComponent<RectTransform>();

            rect.anchoredPosition = new Vector2(data.X * _blockSize.x, data.Y * _blockSize.y);

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

            if (!_isExpanded)
            {
                CenterMapOnIndex(newIndex, instant: !_hasCenteredOnce);
            }
            else
            {
                SetExpanded(false); 
                SetExpanded(true);
            }
            
            _hasCenteredOnce = true;

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

            Vector2 targetPos = new Vector2(-data.X * _blockSize.x, -data.Y * _blockSize.y);

            _mapContainer.DOKill();

            if (instant)
                _mapContainer.anchoredPosition = targetPos;
            else
                _mapContainer.DOAnchorPos(targetPos, _transitionDuration)
                    .SetEase(_transitionEase)
                    .SetUpdate(true);
        }

        private void CalculateFullMapSize(out Vector2 mapSize, out Vector2 mapCenterOffset)
        {
            if (GameStateManager.RunState.FloorMap.Count == 0) 
            {
                mapSize = _defaultRootSize;
                mapCenterOffset = Vector2.zero;
                return;
            }

            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (var room in GameStateManager.RunState.FloorMap.Values)
            {
                if (room.X < minX) minX = room.X;
                if (room.X > maxX) maxX = room.X;
                if (room.Y < minY) minY = room.Y;
                if (room.Y > maxY) maxY = room.Y;
            }

            float contentWidth = (maxX - minX + 1) * _blockSize.x;
            float contentHeight = (maxY - minY + 1) * _blockSize.y;
            
            float neededWidth = contentWidth + _framePadding.x;
            float neededHeight = contentHeight + _framePadding.y;
            
            float scaleX = neededWidth / _defaultRootSize.x;
            float scaleY = neededHeight / _defaultRootSize.y;
            float uniformScale = Mathf.Max(scaleX, scaleY);
            
            uniformScale = Mathf.Max(1f, uniformScale);
            
            mapSize = _defaultRootSize * uniformScale;
            
            float centerX = (minX + maxX) / 2f * _blockSize.x;
            float centerY = (minY + maxY) / 2f * _blockSize.y;
            mapCenterOffset = new Vector2(-centerX, -centerY);
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

            if (data.Type == RoomType.Boss && data.IsCleared)
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