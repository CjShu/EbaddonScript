namespace Skin_Series.Core.Interfaces
{
    public interface IExtension
    {
        string Name { get; }
        bool IsEnabled { get; set; }
        void Load();
        void Dispose();
    }
}