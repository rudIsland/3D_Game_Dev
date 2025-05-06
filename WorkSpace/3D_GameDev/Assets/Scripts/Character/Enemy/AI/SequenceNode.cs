using System.Collections.Generic;

/*
 * �������� ������ ������ ���������� Ž������üũ, Ÿ�ټ��� �ൿ�� ���� ���
 */

public class SequenceNode : ENode
{
    //N���� �ڽ� ����
    private List<ENode> children;

    public SequenceNode(List<ENode> children) //������
    {
        this.children = children;
    }


    //��� �ڽ��� �����ؾ� �������� �����Ѵ�.
    public override ESTATE Evaluate()
    {
       foreach (var child in children) //�ڽ� ��带 ��ȸ
        {
            switch (child.Evaluate()){ //�ڽ� ����� ���¸� �ľ��Ѵ�.
                case ESTATE.SUCCESS: //success�� �ٸ� �ڽ��� �˻��Ϸ� �ٽ� foreach��
                    continue;
                case ESTATE.RUN:
                    return ESTATE.RUN;
                case ESTATE.FAILED:
                    return ESTATE.FAILED;
            }
        }

       //Failed�� �ɸ��� �ʰ� �����������Ƿ� Success
       return currentEState = ESTATE.SUCCESS; ; //��� �����ؾ� SUCCESS��ȯ
    }
}
