using UnityEngine;
using Zenject;

namespace CustomAvatar.Zenject
{
    internal static class BindingExtensions
    {
        public static ConcreteIdArgConditionCopyNonLazyBinder FromNewComponentOnNewGameObject<TContract>(this FromBinderGeneric<TContract> binder)
        {
            return binder.FromNewComponentOn(new GameObject(typeof(TContract).FullName)).AsSingle();
        }

        public static ConcreteIdArgConditionCopyNonLazyBinder FromNewComponentOnNewGameObject(this FromBinderNonGeneric binder, string name)
        {
            return binder.FromNewComponentOn(new GameObject(name)).AsSingle();
        }
    }
}
