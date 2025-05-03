/*
 * ������ �ִ� ����(Idle)
 * �������� ����(Attacking)
 * ���� ��ǥ�� �ϰ� �ִ� ����(Targeting)
 * ���� ����(Dead)
 */
public abstract class State
{
    public abstract void Enter(); //���� �Լ�

    public abstract void Tick(float deltaTime); //���� �Լ�

    public abstract void Exit(); //���� �Լ�

}
