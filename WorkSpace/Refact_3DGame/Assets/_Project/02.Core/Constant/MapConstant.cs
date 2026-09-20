// partial로 ConstantValid 클래스를 나누어 맵, 적, 퀘스트, 아이템 등의 상수를 관리한다.

namespace Core.ConstantValid
{
    public static partial class ConstantValid
    {
        public const string GroundMapName = "Ground";
        public const string UnderGroundMapName = "UnderGround";

        // 맵 이름과 Address는 지금 값이 같아도 서로 다른 설정이다.
        public const string GroundMapAddress = "Ground";
        public const string UnderGroundMapAddress = "UnderGround";

        public const string StartScenePath = "Assets/_Project/00.Scene/Start.unity";
        public const string GroundScenePath = "Assets/_Project/00.Scene/Ground.unity";
        public const string UnderGroundScenePath = "Assets/_Project/00.Scene/UnderGround.unity";

        public const string MapEnvironmentRootName = "Setup&Lights";
        public const string GroundObjectsRootName = "GroundObjects";
        public const string UnderGroundObjectsRootName = "UndergroundObjects";
    }
}
