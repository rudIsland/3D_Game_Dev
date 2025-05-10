
/*
 * �׼��� ���ܳ��(�ڽ� ��尡 ����)�� �θ���鿡 �ൿ�� �ϰڴٰ� ��ȯ���� ���
 * �ൿ�� ������ ���
 */

public class ActionNode : ENode
{
    //ESTATE ��ȯ ��������Ʈ
    public delegate ESTATE ActionNodeDelegate();

    private ActionNodeDelegate action; //������ �� �ൿ

    public ActionNode (ActionNodeDelegate action) //�ൿ�� �����ϴ� ������, ��������Ʈ �Ű�����
    {
        this.action = action;
    }

    public override ESTATE Evaluate()
    {
        currentEState = action(); // �ൿ ���� �� ��� ����
        return currentEState;
    }



}
