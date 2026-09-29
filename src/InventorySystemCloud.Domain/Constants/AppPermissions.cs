using System.Collections.Generic;

namespace InventorySystemCloud.Domain.Constants
{
    public static class AppPermissions
    {
        public static class Products
        {
            public const string View = "Products.View";
            public const string Create = "Products.Create";
            public const string Edit = "Products.Edit";
            public const string Delete = "Products.Delete";
        }

        public static class Clients
        {
            public const string View = "Clients.View";
            public const string Create = "Clients.Create";
            public const string Edit = "Clients.Edit";
            public const string Delete = "Clients.Delete";
        }

        public static class Suppliers
        {
            public const string View = "Suppliers.View";
            public const string Create = "Suppliers.Create";
            public const string Edit = "Suppliers.Edit";
            public const string Delete = "Suppliers.Delete";
        }

        public static class Sales
        {
            public const string View = "Sales.View";
            public const string Create = "Sales.Create";
            public const string Invoice = "Sales.Invoice";
        }

        public static class Purchases
        {
            public const string View = "Purchases.View";
            public const string Create = "Purchases.Create";
        }

        public static class Reports
        {
            public const string Products = "Reports.Products";
            public const string Sales = "Reports.Sales";
            public const string Purchases = "Reports.Purchases";
            public const string Audit = "Reports.Audit";
        }

        public static class Users
        {
            public const string View = "Users.View";
            public const string Manage = "Users.Manage";
        }

        public static readonly IReadOnlyList<string> All = new[]
        {
            Products.View, Products.Create, Products.Edit, Products.Delete,
            Clients.View, Clients.Create, Clients.Edit, Clients.Delete,
            Suppliers.View, Suppliers.Create, Suppliers.Edit, Suppliers.Delete,
            Sales.View, Sales.Create, Sales.Invoice,
            Purchases.View, Purchases.Create,
            Reports.Products, Reports.Sales, Reports.Purchases, Reports.Audit,
            Users.View, Users.Manage
        };
    }
}
