public enum ESTATE
{
    RUN, SUCCESS, FAILED
}

public abstract class ENode
{
    protected ESTATE currentEState;

    //������ �����¸� ���ϰ� �����ϰų� �����ϴ� ����
    public abstract ESTATE Evaluate(); //�ൿ ��ȯ �޼ҵ�

}
