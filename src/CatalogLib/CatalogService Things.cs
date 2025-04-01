using CatalogLib.Database;
using CatalogLib.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogLib;

public partial class CatalogService
{
    public async Task<IQueryable<ThingItem>> ThingsAsync(int pageNumber = 0, int pageSize = 10)
    {
        var query = context.Things
            .Select(xx => new ThingItem()
            {
                Id = xx.Id,
                Name = xx.Name,
                Description = xx.Description
            })
            .OrderByDescending(xx => xx.Name)
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
        ;
        var q = await query.ToArrayAsync();
        return q.AsQueryable();
    }

    public async Task<ThingModel> GetThingAsync(int id)
    {
        var single = await context.Things 
            .Where(xx => xx.Id == id)
            .Select(xx => new ThingModel()
            {
                Name = xx.Name,
                Description = xx.Description
            })
            .SingleOrDefaultAsync()
        ;
        return single;
    }

    public async Task UpdateAsync(int id, ThingModel model)
    {
        var single = await context.Things
            .Where(xx => xx.Id == id)
            .SingleOrDefaultAsync()
        ;
        if (single is not null)
        {
            single.Name = model.Name;
            single.Description = model.Description;
        }
        else
        {
            await CreateImplAsync(context, model);
        }
        await context.SaveChangesAsync();
    }

    public async Task DeleteThingAsync(int id)
    {
        var single = await context.Things
            .Where(xx => xx.Id == id)
            .SingleOrDefaultAsync()
        ;
        if (single is not null)
        {
            context.Remove(single);
        }
        else
        {
            // errore
        }
        await context.SaveChangesAsync();
    }

    public ThingModel NewThing()
    {
        var model = new ThingModel()
        {
        };
        return model;
    }

    public async Task<int> CreateThingAsync(ThingModel model)
    {
        var header = await CreateImplAsync(context, model);
        await context.SaveChangesAsync();
        return header.Id;
    }

    private static async Task<Thing> CreateImplAsync(CatalogContext context, ThingModel model)
    {
        var result = await context.AddAsync(new Thing()
        {
            Name = model.Name,
            Description = model.Description
        });
        return result.Entity;
    }
}
