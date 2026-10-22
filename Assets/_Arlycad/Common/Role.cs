using UnityEngine;

namespace Arlycad
{
    /// <summary>
    /// Пустой ассет-роль. Важен сам файл: Player и Player (1) — разные роли.
    /// Создаётся через Assets > Create > Arlycad > Role или меню Arlycad > Создать базовые роли.
    /// </summary>
    [CreateAssetMenu(fileName = "Role", menuName = "Arlycad/Role", order = 0)]
    public class Role : ScriptableObject
    {
    }
}
