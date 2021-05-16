using System.Linq;
using UnityEngine.SceneManagement;

namespace CustomAvatar.Utilities
{
    internal static class GameScenesManagerExtensions
    {
        public static bool IsSceneInStackAndActive(this GameScenesManager gameScenesManager, string sceneName)
        {
            if (!gameScenesManager.IsSceneInStack(sceneName)) return false;

            Scene scene = SceneManager.GetSceneByName(sceneName);

            return scene.IsValid() && scene.GetRootGameObjects().First().activeInHierarchy;
        }
    }
}
