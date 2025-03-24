using System.Collections.Generic;

/*
 * Selector�� ���� ���� ���� �ൿ�� �ش��ϴ� ���
 */
public class SelectorNode : ENode
{
    protected List<ENode> children; //N���� �ڽ� ���

    public SelectorNode(List<ENode> children)
    {
        this.children = children;
    }


    //������ ���� �ϳ��� �����ڽ��� ������ �������� �����Ѵ�.
    public override ESTATE Evaluate()
    {
        foreach (var child in children) //�ڽ� ��带 ��ȸ
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

        //�ݺ����� �����µ� �ڽ� ������ ��� Failed��� �����ʹ� ���и� ��ȯ
        return currentEState = ESTATE.FAILED; //�ϳ��� �����ϸ� Failed��ȯ
    }
}
