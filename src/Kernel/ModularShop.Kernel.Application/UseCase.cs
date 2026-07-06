namespace ModularShop.Kernel.Application;

/// <summary>
/// Marker base class for every use case. A module registers all of its use cases by scanning its
/// Application assembly for subclasses of this type (see <c>AddUseCases</c>), so adding a use case needs
/// no separate registration line — it only has to inherit <see cref="UseCase"/>. By convention every
/// concrete use case's class name ends with <c>"UseCase"</c>.
/// </summary>
public abstract class UseCase;
