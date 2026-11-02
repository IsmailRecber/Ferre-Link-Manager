namespace Ferre.LinkManager.Application.Categories.UpdateCategory;

public sealed record UpdateCategoryCommand(Guid Id, string Name, bool IsActive);