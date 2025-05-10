
/*
 * 액션은 말단노드(자식 노드가 없음)로 부모노드들에 행동을 하겠다고 반환해줄 노드
 * 행동을 수행할 노드
 */

public class ActionNode : ENode
{
    //ESTATE 반환 델리게이트
    public delegate ESTATE ActionNodeDelegate();

    private ActionNodeDelegate action; //다음에 할 행동

    public ActionNode (ActionNodeDelegate action) //행동을 결정하는 생성자, 델리게이트 매개변수
    {
        this.action = action;
    }

    public override ESTATE Evaluate()
    {
        currentEState = action(); // 행동 실행 후 결과 저장
        return currentEState;
    }



}
