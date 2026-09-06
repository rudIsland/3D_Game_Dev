namespace EditorTools
{
    // 명시적인 코드 호출만 UI Toolkit HUD 생성기로 전달한다.
    public static class CombatHudPrefabBuilder
    {
        public static void BuildCombatHud()
        {
            CombatHudToolkitPrefabBuilder.BuildCombatHud();
        }
    }
}
