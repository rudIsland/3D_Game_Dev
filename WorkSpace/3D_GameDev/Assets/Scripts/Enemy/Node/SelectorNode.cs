using System.Collections.Generic;

/*
 * Selector는 순찰 접근 공격 행동에 해당하는 노드
 */
public class SelectorNode : ENode
{
    protected List<ENode> children; //N개의 자식 노드

    public SelectorNode(List<ENode> children)
    {
        this.children = children;
    }


    //셀럭터 노드는 하나라도 성공자식이 있으면 성공으로 간주한다.
    public override ESTATE Evaluate()
    {
        foreach (var child in children) //자식 노드를 순회
        {
            switch(child.Evaluate()){
                case ESTATE.SUCCESS:
                    currentEState = ESTATE.SUCCESS;
                    return ESTATE.SUCCESS;
                case ESTATE.RUN:
                    currentEState = ESTATE.RUN;
                    return ESTATE.RUN;
                default:
                    continue;
            }
        }

        //반복문이 끝났는데 자식 노드들이 모두 Failed라면 셀렉터는 실패를 반환
        return currentEState = ESTATE.FAILED; //하나라도 실패하면 Failed반환
    }
}
