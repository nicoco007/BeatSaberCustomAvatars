using UnityEngine;
using Zenject;

namespace CustomAvatar.Zenject
{
    internal static class BindingExtensions
    {
        public static ConcreteIdArgConditionCopyNonLazyBinder FromNewComponentOnNewGameObject<T>(this ConcreteIdBinderGeneric<T> binder)
        {
            return binder.FromNewComponentOnNewPrefab(new GameObject(typeof(T).FullName)).AsSingle();
        }
    }
}
