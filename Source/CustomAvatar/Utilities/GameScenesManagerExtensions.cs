using System.Linq;
using UnityEngine.SceneManagement;

namespace CustomAvatar.Utilities
{
    internal static class GameScenesManagerExtensions
    {
        public static bool IsSceneInStackAndActive(this GameScenesManager gameScenesManager, string sceneName)
        {
            if (!gameScenesManager.GetCurrentlyLoadedSceneNames().Contains(sceneName)) return false;

            Scene scene = SceneManager.GetSceneByName(sceneName);

            if (!scene.IsValid()) return false;

            return scene.GetRootGameObjects().First().activeInHierarchy;
        }
    }
}
