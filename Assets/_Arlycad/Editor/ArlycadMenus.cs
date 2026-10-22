using UnityEditor;
using UnityEngine;

namespace Arlycad
{
    public static class ArlycadMenus
    {
        const string Folder = "Assets/_Arlycad/Roles";

        [MenuItem("Arlycad/Создать базовые роли")]
        public static void CreateDefaultRoles()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/_Arlycad", "Roles");

            Create("Player");
            Create("Enemy");
            Create("Pickup");
            Create("Pushable");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Роли лежат в " + Folder + ". Повесьте Role Holder на объект и перетащите туда нужную роль.");
        }

        static void Create(string roleName)
        {
            string path = Folder + "/" + roleName + ".asset";
            if (AssetDatabase.LoadAssetAtPath<Role>(path) != null)
                return;

            Role role = ScriptableObject.CreateInstance<Role>();
            AssetDatabase.CreateAsset(role, path);
        }
    }
}
