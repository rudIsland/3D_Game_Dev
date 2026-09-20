using System;

namespace World.Interaction
{
    // 상호작용 대상이 HUD에 표시할 현재 상태다.
    public readonly struct PlayerInteractionGuide :
        IEquatable<PlayerInteractionGuide>
    {
        public static readonly PlayerInteractionGuide Hidden =
            new PlayerInteractionGuide(string.Empty, false, false);

        public PlayerInteractionGuide(
            string message,
            bool canInteract,
            bool isVisible = true)
        {
            Message = message ?? string.Empty;
            CanInteract = canInteract;
            IsVisible = isVisible;
        }

        public string Message { get; }
        public bool CanInteract { get; }
        public bool IsVisible { get; }

        public bool Equals(PlayerInteractionGuide other)
        {
            return string.Equals(
                    Message,
                    other.Message,
                    StringComparison.Ordinal) &&
                CanInteract == other.CanInteract &&
                IsVisible == other.IsVisible;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerInteractionGuide other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Message != null ? Message.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ CanInteract.GetHashCode();
                hashCode = (hashCode * 397) ^ IsVisible.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(
            PlayerInteractionGuide left,
            PlayerInteractionGuide right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            PlayerInteractionGuide left,
            PlayerInteractionGuide right)
        {
            return !left.Equals(right);
        }
    }
}
