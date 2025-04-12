
public class EnemyStatComponent : CharacterStatsComponent
{
    private void Awake()
    {
        stats = new EnemyStats();
    }

    private void Start()
    {
        UpdateHPUI();
    }
}