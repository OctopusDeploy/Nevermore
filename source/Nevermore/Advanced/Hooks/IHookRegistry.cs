namespace Nevermore.Advanced.Hooks
{
    public interface IHookRegistry : IHook
    {
        void Register(IHook hook);
    }
}