using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using nia_api.Controllers;
using nia_api.Models;
using Xunit;

namespace nia_api.Tests;

public class OrderArchitectureTests
{
    private static readonly Type[] ControllersToEnforce = new[]
    {
        typeof(AdminController),
        typeof(UserController),
        typeof(GuestUserController),
        typeof(PublicController)
    };

    [Fact]
    public void Controllers_Must_Not_Have_Direct_Mongo_Order_Collection_Fields()
    {
        var violatingFields = new List<string>();

        foreach (var controller in ControllersToEnforce)
        {
            var fields = controller.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(IMongoCollection<Order>))
                {
                    violatingFields.Add($"{controller.Name}.{field.Name}");
                }
            }
        }

        Assert.True(
            violatingFields.Count == 0,
            $"Found forbidden direct IMongoCollection<Order> fields in controllers: {string.Join(", ", violatingFields)}. Controllers must depend on IOrderStore or IOrderLifecycleService.");
    }

    [Fact]
    public void Controllers_Must_Not_Have_Direct_Mongo_Order_Collection_Properties()
    {
        var violatingProps = new List<string>();

        foreach (var controller in ControllersToEnforce)
        {
            var props = controller.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var prop in props)
            {
                if (prop.PropertyType == typeof(IMongoCollection<Order>))
                {
                    violatingProps.Add($"{controller.Name}.{prop.Name}");
                }
            }
        }

        Assert.True(
            violatingProps.Count == 0,
            $"Found forbidden direct IMongoCollection<Order> properties in controllers: {string.Join(", ", violatingProps)}.");
    }

    [Fact]
    public void Controllers_Must_Not_Accept_Mongo_Order_Collection_In_Constructors()
    {
        var violatingCtors = new List<string>();

        foreach (var controller in ControllersToEnforce)
        {
            var ctors = controller.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            foreach (var ctor in ctors)
            {
                foreach (var param in ctor.GetParameters())
                {
                    if (param.ParameterType == typeof(IMongoCollection<Order>))
                    {
                        violatingCtors.Add($"{controller.Name}(... {param.Name} ...)");
                    }
                }
            }
        }

        Assert.True(
            violatingCtors.Count == 0,
            $"Found forbidden IMongoCollection<Order> constructor parameters: {string.Join(", ", violatingCtors)}.");
    }
}
