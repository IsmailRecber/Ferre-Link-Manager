using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;

namespace Ferre.LinkManager.Application.Common.Abstractions;


public interface IReadDbContext
{
    IQueryable<Link> Links { get; }

    IQueryable<Category> Categories { get; }
}