using CatalogLib.Database;
using CatalogLib.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogLib;

public partial class CatalogService
{
    public async Task<IQueryable<ThingPropertyItem>> GetThingPropertiesAsync(int thingId, int pageNumber = 0, int pageSize = 10)
    {
        var query = context.ThingsProperties
            .Where(xx => xx.ThingId == thingId)
            .Select(xx => new ThingPropertyItem()
            {
                Id = xx.Id,
                Name = xx.Name,
                Value = xx.Value,
                Type = xx.Type,
                ContentType = xx.ContentType
            })
            .OrderBy(xx => xx.Name)
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
        ;
        var q = await query.ToArrayAsync();
        return q.AsQueryable();
    }

    public async Task<ThingPropertyModel> GetThingPropertyAsync(int id)
    {
        var single = await context.ThingsProperties
            .Where(xx => xx.Id == id)
            .Select(xx => new ThingPropertyModel()
            {
                Name = xx.Name,
                Type = xx.Type,
                Value = xx.Value,
                ContentType = xx.ContentType
            })
            .SingleOrDefaultAsync()
        ;
        return single;
    }

    public async Task<ThingPropertyModel> GetThingPropertyAsync(int thingId, string propertyName)
    {
        var single = await context.ThingsProperties
            .Where(xx => xx.Thing.Id == thingId && xx.Name == propertyName)
            .Select(xx => new ThingPropertyModel()
            {
                Name = xx.Name,
                Type = xx.Type,
                Value = xx.Value,
                ContentType = xx.ContentType
            })
            .SingleOrDefaultAsync()
        ;

        return single;
    }

    public async Task UpdateAsync(int id, ThingPropertyModel model)
    {
        var single = await context.ThingsProperties
            .Where(xx => xx.Id == id)
            .SingleOrDefaultAsync()
        ;
        if (single is not null)
        {
            single.Name = model.Name;
            single.Value = model.Value;
            single.Type = model.Type;
            single.ContentType = model.ContentType;
        }
        else
        {
            await CreateImplAsync(context, id, model);
        }
        await context.SaveChangesAsync();
    }

    public async Task DeleteThingPropertyAsync(int id)
    {
        var single = await context.ThingsProperties
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

    public ThingPropertyModel NewThingProperty()
    {
        var model = new ThingPropertyModel()
        {
        };
        return model;
    }

    public async Task<int> CreateThingPropertyAsync(int headerId, ThingPropertyModel model)
    {
        var row = await CreateImplAsync(context, headerId, model);
        await context.SaveChangesAsync();
        return row.Id;
    }

    private static async Task<ThingsProperty> CreateImplAsync(CatalogContext context, int thingId, ThingPropertyModel model)
    {
        var result = await context.AddAsync(new ThingsProperty()
        {
            ThingId = thingId,
            Name = model.Name,
            Type = model.Type,
            Value = model.Value,
            ContentType = model.ContentType
        });
        return result.Entity;
    }
}
