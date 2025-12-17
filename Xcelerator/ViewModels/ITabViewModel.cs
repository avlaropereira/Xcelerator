namespace Xcelerator.ViewModels
{
    /// <summary>
    /// Interface for tab view models in tabbed interfaces
    /// </summary>
    public interface ITabViewModel
    {
        /// <summary>
        /// The display name shown in the tab header
        /// </summary>
        string HeaderName { get; }
    }
}
