using nia_api.Models;

namespace nia_api.Domain.Orders;

public static class OrderSnapshotPresentation
{
    public static bool HasSnapshots(Order order)
    {
        return order.Lines != null && order.Lines.Count > 0;
    }

    public static (List<Customization> Customizations, List<Product> Products, List<Design> Designs) MaterializeDetails(Order order)
    {
        if (order.Lines == null || order.Lines.Count == 0)
        {
            return (new List<Customization>(), new List<Product>(), new List<Design>());
        }

        var customizations = order.Lines.Select(line => new Customization
        {
            Id = line.CustomizationId,
            UserId = order.UserId.ToString(),
            ProductId = line.ProductId.ToString(),
            ProductColor = line.ProductColor,
            ProductSize = line.ProductSize,
            DesignId = line.DesignId.ToString(),
            Price = line.LineTotal,
            UserDescription = line.CustomizationDescription,
            IsOrdered = true,
            CreatedAt = order.CreatedAt
        }).ToList();

        var products = order.Lines
            .GroupBy(l => l.ProductId)
            .Select(g => new Product
            {
                Id = g.Key,
                Name = g.First().ProductName,
                Colors = g.GroupBy(l => l.ProductColor)
                    .Select(cg => new Colors
                    {
                        Name = cg.Key,
                        FileId = "",
                        PathOfFile = cg.First().ProductImagePath,
                        Sizes = cg.Select(s => new SizeInfo
                        {
                            Size = s.ProductSize,
                            Quantity = 0
                        }).ToList()
                    }).ToList()
            }).ToList();

        var designs = order.Lines
            .GroupBy(l => l.DesignId)
            .Select(g => new Design
            {
                Id = g.Key,
                Name = g.First().DesignName,
                PathOfFile = g.First().DesignImagePath
            }).ToList();

        return (customizations, products, designs);
    }
}
