using UnityEngine;

namespace Stillwater.Core
{
    /// <summary>
    /// Handles player sprite animation based on movement state and facing direction.
    /// Uses a code-driven approach for flexibility with limited animation assets.
    ///
    /// West-side directions (West, NorthWest, SouthWest) use flipped versions of
    /// east-side animations (East, NorthEast, SouthEast).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _frameRate = 8f;

        [Header("Static Direction Sprites")]
        [SerializeField] private Sprite _spriteNorth;
        [SerializeField] private Sprite _spriteNorthEast;
        [SerializeField] private Sprite _spriteEast;
        [SerializeField] private Sprite _spriteSouthEast;
        [SerializeField] private Sprite _spriteSouth;
        [SerializeField] private Sprite _spriteSouthWest;
        [SerializeField] private Sprite _spriteWest;
        [SerializeField] private Sprite _spriteNorthWest;

        [Header("Idle Animations")]
        [SerializeField] private Sprite[] _idleNorthFrames;
        [SerializeField] private Sprite[] _idleNorthEastFrames;
        [SerializeField] private Sprite[] _idleEastFrames;
        [SerializeField] private Sprite[] _idleSouthEastFrames;
        [SerializeField] private Sprite[] _idleSouthFrames;
        // West-side idles use flipped east-side animations

        [Header("Walk Animations")]
        [SerializeField] private Sprite[] _walkNorthFrames;
        [SerializeField] private Sprite[] _walkNorthEastFrames;
        [SerializeField] private Sprite[] _walkEastFrames;
        [SerializeField] private Sprite[] _walkSouthEastFrames;
        [SerializeField] private Sprite[] _walkSouthFrames;
        // West-side walks use flipped east-side animations

        [Header("References")]
        [SerializeField] private PlayerController _playerController;

        private SpriteRenderer _spriteRenderer;
        private FacingDirection _currentDirection = FacingDirection.South;
        private bool _isMoving;
        private float _animationTimer;
        private int _currentFrame;

        public FacingDirection CurrentDirection => _currentDirection;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_playerController == null)
            {
                _playerController = GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            UpdateFacingDirection();
            UpdateAnimation();
        }

        private void UpdateFacingDirection()
        {
            if (_playerController == null) return;

            _isMoving = _playerController.IsMoving;

            if (_isMoving)
            {
                // Get movement direction and convert to facing direction
                Vector2 velocity = GetPlayerVelocity();
                if (velocity.sqrMagnitude > 0.01f)
                {
                    _currentDirection = VelocityToDirection(velocity);
                }
            }
        }

        private Vector2 GetPlayerVelocity()
        {
            return _playerController.CurrentVelocity;
        }

        private FacingDirection VelocityToDirection(Vector2 velocity)
        {
            // Calculate angle and map to 8 directions
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;

            // Normalize to 0-360
            if (angle < 0) angle += 360f;

            // Map angle to direction (each direction covers 45 degrees)
            // East is 0 degrees, rotating counter-clockwise
            if (angle >= 337.5f || angle < 22.5f) return FacingDirection.East;
            if (angle >= 22.5f && angle < 67.5f) return FacingDirection.NorthEast;
            if (angle >= 67.5f && angle < 112.5f) return FacingDirection.North;
            if (angle >= 112.5f && angle < 157.5f) return FacingDirection.NorthWest;
            if (angle >= 157.5f && angle < 202.5f) return FacingDirection.West;
            if (angle >= 202.5f && angle < 247.5f) return FacingDirection.SouthWest;
            if (angle >= 247.5f && angle < 292.5f) return FacingDirection.South;
            return FacingDirection.SouthEast;
        }

        private void UpdateAnimation()
        {
            _animationTimer += Time.deltaTime;
            float frameDuration = 1f / _frameRate;

            if (_animationTimer >= frameDuration)
            {
                _animationTimer -= frameDuration;
                _currentFrame++;
            }

            Sprite[] frames = GetCurrentFrames();
            bool shouldFlip = ShouldFlipSprite();

            if (frames != null && frames.Length > 0)
            {
                // Animated state
                _currentFrame %= frames.Length;
                _spriteRenderer.sprite = frames[_currentFrame];
            }
            else
            {
                // Static sprite fallback
                _spriteRenderer.sprite = GetStaticSprite(_currentDirection);
                _currentFrame = 0;
            }

            _spriteRenderer.flipX = shouldFlip;
        }

        private Sprite[] GetCurrentFrames()
        {
            if (_isMoving)
            {
                return GetWalkFrames(_currentDirection);
            }
            else
            {
                return GetIdleFrames(_currentDirection);
            }
        }

        private Sprite[] GetWalkFrames(FacingDirection direction)
        {
            return direction switch
            {
                FacingDirection.North => _walkNorthFrames,
                FacingDirection.NorthEast => _walkNorthEastFrames,
                FacingDirection.East => _walkEastFrames,
                FacingDirection.SouthEast => _walkSouthEastFrames,
                FacingDirection.South => _walkSouthFrames,
                // West-side directions use flipped east-side animations
                FacingDirection.NorthWest => _walkNorthEastFrames,
                FacingDirection.West => _walkEastFrames,
                FacingDirection.SouthWest => _walkSouthEastFrames,
                _ => null
            };
        }

        private Sprite[] GetIdleFrames(FacingDirection direction)
        {
            return direction switch
            {
                FacingDirection.North => _idleNorthFrames,
                FacingDirection.NorthEast => _idleNorthEastFrames,
                FacingDirection.East => _idleEastFrames,
                FacingDirection.SouthEast => _idleSouthEastFrames,
                FacingDirection.South => _idleSouthFrames,
                // West-side directions use flipped east-side animations
                FacingDirection.NorthWest => _idleNorthEastFrames,
                FacingDirection.West => _idleEastFrames,
                FacingDirection.SouthWest => _idleSouthEastFrames,
                _ => null
            };
        }

        private Sprite GetStaticSprite(FacingDirection direction)
        {
            return direction switch
            {
                FacingDirection.North => _spriteNorth,
                FacingDirection.NorthEast => _spriteNorthEast,
                FacingDirection.East => _spriteEast,
                FacingDirection.SouthEast => _spriteSouthEast,
                FacingDirection.South => _spriteSouth,
                FacingDirection.SouthWest => _spriteSouthWest,
                FacingDirection.West => _spriteWest,
                FacingDirection.NorthWest => _spriteNorthWest,
                _ => _spriteSouth
            };
        }

        private bool ShouldFlipSprite()
        {
            // Flip for all west-side directions (they use east-side animations)
            return _currentDirection == FacingDirection.West ||
                   _currentDirection == FacingDirection.NorthWest ||
                   _currentDirection == FacingDirection.SouthWest;
        }

        /// <summary>
        /// Manually set facing direction (useful for NPCs or cutscenes).
        /// </summary>
        public void SetDirection(FacingDirection direction)
        {
            _currentDirection = direction;
            _currentFrame = 0;
            _animationTimer = 0f;
        }

        /// <summary>
        /// Force a specific animation state.
        /// </summary>
        public void SetMoving(bool moving)
        {
            if (_isMoving != moving)
            {
                _isMoving = moving;
                _currentFrame = 0;
                _animationTimer = 0f;
            }
        }
    }
}
