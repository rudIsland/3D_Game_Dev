public enum ESTATE
{
    RUN, SUCCESS, FAILED
}

public abstract class ENode
{
    protected ESTATE currentEState;

    //현재의 노드상태를 평가하고 실행하거나 제어하는 역할
    public abstract ESTATE Evaluate(); //행동 반환 메소드

}
