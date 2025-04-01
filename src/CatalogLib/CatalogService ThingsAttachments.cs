using CatalogLib.Database;
using CatalogLib.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogLib;

public partial class CatalogService
{
    public async Task<IQueryable<ThingAttachmentItem>> GetThingAttachmentsAsync(int thingId, int pageNumber = 0, int pageSize = 10)
    {
        var query = context.ThingsAttachments
            .Where(xx => xx.ThingId == thingId)
            .Select(xx => new ThingAttachmentItem()
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

    public async Task<ThingAttachmentModel> GetThingAttachmentAsync(int id)
    {
        var single = await context.ThingsAttachments
            .Where(xx => xx.Id == id)
            .Select(xx => new ThingAttachmentModel()
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

    public async Task UpdateAsync(int id, ThingAttachmentModel model)
    {
        var single = await context.ThingsAttachments
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

    public async Task DeleteThingAttachmentAsync(int id)
    {
        var single = await context.ThingsAttachments
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

    public ThingAttachmentModel NewThingAttachment()
    {
        var model = new ThingAttachmentModel()
        {
        };
        return model;
    }

    public async Task<int> CreateThingAttachmentAsync(int headerId, ThingAttachmentModel model)
    {
        var row = await CreateImplAsync(context, headerId, model);
        await context.SaveChangesAsync();
        return row.Id;
    }

    private static async Task<ThingsAttachment> CreateImplAsync(CatalogContext context, int thingId, ThingAttachmentModel model)
    {
        var result = await context.AddAsync(new ThingsAttachment()
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
