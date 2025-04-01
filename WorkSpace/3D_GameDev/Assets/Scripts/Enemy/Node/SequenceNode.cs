using System.Collections.Generic;

/*
 * 시퀀스는 셀럭터 내에서 순찰내에서 탐지범위체크, 타겟설정 행동에 대한 노드
 */

public class SequenceNode : ENode
{
    //N개의 자식 노드들
    private List<ENode> children;

    public SequenceNode(List<ENode> children) //생성자
    {
        this.children = children;
    }


    //모든 자식이 성공해야 성공으로 간주한다.
    public override ESTATE Evaluate()
    {
       foreach (var child in children) //자식 노드를 순회
        {
            switch (child.Evaluate()){ //자식 노드의 상태를 파악한다.
                case ESTATE.SUCCESS: //success면 다른 자식을 검사하러 다시 foreach로
                    continue;
                case ESTATE.RUN:
                    return ESTATE.RUN;
                case ESTATE.FAILED:
                    return ESTATE.FAILED;
            }
        }

       //Failed에 걸리지 않고 빠져나왔으므로 Success
       return currentEState = ESTATE.SUCCESS; ; //모두 성공해야 SUCCESS반환
    }
}
