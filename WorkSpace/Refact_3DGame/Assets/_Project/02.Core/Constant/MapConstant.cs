// partial로 ConstantValid 클래스를 나누어 맵, 적, 퀘스트, 아이템 등의 상수를 관리한다.

namespace Core.ConstantValid
{
    public static partial class ConstantValid
    {
        // 맵 씬 이름과 Addressables 주소를 같게 유지하며 로딩과 씬 조회에 함께 사용한다.
        public const string GroundMapAddress = "Ground";
        public const string UnderGroundMapAddress = "UnderGround";
    }
}
