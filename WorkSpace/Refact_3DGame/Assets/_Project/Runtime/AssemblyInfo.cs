// Runtime 내부 멤버를 EditMode 테스트 프로젝트에서 사용할 수 있게 한다.
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Assembly-CSharp")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

[assembly: InternalsVisibleTo("rudIsland.RPG3D.EditModeTests")]
