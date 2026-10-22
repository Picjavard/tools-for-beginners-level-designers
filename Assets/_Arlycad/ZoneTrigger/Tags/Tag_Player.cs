using UnityEngine;

namespace Arlycad
{
    /// <summary>
    /// Метка игрока. Логики нет: её ищут зона и телепорт.
    /// Для новых смыслов (красный ключ, лодка) лучше ассет Role, а не копия этого файла.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Arlycad/Tags/Tag Player", 40)]
    public class Tag_Player : MonoBehaviour
    {
    }
}
