namespace Skin_Series.Core
{
    using Interfaces;

    public abstract class ExtensionBase : IExtension
    {
        public abstract string Name { get; }
        public abstract bool IsEnabled { get; set; }

        public abstract void Load();
        public abstract void Dispose();
    }
}