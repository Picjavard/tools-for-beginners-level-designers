using System.Collections.Generic;
using UnityEngine;

namespace Arlycad
{
    /// <summary>
    /// Вешается на объект, который должен узнавать зона или телепорт.
    /// Ролей может быть несколько: игрок с ключом — это Player и Key.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Arlycad/Role Holder", 20)]
    public class RoleHolder : MonoBehaviour
    {
        [InspectorName("Роли")]
        [Tooltip("Перетащите сюда ассеты ролей из окна Project. Один объект может нести несколько ролей.")]
        public List<Role> roles = new List<Role>();

        public bool ContainsAny(List<Role> wanted)
        {
            if (roles == null || wanted == null)
                return false;

            for (int i = 0; i < wanted.Count; i++)
            {
                Role role = wanted[i];
                if (role == null)
                    continue;

                for (int j = 0; j < roles.Count; j++)
                {
                    if (roles[j] == role)
                        return true;
                }
            }

            return false;
        }
    }
}
